using System.Windows;
using DiscordGameOverlay.Config;
using DiscordGameOverlay.Services;
using DiscordGameOverlay.Views;

namespace DiscordGameOverlay
{
    public partial class App : Application
    {
        private DiscordService? _discordService;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // 读取配置
                AppConfig config = AppConfig.Load();

                // 创建 Discord 服务
                _discordService = new DiscordService(
                    config.DiscordBotToken,
                    config.DiscordChannelId
                );

                // 启动 Bot
                await _discordService.StartAsync();

                // 打开主窗口
                var mainWindow = new MainWindow();
                mainWindow.Show();
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

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_discordService != null)
            {
                await _discordService.StopAsync();
            }

            base.OnExit(e);
        }
    }
}