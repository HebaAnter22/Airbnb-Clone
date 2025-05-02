using API.Data;
using API.Models;
using API.Services.BookingRepo;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using WebApiDotNet.Repos;
using System.Transactions;

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
            // Use a completely separate database operation for each step
            try
            {
                // Step 1: Check if payment already exists (outside of any transaction)
                var existingPayment = await _context.BookingPayments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.TransactionId == paymentIntentId);

                // If payment exists and is already succeeded, just return success
                if (existingPayment != null && existingPayment.Status == "succeeded")
                {
                    _logger.LogInformation("Payment with ID {PaymentId} already confirmed, skipping processing", existingPayment.Id);
                    return;
                }
                    
                // Get Stripe payment information
                var paymentIntentService = new PaymentIntentService();
                var paymentIntent = await paymentIntentService.GetAsync(paymentIntentId);
                
                // Calculate payment amount
                decimal paymentAmount = paymentIntent.Amount / 100m;
                    
                // Step 2: If payment doesn't exist, create it
                if (existingPayment == null)
                {
                    // Check if the booking exists
                    var booking = await _context.Bookings
                        .AsNoTracking()
                        .Include(b => b.Property)
                        .FirstOrDefaultAsync(b => b.Id == bookingId);
                        
                    if (booking == null)
                    {
                        _logger.LogError("Booking {BookingId} not found", bookingId);
                        throw new Exception($"Booking {bookingId} not found");
                    }
                        
                    // Create new payment record
                    var payment = new BookingPayment
                    {
                        BookingId = bookingId,
                        Amount = paymentAmount,
                        PaymentMethodType = "Stripe",
                        TransactionId = paymentIntentId,
                        Status = paymentIntent.Status,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    // Create a new context using the DbContextOptions
                    using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                    {
                        _context.BookingPayments.Add(payment);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Payment record created for booking {BookingId}", bookingId);
                        scope.Complete();
                    }

                    // Step 3: Update the booking status in a separate operation
                    using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                    {
                        var bookingToUpdate = await _context.Bookings.FindAsync(bookingId);
                        if (bookingToUpdate != null && bookingToUpdate.Status != "Confirmed")
                        {
                            bookingToUpdate.Status = "Confirmed";
                            bookingToUpdate.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Booking {BookingId} status updated to Confirmed", bookingId);
                        }
                        scope.Complete();
                    }
                }
                // Step 4: If payment exists but isn't successful, update it
                else if (existingPayment.Status != "succeeded")
                {
                    using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                    {
                        var paymentToUpdate = await _context.BookingPayments.FindAsync(existingPayment.Id);
                        if (paymentToUpdate != null)
                        {
                            paymentToUpdate.Status = "succeeded";
                            paymentToUpdate.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Payment {PaymentId} status updated to succeeded", existingPayment.Id);
                        }
                        scope.Complete();
                    }
                }
                
                // Step 5: Update host earnings if needed (in a separate transaction)
                if (existingPayment == null || existingPayment.Status != "succeeded")
                {
                    // Get host ID from booking
                    int? hostId = null;
                    var booking = await _context.Bookings
                        .AsNoTracking()
                        .Include(b => b.Property)
                        .ThenInclude(p => p.Host)
                        .FirstOrDefaultAsync(b => b.Id == bookingId);
                        
                    if (booking?.Property?.Host != null)
                    {
                        hostId = booking.Property.Host.HostId;
                        
                        if (hostId.HasValue)
                        {
                            try
                            {
                                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                                {
                                    var host = await _context.HostProfules.FindAsync(hostId.Value);
                                    if (host != null)
                                    {
                                        // Update host earnings (85% of payment amount)
                                        decimal hostAmount = paymentAmount * 0.85m;
                                        host.TotalEarnings += hostAmount;
                                        host.AvailableBalance += hostAmount;
                                        await _context.SaveChangesAsync();
                                        _logger.LogInformation("Host {HostId} earnings updated: added {Amount}", hostId.Value, hostAmount);
                                    }
                                    scope.Complete();
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error updating host earnings for host {HostId}", hostId.Value);
                                // Don't rethrow - payment confirmation was successful
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConfirmBookingPaymentAsync for booking {BookingId}", bookingId);
                throw;
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
            _logger.LogInformation("Starting RefundPaymentAsync for paymentId={PaymentId}, refundAmount={RefundAmount}, reason={Reason}", 
                paymentId, refundAmount, reason);
                
            try
            {
                var payment = await _context.BookingPayments.FindAsync(paymentId);
                if (payment == null)
                {
                    _logger.LogWarning("RefundPaymentAsync: Payment with ID {PaymentId} not found", paymentId);
                    return false;
                }

                _logger.LogInformation("Found payment: ID={PaymentId}, BookingId={BookingId}, Amount={Amount}, Status={Status}, RefundedAmount={RefundedAmount}", 
                    payment.Id, payment.BookingId, payment.Amount, payment.Status, payment.RefundedAmount);

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

                // Validate reason to ensure it's one of the valid Stripe reasons
                // Valid reasons are: 'duplicate', 'fraudulent', or 'requested_by_customer'
                if (reason != "duplicate" && reason != "fraudulent" && reason != "requested_by_customer")
                {
                    _logger.LogWarning("RefundPaymentAsync: Invalid reason '{Reason}'. Using default 'requested_by_customer'", reason);
                    reason = "requested_by_customer";
                }

                // Convert refund amount to cents for Stripe
                long refundAmountCents = (long)(refundAmount * 100);
                _logger.LogInformation("Calling Stripe to process refund: PaymentIntent={PaymentIntent}, Amount={Amount} cents, Reason={Reason}",
                    paymentIntentId, refundAmountCents, reason);

                // Process the refund through Stripe
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = paymentIntentId,
                    Amount = refundAmountCents,
                    Reason = reason
                };

                var refundService = new RefundService();
                var refund = await refundService.CreateAsync(refundOptions);
                _logger.LogInformation("Stripe refund response: ID={RefundId}, Status={Status}, Amount={Amount}", 
                    refund.Id, refund.Status, refund.Amount);

                if (refund.Status == "succeeded")
                {
                    _logger.LogInformation("Stripe refund succeeded. Updating database records.");
                    
                    try
                    {
                        // IMPORTANT: Do not use transactions here as they might be causing issues
                        // Update payment record directly
                        
                        // Fetch a fresh instance of the payment
                        var paymentToUpdate = await _context.BookingPayments.FindAsync(paymentId);
                        if (paymentToUpdate == null)
                        {
                            _logger.LogError("Payment {PaymentId} not found when updating after successful Stripe refund", paymentId);
                            return false;
                        }
                        
                        _logger.LogInformation("Before update: Payment ID={PaymentId}, RefundedAmount={RefundedAmount}, Status={Status}", 
                            paymentToUpdate.Id, paymentToUpdate.RefundedAmount, paymentToUpdate.Status);
                        
                        // Update payment record
                        paymentToUpdate.RefundedAmount += refundAmount;
                        
                        // Set status based on whether this is a full or partial refund
                        if (paymentToUpdate.RefundedAmount >= paymentToUpdate.Amount)
                        {
                            paymentToUpdate.Status = "refunded";
                        }
                        else
                        {
                            paymentToUpdate.Status = "partially_refunded";
                        }
                        
                        paymentToUpdate.UpdatedAt = DateTime.UtcNow;
                        _context.BookingPayments.Update(paymentToUpdate);
                        
                        // Save payment changes immediately
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Payment record updated: ID={PaymentId}, RefundedAmount={RefundedAmount}, Status={Status}", 
                            paymentToUpdate.Id, paymentToUpdate.RefundedAmount, paymentToUpdate.Status);
                        
                        // Get booking for status update
                        var booking = await _context.Bookings
                            .Include(b => b.Property)
                            .ThenInclude(p => p.Host)
                            .FirstOrDefaultAsync(b => b.Id == paymentToUpdate.BookingId);
                        
                        if (booking == null)
                        {
                            _logger.LogWarning("Booking {BookingId} not found when processing refund", paymentToUpdate.BookingId);
                            // Continue with host update even if booking is not found
                        }
                        else
                        {
                            // Update booking status for full refunds
                            if (paymentToUpdate.RefundedAmount >= paymentToUpdate.Amount && booking.Status != "Cancelled")
                            {
                                booking.Status = "Cancelled";
                                booking.UpdatedAt = DateTime.UtcNow;
                                _context.Bookings.Update(booking);
                                await _context.SaveChangesAsync();
                                _logger.LogInformation("Updated booking {BookingId} status to Cancelled due to full refund", booking.Id);
                            }
                        }
                        
                        // Update host earnings in a separate step
                        if (booking?.Property?.Host != null)
                        {
                            var hostId = booking.Property.Host.HostId;
                            var host = await _context.HostProfules.FindAsync(hostId);
                            
                            if (host != null)
                            {
                                _logger.LogInformation("Updating host earnings: HostId={HostId}, CurrentEarnings={CurrentEarnings}, CurrentBalance={CurrentBalance}", 
                                    host.HostId, host.TotalEarnings, host.AvailableBalance);
                                
                                // Determine the host's commission rate (85% to host)
                                decimal hostCommissionRate = 0.85m;
                                decimal hostRefundAmount = refundAmount * hostCommissionRate;
                                
                                // Deduct from host's available balance and total earnings
                                host.AvailableBalance -= hostRefundAmount;
                                host.TotalEarnings -= hostRefundAmount;
                                
                                _context.HostProfules.Update(host);
                                await _context.SaveChangesAsync();
                                
                                _logger.LogInformation(
                                    "Deducted {Amount} from Host {HostId} earnings. New Balance: {Balance}, New Total: {Total}",
                                    hostRefundAmount, host.HostId, host.AvailableBalance, host.TotalEarnings);
                            }
                            else
                            {
                                _logger.LogWarning("Host profile not found for HostId {HostId} when processing refund", hostId);
                            }
                        }
                    
                        _logger.LogInformation("Successfully processed refund of {RefundAmount} for payment {PaymentId}", 
                            refundAmount, paymentId);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Database error when updating payment and host data after successful Stripe refund: {Message}", ex.Message);
                        // The refund was processed in Stripe but we failed to update our database
                        // This requires manual intervention
                        return false;
                    }
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
                _logger.LogError(ex, "Error processing refund for payment {PaymentId}: {Message}", 
                    paymentId, ex.Message);
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
