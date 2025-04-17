using System.Text.Json.Serialization;

namespace API.Models
{
    public class PropertyCategory
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }

        // Relationships
        [JsonIgnore]
        public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
    }
}

