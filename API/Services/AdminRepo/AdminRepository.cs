using API.Data;
using API.Models;
using API.Services.BookingRepo;
using API.Services.HostVerificationRepo;
using Microsoft.EntityFrameworkCore;
using WebApiDotNet.Repos;

namespace API.Services.AdminRepo
{
    public class AdminRepository : IAdminRepository
    {
        private readonly AppDbContext _context;
        private readonly IHostVerificationRepository _hostVerificationRepository;

        public AdminRepository(AppDbContext context,IHostVerificationRepository hostVerificationRepository)
        {
            _context = context;
            _hostVerificationRepository = hostVerificationRepository;
        }

        #region Host Verification Management

        public async Task<bool> ConfirmHostVerificationAsync(int verificationId)
        {
            var verification = await _context.HostVerifications.FindAsync(verificationId);
            if (verification == null)
                return false;

            verification.Status = "verified";
            verification.VerifiedAt = DateTime.UtcNow;
            _context.HostVerifications.Update(verification);

            var host = await _context.HostProfules.FindAsync(verification.Host.HostId);
            if (host != null)
                host.IsVerified = true;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<HostVerification>> GetAllPendingVerificationsAsync()
        {
            return await _context.HostVerifications
                .Include(v => v.Host)
                .ThenInclude(h => h.User)
                .Where(v => v.Status == "pending")
                .ToListAsync();
        }

        #endregion

        #region User Management

        public async Task<bool> BlockUserAsync(int userId, bool isBlocked)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.AccountStatus = isBlocked ? "Blocked" : "Active";
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<User>> GetAllHostsAsync()
        {
            return await _context.Users
                .Include(u => u.Host).ThenInclude(h => h.Properties).Include(h=>h.Bookings)
                .Where(u => u.Role == UserRole.Host.ToString()) 
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetAllGuestsAsync()
        {
            return await _context.Users.Include(u => u.Bookings)
                .Where(u => u.Role == UserRole.Guest.ToString()) 
                .ToListAsync();
        }


        #endregion

        #region Property Management

        public async Task<bool> ApprovePropertyAsync(int propertyId, bool isApproved)
        {
            var property = await _context.Properties.FindAsync(propertyId);
            if (property == null)
                return false;

            property.Status = isApproved ? "Active" : "Pending";
            _context.Properties.Update(property);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Property>> GetAllPendingPropertiesAsync()
        {
            return await _context.Properties
                .Include(p => p.Host)
                .ThenInclude(h => h.User)
                .Where(p => p.Status == "Pending")
                .ToListAsync();
        }

        #endregion

        #region Booking Management

        public async Task<IEnumerable<Booking>> GetAllBookingsAsync()
        {
            return await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Property)
                .ToListAsync();
        }

        public async Task<bool> UpdateBookingStatusAsync(int bookingId, string newStatus)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
                return false;

            booking.Status = newStatus;
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        

    }
}
