namespace API.DTOs.Promotion
{
    public class PromotionOutputDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string DiscountType { get; set; }
        public decimal Amount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxUses { get; set; }
        public bool IsActive { get; set; }
    }

    public class PromotionInputDto
    {
        public string Code { get; set; }
        public string DiscountType { get; set; }
        public decimal Amount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxUses { get; set; }
    }

    public class ApplyPromotionDto
    {
        public string PromoCode { get; set; }
    }
}

