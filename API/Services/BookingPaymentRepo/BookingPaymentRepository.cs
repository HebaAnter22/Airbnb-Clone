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
        private readonly ILogger<BookingPaymentRepository> _logger;


        public BookingPaymentRepository(
            AppDbContext context, 
            IBookingRepository bookingRepository, 
            IConfiguration configuration,
            ILogger<BookingPaymentRepository> logger) : base(context)
        {
            _context = context;
            _bookingRepository = bookingRepository;
            _configuration = configuration;
            _logger = logger;

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
            var booking = _bookingRepository.UpdateBookingStatusAsync(bookingId,  "Confirmed");


            _context.BookingPayments.Add(payment);
            await _context.SaveChangesAsync();
        }
        
        public async Task ConfirmBookingPaymentAsync(int bookingId, string paymentIntentId)
        {
            // Check if payment already exists to avoid duplicates
            var existingPayment = await _context.BookingPayments
                .FirstOrDefaultAsync(p => p.TransactionId == paymentIntentId);

            decimal paymentAmount = 0;
            bool isNewPayment = false;
            int? hostId = null;

            if (existingPayment == null)
            {
                // Fetch the PaymentIntent to get the amount
                var paymentIntentService = new PaymentIntentService();
                var paymentIntent = await paymentIntentService.GetAsync(paymentIntentId);

                paymentAmount = paymentIntent.Amount / 100m; // Convert from cents to dollars
                isNewPayment = true;

                // First, get the host ID to ensure we can update earnings even if the main method fails
                var booking = await _context.Bookings
                    .Include(b => b.Property)
                    .ThenInclude(p => p.Host)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking?.Property?.Host != null)
                {
                    hostId = booking.Property.Host.HostId;
                }

                // Insert the payment record
                await InsertPaymentAsync(
                    bookingId,
                    paymentAmount,
                    paymentIntentId,
                    paymentIntent.Status
                );
            }
            else
            {
                // Update the status if the payment already exists but wasn't successful yet
                if (existingPayment.Status != "succeeded")
                {
                    existingPayment.Status = "succeeded";
                    paymentAmount = existingPayment.Amount;
                    isNewPayment = true;

                    // Get host ID
                    var booking = await _context.Bookings
                        .Include(b => b.Property)
                        .ThenInclude(p => p.Host)
                        .FirstOrDefaultAsync(b => b.Id == bookingId);

                    if (booking?.Property?.Host != null)
                    {
                        hostId = booking.Property.Host.HostId;
                    }

                    await _context.SaveChangesAsync();
                }
            }

            // Only update host earnings for new successful payments
            if (isNewPayment && paymentAmount > 0)
            {
                try
                {
                    // Try the standard method first
                    await UpdateHostEarningsAsync(bookingId, paymentAmount);
                    await _bookingRepository.UpdateBookingStatusAsync(bookingId, "Confirmed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating host earnings through booking. Trying direct method...");
                    
                    // If there's an error, try the direct method if we have the host ID
                    if (hostId.HasValue)
                    {
                        await UpdateHostEarningsDirectlyAsync(hostId.Value, paymentAmount);
                    }
                    else
                    {
                        _logger.LogError("Cannot update host earnings: no host ID available");
                    }
                }
            }
        }

        public async Task UpdateHostEarningsAsync(int bookingId, decimal amount)
        {
            try
            {
                // Get the booking with property and host information
                var booking = await _context.Bookings
                    .Include(b => b.Property)
                    .ThenInclude(p => p.Host)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    _logger.LogWarning("UpdateHostEarningsAsync: Booking {BookingId} not found", bookingId);
                    return;
                }

                if (booking.Property?.Host == null)
                {
                    _logger.LogWarning("UpdateHostEarningsAsync: Host not found for booking {BookingId}", bookingId);
                    return;
                }

                // Get the host directly to ensure proper tracking
                var hostId = booking.Property.Host.HostId;
                var host = await _context.HostProfules.FindAsync(hostId);
                
                if (host == null)
                {
                    _logger.LogWarning("UpdateHostEarningsAsync: Host with ID {HostId} not found in direct query", hostId);
                    return;
                }

                // Determine the host's commission rate (usually hosts pay a percentage to the platform)
                // For simplicity, we'll assume 85% goes to host (15% platform fee)
                decimal hostCommissionRate = 0.85m;
                decimal hostAmount = amount * hostCommissionRate;

                // Log the values before the update for debugging
                _logger.LogInformation(
                    "Before update - Host {HostId}: Current TotalEarnings: {TotalEarnings}, Current AvailableBalance: {AvailableBalance}",
                    host.HostId, host.TotalEarnings, host.AvailableBalance);

                // Update the host's total earnings and available balance
                host.TotalEarnings += hostAmount;
                host.AvailableBalance += hostAmount;

                // Mark entity as modified
                _context.HostProfules.Update(host);

                // Save changes to database
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Updated Host {HostId} earnings: Added {Amount} to TotalEarnings and AvailableBalance. New Total: {Total}, New Balance: {Balance}",
                    host.HostId, hostAmount, host.TotalEarnings, host.AvailableBalance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating host earnings for booking {BookingId}", bookingId);
                throw;
            }
        }

        public async Task UpdateHostEarningsDirectlyAsync(int hostId, decimal amount)
        {
            try
            {
                // Get the host directly from the database
                var host = await _context.HostProfules.FindAsync(hostId);
                
                if (host == null)
                {
                    _logger.LogWarning("UpdateHostEarningsDirectlyAsync: Host with ID {HostId} not found", hostId);
                    return;
                }

                // Determine the host's commission rate
                decimal hostCommissionRate = 0.85m;
                decimal hostAmount = amount * hostCommissionRate;

                // Log before values
                _logger.LogInformation(
                    "Direct update - Before values - Host {HostId}: TotalEarnings: {TotalEarnings}, AvailableBalance: {AvailableBalance}",
                    host.HostId, host.TotalEarnings, host.AvailableBalance);

                // Update using direct SQL for guaranteed update
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE HostProfules SET TotalEarnings = TotalEarnings + {0}, AvailableBalance = AvailableBalance + {0} WHERE HostId = {1}",
                    hostAmount, hostId);

                // Refresh the host entity from database to get updated values
                await _context.Entry(host).ReloadAsync();

                _logger.LogInformation(
                    "Direct update - After values - Host {HostId}: TotalEarnings: {TotalEarnings}, AvailableBalance: {AvailableBalance}",
                    host.HostId, host.TotalEarnings, host.AvailableBalance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error directly updating host earnings for host {HostId}", hostId);
                throw;
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

        public async Task<bool> RefundPaymentAsync(int paymentId, decimal refundAmount, string reason = "requested_by_customer")
        {
            try
            {
                var payment = await _context.BookingPayments.FindAsync(paymentId);
                if (payment == null)
                {
                    _logger.LogWarning("RefundPaymentAsync: Payment with ID {PaymentId} not found", paymentId);
                    return false;
                }

                // Validate refund amount
                if (payment.RefundedAmount + refundAmount > payment.Amount)
                {
                    _logger.LogWarning("RefundPaymentAsync: Refund amount {RefundAmount} exceeds available amount for payment {PaymentId}", 
                        refundAmount, paymentId);
                    return false;
                }

                // Get the Stripe payment intent ID (transaction ID)
                string paymentIntentId = payment.TransactionId;
                if (string.IsNullOrEmpty(paymentIntentId))
                {
                    _logger.LogWarning("RefundPaymentAsync: No transaction ID found for payment {PaymentId}", paymentId);
                    return false;
                }

                // Convert refund amount to cents for Stripe
                long refundAmountCents = (long)(refundAmount * 100);

                // Process the refund through Stripe
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = paymentIntentId,
                    Amount = refundAmountCents,
                    Reason = reason
                };

                var refundService = new RefundService();
                var refund = await refundService.CreateAsync(refundOptions);

                if (refund.Status == "succeeded")
                {
                    // Update our database record
                    payment.RefundedAmount += refundAmount;
                    
                    // Set status based on whether this is a full or partial refund
                    if (payment.RefundedAmount >= payment.Amount)
                    {
                        payment.Status = "refunded";
                    }
                    else
                    {
                        payment.Status = "partially_refunded";
                    }
                    
                    payment.UpdatedAt = DateTime.UtcNow;
                    _context.BookingPayments.Update(payment);
                    
                    // If booking exists, update booking status for full refunds
                    if (payment.RefundedAmount >= payment.Amount)
                    {
                        var booking = await _context.Bookings.FindAsync(payment.BookingId);
                        if (booking != null)
                        {
                            await _bookingRepository.UpdateBookingStatusAsync(payment.BookingId, "Cancelled");
                        }
                    }

                    // Update host earnings (deduct the refunded amount)
                    try
                    {
                        // Deduct from host earnings (85% of the refund amount)
                        decimal hostCommissionRate = 0.85m;
                        decimal hostRefundAmount = refundAmount * hostCommissionRate;
                        
                        var booking = await _context.Bookings
                            .Include(b => b.Property)
                            .ThenInclude(p => p.Host)
                            .FirstOrDefaultAsync(b => b.Id == payment.BookingId);

                        if (booking?.Property?.Host != null)
                        {
                            var hostId = booking.Property.Host.HostId;
                            var host = await _context.HostProfules.FindAsync(hostId);
                            
                            if (host != null)
                            {
                                // Deduct from host's available balance and total earnings
                                host.AvailableBalance -= hostRefundAmount;
                                host.TotalEarnings -= hostRefundAmount;
                                
                                _context.HostProfules.Update(host);
                                
                                _logger.LogInformation(
                                    "Deducted {Amount} from Host {HostId} earnings due to refund. New Balance: {Balance}, New Total: {Total}",
                                    hostRefundAmount, host.HostId, host.AvailableBalance, host.TotalEarnings);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but continue - we've already processed the refund in Stripe
                        _logger.LogError(ex, "Error updating host earnings for refund on payment {PaymentId}", paymentId);
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Successfully processed refund of {RefundAmount} for payment {PaymentId}", 
                        refundAmount, paymentId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("RefundPaymentAsync: Stripe refund failed with status {Status} for payment {PaymentId}", 
                        refund.Status, paymentId);
                    return false;
                }
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error when processing refund for payment {PaymentId}: {Message}", 
                    paymentId, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for payment {PaymentId}", paymentId);
                return false;
            }
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
