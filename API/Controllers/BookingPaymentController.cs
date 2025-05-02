using AirBnb.BL.Dtos.BookingDtos;
using API.Data;
using API.Models;
using API.DTOs.BookingPayment;
using API.Services.BookingPaymentRepo;
using API.Services.BookingRepo;
using API.Services.NotificationRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Microsoft.Extensions.Logging;
using API.Services.PropertyAvailabilityRepo;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingPaymentController : ControllerBase
    {
        private readonly IBookingPaymentRepository _bookingPaymentRepo;
        private readonly AppDbContext _context;
        private readonly ILogger<BookingPaymentController> _logger;
        private readonly IBookingRepository _bookingRepo;
        private readonly INotificationRepository _notificationRepo;
        private readonly IPropertyAvailabilityRepository _propertyAvailabilityRepo;

        public BookingPaymentController(
            IBookingPaymentRepository bookingPaymentRepository,
            AppDbContext context,
            ILogger<BookingPaymentController> logger, IBookingRepository bookingRepo, INotificationRepository notificationRepository, IPropertyAvailabilityRepository propertyAvailabilityRepository)
        {
            _bookingPaymentRepo = bookingPaymentRepository;
            _context = context;
            _logger = logger;
            _bookingRepo = bookingRepo;
            _notificationRepo = notificationRepository;
            _propertyAvailabilityRepo = propertyAvailabilityRepository;
        }

        [HttpPost("create-payment-intent")]
        [Authorize]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentDto createPaymentIntentDto)
        {
            var paymentIntent = await _bookingPaymentRepo.CreatePaymentIntentAsync(createPaymentIntentDto.Amount, createPaymentIntentDto.BookingId);
            return Ok(new { paymentIntentId = paymentIntent.Id, clientSecret = paymentIntent.ClientSecret });
        }

        [HttpPost("create-checkout-session")]
        [Authorize]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreatePaymentIntentDto createPaymentIntentDto)
        {
            try
            {
                var session = await _bookingPaymentRepo.CreateCheckoutSessionAsync(
                    createPaymentIntentDto.Amount,
                    createPaymentIntentDto.BookingId
                );
                return Ok(new { sessionId = session.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Failed to create checkout session: {ex.Message}" });
            }
        }

        [HttpGet("GetPaymentByTransactionId/{transactionId}")]
        public async Task<IActionResult> GetPaymentByTransactionId(string transactionId)
        {
            var payment = await _bookingPaymentRepo.GetPaymentByTransactionIdAsync(transactionId);
            if (payment == null)
            {
                return NotFound("Payment not found");
            }
            return Ok(payment);
        }

        [HttpPut("{paymentId}/status")]
        public async Task<IActionResult> UpdatePaymentStatus(int paymentId, [FromBody] string newStatus)
        {
            var success = await _bookingPaymentRepo.UpdatePaymentStatusAsync(paymentId, newStatus);
            if (!success)
                return NotFound("Payment not found.");
            return NoContent();
        }

        [HttpPost("confirm-booking-payment")]
        [Authorize]
        public async Task<IActionResult> ConfirmBookingPayment([FromBody] ConfirmPaymentDto confirmPaymentDto)
        {
            if (confirmPaymentDto == null || string.IsNullOrEmpty(confirmPaymentDto.PaymentIntentId))
            {
                return BadRequest(new { Message = "Invalid payment data. Payment intent ID is required." });
            }

            _logger.LogInformation("Confirming payment for booking {BookingId} with intent {PaymentIntentId}",
                confirmPaymentDto.BookingId, confirmPaymentDto.PaymentIntentId);

            // Check if booking exists (read only)
            var booking = await _context.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == confirmPaymentDto.BookingId);
                
            if (booking == null)
            {
                _logger.LogWarning("Booking {BookingId} not found for payment confirmation", confirmPaymentDto.BookingId);
                return NotFound(new { Message = $"Booking {confirmPaymentDto.BookingId} not found" });
            }

            // Exponential backoff retry logic
            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var paymentIntentService = new PaymentIntentService();
                    var paymentIntent = await paymentIntentService.GetAsync(confirmPaymentDto.PaymentIntentId);
                    _logger.LogInformation("Attempt {Attempt} of {MaxAttempts} to confirm payment", attempt, maxRetries);
                    
                    // Wait longer between each retry attempt
                    if (attempt > 1)
                    {
                        int delayMs = (int)Math.Pow(2, attempt - 1) * 500; // 500ms, 1000ms, 2000ms
                        await Task.Delay(delayMs);
                        _logger.LogInformation("Retrying after {Delay}ms delay", delayMs);
                    }
                    
                    // Try to confirm the payment
                    await _bookingPaymentRepo.ConfirmBookingPaymentAsync(
                        confirmPaymentDto.BookingId,
                        confirmPaymentDto.PaymentIntentId
                    );

                    var bookingn = await _bookingRepo.getBookingByIdWithData(confirmPaymentDto.BookingId);
                    var notification1 = new Notification
                    {
                        UserId = bookingn.GuestId,
                        SenderId = bookingn.Property.HostId,
                        Message = $"Your payment of {paymentIntent.Amount / 100m}$ was successful.",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    var notification2 = new Notification
                    {
                        UserId = bookingn.Property.HostId,
                        SenderId = bookingn.GuestId,
                        Message = $"You have received a payment of {paymentIntent.Amount / 100m}$ from {bookingn.Guest.FirstName} {bookingn.Guest.LastName}.",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    await _notificationRepo.CreateNotificationAsync(notification1);
                    await _notificationRepo.CreateNotificationAsync(notification2);

                    // Instead of passing the tracked entity, just pass the ID and dates
                    // This prevents entity tracking conflicts
                    await _propertyAvailabilityRepo.UpdateAvailabilityAsync(
                        booking.PropertyId,
                        booking.StartDate,
                        booking.EndDate,
                        isAvailable: false
                    );

                    return Ok(new { Message = "Payment confirmed successfully", Success = true });
                }
                catch (Exception ex)
                {
                    string errorMessage = ex.Message;
                    bool shouldRetry = ex.Message.Contains("second operation was started");
                    
                    // Log detailed error for debugging
                    if (ex.InnerException != null)
                    {
                        _logger.LogError(ex, "Inner exception on attempt {Attempt}: {InnerError}", 
                            attempt, ex.InnerException.Message);
                        errorMessage = $"{errorMessage} - {ex.InnerException.Message}";
                    }
                    
                    _logger.LogError(ex, "Error on attempt {Attempt}: {ErrorMessage}", attempt, errorMessage);
                    
                    // If this is the last attempt or we shouldn't retry this type of error, return error response
                    if (attempt == maxRetries || !shouldRetry)
                    {
                        string friendlyMessage = shouldRetry 
                            ? "Payment system is busy. Please try again in a few moments." 
                            : "Error confirming payment. Please check your payment details and try again.";
                            
                        return BadRequest(new { 
                            Message = friendlyMessage,
                            Details = errorMessage,
                            Success = false
                        });
                    }
                    
                    // Otherwise, continue to the next retry attempt
                    _logger.LogWarning("Will retry payment confirmation. Attempt {Attempt} failed with: {Error}", 
                        attempt, errorMessage);
                }
            }
            
            // This should never be reached because the last attempt will either return Ok or BadRequest
            return StatusCode(500, new { Message = "An unexpected error occurred during payment processing" });
        }

        [HttpGet("GetPaymentBySessionId/{sessionId}")]
        public async Task<IActionResult> GetPaymentBySessionId(string sessionId)
        {
            var sessionService = new SessionService();
            var session = await sessionService.GetAsync(sessionId);
            var paymentIntentId = session.PaymentIntentId;

            var payment = await _bookingPaymentRepo.GetPaymentByTransactionIdAsync(paymentIntentId);
            if (payment == null)
            {
                return NotFound("Payment not found");
            }
            return Ok(payment);
        }

        public class AdminRefundDto
        {
            public int PaymentId { get; set; }
            public decimal RefundAmount { get; set; }
            public int ViolationId { get; set; }
            public string Reason { get; set; } = "fraudulent"; // Default reason for violation-based refunds
            public string AdminNotes { get; set; }
        }

        [HttpPost("admin-refund")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminRefund([FromBody] AdminRefundDto refundDto)
        {
            _logger.LogInformation("AdminRefund called for paymentId: {PaymentId}, amount: {Amount}, violationId: {ViolationId}",
                refundDto.PaymentId, refundDto.RefundAmount, refundDto.ViolationId);

            try
            {
                // Validate refund request
                if (refundDto.RefundAmount <= 0)
                {
                    return BadRequest(new { error = "Refund amount must be greater than zero." });
                }

                // Get the payment to verify it exists and is eligible for refund
                var payment = await _context.BookingPayments.FindAsync(refundDto.PaymentId);
                if (payment == null)
                {
                    return NotFound(new { error = "Payment not found." });
                }

                if (payment.Status == "refunded")
                {
                    return BadRequest(new { error = "Payment has already been fully refunded." });
                }

                if (payment.RefundedAmount + refundDto.RefundAmount > payment.Amount)
                {
                    return BadRequest(new { error = $"Refund amount exceeds available amount. Maximum refund available: {payment.Amount - payment.RefundedAmount:F2}" });
                }

                // Verify that the violation exists and is resolved or under review
                var violation = await _context.Violations.FindAsync(refundDto.ViolationId);
                if (violation == null)
                {
                    return NotFound(new { error = "Violation not found." });
                }

                // Make sure the violation is either under review or resolved
                if (violation.Status != "UnderReview" && violation.Status != "Resolved")
                {
                    return BadRequest(new { error = $"Violation must be under review or resolved to issue a refund. Current status: {violation.Status}" });
                }

                // Make sure the violation is related to the booking payment
                var booking = await _context.Bookings.FindAsync(payment.BookingId);
                if (booking == null)
                {
                    return BadRequest(new { error = "Booking not found." });
                }

                bool isRelated = false;
                if (violation.ReportedPropertyId.HasValue && booking.PropertyId == violation.ReportedPropertyId.Value)
                {
                    isRelated = true;
                }
                else if (violation.ReportedHostId.HasValue && booking.Property?.HostId == violation.ReportedHostId.Value)
                {
                    isRelated = true;
                }

                if (!isRelated)
                {
                    return BadRequest(new { error = "The violation is not related to this booking's property or host." });
                }

                // Validate reason to ensure it's Stripe-compliant
                if (refundDto.Reason != "duplicate" && refundDto.Reason != "fraudulent" && refundDto.Reason != "requested_by_customer")
                {
                    _logger.LogWarning("Invalid refund reason '{Reason}'. Using default 'fraudulent'", refundDto.Reason);
                    refundDto.Reason = "fraudulent"; // Default for admin/violation refunds
                }

                // Process the refund
                var success = await _bookingPaymentRepo.RefundPaymentAsync(
                    refundDto.PaymentId,
                    refundDto.RefundAmount,
                    refundDto.Reason);

                if (!success)
                {
                    return BadRequest(new { error = "Refund failed to process. Please check the logs for details." });
                }

                // Get the updated payment record after refund
                var updatedPayment = await _context.BookingPayments.FindAsync(refundDto.PaymentId);

                // Update the violation status to resolved if not already
                if (violation.Status != "Resolved")
                {
                    violation.Status = "Resolved";
                    violation.AdminNotes = (violation.AdminNotes ?? "") +
                        $"\n[{DateTime.UtcNow}] Refund of ${refundDto.RefundAmount} issued. Admin notes: {refundDto.AdminNotes}";
                    violation.ResolvedAt = DateTime.UtcNow;
                    violation.UpdatedAt = DateTime.UtcNow;

                    _context.Violations.Update(violation);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "Refund processed successfully.",
                    data = new
                    {
                        paymentId = updatedPayment.Id,
                        bookingId = updatedPayment.BookingId,
                        violationId = refundDto.ViolationId,
                        refundedAmount = refundDto.RefundAmount,
                        totalRefunded = updatedPayment.RefundedAmount,
                        remainingAmount = updatedPayment.Amount - updatedPayment.RefundedAmount,
                        paymentStatus = updatedPayment.Status
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing admin refund for payment {PaymentId}, violation {ViolationId}",
                    refundDto.PaymentId, refundDto.ViolationId);
                return StatusCode(500, new { error = "An error occurred while processing the refund." });
            }
        }

        [HttpGet("{paymentId}")]
        public async Task<IActionResult> GetPaymentById(int paymentId)
        {
            var payment = await _context.BookingPayments.FindAsync(paymentId);
            if (payment == null)
            {
                return NotFound("Payment not found");
            }
            return Ok(payment);
        }

        [HttpPost("refund")]
        public async Task<IActionResult> Refund([FromBody] RefundRequestDto refundRequest)
        {
            try
            {
                _logger.LogInformation("Refund called for bookingId: {BookingId}, paymentId: {PaymentId}, reason: {Reason}", 
                    refundRequest.BookingId, refundRequest.PaymentId, refundRequest.CancellationReason);
                
                // Get the payment to verify it exists
                var payment = await _context.BookingPayments.FindAsync(refundRequest.PaymentId);
                if (payment == null)
                {
                    return NotFound(new { error = "Payment not found." });
                }
                
                _logger.LogInformation("Found payment: Amount={Amount}, Status={Status}, RefundedAmount={RefundedAmount}", 
                    payment.Amount, payment.Status, payment.RefundedAmount);
                
                // Get booking with property and cancellation policy
                var booking = await _bookingRepo.getBookingByIdWithData(refundRequest.BookingId);
                if (booking == null)
                {
                    return NotFound(new { error = "Booking not found." });
                }
                
                if (booking.Status == "Cancelled")
                {
                    return BadRequest(new { error = "Booking is already cancelled." });
                }
                
                // Calculate refund amount based on cancellation policy
                decimal refundPercentage = 0;
                string policyName = booking.Property?.CancellationPolicy?.Name?.ToLower() ?? "strict";
                
                // Calculate days until check-in
                int daysUntilCheckIn = (int)Math.Ceiling((booking.StartDate - DateTime.UtcNow).TotalDays);
                
                switch (policyName)
                {
                    case "flexible":
                        refundPercentage = daysUntilCheckIn >= 1 ? 100 : 0;
                        break;
                    case "moderate":
                        refundPercentage = daysUntilCheckIn >= 5 ? 50 : 0;
                        break;
                    case "strict":
                    default:
                        refundPercentage = daysUntilCheckIn >= 7 ? 0 : 0;
                        break;
                }
                
                // Calculate refund amount
                decimal refundAmount = payment.Amount * (refundPercentage / 100m);
                
                _logger.LogInformation("Calculated refund: Policy={Policy}, DaysUntilCheckIn={Days}, RefundPercentage={Percentage}, RefundAmount={Amount}", 
                    policyName, daysUntilCheckIn, refundPercentage, refundAmount);
                
                // Map cancellation reason to Stripe reason
                string stripeReason = "requested_by_customer";
                switch (refundRequest.CancellationReason?.ToLower())
                {
                    case "duplicate":
                        stripeReason = "duplicate";
                        break;
                    case "fraudulent":
                        stripeReason = "fraudulent";
                        break;
                    default:
                        stripeReason = "requested_by_customer";
                        break;
                }
                
                // Process the refund if amount > 0
                bool success = false;
                if (refundAmount > 0)
                {
                    success = await _bookingPaymentRepo.RefundPaymentAsync(
                        refundRequest.PaymentId,
                        refundAmount,
                        stripeReason);
                    
                    if (!success)
                    {
                        _logger.LogError("Refund failed to process");
                        return BadRequest(new { error = "Refund failed to process. Please check the logs for details." });
                    }
                }
                else
                {
                    _logger.LogInformation("No refund to process based on cancellation policy");
                }
                
                // Update booking status to Cancelled regardless of refund amount
                await _bookingRepo.UpdateBookingStatusAsync(refundRequest.BookingId, "Cancelled");
                
                // Get the updated payment after refund
                var updatedPayment = await _context.BookingPayments.FindAsync(refundRequest.PaymentId);
                
                _logger.LogInformation("After refund: Amount={Amount}, Status={Status}, RefundedAmount={RefundedAmount}", 
                    updatedPayment.Amount, updatedPayment.Status, updatedPayment.RefundedAmount);
                
                return Ok(new
                {
                    message = refundAmount > 0 
                        ? "Refund processed successfully." 
                        : "Booking cancelled successfully, but no refund issued based on cancellation policy.",
                    cancellationPolicy = new {
                        name = policyName,
                        description = booking.Property?.CancellationPolicy?.Description,
                        refundPercentage = refundPercentage,
                        daysUntilCheckIn = daysUntilCheckIn
                    },
                    payment = new { 
                        amount = payment.Amount,
                        status = updatedPayment.Status,
                        refundedAmount = updatedPayment.RefundedAmount,
                        refundProcessed = refundAmount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in refund for booking {BookingId}, payment {PaymentId}", 
                    refundRequest.BookingId, refundRequest.PaymentId);
                return StatusCode(500, new { 
                    error = "An error occurred while processing the refund.",
                    details = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        public class RefundRequestDto
        {
            public int PaymentId { get; set; }
            public int BookingId { get; set; }
            public string CancellationReason { get; set; } = "requested_by_customer";
        }

    }
}