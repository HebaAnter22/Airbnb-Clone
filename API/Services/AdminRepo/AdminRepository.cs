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
                .Include(u => u.Host).ThenInclude(h => h.Properties).ThenInclude(p => p.Bookings).ThenInclude(b => b.Review)
                .Where(u => u.Role == UserRole.Host.ToString())
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetAllGuestsAsync()
        {
            return await _context.Users.Include(u => u.Bookings)
                .Where(u => u.Role == UserRole.Guest.ToString()) 
                .ToListAsync();
        }


        public async Task<HostVerification> GetVerificationByhostsAsync(int hostid)
        {
            return await _context.HostVerifications
                .Include(v => v.Host)
                .ThenInclude(h => h.User)
                .Where(v => v.Host.HostId == hostid)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ApproveHostAsync(int hostId, bool isApproved)
        {
            try
            {
                var host = await _context.HostProfules
                    .Include(h => h.User)
                    .FirstOrDefaultAsync(h => h.HostId == hostId);

                if (host == null)
                    return false;

                host.IsVerified = true;
                host.User.AccountStatus = "Active";
                
                _context.HostProfules.Update(host);
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ApproveHostAsync: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Property Management

        public async Task<bool> ApprovePropertyAsync(int propertyId, bool isApproved)
        {
            try
            {
                var property = await _context.Properties.FindAsync(propertyId);
                if (property == null)
                {
                    Console.WriteLine($"Property with ID {propertyId} not found.");
                    return false;
                }

                property.Status = isApproved ? "Active" : "Rejected";
                _context.Properties.Update(property);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Property with ID {propertyId} updated to status: {property.Status}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ApprovePropertyAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> SuspendPropertyAsync(int propertyId, bool isSuspended)
        {
            var property = await _context.Properties.FindAsync(propertyId);
            if (property == null)
                return false;

            property.Status = "Suspended"; 
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


        public async Task<IEnumerable<Property>> GetAllApprovedPropertiesAsync()
        {
            return await _context.Properties
                .Where(p => p.Status == "Active")
                .Include(p => p.Host)
                .ThenInclude(h => h.User)
                .Include(p => p.Bookings)
                .ThenInclude(b => b.Review)
                .ThenInclude(r => r.Reviewer)
                .Include(p => p.PropertyImages)
                .Include(p => p.Amenities)

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
