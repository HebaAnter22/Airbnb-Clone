using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class AmenityDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string IconUrl { get; set; }
    }
} 