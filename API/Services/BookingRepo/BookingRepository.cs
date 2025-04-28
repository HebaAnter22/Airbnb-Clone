using API.Data;
using API.DTOs;
using API.Models;
using API.Services.BookingPaymentRepo;
using API.Services.PromotionRepo;
using API.Services.PropertyAvailabilityRepo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stripe;
using WebApiDotNet.Repos;

namespace API.Services.BookingRepo
{
    public class BookingRepository : GenericRepository<Booking>, IBookingRepository
    {
        private readonly AppDbContext _context;
        private readonly IPropertyAvailabilityRepository _propertyAvailabilityRepo;
        private readonly IPropertyService _propertyService;
        private readonly IPromotionRepository _promotionRepo;
        private readonly IServiceProvider _serviceProvider;

        public BookingRepository(AppDbContext context, IPropertyAvailabilityRepository propertyAvailabilityRepo, IPropertyService propertyService, IPromotionRepository promotionRepo, IServiceProvider serviceProvider) : base(context)
        {
            _context = context;
            _propertyAvailabilityRepo = propertyAvailabilityRepo;
            _propertyService = propertyService;
            _promotionRepo = promotionRepo;
            _serviceProvider = serviceProvider;
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

        public async Task<IEnumerable<Booking>> GetAllBookingsAsync(int hostId)
        {
            return await _context.Bookings
                .Include(b => b.Property)
                    .ThenInclude(p => p.Host)
                .Include(b => b.Guest)
                .Include(b => b.Payments)
                .Where(b => b.Property.HostId == hostId)
                .ToListAsync();
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
                Console.WriteLine($"Starting deletion of booking {id}");
                
                // Find the booking with related data
                var booking = await _context.Bookings
                    .Include(b => b.Property)
                        .ThenInclude(p => p.CancellationPolicy)
                    .Include(b => b.Property)
                        .ThenInclude(p => p.Host)
                    .Include(b => b.Payments.Where(p => p.Status == "succeeded"))
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (booking == null)
                {
                    Console.WriteLine($"Booking {id} not found");
                    throw new KeyNotFoundException("Booking not found.");
                }

                Console.WriteLine($"Found booking {id} for property {booking.PropertyId}");
                
                // Log the booking property and cancellation policy details
                if (booking.Property == null)
                {
                    Console.WriteLine($"Warning: Property is null for booking {id}");
                }
                else 
                {
                    Console.WriteLine($"Property {booking.PropertyId} found for booking {id}");
                    
                    if (booking.Property.CancellationPolicy == null)
                    {
                        Console.WriteLine($"Warning: No cancellation policy found for property {booking.PropertyId}");
                    }
                    else
                    {
                        Console.WriteLine($"Cancellation policy: {booking.Property.CancellationPolicy.Name} with refund percentage: {booking.Property.CancellationPolicy.RefundPercentage}%");
                    }
                }

                // Calculate refund amount based on cancellation policy
                decimal refundAmount = 0;
                bool shouldRefund = false;

                // Check if there are any payments to refund
                if (booking.Payments == null || !booking.Payments.Any())
                {
                    Console.WriteLine($"No payments found for booking {id}");
                }
                else
                {
                    Console.WriteLine($"Found {booking.Payments.Count()} payments for booking {id}");
                    var cancellationPolicy = booking.Property?.CancellationPolicy;
                    
                    if (cancellationPolicy != null)
                    {
                        // Calculate days until check-in
                        int daysUntilCheckIn = (int)(booking.StartDate - DateTime.UtcNow).TotalDays;
                        Console.WriteLine($"Days until check-in: {daysUntilCheckIn}");
                        
                        // Determine refund percentage based on policy and timing
                        decimal refundPercentage = 0;
                        
                        switch (cancellationPolicy.Name.ToLower())
                        {
                            case "flexible":
                                // Full refund if canceled at least 24 hours before check-in
                                if (daysUntilCheckIn >= 1)
                                {
                                    refundPercentage = cancellationPolicy.RefundPercentage;
                                    Console.WriteLine($"Flexible policy: eligible for refund ({refundPercentage}%)");
                                }
                                else
                                {
                                    Console.WriteLine("Flexible policy: not eligible for refund (less than 24 hours before check-in)");
                                }
                                break;
                                
                            case "moderate":
                                // Partial refund if canceled at least 5 days before check-in
                                if (daysUntilCheckIn >= 5)
                                {
                                    refundPercentage = cancellationPolicy.RefundPercentage;
                                    Console.WriteLine($"Moderate policy: eligible for refund ({refundPercentage}%)");
                                }
                                else
                                {
                                    Console.WriteLine("Moderate policy: not eligible for refund (less than 5 days before check-in)");
                                }
                                break;
                                
                            case "strict":
                                // No refund if canceled within 7 days of check-in
                                if (daysUntilCheckIn >= 7)
                                {
                                    refundPercentage = cancellationPolicy.RefundPercentage;
                                    Console.WriteLine($"Strict policy: eligible for refund ({refundPercentage}%)");
                                }
                                else
                                {
                                    Console.WriteLine("Strict policy: not eligible for refund (less than 7 days before check-in)");
                                }
                                break;
                                
                            case "non_refundable":
                                // No refund for non-refundable bookings
                                refundPercentage = 0;
                                Console.WriteLine("Non-refundable policy: no refund");
                                break;
                                    
                            default:
                                // Default to no refund if policy is unknown
                                refundPercentage = 0;
                                Console.WriteLine($"Unknown policy type: {cancellationPolicy.Name}");
                                break;
                        }
                        
                        // Calculate refund amount based on total paid and refund percentage
                        var totalPaid = booking.Payments.Sum(p => p.Amount);
                        refundAmount = totalPaid * (refundPercentage / 100m);
                        Console.WriteLine($"Total paid: {totalPaid}, Refund percentage: {refundPercentage}%, Refund amount: {refundAmount}");
                        
                        // Only process refund if amount is greater than zero
                        shouldRefund = refundAmount > 0;
                    }
                    else
                    {
                        Console.WriteLine("No cancellation policy found, defaulting to no refund");
                    }
                }

                // Update property availability to mark dates as available again
                Console.WriteLine($"Updating availability for property {booking.PropertyId} from {booking.StartDate} to {booking.EndDate}");
                await _propertyAvailabilityRepo.UpdateAvailabilityAsync(
                    booking.PropertyId,
                    booking.StartDate,
                    booking.EndDate,
                    isAvailable: true);

                // Process refund if applicable
                if (shouldRefund && booking.Payments.Any())
                {
                    // Get the payment to refund (typically the first successful payment)
                    var payment = booking.Payments.FirstOrDefault();
                    if (payment != null)
                    {
                        Console.WriteLine($"Processing refund of {refundAmount} for payment {payment.Id}");
                        
                        // Get service to process the refund
                        var bookingPaymentRepository = new BookingPaymentRepository(
                            _context, 
                            this, 
                            _serviceProvider.GetService<IConfiguration>(),
                            _serviceProvider.GetService<ILogger<BookingPaymentRepository>>());
                        
                        // Process the refund
                        var refundSuccess = await bookingPaymentRepository.RefundPaymentAsync(
                            payment.Id, 
                            refundAmount,
                            "booking_canceled");
                        
                        // If refund failed but was expected, log the issue but continue with cancellation
                        if (!refundSuccess)
                        {
                            // Log the failed refund attempt
                            Console.WriteLine($"Failed to process refund of {refundAmount} for booking {id}");
                        }
                        else
                        {
                            Console.WriteLine($"Successfully processed refund of {refundAmount} for booking {id}");
                            
                            // Make sure we have the host information
                            if (booking.Property?.Host != null)
                            {
                                var hostId = booking.Property.Host.HostId;
                                var host = await _context.HostProfules.FindAsync(hostId);
                                
                                if (host != null)
                                {
                                    // Calculate host's portion of the refund
                                    decimal hostCommissionRate = 0.85m;
                                    decimal hostRefundAmount = refundAmount * hostCommissionRate;
                                    
                                    // Explicitly update host earnings to ensure they're updated
                                    host.AvailableBalance -= hostRefundAmount;
                                    host.TotalEarnings -= hostRefundAmount;
                                    
                                    _context.HostProfules.Update(host);
                                    
                                    Console.WriteLine($"Updated host {hostId} earnings: deducted {hostRefundAmount} from TotalEarnings and AvailableBalance");
                                }
                                else
                                {
                                    Console.WriteLine($"Warning: Host profile not found for host ID {hostId}");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Warning: Property or host information not available");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"No refund required for booking {id}");
                }

                // Remove the booking
                Console.WriteLine($"Removing booking {id} from database");
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                Console.WriteLine($"Successfully deleted booking {id}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log the exception
                Console.WriteLine($"Error in DeleteBookingAndUpdateAvailabilityAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
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
                // Ensure we load the cancellation policy with property
                var booking = await _context.Bookings
                    .Include(b => b.Guest)
                    .Include(b => b.Property)
                        .ThenInclude(p => p.CancellationPolicy)
                    .Include(b => b.Payments)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    Console.WriteLine($"Booking not found: {bookingId}");
                    return null;
                }

                // Log whether the policy was loaded or not
                if (booking.Property?.CancellationPolicy == null)
                {
                    Console.WriteLine($"Warning: No cancellation policy found for property {booking.PropertyId} in booking {bookingId}");
                }
                else 
                {
                    Console.WriteLine($"Found cancellation policy '{booking.Property.CancellationPolicy.Name}' for property {booking.PropertyId}");
                }

                return booking;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving booking details: {ex.Message}");
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
                        .ThenInclude(p => p.CancellationPolicy)
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

        public async Task<bool> IsPromotionValidForBookingAsync(int promotionId, int guestId, DateTime bookingStartDate)
        {
            var promotion = await _promotionRepo.GetPromotionByIdAsync(promotionId);
            if (promotion == null || !promotion.IsActive)
                return false;

            if (promotion.EndDate < bookingStartDate)
                return false;

            if (promotion.UsedCount >= promotion.MaxUses)
                return false;

            var isUsed = await _promotionRepo.IsPromotionUsedAsync(promotion.Code, guestId);
            if (isUsed)
                return false;

            return true;
        }

        public async Task<int> GetPromotionIdByCodeAsync(string promoCode)
        {
            var promotion = await _promotionRepo.GetPromotionAsync(promoCode);
            if (promotion == null)
                throw new KeyNotFoundException("Promotion not found.");
            return promotion.Id;
        }

        public async Task AddUserUsedPromotionAsync(UserUsedPromotion usedPromotion)
        {
            if (usedPromotion == null)
                throw new ArgumentNullException(nameof(usedPromotion));
            var existingRecord = await _context.UserUsedPromotions
                .FirstOrDefaultAsync(u => u.BookingId == usedPromotion.BookingId);
            if (existingRecord != null)
            {
                throw new InvalidOperationException("Promotion already used for this booking.");
            }

            _context.UserUsedPromotions.Add(usedPromotion);
            await _context.SaveChangesAsync();
        }

        public async Task<decimal> GetTotalIncomeForHostAsync(int hostId)
        {
            // Sum booking amounts where the host is the specified hostId
            var bookingAmounts = await _context.Bookings
                
                .Where(b => b.Property.HostId == hostId && b.Status == "Confirmed")
                .Include(b => b.Payments)
                .SumAsync(b => b.TotalAmount);

            // Sum payment amounts for the host
            var paymentAmounts = await _context.Bookings
                .Where(b => b.Property.HostId == hostId)
                .Include(b => b.Payments)
                .SelectMany(b => b.Payments)
                .Where(p => p.Status == "succeeded")
                .SumAsync(p => p.Amount);

            // Return the total of both sums
            return bookingAmounts + paymentAmounts;
        }

        public async Task<decimal> GetTotalSpentByGuestAsync(int guestId)
        {
            // Sum confirmed booking amounts
            var bookingAmounts = await _context.Bookings
                .Where(b => b.GuestId == guestId && b.Status == "Confirmed")
                .SumAsync(b => b.TotalAmount);

            // Sum payment amounts
            var paymentAmounts = await _context.Bookings
                .Where(b => b.GuestId == guestId)
                .Include(b => b.Payments)
                .SelectMany(b => b.Payments)
                .Where(p => p.Status == "succeeded")
                .SumAsync(p => p.Amount);

            // Return the total of both sums
            return bookingAmounts + paymentAmounts;
        }

        public async Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, int bookingId)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = "usd",
                PaymentMethodTypes = new List<string> { "card" },
                CaptureMethod = "automatic",
                Metadata = new Dictionary<string, string> { { "bookingId", bookingId.ToString() } }
            };

            var service = new PaymentIntentService();
            return await service.CreateAsync(options);
        }

    }

}

