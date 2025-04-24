using API.Models;

namespace API.Services.ReviewRepo
{
    public interface IReviewRepository
    {
        Task<IEnumerable<Review>> GetAllReviewsAsync();
        Task<Review> GetReviewByGuestIdAndPropertyIdAsync(int guestId, int propertyId);
        Task<IEnumerable<Review>> GetReviewsByGuestIdAsync(int guestId);
        Task<Review> GetReviewByBookingIdAsync(int bookingId);
        Task<IEnumerable<Review>> GetReviewsByPropertyIdAsync(int propertyId);
        Task<Review> CreateReviewAsync(Review review);
    }
}