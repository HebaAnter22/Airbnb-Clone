using System.Security.Claims;
using API.DTOs.Review;
using API.Models;
using API.Services.ReviewRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpDelete("{reviewId}")]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            try
            {
                // Verify the review exists and belongs to the current user
                var review = await _reviewService.GetReviewByIdAsync(reviewId);
                if (review == null)
                {
                    return NotFound("Review not found");
                }

                var currentUserId = GetCurrentUserId();
                if (review.ReviewerId != currentUserId)
                {
                    return Forbid("You can only delete your own reviews");
                }

                await _reviewService.DeleteReviewAsync(reviewId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{reviewId}")]
        public async Task<IActionResult> UpdateReview(int reviewId, [FromBody] updateReviewDto updatedReview)
        {
            try
            {
                // Verify the review exists and belongs to the current user
                var existingReview = await _reviewService.GetReviewByIdAsync(reviewId);
                if (existingReview == null)
                {
                    return NotFound("Review not found");
                }

                var currentUserId = GetCurrentUserId();
                if (existingReview.ReviewerId != currentUserId)
                {
                    return Forbid("You can only edit your own reviews");
                }

                // Validate rating (1-5)
                if (updatedReview.Rating < 1 || updatedReview.Rating > 5)
                {
                    return BadRequest("Rating must be between 1 and 5");
                }

                // Update the review
                var result = await _reviewService.UpdateReviewAsync(reviewId, updatedReview);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("property/{propertyId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPropertyReviews(int propertyId)
        {
            try
            {
                var property = await _reviewService.GetPropertyWithReviewsAsync(propertyId);
                if (property == null)
                {
                    return NotFound("Property not found");
                }

                // Map to DTO if needed, or return directly
                return Ok(new
                {
                    Property = property,
                    Reviews = property.Bookings
                        .Where(b => b.Review != null)
                        .Select(b => new
                        {
                            b.Review.Id,
                            b.Review.Rating,
                            b.Review.Comment,
                            b.Review.CreatedAt,
                            b.Review.UpdatedAt,
                            ReviewerName = b.Review.Reviewer?.FirstName + " " + b.Review.Reviewer?.LastName,
                            ReviewerId = b.Review.ReviewerId
                        })
                        .ToList()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Invalid user ID");
            }
            return userId;
        }
    }
}
