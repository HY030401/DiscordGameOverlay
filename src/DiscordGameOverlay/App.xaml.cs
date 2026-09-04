using System;
using System.Windows;
using DiscordGameOverlay.Config;
using DiscordGameOverlay.Models;
using DiscordGameOverlay.Services;
using DiscordGameOverlay.Views;

namespace DiscordGameOverlay
{
    public partial class App : Application
    {
        private DiscordService? _discordService;
        private MessageManager? _messageManager;

        public StreamWindow? StreamWindow { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // 1. Read configuration
                AppConfig config = AppConfig.Load();

                // 2. Create the shared message manager
                _messageManager = new MessageManager();

                // 3. Create Discord service
                _discordService = new DiscordService(
                    config.DiscordBotToken,
                    config.DiscordChannelId
                );

                // 4. Listen for new Discord messages
                _discordService.MessageReceived += OnDiscordMessageReceived;

                // 5. Start Discord bot
                await _discordService.StartAsync();

                // 6. Create streamer overlay
                OverlayWindow overlayWindow =
                    new OverlayWindow(_messageManager);

                // 7. Create viewer stream window
                StreamWindow =
                    new StreamWindow();

                // 8. Show both windows
                overlayWindow.Show();
                StreamWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "启动失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                Shutdown();
            }
        }

        private void OnDiscordMessageReceived(ChatMessage message)
        {
            if (_messageManager == null)
                return;

            // Discord events may arrive on a background thread.
            // WPF UI collections should be updated on the UI thread.
            Dispatcher.BeginInvoke(() =>
            {
                _messageManager.AddMessage(message);
            });
        }

        public void ExitApplication()
        {
            StreamWindow?.AllowClose();

            Shutdown();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_discordService != null)
            {
                _discordService.MessageReceived -= OnDiscordMessageReceived;

                await _discordService.StopAsync();
            }

            base.OnExit(e);
        }
    }
}