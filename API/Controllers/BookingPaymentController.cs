using API.DTOs.BookingPayment;
using API.Services.BookingPaymentRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingPaymentController : ControllerBase
    {
        public readonly IBookingPaymentRepository _bookingPaymentRepo;
        public BookingPaymentController(IBookingPaymentRepository bookingPaymentRepository)
        {
            _bookingPaymentRepo = bookingPaymentRepository;
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

        [HttpPost("confirm-payment")]
        [Authorize]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentDto confirmPaymentDto)
        {
            var paymentIntentService = new PaymentIntentService();
            var paymentIntent = await paymentIntentService.ConfirmAsync(confirmPaymentDto.PaymentIntentId, new PaymentIntentConfirmOptions
            {
                PaymentMethod = confirmPaymentDto.PaymentMethodId
            });

            if (paymentIntent.Status == "succeeded")
            {
                await _bookingPaymentRepo.ConfirmBookingPaymentAsync(confirmPaymentDto.BookingId, paymentIntent.Id);

                return Ok(new { Message = "Payment confirmed and booking updated successfully." });
            }

            return BadRequest(new { Message = $"Payment confirmation failed. Status: {paymentIntent.Status}" });
        }




    }
}
