using API.Models;
using API.Services.PromotionRepo;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromotionController:ControllerBase
    {
        private readonly IPromotionRepository _promotionRepository;
        public PromotionController(IPromotionRepository promRepo)
        {

            _promotionRepository = promRepo;
        }

        [HttpGet]
        public async Task<ActionResult<Promotion>> GetPromotion([FromQuery]string promoCode)
        {
            var promotion = await _promotionRepository.GetPromotionAsync(promoCode);

            if (promotion == null)
            {
                return NotFound($"Promotion with code {promoCode} not found.");
            }

            return Ok(promotion);
        }
        [HttpPut("Use")]
        public async Task<IActionResult> UsePromoCode([FromQuery] string promoCode)
        {
            var promotion = await _promotionRepository.GetPromotionAsync(promoCode);

            if (promotion == null)
                return NotFound("Invalid promo code");


            promotion.UsedCount += 1;

            await _promotionRepository.UpdateAsync(promotion);

            return Ok(promotion);
        }
    }
}
