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

        public StreamerEffectWindow? StreamerEffectWindow { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // 1. 如果没有配置文件，
                //    先显示第一次启动设置窗口
                if (!AppConfig.Exists())
                {
                    SettingsWindow settingsWindow =
                        new SettingsWindow();

                    bool? result =
                        settingsWindow.ShowDialog();

                    // 用户没有完成配置，程序直接退出
                    if (result != true)
                    {
                        Shutdown();
                        return;
                    }
                }

                // 2. 配置存在后才读取
                AppConfig config =
                    AppConfig.Load();

                // 3. Create the shared message manager
                _messageManager =
                    new MessageManager();

                // 4. Create Discord service
                _discordService =
                    new DiscordService(
                        config.DiscordBotToken,
                        config.DiscordChannelId
                    );

                // 5. Listen for new Discord messages
                _discordService.MessageReceived +=
                    OnDiscordMessageReceived;

                // 6. Start Discord bot
                await _discordService.StartAsync();

                // 7. Create streamer overlay
                OverlayWindow overlayWindow =
                    new OverlayWindow(_messageManager);

                // 8. Create viewer stream window
                StreamWindow =
                    new StreamWindow(_messageManager);

                // 9. Create streamer effect window
                StreamerEffectWindow =
                    new StreamerEffectWindow();

                OverlayControlWindow controlWindow =
                    new OverlayControlWindow(
                        overlayWindow,
                        StreamWindow
                    );

                // 10. Show application windows
                overlayWindow.Show();
                StreamerEffectWindow.Show();
                controlWindow.Show();
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