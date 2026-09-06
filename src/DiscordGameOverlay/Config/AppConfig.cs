using System;
using System.IO;
using System.Text.Json;

namespace DiscordGameOverlay.Config
{
    public class AppConfig
    {
        public string DiscordBotToken { get; set; } = "";

        public ulong DiscordChannelId { get; set; }

        public ulong PoopEmojiId { get; set; }
            = 1546008982026584094;

        public ulong PigeonPoopEmojiId { get; set; }
            = 1546035873668276234;

        public ulong HeartEmojiId { get; set; }
            = 1546009038943424522;

        public ulong EggEmojiId { get; set; }
            = 1546009077107269794;

        public static string ConfigDirectory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "DiscordGameOverlay"
                );
            }
        }

        public static string ConfigPath
        {
            get
            {
                return Path.Combine(
                    ConfigDirectory,
                    "config.json"
                );
            }
        }

        // 旧版本配置文件位置
        public static string LegacyConfigPath
        {
            get
            {
                return Path.Combine(
                    AppContext.BaseDirectory,
                    "config.json"
                );
            }
        }

        public static bool Exists()
        {
            // 新位置已经有配置
            if (File.Exists(ConfigPath))
            {
                return true;
            }

            // 如果旧位置还有 config.json，
            // 自动迁移到新的 LocalAppData 目录
            if (File.Exists(LegacyConfigPath))
            {
                AppConfig oldConfig =
                    LoadFromPath(LegacyConfigPath);

                oldConfig.Save();

                return true;
            }

            return false;
        }

        public static AppConfig Load()
        {
            if (!File.Exists(ConfigPath))
            {
                throw new FileNotFoundException(
                    $"找不到配置文件：{ConfigPath}"
                );
            }

            return LoadFromPath(ConfigPath);
        }

        private static AppConfig LoadFromPath(
            string path)
        {
            string json =
                File.ReadAllText(path);

            AppConfig? config =
                JsonSerializer.Deserialize<AppConfig>(
                    json
                );

            if (config == null)
            {
                throw new Exception(
                    $"配置文件读取失败：{path}"
                );
            }

            return config;
        }

        public void Save()
        {
            Directory.CreateDirectory(
                ConfigDirectory
            );

            JsonSerializerOptions options =
                new JsonSerializerOptions
                {
                    WriteIndented = true
                };

            string json =
                JsonSerializer.Serialize(
                    this,
                    options
                );

            File.WriteAllText(
                ConfigPath,
                json
            );
        }
    }
}
