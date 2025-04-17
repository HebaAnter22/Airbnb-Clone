using API.Data;
using API.Models;
using WebApiDotNet.Repos;

namespace API.Services.PromotionRepo
{
    public class PromotionRepository : GenericRepository<Promotion>, IPromotionRepository
    {
        private readonly AppDbContext _context;
        public PromotionRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }
        
    }
}
