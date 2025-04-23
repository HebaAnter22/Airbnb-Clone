using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using WebApiDotNet.Repos;
using Microsoft.EntityFrameworkCore;

namespace API.Services.PromotionRepo
{
    public class PromotionRepository : GenericRepository<Promotion>, IPromotionRepository
    {
        private readonly AppDbContext _context;
        public PromotionRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Promotion> GetPromotionAsync(string promoCode)
        {
            var promotion = await _context.Promotions
                .Where(p => p.Code == promoCode)
                .FirstOrDefaultAsync();
            if (promotion == null)
            {
                return null;
            }
            return promotion;
        }

        public async Task<bool> IsPromotionUsedAsync(string promoCode, int guestId)
        {
            var promotion = await GetPromotionAsync(promoCode);
            if (promotion == null)
            {
                return false;
            }
            var usedCount = await _context.UserUsedPromotions
                .Where(u => u.PromotionId == promotion.Id)
                .CountAsync();
            return usedCount >= promotion.MaxUses;
        }

        public async Task<bool> UsePromotionAsync(string promoCode, int guestId)
        {
            var promotion = await GetPromotionAsync(promoCode);
            if (promotion == null || promotion.UsedCount >= promotion.MaxUses)
            {
                return false;
            }
            var userUsedPromotion = new UserUsedPromotion
            {
                PromotionId = promotion.Id,
                UserId = guestId,
                UsedAt = DateTime.UtcNow
            };
            _context.UserUsedPromotions.Add(userUsedPromotion);
            promotion.UsedCount++;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Promotion>> GetAllPromotionsAsync()
        {
            return await _context.Promotions.ToListAsync();
        }

        public async Task<Promotion> GetPromotionByIdAsync(int id)
        {
            return await _context.Promotions.FindAsync(id);
        }

        public async Task<bool> CreatePromotionAsync(Promotion promotion)
        {
            _context.Promotions.Add(promotion);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdatePromotionAsync(Promotion promotion)
        {
            _context.Promotions.Update(promotion);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeletePromotionAsync(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null) return false;

            _context.Promotions.Remove(promotion);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> IsPromotionValidAsync(int promotionId, DateTime currentDate)
        {
            var promotion = await _context.Promotions.FindAsync(promotionId);
            if (promotion == null) return false;

            return promotion.IsActive &&
                   promotion.StartDate <= currentDate &&
                   promotion.EndDate >= currentDate &&
                   promotion.UsedCount < promotion.MaxUses;
        }

        public async Task<decimal> CalculateDiscountedAmountAsync(int promotionId, decimal totalAmount)
        {
            var promotion = await _context.Promotions.FindAsync(promotionId);
            if (promotion == null || !promotion.IsActive) return totalAmount;

            if (promotion.DiscountType == "fixed")
                return Math.Max(totalAmount - promotion.Amount, 0);

            if (promotion.DiscountType == "percentage")
                return Math.Max(totalAmount - (totalAmount * promotion.Amount / 100), 0);

            return totalAmount;

        }

        public async Task DeactivateExpiredPromotionsAsync()
        {
            var currentTime = DateTime.UtcNow;

            var expiredPromotions = await _context.Promotions
                .Where(p => p.IsActive && p.EndDate < currentTime)
                .ToListAsync();

            if (expiredPromotions.Any())
            {
                foreach (var promotion in expiredPromotions)
                {
                    promotion.IsActive = false;
                }

                await _context.SaveChangesAsync();
            }
        }
    }
}
