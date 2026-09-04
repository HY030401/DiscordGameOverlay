using System.IO;
using System.Text.Json;

namespace DiscordGameOverlay.Config
{
    public class AppConfig
    {
        public string DiscordBotToken { get; set; } = "";

        public ulong DiscordChannelId { get; set; }

        public static AppConfig Load()
        {
            string path = Path.Combine(
                AppContext.BaseDirectory,
                "config.json"
            );

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"找不到配置文件：{path}"
                );
            }

            string json = File.ReadAllText(path);

            AppConfig? config =
                JsonSerializer.Deserialize<AppConfig>(json);

            if (config == null)
            {
                throw new Exception("config.json 读取失败");
            }

            return config;
        }
    }
}