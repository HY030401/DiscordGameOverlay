using System.Windows;
using DiscordGameOverlay.Models;
using DiscordGameOverlay.Services;

namespace DiscordGameOverlay.Views
{
    public partial class MainWindow : Window
    {
        private MessageManager messageManager;

        private OverlayWindow? overlayWindow;
        private StreamWindow? streamWindow;

        public MainWindow()
        {
            InitializeComponent();

            messageManager = new MessageManager();

            // Temporary test messages
            messageManager.AddMessage(new ChatMessage
            {
                DisplayName = "Alice",
                Content = "Hello!"
            });

            messageManager.AddMessage(new ChatMessage
            {
                DisplayName = "Bob",
                Content = "Nice shot!"
            });

            messageManager.AddMessage(new ChatMessage
            {
                DisplayName = "Charlie",
                Content = "Behind you!"
            });

            overlayWindow = new OverlayWindow(messageManager);
            overlayWindow.Show();

            streamWindow = new StreamWindow(messageManager);
            streamWindow.Show();
        }

        private void ShowOverlay_Click(object sender, RoutedEventArgs e)
        {
            if (overlayWindow == null)
            {
                overlayWindow = new OverlayWindow(messageManager);
            }

            overlayWindow.Show();
        }

        private void HideOverlay_Click(object sender, RoutedEventArgs e)
        {
            overlayWindow?.Hide();
        }

        private void ShowStream_Click(object sender, RoutedEventArgs e)
        {
            if (streamWindow == null)
            {
                streamWindow = new StreamWindow(messageManager);
            }

            streamWindow.Show();
        }

        private void HideStream_Click(object sender, RoutedEventArgs e)
        {
            streamWindow?.Hide();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
