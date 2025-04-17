using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models
{
    [Table("Amenities")]
    public class Amenity
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Required]
        [Column("name")]
        public string Name { get; set; } = null!;
        
        [Required]
        [Column("category")]
        public string Category { get; set; } = null!;
        
        [Required]
        [Column("icon_url")]
        public string IconUrl { get; set; } = null!;


        // Relationships
        public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
    }
}