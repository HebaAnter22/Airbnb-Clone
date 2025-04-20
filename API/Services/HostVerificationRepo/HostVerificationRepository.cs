using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using WebApiDotNet.Repos;
using Microsoft.AspNetCore.Http;

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

        public async Task<HostVerification> CreateVerificationWithImagesAsync(int hostId, List<IFormFile> files)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create verification record
                var verification = new HostVerification
                {
                    UserId = hostId,
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

                            // Update verification with the first image URL or maintain a list
                            verification.DocumentUrl = verification.DocumentUrl ?? fullImageUrl; // Store first image as primary
                        }
                    }

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
    }
}