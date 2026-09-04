using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using DiscordGameOverlay.Services;

namespace DiscordGameOverlay.Views
{
    public partial class OverlayWindow : Window
    {
        private readonly MessageManager messageManager;

        // Windows message used to determine
        // which part of the window the mouse is over.
        private const int WM_NCHITTEST = 0x0084;

        // Tells Windows to pass the mouse event
        // through this window.
        private const int HTTRANSPARENT = -1;

        public OverlayWindow(MessageManager manager)
        {
            InitializeComponent();

            // Save the shared MessageManager.
            messageManager = manager;

            // Allow XAML to bind to Messages.
            DataContext = messageManager;

            // Set initial position after the window loads.
            Loaded += OverlayWindow_Loaded;

            // Add the Windows mouse hit-test hook.
            SourceInitialized += OverlayWindow_SourceInitialized;
        }

        private void OverlayWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            // Place overlay near the top-right corner.
            Left = SystemParameters.WorkArea.Right - Width - 20;
            Top = SystemParameters.WorkArea.Top + 20;
        }

        private void OverlayWindow_SourceInitialized(
            object? sender,
            EventArgs e)
        {
            HwndSource? source =
                PresentationSource.FromVisual(this) as HwndSource;

            if (source != null)
            {
                source.AddHook(WindowProc);
            }
        }

        private IntPtr WindowProc(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (msg == WM_NCHITTEST)
            {
                // Get mouse position in screen coordinates.
                int x = (short)(lParam.ToInt64() & 0xFFFF);
                int y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);

                Point screenPoint = new Point(x, y);

                // Convert screen coordinates to OverlayWindow coordinates.
                Point windowPoint = PointFromScreen(screenPoint);

                // These two areas are allowed to receive mouse input.
                if (IsPointInsideElement(DragArea, windowPoint) ||
                    IsPointInsideElement(CloseButton, windowPoint))
                {
                    handled = false;
                    return IntPtr.Zero;
                }

                // Everything else is click-through.
                handled = true;
                return new IntPtr(HTTRANSPARENT);
            }

            return IntPtr.Zero;
        }

        private bool IsPointInsideElement(
            FrameworkElement element,
            Point windowPoint)
        {
            Point elementPosition =
                element.TranslatePoint(new Point(0, 0), this);

            Rect elementBounds = new Rect(
                elementPosition,
                new Size(
                    element.ActualWidth,
                    element.ActualHeight
                )
            );

            return elementBounds.Contains(windowPoint);
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
            // Only the Overlay close button is allowed
            // to terminate the entire application.
            if (Application.Current is App app)
            {
                app.ExitApplication();
            }
        }
    }
}