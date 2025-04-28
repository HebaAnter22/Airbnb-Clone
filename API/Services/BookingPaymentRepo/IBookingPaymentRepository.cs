using API.Models;
using WebApiDotNet.Repos;
using Stripe.Checkout;
using Stripe;

namespace API.Services.BookingPaymentRepo
{
    public interface IBookingPaymentRepository : IGenericRepository<BookingPayment>
    {
        Task<BookingPayment> GetPaymentByTransactionIdAsync(string transactionId);
        Task<decimal> GetTotalPaymentsForBookingAsync(int bookingId);
        Task<bool> UpdatePaymentStatusAsync(int paymentId, string newStatus);
        Task<bool> RefundPaymentAsync(int paymentId, decimal refundAmount, string reason = "requested_by_customer");
        Task ConfirmBookingPaymentAsync(int bookingId, string paymentIntentId);
        Task UpdateHostEarningsAsync(int bookingId, decimal amount);
        Task UpdateHostEarningsDirectlyAsync(int hostId, decimal amount);
        //Task<bool> CreatePayoutAsync(int bookingId, decimal amount);
        Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, int bookingId);

        Task<Session> CreateCheckoutSessionAsync(decimal amount, int bookingId);
        Task InsertPaymentAsync(int bookingId, decimal amount, string transactionId, string status);
    }

}
