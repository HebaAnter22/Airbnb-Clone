using API.Models;
using WebApiDotNet.Repos;

namespace API.Services.BookingPaymentRepo
{
    public interface IBookingPaymentRepository : IGenericRepository<BookingPayment>
    {
        Task<BookingPayment> GetPaymentByTransactionIdAsync(string transactionId);
        Task<decimal> GetTotalPaymentsForBookingAsync(int bookingId);
        Task<bool> UpdatePaymentStatusAsync(int paymentId, string newStatus);
        Task<bool> RefundPaymentAsync(int paymentId, decimal refundAmount);
        Task ConfirmBookingPaymentAsync(int bookingId, string paymentIntentId);
        //Task<bool> CreatePayoutAsync(int bookingId, decimal amount);
    }

}
