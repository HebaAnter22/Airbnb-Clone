using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;
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
            if (files == null || !files.Any() || files.Count != 2)
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



        // Method to onboard a host and create a Stripe account
        public async Task<string> OnboardHostAsync(string email, string firstName, string lastName, string country)
        {
            // Create a connected Stripe account for the host
            var accountService = new AccountService();
            var accountOptions = new AccountCreateOptions
            {
                Type = "express", // Or "standard" / "custom"
                Country = country,
                Email = email,
                Capabilities = new AccountCapabilitiesOptions
                {
                    CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
                }
            };

            var account = await accountService.CreateAsync(accountOptions);

            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PasswordHash = "ASDDDDDDDD",
                DateOfBirth = DateTime.UtcNow,
                ProfilePictureUrl = "https://example.com/profile.jpg", // Placeholder URL
                PhoneNumber = "1234567890", // Placeholder phone number
                AccountStatus = Account_Status.Active.ToString(),
                EmailVerified = false,
                PhoneVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Role = UserRole.Host.ToString(), // Set the role to Host

            };
            // Save the user in the database
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            // Save the Stripe account ID in the database
            var host = new Models.Host
            {
                HostId = user.Id,
                StripeAccountId = account.Id
            };

            _context.HostProfules.Add(host);
            await _context.SaveChangesAsync();

            // Create a Stripe account link for onboarding
            var accountLinkService = new AccountLinkService();
            var accountLinkOptions = new AccountLinkCreateOptions
            {
                Account = account.Id,
                RefreshUrl = "https://your-website.com/reauth", // URL to redirect to if the user needs to re-authenticate
                ReturnUrl = "https://your-website.com/return", // URL to redirect to after onboarding
                Type = "account_onboarding"
            };
            var accountLink = await accountLinkService.CreateAsync(accountLinkOptions);
            // Redirect the user to the account link URL for onboarding
            // You can return the URL to the client or handle it in your frontend
            // For example:
             return accountLink.Url;

            //return account.Id; 
        }

        public async Task<bool> CreatePayoutAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Property)
                .ThenInclude(p => p.Host)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null || booking.Status != "Confirmed")
                return false;

            var hostStripeAccountId = booking.Property.Host.StripeAccountId;
            if (string.IsNullOrEmpty(hostStripeAccountId))
                throw new InvalidOperationException("Host does not have a Stripe account linked.");

            var transferService = new TransferService();
            var transferOptions = new TransferCreateOptions
            {
                Amount = (long)(booking.TotalAmount * 100), // Convert to cents
                Currency = "usd",
                Destination = hostStripeAccountId,
                Description = $"Payout for booking #{bookingId}"
            };

            var transfer = await transferService.CreateAsync(transferOptions);

            // Record the payout in the database
            var payout = new BookingPayout
            {
                BookingId = bookingId,
                Amount = booking.TotalAmount,
                Status = "completed", // Replace 'transfer.Status' with a hardcoded or meaningful value
                CreatedAt = DateTime.UtcNow
            };

            _context.BookingPayouts.Add(payout);
            await _context.SaveChangesAsync();

            return true;
        }
    }

}
