using API.DTOs.Amenity;
using API.DTOs.Review;
using AirBnb.BL.Dtos.BookingDtos;

namespace API.DTOs
{
    public class PropertyDto
    {
        public int Id { get; set; }
         
        public int cancellationPolicyId { get; set; }
        public int? HostId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string PropertyType { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
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

        // Host information
        public string HostName { get; set; }
        public string HostProfileImage { get; set; }
        
        // Cancellation Policy
        public CancellationPolicyDTO CancellationPolicy { get; set; }

        public int CategoryID { get; set; }

        public string CategoryName { get; set; }


        // Images
        public List<PropertyImageDto> Images { get; set; } = new List<PropertyImageDto>();


        // Amenities
        public List<AmenityDto> Amenities { get; set; } = new List<AmenityDto>();

        // Availability
        //public List<PropertyAvailabilityDto> Availabilities { get; set; } = new List<PropertyAvailabilityDto>();

        // Reviews
        public List<ReviewDto> Reviews { get; set; } = new List<ReviewDto>();

        // Ratings and Reviews Summary
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int FavoriteCount { get; set; }
        public bool IsGuestFavorite { get; set; }

        // Additional UI properties
        public bool IsAvailable { get; set; }
        public string Currency { get; set; } = "USD";
        public bool InstantBook { get; set; }
    }
}