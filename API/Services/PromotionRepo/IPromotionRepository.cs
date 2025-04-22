using API.Models;
using Microsoft.EntityFrameworkCore;
using WebApiDotNet.Repos;

namespace API.Services.PromotionRepo
{
    public interface IPromotionRepository : IGenericRepository<Promotion>
    {
        Task<Promotion> GetPromotionAsync(string promoCode);
        Task<IEnumerable<Promotion>> GetAllPromotionsAsync();
        Task<Promotion> GetPromotionByIdAsync(int id);
        Task<bool> UsePromotionAsync(string promoCode, int guestId);
        Task<bool> CreatePromotionAsync(Promotion promotion);
        Task<bool> UpdatePromotionAsync(Promotion promotion);
        Task<bool> DeletePromotionAsync(int id);
        Task<bool> IsPromotionValidAsync(int promotionId, DateTime currentDate);
        Task<bool> IsPromotionUsedAsync(string promoCode, int guestId);
        Task<decimal> CalculateDiscountedAmountAsync(int promotionId, decimal totalAmount);
        Task DeactivateExpiredPromotionsAsync();
    }
}
