using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DiscordGameOverlay.Views
{
    public partial class OverlayWindow : Window
    {
        // Get the current extended window style.
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(
            IntPtr hWnd,
            int nIndex
        );

        // Change the extended window style.
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(
            IntPtr hWnd,
            int nIndex,
            int dwNewLong
        );

        private const int GWL_EXSTYLE = -20;

        // Makes the window transparent to mouse input.
        private const int WS_EX_TRANSPARENT = 0x00000020;

        public OverlayWindow()
        {
            InitializeComponent();

            Loaded += OverlayWindow_Loaded;
        }

        private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Move overlay to the top-right corner.
            Left = SystemParameters.WorkArea.Right - Width - 20;
            Top = SystemParameters.WorkArea.Top + 20;

            // Get the native Windows handle of this WPF window.
            IntPtr hwnd = new WindowInteropHelper(this).Handle;

            // Read the current window style.
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            // Add mouse click-through behavior.
            SetWindowLong(
                hwnd,
                GWL_EXSTYLE,
                extendedStyle | WS_EX_TRANSPARENT
            );
        }
    }
}