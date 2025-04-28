using AirBnb.BL.Dtos.BookingDtos;
using API.Data;
using API.Models;
using API.DTOs.BookingPayment;
using API.Models;
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

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingPaymentController : ControllerBase
    {
        private readonly IBookingPaymentRepository _bookingPaymentRepo;
        private readonly AppDbContext _context;
        private readonly ILogger<BookingPaymentController> _logger;

        public BookingPaymentController(
            IBookingPaymentRepository bookingPaymentRepository, 
            AppDbContext context,
            ILogger<BookingPaymentController> logger)
        {
            _bookingPaymentRepo = bookingPaymentRepository;
            _context = context;
            _logger = logger;
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

        public class RefundPaymentDto
        {
            public int PaymentId { get; set; }
            public decimal RefundAmount { get; set; }
            public string Reason { get; set; } = "requested_by_customer"; // Default reason
        }

        [HttpPost("refund")]
        [Authorize]
        public async Task<IActionResult> RefundPayment([FromBody] RefundPaymentDto refundDto)
        {
            _logger.LogInformation("RefundPayment called for paymentId: {PaymentId}, amount: {Amount}", 
                refundDto.PaymentId, refundDto.RefundAmount);
            
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
                
                // Process the refund
                var success = await _bookingPaymentRepo.RefundPaymentAsync(
                    refundDto.PaymentId, 
                    refundDto.RefundAmount, 
                    refundDto.Reason);
                
                if (!success)
                {
                    return BadRequest(new { error = "Refund failed to process. Please check the logs for details." });
                }
                
                return Ok(new { 
                    message = "Refund processed successfully.",
                    data = new {
                        paymentId = payment.Id,
                        bookingId = payment.BookingId,
                        refundedAmount = refundDto.RefundAmount,
                        totalRefunded = payment.RefundedAmount + refundDto.RefundAmount,
                        remainingAmount = payment.Amount - (payment.RefundedAmount + refundDto.RefundAmount)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for payment {PaymentId}", refundDto.PaymentId);
                return StatusCode(500, new { error = "An error occurred while processing the refund." });
            }
        }

        [HttpPost("{paymentId}/refund")]
        [Authorize]
        public async Task<IActionResult> RefundPaymentById(int paymentId, [FromBody] decimal refundAmount)
        {
            // This endpoint is maintained for backward compatibility
            // Create a DTO and delegate to the main refund method
            var refundDto = new RefundPaymentDto
            {
                PaymentId = paymentId,
                RefundAmount = refundAmount
            };
            
            return await RefundPayment(refundDto);
        }

        [HttpPost("confirm-booking-payment")]
        [Authorize]
        public async Task<IActionResult> ConfirmBookingPaymentAsync([FromBody] ConfirmPaymentDto confirmPaymentDto)
        {
            _logger.LogInformation("ConfirmBookingPaymentAsync called for bookingId: {BookingId}, paymentIntentId: {PaymentIntentId}", 
                confirmPaymentDto.BookingId, confirmPaymentDto.PaymentIntentId);

            // Try to use the repository method first, which handles everything including earnings update
            try
            {
                await _bookingPaymentRepo.ConfirmBookingPaymentAsync(
                    confirmPaymentDto.BookingId, 
                    confirmPaymentDto.PaymentIntentId);
                
                _logger.LogInformation("Payment confirmed and host earnings updated via repository.");
                return Ok(new { Message = "Payment confirmed and host earnings updated." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error using repository method to confirm payment. Falling back to controller implementation.");
                // Fall back to controller implementation if the repository method fails
            }

            // Fall back implementation
            var existingPayment = await _context.BookingPayments
                .FirstOrDefaultAsync(p => p.TransactionId == confirmPaymentDto.PaymentIntentId);

            decimal paymentAmount = 0;
            bool isNewPayment = false;

            if (existingPayment == null)
            {
                try
                {
                    var paymentIntentService = new PaymentIntentService();
                    var paymentIntent = await paymentIntentService.GetAsync(confirmPaymentDto.PaymentIntentId);
                    _logger.LogInformation("PaymentIntent retrieved: Amount: {Amount}, Status: {Status}", 
                        paymentIntent.Amount, paymentIntent.Status);

                    paymentAmount = paymentIntent.Amount / 100m;
                    isNewPayment = true;

                    await InsertPaymentAsync(
                        confirmPaymentDto.BookingId,
                        paymentAmount,
                        confirmPaymentDto.PaymentIntentId,
                        paymentIntent.Status
                    );
                    _logger.LogInformation("Payment record inserted successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error inserting payment");
                    return BadRequest(new { Message = $"Error inserting payment: {ex.Message}" });
                }
            }
            else
            {
                _logger.LogInformation("Payment already exists with TransactionId: {TransactionId}. Updating status to succeeded.", 
                    confirmPaymentDto.PaymentIntentId);
                
                if (existingPayment.Status != "succeeded")
                {
                    existingPayment.Status = "succeeded";
                    paymentAmount = existingPayment.Amount;
                    isNewPayment = true;
                    
                    // Update booking status to Confirmed when payment is marked as successful
                    var booking = await _context.Bookings.FindAsync(confirmPaymentDto.BookingId);
                    if (booking != null && booking.Status == "Pending")
                    {
                        booking.Status = "Confirmed";
                        _context.Update(booking);
                        _logger.LogInformation("Booking {BookingId} status updated to Confirmed", booking.Id);
                    }
                    
                    await _context.SaveChangesAsync();
                }
            }

            // Update host earnings if this is a new/updated payment
            if (isNewPayment && paymentAmount > 0)
            {
                try
                {
                    _logger.LogInformation("Updating host earnings for booking {BookingId}, amount {Amount}", 
                        confirmPaymentDto.BookingId, paymentAmount);
                    
                    await _bookingPaymentRepo.UpdateHostEarningsAsync(confirmPaymentDto.BookingId, paymentAmount);
                    
                    _logger.LogInformation("Host earnings updated successfully");
                }
                catch (Exception ex)
                {
                    // Don't fail the request if earnings update fails, just log it
                    _logger.LogError(ex, "Error updating host earnings");
                }
            }

            return Ok(new { Message = "Payment confirmed successfully." });
        }

        private async Task InsertPaymentAsync(int bookingId, decimal amount, string transactionId, string status)
        {
            var payment = new BookingPayment
            {
                BookingId = bookingId,
                Amount = amount,
                PaymentMethodType = "Stripe",
                TransactionId = transactionId,
                Status = "succeeded", // Fixed status value to match expected status
                CreatedAt = DateTime.UtcNow
            };

            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
            {
                throw new Exception($"Booking with ID {bookingId} not found.");
            }
            else if (booking.Status != "Pending")
            {
                throw new Exception($"Booking with ID {bookingId} is not in a valid state for payment.");
            }
            
            booking.Status = "Confirmed"; // Update booking status to Confirmed
            _context.Update(booking);
            _context.BookingPayments.Add(payment);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Booking {BookingId} status updated to Confirmed", bookingId);
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
                
                // Process the refund
                var success = await _bookingPaymentRepo.RefundPaymentAsync(
                    refundDto.PaymentId, 
                    refundDto.RefundAmount, 
                    refundDto.Reason);
                
                if (!success)
                {
                    return BadRequest(new { error = "Refund failed to process. Please check the logs for details." });
                }
                
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
                
                return Ok(new { 
                    message = "Refund processed successfully.",
                    data = new {
                        paymentId = payment.Id,
                        bookingId = payment.BookingId,
                        violationId = refundDto.ViolationId,
                        refundedAmount = refundDto.RefundAmount,
                        totalRefunded = payment.RefundedAmount + refundDto.RefundAmount,
                        remainingAmount = payment.Amount - (payment.RefundedAmount + refundDto.RefundAmount)
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

    }
}
