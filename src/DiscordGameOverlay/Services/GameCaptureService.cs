using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace DiscordGameOverlay.Services
{
    public sealed class GameCaptureFrameEventArgs : EventArgs
    {
        public GameCaptureFrameEventArgs(IntPtr texturePointer)
        {
            TexturePointer = texturePointer;
        }

        public IntPtr TexturePointer { get; }
    }

    public sealed class GameCaptureService : IDisposable
    {
        private const int TargetFramesPerSecond = 30;

        private static readonly Guid Id3D11Texture2DGuid =
            new("6F15AAF2-D208-4E89-9AB4-489535D34F9C");

        private readonly long minimumFrameInterval =
            Stopwatch.Frequency / TargetFramesPerSecond;
        private readonly IDirect3DDevice direct3DDevice;

        private GraphicsCaptureItem? captureItem;
        private Direct3D11CaptureFramePool? framePool;
        private GraphicsCaptureSession? captureSession;
        private SizeInt32 framePoolSize;

        private long lastFrameTimestamp;
        private int isProcessingFrame;
        private int hasReportedFrameFailure;
        private bool isDisposed;

        public event EventHandler<GameCaptureFrameEventArgs>? FrameArrived;
        public event Action<string>? CaptureStarted;
        public event Action? CaptureStopped;
        public event Action<string>? CaptureFailed;

        public bool IsCapturing => captureSession != null;

        public GameCaptureService(IDirect3DDevice device)
        {
            direct3DDevice = device;
        }

        public async Task<bool> SelectAndStartAsync(IntPtr ownerWindowHandle)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            if (!GraphicsCaptureSession.IsSupported())
            {
                throw new NotSupportedException(
                    "当前 Windows 版本或显卡不支持画面采集。");
            }

            var picker = new GraphicsCapturePicker();
            WinRT.Interop.InitializeWithWindow.Initialize(
                picker,
                ownerWindowHandle);

            GraphicsCaptureItem? item =
                await picker.PickSingleItemAsync();

            if (item == null)
                return false;

            StartCapture(item);
            return true;
        }

        public void StopCapture()
        {
            StopCapture(notify: true);
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            StopCapture(notify: false);
        }

        private void StartCapture(GraphicsCaptureItem item)
        {
            StopCapture(notify: false);

            try
            {
                captureItem = item;
                framePoolSize = item.Size;
                captureItem.Closed += CaptureItem_Closed;

                framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                    direct3DDevice,
                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    2,
                    item.Size);

                framePool.FrameArrived += FramePool_FrameArrived;
                captureSession = framePool.CreateCaptureSession(item);
                captureSession.StartCapture();

                lastFrameTimestamp = 0;
                hasReportedFrameFailure = 0;
                CaptureStarted?.Invoke(item.DisplayName);
            }
            catch
            {
                StopCapture(notify: false);
                throw;
            }
        }

        private void StopCapture(bool notify)
        {
            bool wasCapturing =
                captureItem != null ||
                framePool != null ||
                captureSession != null;

            GraphicsCaptureItem? oldItem = captureItem;
            Direct3D11CaptureFramePool? oldFramePool = framePool;
            GraphicsCaptureSession? oldSession = captureSession;
            captureItem = null;
            framePool = null;
            captureSession = null;

            if (oldItem != null)
                oldItem.Closed -= CaptureItem_Closed;

            if (oldFramePool != null)
                oldFramePool.FrameArrived -= FramePool_FrameArrived;

            oldSession?.Dispose();
            oldFramePool?.Dispose();

            if (notify && wasCapturing)
                CaptureStopped?.Invoke();
        }

        private void CaptureItem_Closed(
            GraphicsCaptureItem sender,
            object args)
        {
            StopCapture();
        }

        private void FramePool_FrameArrived(
            Direct3D11CaptureFramePool sender,
            object args)
        {
            if (Interlocked.CompareExchange(
                    ref isProcessingFrame,
                    1,
                    0) != 0)
            {
                using Direct3D11CaptureFrame? droppedFrame =
                    sender.TryGetNextFrame();
                return;
            }

            try
            {
                SizeInt32 contentSize;
                bool sizeChanged;

                using (Direct3D11CaptureFrame? frame =
                       sender.TryGetNextFrame())
                {
                    if (frame == null)
                        return;

                    contentSize = frame.ContentSize;

                    if (contentSize.Width <= 0 || contentSize.Height <= 0)
                        return;

                    long timestamp = Stopwatch.GetTimestamp();
                    long previousTimestamp =
                        Interlocked.Read(ref lastFrameTimestamp);

                    if (previousTimestamp != 0 &&
                        timestamp - previousTimestamp < minimumFrameInterval)
                    {
                        return;
                    }

                    Interlocked.Exchange(ref lastFrameTimestamp, timestamp);

                    IDirect3DDxgiInterfaceAccess surfaceAccess =
                        frame.Surface.As<IDirect3DDxgiInterfaceAccess>();

                    Guid textureGuid = Id3D11Texture2DGuid;
                    IntPtr texturePointer =
                        surfaceAccess.GetInterface(ref textureGuid);

                    try
                    {
                        if (ReferenceEquals(sender, framePool))
                        {
                            FrameArrived?.Invoke(
                                this,
                                new GameCaptureFrameEventArgs(texturePointer));
                        }
                    }
                    finally
                    {
                        Marshal.Release(texturePointer);
                    }

                    sizeChanged =
                        framePoolSize.Width != contentSize.Width ||
                        framePoolSize.Height != contentSize.Height;
                }

                if (sizeChanged &&
                    ReferenceEquals(sender, framePool))
                {
                    sender.Recreate(
                        direct3DDevice,
                        DirectXPixelFormat.B8G8R8A8UIntNormalized,
                        2,
                        contentSize);

                    framePoolSize = contentSize;
                }
            }
            catch (ObjectDisposedException)
            {
                // Capture was stopped while a frame was being copied.
            }
            catch (COMException ex)
            {
                ReportFrameFailure($"采集画面失败：{ex.Message}");
            }
            catch (Exception ex)
            {
                ReportFrameFailure($"处理游戏画面失败：{ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref isProcessingFrame, 0);
            }
        }

        private void ReportFrameFailure(string message)
        {
            if (Interlocked.CompareExchange(
                    ref hasReportedFrameFailure,
                    1,
                    0) == 0)
            {
                CaptureFailed?.Invoke(message);
            }
        }

        [ComImport]
        [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDirect3DDxgiInterfaceAccess
        {
            IntPtr GetInterface([In] ref Guid iid);
        }
    }
}
