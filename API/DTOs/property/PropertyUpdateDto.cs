using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class PropertyUpdateDto
    {
        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? PropertyType { get; set; }

        public string? Country { get; set; }

        public string? Address { get; set; }

        public string? City { get; set; }

        public string? PostalCode { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public string? Currency { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? PricePerNight { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CleaningFee { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ServiceFee { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int? MinNights { get; set; }


        [Range(1, int.MaxValue)]
        public int? MaxNights { get; set; }


        [Range(1, int.MaxValue)]
        public int? Bathrooms { get; set; }

        [Range(1, int.MaxValue)]
        public int? Bedrooms { get; set; }


        [Range(1, int.MaxValue)]
        public int? MaxGuests { get; set; }

        public string? CheckInTime { get; set; }

        public string? CheckOutTime { get; set; }

        public bool? InstantBook { get; set; }

        public string? Status { get; set; }

        public int? CategoryId { get; set; }

        public int? CancellationPolicyId { get; set; }

        // Amenities to add or update
        public List<int>? AmenityIds { get; set; } = new List<int>();

        // Images to add or update
        public List<PropertyImageUpdateDto>? Images { get; set; } = new List<PropertyImageUpdateDto>();
    }

    public class PropertyImageUpdateDto
    {
        public int? Id { get; set; } // Null for new images
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public bool? IsPrimary { get; set; }
        public string? Category { get; set; }
    }
}