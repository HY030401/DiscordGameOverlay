using Discord;
using Discord.WebSocket;
using DiscordGameOverlay.Models;
using System.Text.RegularExpressions;

namespace DiscordGameOverlay.Services
{
    public class DiscordService
    {
        private DiscordSocketClient? _client;

        private readonly string _token;
        private readonly ulong _channelId;
        private readonly Dictionary<ulong, OverlayEffectType> _effectByEmojiId;

        private static readonly Regex CustomEmojiPattern = new(
            @"<a?:[A-Za-z0-9_]+:(\d+)>",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public event Action<ChatMessage>? MessageReceived;
        public event Action<OverlayEffectType>? EffectRequested;

        public DiscordService(
            string token,
            ulong channelId,
            ulong poopEmojiId,
            ulong pigeonPoopEmojiId,
            ulong heartEmojiId,
            ulong eggEmojiId)
        {
            _token = token;
            _channelId = channelId;
            _effectByEmojiId = new Dictionary<ulong, OverlayEffectType>();

            AddEffectEmoji(poopEmojiId, OverlayEffectType.Poop);
            AddEffectEmoji(
                pigeonPoopEmojiId,
                OverlayEffectType.PigeonPoop);
            AddEffectEmoji(heartEmojiId, OverlayEffectType.Heart);
            AddEffectEmoji(eggEmojiId, OverlayEffectType.Egg);
        }

        public async Task StartAsync()
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents =
                GatewayIntents.Guilds |
                    GatewayIntents.GuildMessages |
                    GatewayIntents.GuildMessageReactions |
                    GatewayIntents.MessageContent
            };

            _client = new DiscordSocketClient(config);

            _client.Log += OnLog;
            _client.Ready += OnReady;
            _client.MessageReceived += OnMessageReceived;
            _client.ReactionAdded += OnReactionAdded;

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

            _client.Log -= OnLog;
            _client.Ready -= OnReady;
            _client.MessageReceived -= OnMessageReceived;
            _client.ReactionAdded -= OnReactionAdded;

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

            List<OverlayEffectType> effects =
                FindMessageEffects(message.Content).ToList();

            if (effects.Count > 0)
            {
                foreach (OverlayEffectType effect in effects)
                {
                    EffectRequested?.Invoke(effect);
                }

                // 动画触发消息不进入主播弹幕或观众弹幕。
                return Task.CompletedTask;
            }

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

        private Task OnReactionAdded(
            Cacheable<IUserMessage, ulong> message,
            Cacheable<IMessageChannel, ulong> channel,
            SocketReaction reaction)
        {
            if (channel.Id != _channelId)
                return Task.CompletedTask;

            if (_client != null && reaction.UserId == _client.CurrentUser.Id)
                return Task.CompletedTask;

            if (reaction.User.IsSpecified && reaction.User.Value.IsBot)
                return Task.CompletedTask;

            if (reaction.Emote is Emote customEmoji &&
                _effectByEmojiId.TryGetValue(
                    customEmoji.Id,
                    out OverlayEffectType effect))
            {
                EffectRequested?.Invoke(effect);
            }

            return Task.CompletedTask;
        }

        private IEnumerable<OverlayEffectType> FindMessageEffects(
            string content)
        {
            foreach (Match match in CustomEmojiPattern.Matches(content))
            {
                if (ulong.TryParse(
                        match.Groups[1].Value,
                        out ulong emojiId) &&
                    _effectByEmojiId.TryGetValue(
                        emojiId,
                        out OverlayEffectType effect))
                {
                    // 每一个表情都计数，同一条消息中的重复表情不会合并。
                    yield return effect;
                }
            }
        }

        private void AddEffectEmoji(
            ulong emojiId,
            OverlayEffectType effect)
        {
            if (emojiId != 0)
                _effectByEmojiId[emojiId] = effect;
        }
    }
}
