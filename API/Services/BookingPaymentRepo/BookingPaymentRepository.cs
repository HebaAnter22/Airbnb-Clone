using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;
using WebApiDotNet.Repos;

namespace API.Services.BookingPaymentRepo
{
    public class BookingPaymentRepository : GenericRepository<BookingPayment>, IBookingPaymentRepository
    {
        private readonly AppDbContext _context;

        public BookingPaymentRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<BookingPayment> GetPaymentByTransactionIdAsync(string transactionId)
        {
            return await _context.BookingPayments.FirstOrDefaultAsync(p => p.TransactionId == transactionId);
        }

        public async Task<decimal> GetTotalPaymentsForBookingAsync(int bookingId)
        {
            return await _context.BookingPayments
                .Where(p => p.BookingId == bookingId && p.Status == "succeeded")
                .SumAsync(p => p.Amount);
        }

        public async Task<bool> UpdatePaymentStatusAsync(int paymentId, string newStatus)
        {
            var payment = await _context.BookingPayments.FindAsync(paymentId);
            if (payment == null)
                return false;

            payment.Status = newStatus;
            payment.UpdatedAt = DateTime.UtcNow;
            _context.BookingPayments.Update(payment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RefundPaymentAsync(int paymentId, decimal refundAmount)
        {
            var payment = await _context.BookingPayments.FindAsync(paymentId);
            if (payment == null || payment.RefundedAmount + refundAmount > payment.Amount)
                return false;

            payment.RefundedAmount += refundAmount;
            payment.Status = "refunded";
            payment.UpdatedAt = DateTime.UtcNow;
            _context.BookingPayments.Update(payment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ConfirmBookingPaymentAsync(int bookingId, string paymentIntentId)
        {
            var paymentIntentService = new PaymentIntentService();
            var paymentIntent = await paymentIntentService.GetAsync(paymentIntentId, new PaymentIntentGetOptions
            {
                Expand = new List<string> { "payment_method" }
            });

            if (paymentIntent.Status == "succeeded")
            {
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking != null)
                {
                    var payment = new BookingPayment
                    {
                        BookingId = booking.Id,
                        Amount = paymentIntent.Amount / 100m,
                        PaymentMethodType = paymentIntent.PaymentMethod?.Type ?? "unknown",
                        Status = paymentIntent.Status,
                        TransactionId = paymentIntent.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _context.BookingPayments.AddAsync(payment);
                    await _context.SaveChangesAsync();
                }
            }
        }

        //public async Task<bool> CreatePayoutAsync(int bookingId, decimal amount)
        //{
        //    var booking = await _context.Bookings
        //        .Include(b => b.Property)
        //        .FirstOrDefaultAsync(b => b.Id == bookingId);

        //    if (booking == null || booking.Status != "Confirmed")
        //        return false;

        //    var hostStripeAccountId = booking.Property.Host.StripeAccountId; 
        //    if (string.IsNullOrEmpty(hostStripeAccountId))
        //        throw new InvalidOperationException("Host does not have a Stripe account linked.");

        //    var transferService = new TransferService();
        //    var transferOptions = new TransferCreateOptions
        //    {
        //        Amount = (long)(amount * 100), // Convert to cents
        //        Currency = "usd",
        //        Destination = hostStripeAccountId,
        //        Description = $"Payout for booking #{bookingId}"
        //    };

        //    var transfer = await transferService.CreateAsync(transferOptions);
            
        //        // Record the payout in the database
        //        var payout = new BookingPayout
        //        {
        //            BookingId = bookingId,
        //            Amount = amount,
        //            Status = "paid",
        //            CreatedAt = DateTime.UtcNow
        //        };
        //        _context.BookingPayouts.Add(payout);
        //    await _context.SaveChangesAsync();

        //        return true;
            

            
        //}

    }

}
