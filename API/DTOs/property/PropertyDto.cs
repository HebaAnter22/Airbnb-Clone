namespace API.DTOs
{
    public class PropertyDto
    {
        public int Id { get; set; }
        public int? HostId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string PropertyType { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public decimal? PricePerNight { get; set; }
        public decimal? CleaningFee { get; set; }
        public decimal? ServiceFee { get; set; }
        public int? MinNights { get; set; }
        public int? MaxNights { get; set; }
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }
        public int? MaxGuests { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<PropertyImageDto> Images { get; set; } = new List<PropertyImageDto>();
    }

  
}