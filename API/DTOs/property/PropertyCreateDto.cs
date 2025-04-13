using System.ComponentModel.DataAnnotations;

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

        [Range(-90, 90)]
        public decimal? Latitude { get; set; }

        [Range(-180, 180)]
        public decimal? Longitude { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PricePerNight { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CleaningFee { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ServiceFee { get; set; }

        [Range(1, int.MaxValue)]
        public int? MinNights { get; set; }

        [Range(1, int.MaxValue)]
        public int? MaxNights { get; set; }

        [Range(0, int.MaxValue)]
        public int? Bedrooms { get; set; }

        [Range(0, int.MaxValue)]
        public int? Bathrooms { get; set; }

        [Range(1, int.MaxValue)]
        public int? MaxGuests { get; set; }


        [Required]
        [StringLength(3)]
        public string? Currency { get; set; }

        public bool? InstantBook { get; set; }


        public int? CancellationPolicyId { get; set; }

    }
}