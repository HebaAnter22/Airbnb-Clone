using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Microsoft.Extensions.Configuration;

namespace API.Services.Payoutrepo
{
    public class PayoutService : IPayoutService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _stripeSecretKey;

        public PayoutService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _stripeSecretKey = _configuration["Stripe:SecretKey"];
            StripeConfiguration.ApiKey = _stripeSecretKey;
        }

        public async Task<decimal> GetHostBalance(int hostId)
        {
            var host = await _context.HostProfules.FindAsync(hostId);
            if (host == null)
                throw new Exception("Host not found");

            return host.AvailableBalance;
        }

        public async Task<HostPayout> RequestPayout(int hostId, decimal amount)
        {
            try 
            {
                var host = await _context.HostProfules
                    .Include(h => h.Payouts)
                    .FirstOrDefaultAsync(h => h.HostId == hostId);

                if (host == null)
                    throw new Exception("Host not found");

                if (host.AvailableBalance < amount)
                    throw new Exception("Insufficient balance");

                if (amount <= 0)
                    throw new Exception("Amount must be greater than zero");
                
                // Check if host has a Stripe account
                if (string.IsNullOrEmpty(host.StripeAccountId))
                    throw new Exception("You must set up a payout method first");

                var payoutMethod = !string.IsNullOrEmpty(host.DefaultPayoutMethod) 
                    ? host.DefaultPayoutMethod 
                    : "Stripe";

                var payout = new HostPayout
                {
                    HostId = hostId,
                    Amount = amount,
                    Status = "Pending",
                    PayoutMethod = payoutMethod,
                    CreatedAt = DateTime.UtcNow,
                    Notes = "Payout request submitted"
                };

                host.AvailableBalance -= amount;
                await _context.HostPayouts.AddAsync(payout);
                await _context.SaveChangesAsync();

                // For testing purposes, we'll immediately attempt to process the payout via Stripe
                if (payoutMethod == "Stripe")
                {
                    try 
                    {
                        string transactionId = await CreateStripePayoutToHost(hostId, payout.Id);
                        payout.TransactionId = transactionId;
                        payout.Status = "Processing";
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        payout.Notes += $" | Stripe payout failed: {ex.Message}";
                        await _context.SaveChangesAsync();
                    }
                }

                return payout;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to process payout request: {ex.Message}");
            }
        }

        public async Task<List<HostPayout>> GetHostPayouts(int hostId)
        {
            try
            {
                return await _context.HostPayouts
                    .Where(p => p.HostId == hostId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get host payouts: {ex.Message}");
            }
        }

        public async Task<HostPayout> GetPayoutDetails(int payoutId)
        {
            try
            {
                var payout = await _context.HostPayouts
                    .FirstOrDefaultAsync(p => p.Id == payoutId);

                if (payout == null)
                    throw new Exception("Payout not found");

                return payout;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get payout details: {ex.Message}");
            }
        }

        public async Task UpdatePayoutStatus(int payoutId, string status)
        {
            try
            {
                var payout = await _context.HostPayouts.FindAsync(payoutId);
                if (payout == null)
                    throw new Exception("Payout not found");

                // Validate status
                var validStatuses = new[] { "Pending", "Completed", "Failed", "Processing" };
                if (!validStatuses.Contains(status))
                    throw new Exception("Invalid status. Valid statuses are: Pending, Completed, Failed, Processing");

                payout.Status = status;
                if (status == "Completed")
                {
                    payout.ProcessedAt = DateTime.UtcNow;
                }
                else if (status == "Failed")
                {
                    // Refund the amount back to host's available balance
                    var host = await _context.HostProfules.FindAsync(payout.HostId);
                    if (host != null)
                    {
                        host.AvailableBalance += payout.Amount;
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update payout status: {ex.Message}");
            }
        }

        // Stripe Connect implementations
        public async Task<string> CreateStripeConnectAccount(int hostId)
        {
            var host = await _context.HostProfules
                .Include(h => h.User)
                .FirstOrDefaultAsync(h => h.HostId == hostId);

            if (host == null)
                throw new Exception("Host not found");

            if (!string.IsNullOrEmpty(host.StripeAccountId))
                return host.StripeAccountId; // Account already exists

            try
            {
                // Create a test Connect account
                var options = new AccountCreateOptions
                {
                    Type = "express", // Use Express for simplest integration
                    Capabilities = new AccountCapabilitiesOptions
                    {
                        Transfers = new AccountCapabilitiesTransfersOptions
                        {
                            Requested = true,
                        },
                    },
                    BusinessType = "individual",
                    BusinessProfile = new AccountBusinessProfileOptions
                    {
                        Mcc = "5499", // Miscellaneous Food Stores
                        ProductDescription = "Vacation rental host on Airbnb clone",
                    },
                    // For testing, we don't need real data
                    Email = host.User?.Email ?? $"test-host-{hostId}@example.com",
                    Metadata = new Dictionary<string, string>
                    {
                        { "HostId", hostId.ToString() }
                    }
                };

                var service = new AccountService();
                var account = await service.CreateAsync(options);

                // Save the account ID to the host
                host.StripeAccountId = account.Id;
                host.DefaultPayoutMethod = "Stripe";
                await _context.SaveChangesAsync();

                return account.Id;
            }
            catch (StripeException ex)
            {
                throw new Exception($"Stripe error: {ex.Message}");
            }
        }

        public async Task<string> GetStripeConnectAccountLink(int hostId)
        {
            var host = await _context.HostProfules.FindAsync(hostId);
            if (host == null)
                throw new Exception("Host not found");

            if (string.IsNullOrEmpty(host.StripeAccountId))
            {
                // Create an account first
                await CreateStripeConnectAccount(hostId);
                host = await _context.HostProfules.FindAsync(hostId); // Refresh data
            }

            try
            {
                // Create an account link for the user to onboard
                var frontendUrl = _configuration["FrontendUrl"];
                var options = new AccountLinkCreateOptions
                {
                    Account = host.StripeAccountId,
                    RefreshUrl = $"{frontendUrl}/host/dashboard?refresh=true",
                    ReturnUrl = $"{frontendUrl}/host/dashboard?onboarding=complete",
                    Type = "account_onboarding",
                };

                var service = new AccountLinkService();
                var accountLink = await service.CreateAsync(options);

                return accountLink.Url;
            }
            catch (StripeException ex)
            {
                throw new Exception($"Stripe error: {ex.Message}");
            }
        }

        public async Task<string> CreateStripePayoutToHost(int hostId, int payoutId)
        {
            var host = await _context.HostProfules.FindAsync(hostId);
            if (host == null)
                throw new Exception("Host not found");

            if (string.IsNullOrEmpty(host.StripeAccountId))
                throw new Exception("Host doesn't have a Stripe account");

            var payout = await _context.HostPayouts.FindAsync(payoutId);
            if (payout == null)
                throw new Exception("Payout not found");

            try
            {
                // For test mode, we'll create a simple transfer to the connected account
                var amountInCents = Convert.ToInt64(payout.Amount * 100);
                
                var options = new TransferCreateOptions
                {
                    Amount = amountInCents,
                    Currency = "usd",
                    Destination = host.StripeAccountId,
                    Description = $"Payout #{payoutId} for Host #{hostId}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "HostId", hostId.ToString() },
                        { "PayoutId", payoutId.ToString() }
                    }
                };

                var service = new TransferService();
                var transfer = await service.CreateAsync(options);

                return transfer.Id;
            }
            catch (StripeException ex)
            {
                throw new Exception($"Stripe error: {ex.Message}");
            }
        }

        public async Task<bool> CheckStripeAccountStatus(int hostId)
        {
            var host = await _context.HostProfules.FindAsync(hostId);
            if (host == null)
                throw new Exception("Host not found");

            if (string.IsNullOrEmpty(host.StripeAccountId))
                return false;

            try
            {
                var service = new AccountService();
                var account = await service.GetAsync(host.StripeAccountId);

                // In test mode, we'll consider any account with charges_enabled as ready
                return account.ChargesEnabled;
            }
            catch (StripeException)
            {
                return false;
            }
        }
    }
} 