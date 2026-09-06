using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using DiscordGameOverlay.Models;
using DiscordGameOverlay.Services;

namespace DiscordGameOverlay.Views
{
    public partial class StreamerEffectWindow : Window, IOverlayEffectHost
    {
        private const int GWL_EXSTYLE = -20;

        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private readonly OverlayEffectManager overlayEffectManager;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(
            IntPtr hWnd,
            int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(
            IntPtr hWnd,
            int nIndex,
            int dwNewLong);

        public StreamerEffectWindow()
        {
            InitializeComponent();

            overlayEffectManager =
                new OverlayEffectManager(EffectCanvas);

            Loaded += StreamerEffectWindow_Loaded;
        }

        public void PlayEffect(OverlayEffectRequest request)
        {
            if (Dispatcher.CheckAccess())
            {
                overlayEffectManager.Play(request);
                return;
            }

            if (!Dispatcher.HasShutdownStarted &&
                !Dispatcher.HasShutdownFinished)
            {
                Dispatcher.BeginInvoke(
                    () => overlayEffectManager.Play(request));
            }
        }

        private void StreamerEffectWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            // 覆盖整个当前桌面可用区域。
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;

            IntPtr hwnd =
                new WindowInteropHelper(this).Handle;

            int extendedStyle =
                GetWindowLong(hwnd, GWL_EXSTYLE);

            SetWindowLong(
                hwnd,
                GWL_EXSTYLE,
                extendedStyle |
                WS_EX_TRANSPARENT |
                WS_EX_NOACTIVATE);

            // 不允许本窗口被直播画面捕获。
            WindowCaptureProtection.ExcludeFromCapture(this);
        }
    }
}
