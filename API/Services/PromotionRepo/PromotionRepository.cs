using API.Data;
using API.Models;
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

    }
}
