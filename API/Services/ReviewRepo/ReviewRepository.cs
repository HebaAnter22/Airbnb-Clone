using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using WebApiDotNet.Repos;

namespace API.Services.ReviewRepo
{
    public class ReviewRepository : GenericRepository<Review>, IReviewRepository
    {
        private readonly AppDbContext _context;
        public ReviewRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Review>> GetReviewsByPropertyIdAsync(int propertyId)
        {
            return await _context.Reviews
                .Include(r => r.Booking).ThenInclude(b => b.Property)
                .Where(r => r.Booking.PropertyId == propertyId)
                .ToListAsync();
        }
        public async Task<IEnumerable<Review>> GetReviewsByGuestIdAsync(int guestId)
        {
            return await _context.Reviews
                .Include(r => r.Booking)
                .Where(r => r.Booking.GuestId == guestId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Review>> GetAllReviewsAsync()
        {
            return await _context.Reviews
                .Include(r => r.Booking)
                .ThenInclude(b => b.Property)
                .ThenInclude(p => p.Host)
                .ToListAsync();
        }

        public async Task<Review?> GetReviewByGuestIdAndPropertyIdAsync(int guestId, int propertyId)
        {
            return await _context.Reviews
                .FirstOrDefaultAsync(r => r.Booking.GuestId == guestId && r.Booking.PropertyId == propertyId);
        }

        public async Task<Review> GetReviewByBookingIdAsync(int bookingId)
        {
            return await _context.Reviews
                .Include(r => r.Booking)
                .ThenInclude(b => b.Property)
                .FirstOrDefaultAsync(r => r.BookingId == bookingId);
        }

        public async Task<Review> CreateReviewAsync(Review review)
        {
            await _context.Reviews.AddAsync(review);
            await _context.SaveChangesAsync();
            return review; 
        }


    }
}
