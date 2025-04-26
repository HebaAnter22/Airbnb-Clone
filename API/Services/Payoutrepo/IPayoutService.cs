using API.Models;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace API.Services.Payoutrepo
{
    public interface IPayoutService
    {
        Task<HostPayout> RequestPayout(int hostId, decimal amount);
        Task<List<HostPayout>> GetHostPayouts(int hostId);
        Task<HostPayout> GetPayoutDetails(int payoutId);
        Task<decimal> GetHostBalance(int hostId);

        Task UpdatePayoutStatus(int payoutId, string status);
    }
}

