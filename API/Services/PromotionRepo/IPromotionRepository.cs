using API.Models;
using Microsoft.EntityFrameworkCore;
using WebApiDotNet.Repos;

namespace API.Services.PromotionRepo
{
    public interface IPromotionRepository:IGenericRepository<Promotion>
    {

        Task<Promotion> GetPromotionAsync(string promoCode);
      
    }
}
