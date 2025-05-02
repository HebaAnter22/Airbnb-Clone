using API.DTOs.Review;
using API.Models;

namespace API.Services.ReviewRepo
{
    public interface IReviewService
    {
        Task<Review> GetReviewByIdAsync(int reviewId);
        Task DeleteReviewAsync(int reviewId);
        Task<Review> UpdateReviewAsync(int reviewId, updateReviewDto updatedReview);
        Task<Property> GetPropertyWithReviewsAsync(int propertyId);
    }
}
