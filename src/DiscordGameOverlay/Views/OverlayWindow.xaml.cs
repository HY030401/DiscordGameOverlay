using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using DiscordGameOverlay.Services;

namespace DiscordGameOverlay.Views
{
    public partial class OverlayWindow : Window
    {
        private readonly MessageManager messageManager;

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(
            IntPtr hWnd,
            int nIndex
        );

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(
            IntPtr hWnd,
            int nIndex,
            int dwNewLong
        );

        public OverlayWindow(MessageManager manager)
        {
            InitializeComponent();

            messageManager = manager;
            DataContext = messageManager;

            Loaded += OverlayWindow_Loaded;
        }

        private void OverlayWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Left = SystemParameters.WorkArea.Right - Width - 20;
            Top = SystemParameters.WorkArea.Top + 60;

            IntPtr hwnd =
                new WindowInteropHelper(this).Handle;

            int extendedStyle =
                GetWindowLong(hwnd, GWL_EXSTYLE);

            SetWindowLong(
                hwnd,
                GWL_EXSTYLE,
                extendedStyle | WS_EX_TRANSPARENT
            );
        }
    }
}