using System.ComponentModel.DataAnnotations;
using API.DTOs.Amenity;

namespace API.DTOs
{
    public class PropertyCreateDto
    {

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; }



        [Required]
        public string Description { get; set; }


        [Required]
        [StringLength(50)]
        public string PropertyType { get; set; }

        [Required]
        [StringLength(100)]
        public string Country { get; set; }

        [Required]
        [StringLength(255)]
        public string Address { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; }


        [Required]
        [StringLength(20)]
        public string PostalCode { get; set; }

        [Required]
        [Range(-90, 90)]
        public decimal? Latitude { get; set; }

        [Required]
        [Range(-180, 180)]
        public decimal? Longitude { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PricePerNight { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CleaningFee { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ServiceFee { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int? MinNights { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int? MaxNights { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int? Bedrooms { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int? Bathrooms { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int? MaxGuests { get; set; }

        [StringLength(3)]
        public string? Currency { get; set; }

        [Required]
        public bool? InstantBook { get; set; }

        [Required]
        public int? CancellationPolicyId { get; set; }

        [Required]
        public List<int> Amenities { get; set; } = new List<int>();

        [Required]
        public List<PropertyImageCreateDto> Images { get; set; } = new List<PropertyImageCreateDto>();
    }

    public class PropertyImageCreateDto
    {
        [Required]
        public string ImageUrl { get; set; }

        [Required]
        public bool IsPrimary { get; set; }
    }
}