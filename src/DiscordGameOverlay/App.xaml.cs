using System.Windows;
using DiscordGameOverlay.Models;
using DiscordGameOverlay.Services;
using DiscordGameOverlay.Views;

namespace DiscordGameOverlay
{
    public partial class App : Application
    {
        public StreamWindow? StreamWindow { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MessageManager messageManager = new MessageManager();

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

            OverlayWindow overlayWindow =
                new OverlayWindow(messageManager);

            StreamWindow =
                new StreamWindow();

            overlayWindow.Show();
            StreamWindow.Show();
        }

        public void ExitApplication()
        {
            StreamWindow?.AllowClose();

            Shutdown();
        }
    }
}