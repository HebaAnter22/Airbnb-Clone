using API.Models;
using WebApiDotNet.Repos;

namespace API.Services.HostVerificationRepo
{
    public interface IHostVerificationRepository : IGenericRepository<HostVerification>
    {
        Task<HostVerification> GetVerificationByIdAsync(int verificationId);
        Task<bool> UpdateVerificationStatusAsync(int verificationId, string newStatus);
    }

}
