using API.DTOs.BookingPayment;
using API.Models;
using API.Services.BookingPaymentRepo;
using API.Services.BookingRepo;
using API.Services.NotificationRepository;
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
        private readonly IBookingRepository _bookingRepo;
        private readonly INotificationRepository _notificationRepo;
        public BookingPaymentController(IBookingPaymentRepository bookingPaymentRepository,IBookingRepository bookingRepo,INotificationRepository notificationRepository)
        {
            _bookingPaymentRepo = bookingPaymentRepository;
            _bookingRepo = bookingRepo;
            _notificationRepo = notificationRepository;
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
                var booking = await _bookingRepo.getBookingByIdWithData(confirmPaymentDto.BookingId);
                var notification1 = new Notification
                {
                    UserId = booking.GuestId,
                    SenderId = booking.Property.HostId,
                    Message = $"Your payment of {paymentIntent.Amount / 100m} was successful.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                var notification2 = new Notification
                {
                    UserId = booking.Property.HostId,
                    SenderId = booking.GuestId,
                    Message = $"You have received a payment of {paymentIntent.Amount / 100m} from {booking.Guest.FirstName} {booking.Guest.LastName}.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                await _notificationRepo.CreateNotificationAsync(notification1);

                return Ok(new { Message = "Payment confirmed and booking updated successfully." });
            }

            return BadRequest(new { Message = $"Payment confirmation failed. Status: {paymentIntent.Status}" });
        }



        [HttpPost("{bookingId}/payout")]
        [Authorize(Roles = "Host")] // Restrict access to admins or hosts
        public async Task<IActionResult> CreatePayout(int bookingId)
        {
            try
            {
                var booking = await _bookingRepo.GetByIdAsync(bookingId);
                if (booking == null)
                    return NotFound("Booking not found.");

                if (booking.Status != "Confirmed")
                    return BadRequest("Booking must be confirmed before creating a payout.");

                var success = await _bookingPaymentRepo.CreatePayoutAsync(bookingId,booking.TotalAmount);
                if (!success)
                    return BadRequest("Failed to create payout.");

                return Ok(new { Message = "Payout created successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}
