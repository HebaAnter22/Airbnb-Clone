namespace API.Models
{
    public class PropertyImage
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public string ImageUrl { get; set; }
        public string? Description { get; set; }
        public bool IsPrimary { get; set; } = false;    
        public string Category { get; set; } = "Additional";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property
        public virtual Property Property { get; set; } = null!;
    }
}