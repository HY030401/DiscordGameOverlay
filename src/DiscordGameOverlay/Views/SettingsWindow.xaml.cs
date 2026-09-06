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

                PoopEmojiIdBox.Text =
                    FormatOptionalId(config.PoopEmojiId);

                PigeonPoopEmojiIdBox.Text =
                    FormatOptionalId(config.PigeonPoopEmojiId);

                HeartEmojiIdBox.Text =
                    FormatOptionalId(config.HeartEmojiId);

                EggEmojiIdBox.Text =
                    FormatOptionalId(config.EggEmojiId);
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

            if (!TryReadOptionalId(
                    PoopEmojiIdBox.Text,
                    "扔屎表情 ID",
                    out ulong poopEmojiId) ||
                !TryReadOptionalId(
                    PigeonPoopEmojiIdBox.Text,
                    "鸽子屎表情 ID",
                    out ulong pigeonPoopEmojiId) ||
                !TryReadOptionalId(
                    HeartEmojiIdBox.Text,
                    "爱心中箭表情 ID",
                    out ulong heartEmojiId) ||
                !TryReadOptionalId(
                    EggEmojiIdBox.Text,
                    "扔鸡蛋表情 ID",
                    out ulong eggEmojiId))
            {
                return;
            }

            AppConfig config =
                new AppConfig
                {
                    DiscordBotToken = token,
                    DiscordChannelId = channelId,
                    PoopEmojiId = poopEmojiId,
                    PigeonPoopEmojiId = pigeonPoopEmojiId,
                    HeartEmojiId = heartEmojiId,
                    EggEmojiId = eggEmojiId
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

        private static string FormatOptionalId(ulong value)
        {
            return value == 0 ? "" : value.ToString();
        }

        private bool TryReadOptionalId(
            string text,
            string displayName,
            out ulong value)
        {
            string trimmed = text.Trim();
            if (trimmed.Length == 0)
            {
                value = 0;
                return true;
            }

            if (ulong.TryParse(trimmed, out value) && value != 0)
            {
                return true;
            }

            MessageBox.Show(
                $"请输入有效的 {displayName}，或留空禁用。",
                "表情 ID 无效",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return false;
        }
    }
}
