using System.Security.Claims;
using API.DTOs.Review;
using API.Models;
using API.Services.BookingRepo;
using API.Services.ReviewRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewRepository _reviewRepo;
        private readonly IBookingRepository _bookingRepo;

        public ReviewController(IReviewRepository reviewRepo, IBookingRepository bookingRepo)
        {
            _reviewRepo = reviewRepo;
            _bookingRepo = bookingRepo;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Invalid or missing user ID in token.");
            }
            return userId;
        }

        [HttpGet("{propertyId}")]
        public async Task<IActionResult> GetReviewsByPropertyId(int propertyId)
        {
            var reviews = await _reviewRepo.GetReviewsByPropertyIdAsync(propertyId);
            if (!reviews.Any())
                return NotFound("No reviews found for this property.");

            return Ok(reviews);
        }

        [HttpGet("guest/{guestId}")]
        public async Task<IActionResult> GetReviewsByGuestId(int guestId)
        {
            var reviews = await _reviewRepo.GetReviewsByGuestIdAsync(guestId);
            if (!reviews.Any())
                return NotFound("No reviews found for this guest.");

            return Ok(reviews);
        }

        [HttpPost]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> CreateReview([FromBody] ReviewInputDto input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var booking = await _bookingRepo.getBookingByIdWithData(input.BookingId);
            if (booking == null || booking.GuestId != GetCurrentUserId())
                return BadRequest("Invalid booking ID or unauthorized access.");
            
            if(booking.EndDate < DateTime.UtcNow)
                booking.Status = "Completed";

            if (booking.Status != "Completed")
                return BadRequest("Booking must be completed to leave a review.");

            var existingReview = await _reviewRepo.GetReviewByBookingIdAsync(input.BookingId);
            if (existingReview != null)
                return BadRequest("A review for this booking already exists.");

            var review = new Review
            {
                BookingId = input.BookingId,
                ReviewerId = GetCurrentUserId(),
                Rating = input.Rating,
                Comment = input.Comment,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _reviewRepo.CreateReviewAsync(review);
            var dto = new ReviewOutputDto
            {
                Id = review.Id,
                BookingId = review.BookingId,
                ReviewerId = review.ReviewerId,
                ReviewerName = booking.Guest.FirstName,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
            };


            return CreatedAtAction(nameof(GetReviewsByPropertyId), new { propertyId = booking.PropertyId }, dto);
        }
    }
}
