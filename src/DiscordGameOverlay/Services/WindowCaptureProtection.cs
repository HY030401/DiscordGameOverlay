using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DiscordGameOverlay.Services
{
    public static class WindowCaptureProtection
    {
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowDisplayAffinity(
            IntPtr hWnd,
            uint dwAffinity);

        public static bool ExcludeFromCapture(Window window)
        {
            IntPtr hwnd =
                new WindowInteropHelper(window).Handle;

            if (hwnd == IntPtr.Zero)
            {
                return false;
            }

            return SetWindowDisplayAffinity(
                hwnd,
                WDA_EXCLUDEFROMCAPTURE);
        }
    }
}