using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace DiscordGameOverlay.Services
{
    public sealed class ProcessAudioRelayService : IDisposable
    {
        private static readonly WaveFormat CaptureFormat =
            WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

        private readonly object stateLock = new();

        private WasapiRecorder? recorder;
        private WasapiPlayer? player;
        private MMDevice? outputDevice;
        private BufferedWaveProvider? audioBuffer;
        private string? outputDeviceName;
        private bool usesDefaultOutputDevice;
        private int operationGeneration;
        private bool isDisposed;

        public event Action<string>? RelayFailed;

        public bool IsRelaying
        {
            get
            {
                lock (stateLock)
                {
                    return recorder != null && player != null;
                }
            }
        }

        public string? OutputDeviceName
        {
            get
            {
                lock (stateLock)
                {
                    return outputDeviceName;
                }
            }
        }

        public bool UsesDefaultOutputDevice
        {
            get
            {
                lock (stateLock)
                {
                    return usesDefaultOutputDevice;
                }
            }
        }

        public async Task<bool> StartAsync(int processId)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 20348))
            {
                throw new NotSupportedException(
                    "当前 Windows 版本不支持捕获单个应用程序的音频。");
            }

            if (processId <= 0)
                throw new ArgumentOutOfRangeException(nameof(processId));

            int generation = Interlocked.Increment(
                ref operationGeneration);

            StopCore();

            WasapiRecorder? newRecorder = null;
            WasapiPlayer? newPlayer = null;
            MMDevice? newOutputDevice = null;
            bool resourcesAdopted = false;

            try
            {
                newRecorder = await Task.Run(async () =>
                    await new WasapiRecorderBuilder()
                        .WithProcessLoopback(
                            checked((uint)processId),
                            ProcessLoopbackMode.IncludeTargetProcessTree)
                        .WithFormat(CaptureFormat)
                        .WithBufferLength(50)
                        .BuildAsync()
                        .ConfigureAwait(false));

                var newAudioBuffer = new BufferedWaveProvider(
                    newRecorder.WaveFormat,
                    TimeSpan.FromMilliseconds(300))
                {
                    DiscardOnBufferOverflow = true,
                    ReadFully = true
                };

                RelayOutputSelection outputSelection =
                    SelectRelayOutputDevice();
                newOutputDevice = outputSelection.Device;

                newPlayer = new WasapiPlayerBuilder()
                    .WithDevice(newOutputDevice)
                    .WithSharedMode()
                    .WithEventSync()
                    .WithLatency(50)
                    .WithMmcssThreadPriority("Pro Audio")
                    .Build();

                newPlayer.Init(newAudioBuffer);

                lock (stateLock)
                {
                    if (isDisposed ||
                        generation != Volatile.Read(ref operationGeneration))
                    {
                        return false;
                    }

                    recorder = newRecorder;
                    player = newPlayer;
                    outputDevice = newOutputDevice;
                    audioBuffer = newAudioBuffer;
                    outputDeviceName = outputSelection.Name;
                    usesDefaultOutputDevice =
                        outputSelection.UsesDefaultDevice;

                    recorder.DataAvailable += Recorder_DataAvailable;
                    recorder.RecordingStopped += Recorder_RecordingStopped;
                    player.PlaybackStopped += Player_PlaybackStopped;
                    resourcesAdopted = true;
                }

                newPlayer.Play();
                newRecorder.StartRecording();
                return true;
            }
            catch
            {
                lock (stateLock)
                {
                    if (ReferenceEquals(recorder, newRecorder))
                    {
                        recorder = null;
                        player = null;
                        outputDevice = null;
                        audioBuffer = null;
                        outputDeviceName = null;
                        usesDefaultOutputDevice = false;
                    }
                }

                Unsubscribe(newRecorder, newPlayer);
                resourcesAdopted = false;
                throw;
            }
            finally
            {
                if (!resourcesAdopted)
                {
                    Unsubscribe(newRecorder, newPlayer);
                    newRecorder?.Dispose();
                    newPlayer?.Dispose();
                    newOutputDevice?.Dispose();
                }
            }
        }

        public void Stop()
        {
            if (isDisposed)
                return;

            Interlocked.Increment(ref operationGeneration);
            StopCore();
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            Interlocked.Increment(ref operationGeneration);
            StopCore();
        }

        private void StopCore()
        {
            WasapiRecorder? oldRecorder;
            WasapiPlayer? oldPlayer;
            MMDevice? oldOutputDevice;

            lock (stateLock)
            {
                oldRecorder = recorder;
                oldPlayer = player;
                oldOutputDevice = outputDevice;

                recorder = null;
                player = null;
                outputDevice = null;
                audioBuffer = null;
                outputDeviceName = null;
                usesDefaultOutputDevice = false;
            }

            Unsubscribe(oldRecorder, oldPlayer);

            try
            {
                oldRecorder?.StopRecording();
            }
            catch
            {
                // The audio process or device may already be gone.
            }

            try
            {
                oldPlayer?.Stop();
            }
            catch
            {
                // The output device may already be gone.
            }

            oldRecorder?.Dispose();
            oldPlayer?.Dispose();
            oldOutputDevice?.Dispose();
        }

        private static RelayOutputSelection SelectRelayOutputDevice()
        {
            using var enumerator = new MMDeviceEnumerator();
            using MMDevice defaultDevice =
                enumerator.GetDefaultAudioEndpoint(
                    DataFlow.Render,
                    Role.Multimedia);
            using MMDeviceCollection activeDevices =
                enumerator.EnumerateAudioEndPoints(
                    DataFlow.Render,
                    DeviceState.Active);

            var candidates = new List<RelayOutputCandidate>();

            foreach (MMDevice device in activeDevices)
            {
                using (device)
                {
                    if (device.ID == defaultDevice.ID)
                        continue;

                    candidates.Add(new RelayOutputCandidate(
                        device.ID,
                        device.FriendlyName,
                        GetRelayDevicePreference(device.FriendlyName)));
                }
            }

            RelayOutputCandidate? candidate = candidates
                .OrderByDescending(item => item.Preference)
                .ThenBy(item => item.Name, StringComparer.CurrentCulture)
                .FirstOrDefault();

            if (candidate != null)
            {
                return new RelayOutputSelection(
                    enumerator.GetDevice(candidate.Id),
                    candidate.Name,
                    UsesDefaultDevice: false);
            }

            return new RelayOutputSelection(
                enumerator.GetDevice(defaultDevice.ID),
                defaultDevice.FriendlyName,
                UsesDefaultDevice: true);
        }

        private static int GetRelayDevicePreference(string deviceName)
        {
            if (ContainsAny(
                    deviceName,
                    "Digital Output",
                    "S/PDIF",
                    "SPDIF",
                    "数字输出",
                    "デジタル出力"))
            {
                return 300;
            }

            if (ContainsAny(
                    deviceName,
                    "Virtual",
                    "CABLE",
                    "Voicemeeter"))
            {
                return 200;
            }

            if (ContainsAny(
                    deviceName,
                    "HDMI",
                    "Display Audio",
                    "High Definition Audio"))
            {
                return 100;
            }

            return 0;
        }

        private static bool ContainsAny(
            string value,
            params string[] candidates)
        {
            return candidates.Any(candidate =>
                value.Contains(
                    candidate,
                    StringComparison.OrdinalIgnoreCase));
        }

        private void Recorder_DataAvailable(
            ReadOnlySpan<byte> data,
            AudioClientBufferFlags flags,
            long devicePosition,
            long qpcPosition)
        {
            if ((flags & AudioClientBufferFlags.Silent) != 0 || data.IsEmpty)
                return;

            BufferedWaveProvider? targetBuffer;

            lock (stateLock)
            {
                targetBuffer = audioBuffer;
            }

            targetBuffer?.AddSamples(data);
        }

        private void Recorder_RecordingStopped(
            object? sender,
            StoppedEventArgs e)
        {
            if (e.Exception != null && IsRelaying)
            {
                RelayFailed?.Invoke(
                    $"窗口音频采集已停止：{e.Exception.Message}");
            }
        }

        private void Player_PlaybackStopped(
            object? sender,
            StoppedEventArgs e)
        {
            if (e.Exception != null && IsRelaying)
            {
                RelayFailed?.Invoke(
                    $"窗口音频转发已停止：{e.Exception.Message}");
            }
        }

        private void Unsubscribe(
            WasapiRecorder? targetRecorder,
            WasapiPlayer? targetPlayer)
        {
            if (targetRecorder != null)
            {
                targetRecorder.DataAvailable -= Recorder_DataAvailable;
                targetRecorder.RecordingStopped -= Recorder_RecordingStopped;
            }

            if (targetPlayer != null)
            {
                targetPlayer.PlaybackStopped -= Player_PlaybackStopped;
            }
        }

        private sealed record RelayOutputCandidate(
            string Id,
            string Name,
            int Preference);

        private sealed record RelayOutputSelection(
            MMDevice Device,
            string Name,
            bool UsesDefaultDevice);
    }
}
