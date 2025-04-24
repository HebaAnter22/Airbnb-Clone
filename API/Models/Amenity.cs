using System.Text.Json.Serialization;

namespace API.Models
{
    public class Amenity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string IconUrl { get; set; }

        // Navigation Properties
        [JsonIgnore]
        public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
    }
}