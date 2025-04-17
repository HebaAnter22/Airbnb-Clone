using System.Text.Json.Serialization;

namespace API.Models
{
    public class Conversation
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public string Subject { get; set; }
        public int user1Id { get; set; }
        public int user2Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [JsonIgnore]
        public virtual Property Property { get; set; }
        public virtual User User1 { get; set; }
        public virtual User User2 { get; set; }
        public virtual ICollection<Message> Messages { get; set; }= new List<Message>();

    }
}
