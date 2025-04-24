using API.Services.BookingPaymentRepo;
using Microsoft.AspNetCore.Http;
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

            if (stripeEvent.Type == "payment_intent.succeeded")
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;

                if (paymentIntent != null && paymentIntent.Metadata.ContainsKey("bookingId"))
                {
                    var bookingId = int.Parse(paymentIntent.Metadata["bookingId"]);
                    await _bookingPaymentRepo.ConfirmBookingPaymentAsync(bookingId, paymentIntent.Id);
                }
            }

            return Ok();
        }
    }
}
