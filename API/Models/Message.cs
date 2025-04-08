namespace API.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        // Navigation Properties
        public virtual Conversation Conversation { get; set; } = null!;
        public virtual User Sender { get; set; }= null!;
    }
}