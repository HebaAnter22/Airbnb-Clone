using API.Models;
using WebApiDotNet.Repos;

namespace API.Services.HostVerificationRepo
{
    public interface IHostVerificationRepository : IGenericRepository<HostVerification>
    {
        Task<IEnumerable<HostVerification>> GetAllVerificationsAsync();
        Task<HostVerification> GetVerificationByIdAsync(int verificationId);
        Task<HostVerification> GetVerificationByhostsAsync(int hostid);
        Task<HostVerification> CreateVerificationWithImagesAsync(int hostId, List<IFormFile> files);
        Task<bool> UpdateVerificationStatusAsync(int verificationId, string newStatus);
        Task<Models.Host> GetHostByIdAsync(int hostId);
        Task<string> OnboardHostAsync(string email, string firstName, string lastName, string country);
        Task<bool> CreatePayoutAsync(int bookingId);
    }

}
