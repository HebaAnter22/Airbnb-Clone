using API.Data;
using API.Models;
using API.Services.BookingRepo;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using WebApiDotNet.Repos;

namespace API.Services.BookingPaymentRepo
{
    public class BookingPaymentRepository : GenericRepository<BookingPayment>, IBookingPaymentRepository
    {
        private readonly AppDbContext _context;
        private readonly IBookingRepository _bookingRepository;
        private readonly IConfiguration _configuration;

        public BookingPaymentRepository(AppDbContext context, IBookingRepository bookingRepository, IConfiguration configuration) : base(context)
        {
            _context = context;
            _bookingRepository = bookingRepository;
            _configuration = configuration;

            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<BookingPayment> GetPaymentByTransactionIdAsync(string transactionId)
        {
            return await _context.BookingPayments.FirstOrDefaultAsync(p => p.TransactionId == transactionId);
        }

        public async Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, int bookingId)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = "usd",
                PaymentMethodTypes = new List<string> { "card" },
                CaptureMethod = "automatic",
                Metadata = new Dictionary<string, string> { { "bookingId", bookingId.ToString() } }
            };

            var service = new PaymentIntentService();
            return await service.CreateAsync(options);
        }


        public async Task<Session> CreateCheckoutSessionAsync(decimal amount, int bookingId)
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "usd",
                    UnitAmount = (long)(amount * 100),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"Booking #{bookingId}",
                    },
                },
                Quantity = 1,
            },
        },
                Mode = "payment",
                SuccessUrl = "http://localhost:4200/payment-success?paymentIntentId={CHECKOUT_SESSION_ID}",
                CancelUrl = $"http://localhost:4200/payment-cancel?bookingId={bookingId}",
                Metadata = new Dictionary<string, string> { { "bookingId", bookingId.ToString() } },
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);
            return session;
        }

        public async Task InsertPaymentAsync(int bookingId, decimal amount, string transactionId, string status)
        {
            var payment = new BookingPayment
            {
                BookingId = bookingId,
                Amount = amount,
                PaymentMethodType = "Stripe",
                TransactionId = transactionId,
                Status = status, // Use the passed status (e.g., "succeeded")
                CreatedAt = DateTime.UtcNow
            };

            _context.BookingPayments.Add(payment);
            await _context.SaveChangesAsync();
        }
        public async Task ConfirmBookingPaymentAsync(int bookingId, string paymentIntentId)
        {
            // Check if payment already exists to avoid duplicates
            var existingPayment = await _context.BookingPayments
                .FirstOrDefaultAsync(p => p.TransactionId == paymentIntentId);

            if (existingPayment == null)
            {
                // Fetch the PaymentIntent to get the amount
                var paymentIntentService = new PaymentIntentService();
                var paymentIntent = await paymentIntentService.GetAsync(paymentIntentId);

                // Insert the payment record
                await InsertPaymentAsync(
                    bookingId,
                    paymentIntent.Amount / 100m, // Convert from cents to dollars
                    paymentIntentId,
                    paymentIntent.Status
                );
            }
            else
            {
                // Update the status if the payment already exists
                existingPayment.Status = "succeeded";
                await _context.SaveChangesAsync();
            }
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

        //public async Task ConfirmBookingPaymentAsync(int bookingId, string paymentIntentId)
        //{
        //    var paymentIntentService = new PaymentIntentService();
        //    var paymentIntent = await paymentIntentService.GetAsync(paymentIntentId, new PaymentIntentGetOptions
        //    {
        //        Expand = new List<string> { "payment_method" }
        //    });

        //    if (paymentIntent.Status == "succeeded")
        //    {
        //        var booking = await _context.Bookings.FindAsync(bookingId);
        //        if (booking != null)
        //        {
        //            var payment = new BookingPayment
        //            {
        //                BookingId = booking.Id,
        //                Amount = paymentIntent.Amount / 100m,
        //                PaymentMethodType = paymentIntent.PaymentMethod?.Type ?? "unknown",
        //                Status = paymentIntent.Status,
        //                TransactionId = paymentIntent.Id,
        //                CreatedAt = DateTime.UtcNow
        //            };

        //            await _context.BookingPayments.AddAsync(payment);
        //            await _context.SaveChangesAsync();
        //        }
        //    }
        //}

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
