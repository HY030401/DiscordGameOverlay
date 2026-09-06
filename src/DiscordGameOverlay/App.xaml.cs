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
        private readonly List<IOverlayEffectHost> _effectHosts = new();
        private readonly EffectTriggerCoordinator _effectTriggerCoordinator =
            new();

        public StreamWindow? StreamWindow { get; private set; }

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
                        config.DiscordChannelId,
                        config.PoopEmojiId,
                        config.PigeonPoopEmojiId,
                        config.HeartEmojiId,
                        config.EggEmojiId
                    );

                // 5. Listen for new Discord messages
                _discordService.MessageReceived +=
                    OnDiscordMessageReceived;

                _discordService.EffectRequested +=
                    OnDiscordEffectRequested;

                _effectTriggerCoordinator.EffectReady +=
                    OnEffectReady;

                // 6. Start Discord bot
                await _discordService.StartAsync();

                // 7. Create streamer overlay
                OverlayWindow overlayWindow =
                    new OverlayWindow(_messageManager);

                // 8. Create viewer stream window
                StreamWindow =
                    new StreamWindow(_messageManager);

                RegisterEffectHost(StreamWindow);

                OverlayControlWindow controlWindow =
                    new OverlayControlWindow(
                        overlayWindow,
                        StreamWindow
                    );

                // 9. Show application windows
                overlayWindow.Show();
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

        private void OnDiscordEffectRequested(OverlayEffectType effect)
        {
            _effectTriggerCoordinator.Register(effect);
        }

        private void OnEffectReady(OverlayEffectRequest request)
        {
            Dispatcher.BeginInvoke(() =>
            {
                foreach (IOverlayEffectHost host in _effectHosts.ToArray())
                {
                    host.PlayEffect(request);
                }
            });
        }

        public void RegisterEffectHost(IOverlayEffectHost host)
        {
            if (!_effectHosts.Contains(host))
            {
                _effectHosts.Add(host);
            }
        }

        public void UnregisterEffectHost(IOverlayEffectHost host)
        {
            _effectHosts.Remove(host);
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
                _discordService.EffectRequested -= OnDiscordEffectRequested;

                await _discordService.StopAsync();
            }

            _effectTriggerCoordinator.EffectReady -= OnEffectReady;
            _effectTriggerCoordinator.Dispose();

            base.OnExit(e);
        }
    }
}
