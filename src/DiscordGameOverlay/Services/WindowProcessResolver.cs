using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace DiscordGameOverlay.Services
{
    public sealed record WindowProcessTarget(
        int ProcessId,
        string ProcessName,
        string WindowTitle);

    internal static class WindowProcessResolver
    {
        private delegate bool EnumWindowsCallback(
            IntPtr windowHandle,
            IntPtr parameter);

        public static WindowProcessTarget? TryResolve(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return null;

            string targetName = displayName.Trim();
            List<WindowProcessTarget> windows = EnumerateWindows();

            List<WindowProcessTarget> exactMatches = windows
                .Where(window => string.Equals(
                    window.WindowTitle,
                    targetName,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            WindowProcessTarget? exactMatch =
                SelectUnambiguousProcess(exactMatches);

            if (exactMatch != null)
                return exactMatch;

            return null;
        }

        private static WindowProcessTarget? SelectUnambiguousProcess(
            IReadOnlyCollection<WindowProcessTarget> matches)
        {
            WindowProcessTarget[] processes = matches
                .GroupBy(match => match.ProcessId)
                .Select(group => group.First())
                .ToArray();

            return processes.Length == 1 ? processes[0] : null;
        }

        private static List<WindowProcessTarget> EnumerateWindows()
        {
            var windows = new List<WindowProcessTarget>();
            int currentProcessId = Environment.ProcessId;

            EnumWindows((windowHandle, _) =>
            {
                if (!IsWindowVisible(windowHandle))
                    return true;

                int textLength = GetWindowTextLength(windowHandle);
                if (textLength <= 0)
                    return true;

                var titleBuilder = new StringBuilder(textLength + 1);
                if (GetWindowText(
                        windowHandle,
                        titleBuilder,
                        titleBuilder.Capacity) <= 0)
                {
                    return true;
                }

                GetWindowThreadProcessId(
                    windowHandle,
                    out uint processIdValue);

                int processId = checked((int)processIdValue);
                if (processId <= 0 || processId == currentProcessId)
                    return true;

                try
                {
                    using Process process =
                        Process.GetProcessById(processId);

                    windows.Add(new WindowProcessTarget(
                        processId,
                        process.ProcessName,
                        titleBuilder.ToString().Trim()));
                }
                catch (ArgumentException)
                {
                    // The process exited while its window was being inspected.
                }
                catch (InvalidOperationException)
                {
                    // The process is no longer available.
                }
                catch (Win32Exception)
                {
                    // Some protected system processes cannot be inspected.
                }

                return true;
            }, IntPtr.Zero);

            return windows;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(
            EnumWindowsCallback callback,
            IntPtr parameter);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr windowHandle);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr windowHandle);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(
            IntPtr windowHandle,
            StringBuilder text,
            int maximumCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(
            IntPtr windowHandle,
            out uint processId);
    }
}
