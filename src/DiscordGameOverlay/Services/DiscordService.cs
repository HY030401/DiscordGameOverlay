using Discord;
using Discord.WebSocket;
using DiscordGameOverlay.Models;

namespace DiscordGameOverlay.Services
{
    public class DiscordService
    {
        private DiscordSocketClient? _client;

        private readonly string _token;
        private readonly ulong _channelId;

        public event Action<ChatMessage>? MessageReceived;

        public DiscordService(string token, ulong channelId)
        {
            _token = token;
            _channelId = channelId;
        }

        public async Task StartAsync()
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents =
                    GatewayIntents.Guilds |
                    GatewayIntents.GuildMessages |
                    GatewayIntents.MessageContent
            };

            _client = new DiscordSocketClient(config);

            _client.Log += OnLog;
            _client.Ready += OnReady;
            _client.MessageReceived += OnMessageReceived;

            await _client.LoginAsync(
                TokenType.Bot,
                _token
            );

            await _client.StartAsync();
        }

        public async Task StopAsync()
        {
            if (_client == null)
                return;

            await _client.StopAsync();
            await _client.LogoutAsync();
        }

        private Task OnLog(LogMessage message)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Discord] {message}"
            );

            return Task.CompletedTask;
        }

        private Task OnReady()
        {
            System.Diagnostics.Debug.WriteLine(
                $"Bot 已登录：{_client?.CurrentUser}"
            );

            Console.WriteLine(
                $"Bot 已登录：{_client?.CurrentUser}"
            );

            return Task.CompletedTask;
        }

        private Task OnMessageReceived(SocketMessage message)
        {
            // 忽略机器人消息
            if (message.Author.IsBot)
                return Task.CompletedTask;

            // 只读取指定频道
            if (message.Channel.Id != _channelId)
                return Task.CompletedTask;

            string displayName = message.Author.Username;

            // 如果消息来自服务器频道，优先使用服务器昵称
            if (message.Author is SocketGuildUser guildUser)
            {
                displayName = guildUser.DisplayName;
            }

            var chatMessage = new ChatMessage
            {
                UserId = message.Author.Id,
                DisplayName = displayName,
                Content = message.Content,
                MessageId = message.Id,
                ChannelId = message.Channel.Id,
                Timestamp = DateTime.Now,
                AvatarUrl = message.Author.GetAvatarUrl()
                            ?? message.Author.GetDefaultAvatarUrl()
            };

            System.Diagnostics.Debug.WriteLine(
                $"[弹幕] {chatMessage.DisplayName}: {chatMessage.Content}"            
            );
            
            Console.WriteLine(
                 $"[弹幕] {chatMessage.DisplayName}: {chatMessage.Content}"
            );

            MessageReceived?.Invoke(chatMessage);

            return Task.CompletedTask;
        }
    }
}