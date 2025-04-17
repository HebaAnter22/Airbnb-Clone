namespace API.DTOs
{
    public class PropertyImageDto
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public string ImageUrl { get; set; }
        public bool IsPrimary { get; set; }
        public string Category { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
