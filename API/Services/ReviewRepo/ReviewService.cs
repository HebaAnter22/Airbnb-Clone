using API.Data;
using API.DTOs.Review;
using API.Models;
using Google;
using Microsoft.EntityFrameworkCore;

namespace API.Services.ReviewRepo
{
    public class ReviewsService : IReviewService
    {
        private readonly AppDbContext _context;

        public ReviewsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Review> GetReviewByIdAsync(int reviewId)
        {
            return await _context.Reviews
                .Include(r => r.Reviewer)
                .FirstOrDefaultAsync(r => r.Id == reviewId);
        }

        public async Task DeleteReviewAsync(int reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null)
            {
                throw new KeyNotFoundException("Review not found");
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
        }

        public async Task<Review> UpdateReviewAsync(int reviewId, updateReviewDto updatedReview)
        {
            var existingReview = await _context.Reviews.FindAsync(reviewId);
            if (existingReview == null)
            {
                throw new KeyNotFoundException("Review not found");
            }

            // Update only the allowed fields
            existingReview.Rating = updatedReview.Rating;
            existingReview.Comment = updatedReview.Comment;
            existingReview.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingReview;
        }

        public async Task<Property> GetPropertyWithReviewsAsync(int propertyId)
        {
            return await _context.Properties
                .Include(p => p.Bookings)
                    .ThenInclude(b => b.Review)
                        .ThenInclude(r => r.Reviewer)
                .FirstOrDefaultAsync(p => p.Id == propertyId);
        }
    }
}
