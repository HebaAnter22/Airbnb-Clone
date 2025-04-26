using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Payoutrepo
{
    public class PayoutService : IPayoutService
    {
        private readonly AppDbContext _context;

        public PayoutService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> GetHostBalance(int hostId)
        {
            var host = await _context.Hosts.FindAsync(hostId);
            if (host == null)
                throw new Exception("Host not found");

            return host.AvailableBalance;
        }

        public async Task<HostPayout> RequestPayout(int hostId, decimal amount)
        {
            try 
            {
                var host = await _context.Hosts
                    .Include(h => h.Payouts)
                    .FirstOrDefaultAsync(h => h.HostId == hostId);

                if (host == null)
                    throw new Exception("Host not found");

                if (host.AvailableBalance < amount)
                    throw new Exception("Insufficient balance");

                if (amount <= 0)
                    throw new Exception("Amount must be greater than zero");

                var payout = new HostPayout
                {
                    HostId = hostId,
                    Amount = amount,
                    Status = "Pending",
                    PayoutMethod = "Bank Transfer",
                    CreatedAt = DateTime.UtcNow,
                    Notes = "Payout request submitted"
                };

                host.AvailableBalance -= amount;
                await _context.HostPayouts.AddAsync(payout);
                await _context.SaveChangesAsync();

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
                    var host = await _context.Hosts.FindAsync(payout.HostId);
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
    }
} 