using API.Services.BookingPaymentRepo;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace API.Controllers
{
    [Route("api/webhook/stripe")]
    [ApiController]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IBookingPaymentRepository _bookingPaymentRepo;
        private readonly IConfiguration _configuration;

        public StripeWebhookController(IBookingPaymentRepository bookingPaymentRepo, IConfiguration configuration)
        {
            _bookingPaymentRepo = bookingPaymentRepo;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> HandleStripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            var stripeSignature = Request.Headers["Stripe-Signature"];
            var webhookSecret = _configuration["Stripe:WebhookSecret"];

            Event stripeEvent;

            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
            }
            catch (Exception e)
            {
                return BadRequest($"Webhook Error: {e.Message}");
            }

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                if (session != null && session.Metadata.ContainsKey("bookingId"))
                {
                    var bookingId = int.Parse(session.Metadata["bookingId"]);
                    var paymentIntentId = session.PaymentIntentId;

                    // Confirm the payment and insert/update the payment record
                    await _bookingPaymentRepo.ConfirmBookingPaymentAsync(bookingId, paymentIntentId);
                }
            }

            return Ok();
        }
    }
}