namespace DiscordGameOverlay.Models
{
    public class ChatMessage
    {
        public ulong UserId { get; set; }

        public string DisplayName { get; set; } = "";

        public string Content { get; set; } = "";

        public ulong MessageId { get; set; }

        public ulong ChannelId { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string AvatarUrl { get; set; } = "";
    }
}