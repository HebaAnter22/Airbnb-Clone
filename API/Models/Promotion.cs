namespace API.Models
{
    public class Promotion
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string DiscountType { get; set; }
        public decimal Amount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxUses { get; set; }
        public int UsedCount { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<UserUsedPromotion> UserUsedPromotions { get; set; } = new List<UserUsedPromotion>();
    }
}
