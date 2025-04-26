using AirBnb.BL.Dtos.BookingDtos;
using API.Data;
using API.Models;
using API.DTOs.BookingPayment;
using API.Services.BookingPaymentRepo;
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

        [HttpPost("{paymentId}/refund")]
        public async Task<IActionResult> RefundPayment(int paymentId, [FromBody] decimal refundAmount)
        {
            var success = await _bookingPaymentRepo.RefundPaymentAsync(paymentId, refundAmount);
            if (!success)
                return BadRequest("Refund failed.");
            return NoContent();
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

    }
}
