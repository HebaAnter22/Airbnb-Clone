namespace API.DTOs.Profile
{
    
    public class HostProfileListingsDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal PricePerNight { get; set; }
        public double Rating { get; set; }
        public string PropertyType { get; set; }
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public List<PropertyImageDto> Images { get; set; } = new List<PropertyImageDto>();
    }

    public class HostProfileListingsImageDto
    {
        public string ImageUrl { get; set; }
        public bool IsFeatured { get; set; }
    }
}
