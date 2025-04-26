using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stripe;
using System;
using System.IO;
using System.Threading.Tasks;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly ILogger<StripeWebhookController> _logger;
        private readonly string _webhookSecret;
        private readonly IWebHostEnvironment _environment;

        public StripeWebhookController(
            IConfiguration configuration,
            AppDbContext context,
            ILogger<StripeWebhookController> logger,
            IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _webhookSecret = _configuration["Stripe:WebhookSecret"];
            _environment = environment;
        }

        [HttpPost]
        public async Task<IActionResult> Index()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            _logger.LogInformation("Received webhook: {json}", json);

            Event stripeEvent;
            try
            {
                if (_environment.IsDevelopment() && !Request.Headers.ContainsKey("Stripe-Signature"))
                {
                    // In development, allow testing without signature verification
                    _logger.LogWarning("Development mode: Bypassing signature verification");
                    stripeEvent = JsonConvert.DeserializeObject<Event>(json);
                }
                else
                {
                    // In production, always verify signatures
                    _logger.LogInformation("Received webhook payload: {0}", json);

                    stripeEvent = EventUtility.ConstructEvent(
                        json,
                        Request.Headers["Stripe-Signature"],
                        _webhookSecret
                    );
                }

                // Handle the event based on its type
                switch (stripeEvent.Type)
                {
                    case "account.updated":
                        var account = stripeEvent.Data.Object as Account;
                        await HandleAccountUpdated(account);
                        break;
                    
                    case "transfer.created":
                        var transferCreated = stripeEvent.Data.Object as Transfer;
                        await HandleTransferCreated(transferCreated);
                        break;
                    
                    case "transfer.paid":
                        var transferPaid = stripeEvent.Data.Object as Transfer;
                        await HandleTransferPaid(transferPaid);
                        break;
                    
                    case "transfer.failed":
                        var transferFailed = stripeEvent.Data.Object as Transfer;
                        await HandleTransferFailed(transferFailed);
                        break;
                    
                    // Add more event types as needed
                    
                    default:
                        _logger.LogInformation("Unhandled event type: {0}", stripeEvent.Type);
                        break;
                }

                return Ok(new { received = true });
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "Error handling Stripe webhook");
                return BadRequest(new { error = e.Message });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "General error handling webhook");
                return StatusCode(500, new { error = e.Message });
            }
        }

        private async Task HandleAccountUpdated(Account account)
        {
            try
            {
                _logger.LogInformation("Processing account.updated event for account {0}", account.Id);
                
                // Find the host associated with this Stripe account
                var host = await _context.HostProfules
                    .FirstOrDefaultAsync(h => h.StripeAccountId == account.Id);

                if (host == null)
                {
                    _logger.LogWarning("No host found with Stripe account ID: {0}", account.Id);
                    return;
                }

                // Update host details based on account status if needed
                // For test mode, we'll consider charges_enabled as the key indicator
                if (account.ChargesEnabled)
                {
                    _logger.LogInformation("Host {0} Stripe account is now fully configured", host.HostId);
                    // You could set a flag or update status in your database
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling account.updated event");
            }
        }

        private async Task HandleTransferCreated(Transfer transfer)
        {
            try
            {
                _logger.LogInformation("Processing transfer.created event for transfer {0}", transfer.Id);
                
                if (transfer.Metadata.TryGetValue("PayoutId", out string payoutIdStr) && 
                    int.TryParse(payoutIdStr, out int payoutId))
                {
                    var payout = await _context.HostPayouts.FindAsync(payoutId);
                    if (payout != null)
                    {
                        payout.Status = "Processing";
                        payout.TransactionId = transfer.Id;
                        payout.Notes = $"Transfer created: {transfer.Id}";
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Updated payout {0} status to Processing", payoutId);
                    }
                    else
                    {
                        _logger.LogWarning("Payout not found with ID: {0}", payoutId);
                    }
                }
                else
                {
                    _logger.LogWarning("PayoutId not found in transfer metadata or not a valid integer");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling transfer.created event");
            }
        }

        private async Task HandleTransferPaid(Transfer transfer)
        {
            try
            {
                _logger.LogInformation("Processing transfer.paid event for transfer {0}", transfer.Id);
                
                if (transfer.Metadata.TryGetValue("PayoutId", out string payoutIdStr) && 
                    int.TryParse(payoutIdStr, out int payoutId))
                {
                    var payout = await _context.HostPayouts.FindAsync(payoutId);
                    if (payout != null)
                    {
                        payout.Status = "Completed";
                        payout.ProcessedAt = DateTime.UtcNow;
                        payout.Notes = $"Transfer completed: {transfer.Id}";
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Updated payout {0} status to Completed", payoutId);
                    }
                    else
                    {
                        _logger.LogWarning("Payout not found with ID: {0}", payoutId);
                    }
                }
                else
                {
                    _logger.LogWarning("PayoutId not found in transfer metadata or not a valid integer");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling transfer.paid event");
            }
        }

        private async Task HandleTransferFailed(Transfer transfer)
        {
            try
            {
                _logger.LogInformation("Processing transfer.failed event for transfer {0}", transfer.Id);
                
                if (transfer.Metadata.TryGetValue("PayoutId", out string payoutIdStr) && 
                    int.TryParse(payoutIdStr, out int payoutId))
                {
                    var payout = await _context.HostPayouts.FindAsync(payoutId);
                    if (payout != null)
                    {
                        payout.Status = "Failed";
                        payout.Notes = $"Transfer failed: {transfer.Id}";

                        // Refund the amount back to host's available balance
                        var host = await _context.HostProfules.FindAsync(payout.HostId);
                        if (host != null)
                        {
                            host.AvailableBalance += payout.Amount;
                            _logger.LogInformation("Refunded {0} back to host {1}", payout.Amount, host.HostId);
                        }
                        else
                        {
                            _logger.LogWarning("Host not found with ID: {0}", payout.HostId);
                        }

                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Updated payout {0} status to Failed", payoutId);
                    }
                    else
                    {
                        _logger.LogWarning("Payout not found with ID: {0}", payoutId);
                    }
                }
                else
                {
                    _logger.LogWarning("PayoutId not found in transfer metadata or not a valid integer");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling transfer.failed event");
            }
        }
    }
}