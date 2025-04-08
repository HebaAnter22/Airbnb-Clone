using Stripe;

namespace API.Models
{
    public enum BookingStatus
    {
        Confirmed,
        Denied,
        Pending,
        Cancelled,
        Completed
    }
    public class Booking
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public int GuestId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string CheckInStatus { get; set; } = string.Empty;
        public string CheckOutStatus { get; set; } = string.Empty;
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public int PromotionId { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


        // Navigation Properties
        public virtual Property Property { get; set; }
        public virtual User Guest { get; set; }
        public virtual Review Review { get; set; }
        public virtual UserUsedPromotion UsedPromotion { get; set; }
        public virtual ICollection<BookingPayment> Payments { get; set; }= new List<BookingPayment>();

    }
}