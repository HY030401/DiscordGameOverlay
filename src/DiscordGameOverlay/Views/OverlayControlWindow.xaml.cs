using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace DiscordGameOverlay.Views
{
    public partial class OverlayControlWindow : Window
    {
        private readonly OverlayWindow overlayWindow;
        private readonly StreamWindow streamWindow;

        public OverlayControlWindow(
            OverlayWindow overlay,
            StreamWindow stream)
        {
            InitializeComponent();

            overlayWindow = overlay;
            streamWindow = stream;

            streamWindow.CaptureStatusChanged += StreamWindow_CaptureStatusChanged;

            Loaded += OverlayControlWindow_Loaded;
            LocationChanged += OverlayControlWindow_LocationChanged;
            Closed += OverlayControlWindow_Closed;
        }

        private void OverlayControlWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            // 放在 OverlayWindow 正上方
            Left = overlayWindow.Left;
            Top = overlayWindow.Top - Height;
        }

        private void OverlayControlWindow_LocationChanged(
            object? sender,
            EventArgs e)
        {
            // 控制窗口移动时，让 Overlay 跟着移动
            overlayWindow.Left = Left;
            overlayWindow.Top = Top + Height;
        }

        private void DragArea_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private async void SelectCaptureButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string previousStatus = CaptureStatusText.Text;

            try
            {
                IntPtr ownerHandle =
                    new WindowInteropHelper(this).Handle;

                CaptureStatusText.Text = "请选择窗口或屏幕…";

                bool selected =
                    await streamWindow.SelectCaptureSourceAsync(ownerHandle);

                if (!selected)
                {
                    CaptureStatusText.Text = previousStatus;
                }
            }
            catch (Exception ex)
            {
                CaptureStatusText.Text = "画面采集启动失败";

                MessageBox.Show(
                    this,
                    ex.Message,
                    "无法采集画面",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void StopCaptureButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            streamWindow.StopGameCapture();
        }

        private void StreamWindow_CaptureStatusChanged(string status)
        {
            CaptureStatusText.Text = status;
        }

        private void OverlayControlWindow_Closed(
            object? sender,
            EventArgs e)
        {
            streamWindow.CaptureStatusChanged -=
                StreamWindow_CaptureStatusChanged;
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Application.Current is App app)
            {
                app.ExitApplication();
            }
        }
    }
}
