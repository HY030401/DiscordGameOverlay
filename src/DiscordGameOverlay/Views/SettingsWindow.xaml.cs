using System;
using System.Windows;
using DiscordGameOverlay.Config;

namespace DiscordGameOverlay.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            LoadCurrentSettings();

        }

        private void LoadCurrentSettings()
        {
            if (!AppConfig.Exists())
            {
                return;
            }

            try
            {
                AppConfig config =
                    AppConfig.Load();

                TokenBox.Password =
                    config.DiscordBotToken;

                ChannelIdBox.Text =
                    config.DiscordChannelId.ToString();
            }
            catch
            {
                // 如果读取失败，保持输入框为空
            }
        }


        private void SaveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string token =
                TokenBox.Password.Trim();

            string channelIdText =
                ChannelIdBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(token))
            {
                MessageBox.Show(
                    "Please enter a Discord Bot Token.",
                    "Missing Token",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            if (!ulong.TryParse(
                    channelIdText,
                    out ulong channelId))
            {
                MessageBox.Show(
                    "Please enter a valid Discord Channel ID.",
                    "Invalid Channel ID",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            AppConfig config =
                new AppConfig
                {
                    DiscordBotToken = token,
                    DiscordChannelId = channelId
                };

            try
            {
                config.Save();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Failed to Save Configuration",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}