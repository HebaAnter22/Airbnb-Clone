using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using WebApiDotNet.Repos;

namespace API.Services.HostVerificationRepo
{
    public class HostVerificationRepository : GenericRepository<HostVerification>, IHostVerificationRepository
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public HostVerificationRepository(AppDbContext context, IWebHostEnvironment environment) : base(context)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IEnumerable<HostVerification>> GetAllVerificationsAsync()
        {
            return await _context.HostVerifications
                .Include(v => v.Host)
                .ThenInclude(h => h.User) 
                .ToListAsync();
        }

        public async Task<HostVerification> GetVerificationByIdAsync(int verificationId)
        {
            return await _context.HostVerifications
                .Include(v => v.Host)
                .ThenInclude(h => h.User)
                .FirstOrDefaultAsync(v => v.Id == verificationId);
        }

        public async Task<HostVerification> GetVerificationByhostsAsync(int hostid)
        {
            return await _context.HostVerifications
                .Include(v => v.Host)
                .ThenInclude(h => h.User)
                .Where(v => v.Host.HostId == hostid)
                .FirstOrDefaultAsync();
        }

        public async Task<HostVerification> CreateVerificationWithImagesAsync(int hostId, List<IFormFile> files)
        {
            if(files == null || !files.Any() || files.Count !=2)
                throw new ArgumentException("Exactly two images must be provided for verification.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create verification record
                var verification = new HostVerification
                {
                    HostId = hostId,
                    Status = "pending",
                    SubmittedAt = DateTime.UtcNow
                };

                _context.HostVerifications.Add(verification);
                await _context.SaveChangesAsync();

                // Handle image uploads if provided
                if (files != null && files.Any())
                {
                    var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "hostverifications", verification.Id.ToString());
                    Directory.CreateDirectory(uploadPath);

                    var imageUrls = new List<string>();

                    var baseUrl = "https://localhost:7228"; // Should come from configuration

                    foreach (var file in files)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                            var filePath = Path.Combine(uploadPath, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var relativePath = $"/uploads/hostverifications/{verification.Id}/{fileName}";
                            var fullImageUrl = $"{baseUrl}{relativePath}";
                            imageUrls.Add(fullImageUrl);

                        }
                    }
                    // Update verification with the first image URL or maintain a list
                    verification.DocumentUrl1 = imageUrls.Count > 0 ? imageUrls[0] : null;
                    verification.DocumentUrl2 = imageUrls.Count > 1 ? imageUrls[1] : null;

                    _context.HostVerifications.Update(verification);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return verification;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
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

        public async Task<Models.Host> GetHostByIdAsync(int hostId)
        {
            return await _context.HostProfules
                .Include(h => h.User)
                .FirstOrDefaultAsync(h => h.HostId == hostId);
        }
    }

}
