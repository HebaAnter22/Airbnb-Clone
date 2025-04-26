using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Services.Payoutrepo;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PayoutController : ControllerBase
    {
        private readonly IPayoutService _payoutService;
        private readonly ILogger<PayoutController> _logger;

        public PayoutController(IPayoutService payoutService, ILogger<PayoutController> logger)
        {
            _payoutService = payoutService;
            _logger = logger;
        }

        [HttpGet("host/balance/{hostId}")]
        public async Task<ActionResult<decimal>> GetHostBalance(int hostId)
        {
            try
            {
                var balance = await _payoutService.GetHostBalance(hostId);
                return Ok(new { availableBalance = balance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting host balance for hostId: {HostId}", hostId);
                return BadRequest(ex.Message);
            }
        }

        //[HttpPost("stripe/connect")]
        //public async Task<IActionResult> CreateStripeConnectAccount([FromBody] int hostId)
        //{
        //    try
        //    {
        //        var accountId = await _payoutService.CreateStripeConnectAccount(hostId);
        //        return Ok(new { accountId });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}

        //[HttpGet("stripe/connect/link")]
        //public async Task<IActionResult> GetStripeConnectAccountLink([FromQuery] int hostId)
        //{
        //    try
        //    {
        //        var link = await _payoutService.GetStripeConnectAccountLink(hostId);
        //        return Ok(new { link });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}

        [HttpPost("request")]
        public async Task<ActionResult<HostPayout>> RequestPayout([FromBody] PayoutRequest request)
        {
            try
            {
                if (request.Amount <= 0)
                    return BadRequest("Amount must be greater than zero");

                var payout = await _payoutService.RequestPayout(request.HostId, request.Amount);
                return Ok(payout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payout request for hostId: {HostId}, amount: {Amount}", 
                    request.HostId, request.Amount);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("host/{hostId}")]
        public async Task<ActionResult<List<HostPayout>>> GetHostPayouts(int hostId)
        {
            try
            {
                var payouts = await _payoutService.GetHostPayouts(hostId);
                return Ok(payouts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payouts for hostId: {HostId}", hostId);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{payoutId}")]
        public async Task<ActionResult<HostPayout>> GetPayoutDetails(int payoutId)
        {
            try
            {
                var payout = await _payoutService.GetPayoutDetails(payoutId);
                if (payout == null)
                    return NotFound($"Payout with ID {payoutId} not found");
                    
                return Ok(payout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payout details for payoutId: {PayoutId}", payoutId);
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{payoutId}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePayoutStatus(int payoutId, [FromBody] string status)
        {
            try
            {
                await _payoutService.UpdatePayoutStatus(payoutId, status);
                return Ok(new { message = $"Payout status updated to {status}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payout status for payoutId: {PayoutId}, status: {Status}", 
                    payoutId, status);
                return BadRequest(ex.Message);
            }
        }
    }

    public class PayoutRequest
    {
        [Required]
        public int HostId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal Amount { get; set; }
    }
} 