using API.DTOs.Promotion;
using API.Models;
using API.Services.PromotionRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionRepository _promotionRepo;

        public PromotionController(IPromotionRepository promotionRepo)
        {
            _promotionRepo = promotionRepo;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllPromotions()
        {
            try
            {
                var promotions = await _promotionRepo.GetAllPromotionsAsync();

                var dtos = promotions.Select(p => new PromotionOutputDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    DiscountType = p.DiscountType,
                    Amount = p.Amount,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    MaxUses = p.MaxUses,
                    IsActive = p.IsActive,
                }).ToList();

                return Ok(new { Promotions = dtos });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetPromotionById(int id)
        {
            try
            {
                var promotion = await _promotionRepo.GetPromotionByIdAsync(id);
                if (promotion == null)
                    return NotFound("Promotion not found.");

                var dto = new PromotionOutputDto
                {
                    Id = promotion.Id,
                    Code = promotion.Code,
                    DiscountType = promotion.DiscountType,
                    Amount = promotion.Amount,
                    StartDate = promotion.StartDate,
                    EndDate = promotion.EndDate,
                    MaxUses = promotion.MaxUses,
                    IsActive = promotion.IsActive,
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpGet("code/{code}")]
        [Authorize]
        public async Task<IActionResult> GetPromotionByCode(string code)
        {
            try
            {
                var promotion = await _promotionRepo.GetPromotionAsync(code);
                if (promotion == null)
                    return NotFound("Promotion not found or invalid.");

                var dto = new PromotionOutputDto
                {
                    Id = promotion.Id,
                    Code = promotion.Code,
                    DiscountType = promotion.DiscountType,
                    Amount = promotion.Amount,
                    StartDate = promotion.StartDate,
                    EndDate = promotion.EndDate,
                    MaxUses = promotion.MaxUses,
                    IsActive = promotion.IsActive,
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreatePromotion([FromBody] PromotionInputDto input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var promotion = new Promotion
                {
                    Code = input.Code,
                    DiscountType = input.DiscountType,
                    Amount = input.Amount,
                    StartDate = input.StartDate,
                    EndDate = input.EndDate,
                    MaxUses = input.MaxUses,
                    CreatedAt = DateTime.UtcNow
                };

                var created = await _promotionRepo.CreatePromotionAsync(promotion);
                if (!created)
                    return StatusCode(500, "Failed to create promotion.");
                var dto = new PromotionOutputDto
                {
                    Id = promotion.Id,
                    Code = promotion.Code,
                    DiscountType = promotion.DiscountType,
                    Amount = promotion.Amount,
                    StartDate = promotion.StartDate,
                    EndDate = promotion.EndDate,
                    MaxUses = promotion.MaxUses,
                    IsActive = promotion.IsActive,
                };

                return CreatedAtAction(nameof(GetPromotionById), dto );
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> UpdatePromotion(int id, [FromBody] PromotionInputDto input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var promotion = await _promotionRepo.GetPromotionByIdAsync(id);
                if (promotion == null)
                    return NotFound("Promotion not found.");

                promotion.Code = input.Code;
                promotion.DiscountType = input.DiscountType;
                promotion.Amount = input.Amount;
                promotion.StartDate = input.StartDate;
                promotion.EndDate = input.EndDate;
                promotion.MaxUses = input.MaxUses;

                var updated = await _promotionRepo.UpdatePromotionAsync(promotion);
                if (!updated)
                    return StatusCode(500, "Failed to update promotion.");
                var dto = new PromotionOutputDto
                {
                    Id = promotion.Id,
                    Code = promotion.Code,
                    DiscountType = promotion.DiscountType,
                    Amount = promotion.Amount,
                    StartDate = promotion.StartDate,
                    EndDate = promotion.EndDate,
                    MaxUses = promotion.MaxUses,
                    IsActive = promotion.IsActive,
                };
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> DeletePromotion(int id)
        {
            try
            {
                var deleted = await _promotionRepo.DeletePromotionAsync(id);
                if (!deleted)
                    return NotFound("Promotion not found.");

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpGet("validate/{id}")]
        [Authorize]
        public async Task<IActionResult> ValidatePromotion(int id)
        {
            try
            {
                var isValid = await _promotionRepo.IsPromotionValidAsync(id, DateTime.UtcNow);
                return Ok(new { IsValid = isValid });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpPost("deactivate-expired")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> DeactivateExpiredPromotions()
        {
            try
            {
                await _promotionRepo.DeactivateExpiredPromotionsAsync();
                return Ok("Expired promotions have been deactivated.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }
    }

}
