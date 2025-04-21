using API.Models;

namespace API.Services.AdminRepo
{
    public interface IAdminRepository
    {
        Task<bool> ConfirmHostVerificationAsync(int verificationId);
        Task<IEnumerable<HostVerification>> GetAllPendingVerificationsAsync();

        Task<bool> BlockUserAsync(int userId, bool isBlocked);
        Task<IEnumerable<User>> GetAllHostsAsync();

        Task<IEnumerable<User>> GetAllGuestsAsync();

        Task<bool> ApproveHostAsync(int hostId, bool isApproved);
        Task<bool> ApprovePropertyAsync(int propertyId, bool isApproved);
        Task<IEnumerable<Property>> GetAllPendingPropertiesAsync();

        Task<IEnumerable<Property>> GetAllApprovedPropertiesAsync();
        Task<HostVerification> GetVerificationByhostsAsync(int hostid);

        Task<bool> SuspendPropertyAsync(int propertyId, bool isSuspended);
        Task<IEnumerable<Booking>> GetAllBookingsAsync();
        Task<bool> UpdateBookingStatusAsync(int bookingId, string newStatus);
    }
}