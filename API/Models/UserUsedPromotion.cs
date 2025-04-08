namespace API.Models
{
    public class UserUsedPromotion
    {
        public int Id { get; set; }
        public int PromotionId { get; set; }
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public decimal DiscountedAmount { get; set; }
        public DateTime UsedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Promotion Promotion { get; set; }
        public virtual Booking Booking { get; set; }
        public virtual User User { get; set; }
    }
}
