using System.Windows;
using System.Windows.Input;

namespace DiscordGameOverlay.Views
{
    public partial class OverlayControlWindow : Window
    {
        private readonly OverlayWindow overlayWindow;

        public OverlayControlWindow(OverlayWindow overlay)
        {
            InitializeComponent();

            overlayWindow = overlay;

            Loaded += OverlayControlWindow_Loaded;
            LocationChanged += OverlayControlWindow_LocationChanged;
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