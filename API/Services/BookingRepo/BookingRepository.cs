using API.Data;
using API.DTOs;
using API.Models;
using API.Services.PromotionRepo;
using API.Services.PropertyAvailabilityRepo;
using Microsoft.EntityFrameworkCore;
using WebApiDotNet.Repos;

namespace API.Services.BookingRepo
{
    public class BookingRepository : GenericRepository<Booking>, IBookingRepository
    {
        private readonly AppDbContext _context;
        private readonly IPropertyAvailabilityRepository _propertyAvailabilityRepo;
        private readonly IPropertyService _propertyService;
        private readonly IPromotionRepository _promotionRepo;

        public BookingRepository(AppDbContext context, IPropertyAvailabilityRepository propertyAvailabilityRepo, IPropertyService propertyService, IPromotionRepository promotionRepo) : base(context)
        {
            _context = context;
            _propertyAvailabilityRepo = propertyAvailabilityRepo;
            _propertyService = propertyService;
            _promotionRepo = promotionRepo;
        }


        #region Host Methods

        // Get all bookings for a specific property with pagination.
        public async Task<(IEnumerable<Booking> bookings, int totalCount)> GetAllBookingForProperty(int propertyId, int page = 1, int pageSize = 10)
        {
            if (propertyId <= 0)
                throw new ArgumentException("Property ID must be greater than zero.");
            var property = await _context.Properties.FindAsync(propertyId);
            if (property == null)
                throw new KeyNotFoundException("Property not found.");
            var query = _context.Bookings
                .Where(b => b.PropertyId == propertyId)
                .AsNoTracking();

            var totalCount = await query.CountAsync();
            var bookings = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (bookings, totalCount);
        }

        // Get detailed bookings for a property including related data.
        public async Task<IEnumerable<Booking>> GetPropertyBookingDetails(int propertyId)
        {
            if (propertyId <= 0)
                throw new ArgumentException("Property ID must be greater than zero.");
            var property = await _context.Properties.FindAsync(propertyId);
            if (property == null)
                throw new KeyNotFoundException("Property not found.");

            try
            {
                return await _context.Bookings
                    .Include(b => b.Guest)
                    .Include(b => b.Property)
                    .Include(b => b.Payments)
                    .Where(b => b.PropertyId == propertyId)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while fetching property booking details.", ex);
            }
        }

        // Get bookings filtered by both guest and property.
        public async Task<IEnumerable<Booking>> GetBookingsByGuestAndPropertyAsync(string guestId, int propertyId)
        {
            if (string.IsNullOrWhiteSpace(guestId))
                throw new ArgumentException("User ID cannot be null or empty.");
            if (propertyId <= 0)
                throw new ArgumentException("Property ID must be greater than zero.");

            try
            {
                var guestIdInt = int.Parse(guestId);
                var property = await _context.Properties.FindAsync(propertyId);
                if (property == null)
                    throw new KeyNotFoundException("Property not found.");
                var guest = await _context.Users.FindAsync(guestIdInt);
                if (guest == null)
                    throw new KeyNotFoundException("Guest not found.");
                // Fetch bookings for the specified guest and property.
                return await _context.Bookings
                    .Where(b => b.GuestId == guestIdInt && b.PropertyId == propertyId)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (FormatException)
            {
                throw new ArgumentException("Invalid User ID format.");
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while fetching bookings by user and property.", ex);
            }
        }


        // Get a booking by user ID and property ID.
        public async Task<Booking> GetBookingByPropertyandUserAsync(string userId, int propertyId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.");
            if (propertyId <= 0)
                throw new ArgumentException("Property ID must be greater than zero.");

            try
            {
                return await _context.Bookings
                    .FirstOrDefaultAsync(b => b.GuestId == int.Parse(userId) && b.PropertyId == propertyId);
            }
            catch (FormatException)
            {
                throw new ArgumentException("Invalid User ID format.");
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while fetching booking by user and property.", ex);
            }
        }

        public async Task<bool> IsBookingOwnedByHostAsync(int bookingId, int hostId)
        {
            return await _context.Bookings
                .Include(b => b.Property)
                .ThenInclude(p => p.Host)
                .AnyAsync(b => b.Id == bookingId && b.Property.HostId == hostId);
        }

        public async Task<bool> UpdateBookingStatusAsync(int bookingId, string newStatus)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
                return false;

            booking.Status = newStatus;
            booking.UpdatedAt = DateTime.UtcNow;

            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            return true;
        }
        #endregion

        #region Guest Methods


        //Check if a property is available for booking within a specified date range. (IMPORTANT)
        public async Task<bool> IsPropertyAvailableForBookingAsync(int propertyId, DateTime startDate, DateTime endDate)
        {
            return await _propertyAvailabilityRepo.IsPropertyAvailableAsync(propertyId, startDate, endDate);
        }

        public async Task<DateTime?> GetLastAvailableDateForPropertyAsync(int propertyId)
        {
            var lastAvailableDate = await _context.PropertyAvailabilities
                .Where(pa => pa.PropertyId == propertyId && pa.IsAvailable)
                .OrderByDescending(pa => pa.Date)
                .Select(pa => pa.Date)
                .FirstOrDefaultAsync();

            return lastAvailableDate;
        }

        //Create a new booking for a property.
        public async Task CreateBookingAndUpdateAvailabilityAsync(Booking booking)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _context.Bookings.AddAsync(booking);

                await _propertyAvailabilityRepo.UpdateAvailabilityAsync(
                    booking.PropertyId,
                    booking.StartDate,
                    booking.EndDate,
                    isAvailable: false);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateBookingAndUpdateAvailabilityAsync(Booking booking, DateTime oldStartDate, DateTime oldEndDate)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _propertyAvailabilityRepo.UpdateAvailabilityAsync(
                    booking.PropertyId,
                    oldStartDate,
                    oldEndDate,
                    isAvailable: true);

                await _propertyAvailabilityRepo.UpdateAvailabilityAsync(
                    booking.PropertyId,
                    booking.StartDate,
                    booking.EndDate,
                    isAvailable: false);

                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteBookingAndUpdateAvailabilityAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var booking = await _context.Bookings.FindAsync(id);
                if (booking == null)
                {
                    throw new KeyNotFoundException("Booking not found.");
                }

                await _propertyAvailabilityRepo.UpdateAvailabilityAsync(
                    booking.PropertyId,
                    booking.StartDate,
                    booking.EndDate,
                    isAvailable: true);

                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }



        // Get all bookings made by a specific user with pagination.
        public async Task<(IEnumerable<Booking> bookings, int totalCount)> GetAllUserBooking(string userId, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.");

            try
            {
                var guestId = int.Parse(userId);

                var query = _context.Bookings
                    .Where(b => b.GuestId == guestId)
                    .AsNoTracking();

                var totalCount = await query.CountAsync();
                var bookings = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (bookings, totalCount);
            }
            catch (FormatException)
            {
                throw new ArgumentException("Invalid User ID format.");
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while fetching user bookings.", ex);
            }
        }


        // Get detailed information about a specific booking by ID.
        public async Task<Booking> GetUserBookingetails(int bookingId)
        {
            if (bookingId <= 0)
                throw new ArgumentException("Booking ID must be greater than zero.");

            try
            {
                return await _context.Bookings
                    .Include(b => b.Guest)
                    .Include(b => b.Property)
                    .Include(b => b.Payments)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while fetching booking details.", ex);
            }
        }


        // Get a booking with all related data included.
        public async Task<Booking> getBookingByIdWithData(int bookingId)
        {
            if (bookingId <= 0)
                throw new ArgumentException("Booking ID must be greater than zero.");

            try
            {
                return await _context.Bookings
                    .Include(b => b.Guest)
                    .Include(b => b.Property)
                    .Include(b => b.Review)
                    .Include(b => b.Payments)
                    .Include(b => b.UsedPromotion)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while fetching booking with related data.", ex);
            }
        }


        #endregion

        public async Task<PropertyDto> getPropertyByIdAsync(int propertyId)
        {
            if (propertyId <= 0)
                throw new ArgumentException("Property ID must be greater than zero.");
            try
            {
                return await _propertyService.GetPropertyByIdAsync(propertyId);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while fetching property by ID.", ex);
            }
        }

        public async Task<Promotion> GetPromotionByIdAsync(int promotionId)
        {
            if (promotionId <= 0)
                throw new ArgumentException("Promotion ID must be greater than zero.");
            try
            {
                return await _promotionRepo.GetByIdAsync(promotionId);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while fetching promotion by ID.", ex);
            }
        }
    }

}

