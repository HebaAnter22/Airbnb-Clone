using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using WebApiDotNet.Repos;

namespace API.Services.HostVerificationRepo
{
    public class HostVerificationRepository : GenericRepository<HostVerification>, IHostVerificationRepository
    {
        private readonly AppDbContext _context;

        public HostVerificationRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<HostVerification> GetVerificationByIdAsync(int verificationId)
        {
            return await _context.HostVerifications
                .Include(v => v.Host)
                .ThenInclude(h => h.User)
                .FirstOrDefaultAsync(v => v.Id == verificationId);
        }

        public async Task<bool> UpdateVerificationStatusAsync(int verificationId, string newStatus)
        {
            var verification = await _context.HostVerifications.FindAsync(verificationId);
            if (verification == null)
                return false;

            verification.Status = newStatus;
            if (newStatus.Equals("verified", StringComparison.OrdinalIgnoreCase))
                verification.VerifiedAt = DateTime.UtcNow;

            _context.HostVerifications.Update(verification);
            await _context.SaveChangesAsync();
            return true;
        }
    }

}
