using System;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using DiscordGameOverlay.Services;

namespace DiscordGameOverlay.Views
{
    public partial class OverlayWindow : Window
    {
        private readonly MessageManager messageManager;

        private ScrollViewer? messageScrollViewer;

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

            messageManager.Messages.CollectionChanged +=
                Messages_CollectionChanged;

            Loaded += OverlayWindow_Loaded;
            Closed += OverlayWindow_Closed;
        }

        private void OverlayWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            // Find the ScrollViewer from XAML at runtime.
            messageScrollViewer =
                FindName("MessageScrollViewer") as ScrollViewer;

            // Set the initial overlay position.
            Left = SystemParameters.WorkArea.Right - Width - 20;
            Top = SystemParameters.WorkArea.Top + 60;

            // Make the overlay click-through.
            IntPtr hwnd =
                new WindowInteropHelper(this).Handle;

            int extendedStyle =
                GetWindowLong(hwnd, GWL_EXSTYLE);

            SetWindowLong(
                hwnd,
                GWL_EXSTYLE,
                extendedStyle | WS_EX_TRANSPARENT
            );

            // Start at the newest message.
            messageScrollViewer?.ScrollToEnd();
        }

        private void Messages_CollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                messageScrollViewer?.ScrollToEnd();
            });
        }

        private void OverlayWindow_Closed(
            object? sender,
            EventArgs e)
        {
            messageManager.Messages.CollectionChanged -=
                Messages_CollectionChanged;
        }
    }
}