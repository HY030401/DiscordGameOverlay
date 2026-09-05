using System.Collections.ObjectModel;
using DiscordGameOverlay.Models;

namespace DiscordGameOverlay.Services
{
    public class MessageManager
    {
        public ObservableCollection<ChatMessage> Messages { get; }
            = new ObservableCollection<ChatMessage>();

        public int MaxMessages { get; set; } = 5;

        public int MaxMessageLength { get; set; } = 60;

        public void AddMessage(ChatMessage message)
        {
            if (message.Content.Length > MaxMessageLength)
            {
                message.Content =
                    message.Content.Substring(
                        0,
                        MaxMessageLength
                    ) + "...";
            }

            Messages.Add(message);

            while (Messages.Count > MaxMessages)
            {
                Messages.RemoveAt(0);
            }
        }

        public void ClearMessages()
        {
            Messages.Clear();
        }
    }
}