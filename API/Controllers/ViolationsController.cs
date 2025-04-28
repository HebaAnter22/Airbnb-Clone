using System.Security.Claims;
using API.DTOs;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ViolationsController : ControllerBase
    {
        private readonly IViolationService _violationService;

        public ViolationsController(IViolationService violationService)
        {
            _violationService = violationService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ReportViolation([FromBody] CreateViolationDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _violationService.ReportViolation(userId, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllViolations()
        {
            try
            {
                var violations = await _violationService.GetAllViolations();
                return Ok(violations);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetViolationsByStatus(string status)
        {
            try
            {
                var violations = await _violationService.GetViolationsByStatus(status);
                return Ok(violations);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetMyViolations()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var violations = await _violationService.GetViolationsByUser(userId);
                return Ok(violations);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetViolationById(int id)
        {
            try
            {
                var violation = await _violationService.GetViolationById(id);
                if (violation == null)
                    return NotFound();

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var role = User.FindFirstValue(ClaimTypes.Role);

                // Only admin or the user who reported the violation can view it
                if (role != "Admin" && violation.ReportedById != userId)
                    return Forbid();

                return Ok(violation);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateViolationStatus(int id, [FromBody] UpdateViolationStatusDto dto)
        {
            try
            {
                var result = await _violationService.UpdateViolationStatus(id, dto);
                if (result == null)
                    return NotFound();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("host/{hostId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetViolationsByHostId(int hostId)
        {
            try
            {
                var violations = await _violationService.GetViolationsByHostId(hostId);
                return Ok(violations);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("block-host/{hostId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BlockHost(int hostId)
        {
            try
            {
                var result = await _violationService.BlockHost(hostId);
                if (!result)
                    return NotFound();

                return Ok(new { message = "Host blocked successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/bookings")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetBookingsRelatedToViolation(int id)
        {
            try
            {
                var bookings = await _violationService.GetBookingsRelatedToViolation(id);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
} 