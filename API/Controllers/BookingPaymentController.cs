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

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingPaymentController : ControllerBase
    {
        private readonly IBookingPaymentRepository _bookingPaymentRepo;
        private readonly AppDbContext _context;

        public BookingPaymentController(IBookingPaymentRepository bookingPaymentRepository, AppDbContext context)
        {
            _bookingPaymentRepo = bookingPaymentRepository;
            _context = context;
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
            Console.WriteLine($"ConfirmBookingPaymentAsync called for bookingId: {confirmPaymentDto.BookingId}, paymentIntentId: {confirmPaymentDto.PaymentIntentId}");

            var existingPayment = await _context.BookingPayments
                .FirstOrDefaultAsync(p => p.TransactionId == confirmPaymentDto.PaymentIntentId);

            if (existingPayment == null)
            {
                try
                {
                    var paymentIntentService = new PaymentIntentService();
                    var paymentIntent = await paymentIntentService.GetAsync(confirmPaymentDto.PaymentIntentId);
                    Console.WriteLine($"PaymentIntent retrieved: Amount: {paymentIntent.Amount}, Status: {paymentIntent.Status}");

                    await InsertPaymentAsync(
                        confirmPaymentDto.BookingId,
                        paymentIntent.Amount / 100m,
                        confirmPaymentDto.PaymentIntentId,
                        paymentIntent.Status
                    );
                    Console.WriteLine("Payment record inserted successfully.");
                    return Ok(new { Message = "Payment confirmed and record inserted successfully." });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error inserting payment: {ex.Message}");
                    return BadRequest(new { Message = $"Error inserting payment: {ex.Message}" });
                }
            }
            else
            {
                Console.WriteLine($"Payment already exists with TransactionId: {confirmPaymentDto.PaymentIntentId}. Updating status to succeeded.");
                existingPayment.Status = "succeeded";
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Payment status updated to succeeded." });
            }
        }

        private async Task InsertPaymentAsync(int bookingId, decimal amount, string transactionId, string status)
        {
            var payment = new BookingPayment
            {
                BookingId = bookingId,
                Amount = amount,
                PaymentMethodType = "Stripe",
                TransactionId = transactionId,
                Status = "Confrimed",
                CreatedAt = DateTime.UtcNow
            };

            _context.BookingPayments.Add(payment);
            await _context.SaveChangesAsync();
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
