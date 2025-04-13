namespace API.Models
{
    public enum PropertyStatus
    {
        Active,
        Pending,
        Suspended,
    }
    public class Property
    {
        public int Id { get; set; }
        public int HostId { get; set; }
        public int CategoryId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string PropertyType { get; set; }
        public string Country { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal PricePerNight { get; set; }
        public decimal CleaningFee { get; set; }
        public decimal ServiceFee { get; set; }
        public int MinNights { get; set; } = 1;
        public int MaxNights { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int MaxGuests { get; set; }
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public bool InstantBook { get; set; } = false;
        public string Status { get; set; } = PropertyStatus.Pending.ToString();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } 
        public int CancellationPolicyId { get; set; }

        // Navigation Properties
        public virtual Host Host { get; set; } = null!;
        public virtual CancellationPolicy CancellationPolicy { get; set; }
        public virtual PropertyCategory Category { get; set; }
        public virtual ICollection<PropertyImage> PropertyImages { get; set; } = new List<PropertyImage>();
        public virtual ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Favourite> Favourites { get; set; } = new List<Favourite>();
        public virtual ICollection<PropertyAvailability> Availabilities { get; set; } = new List<PropertyAvailability>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();


    }
}
