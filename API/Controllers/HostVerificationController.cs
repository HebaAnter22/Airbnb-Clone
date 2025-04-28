using System.Security.Claims;
using API.DTOs.HostVerification;
using API.Models;
using API.Services.HostVerificationRepo;
using API.Services.NotificationRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HostVerificationController : ControllerBase
    {
        private readonly IHostVerificationRepository _hostVerificationRepository;
        private readonly INotificationRepository _notificationRepository;

        public HostVerificationController(IHostVerificationRepository hostVerificationRepository, INotificationRepository notificationRepository)
        {
            _hostVerificationRepository = hostVerificationRepository;
            _notificationRepository = notificationRepository;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Invalid or missing user ID in token.");
            }
            return userId;
        }

        [HttpGet]
        [Route("GetAllVerifications")]
        public async Task<IActionResult> GetAllVerifications()
        {
            var verifications = await _hostVerificationRepository.GetAllVerificationsAsync();
            var dtos = verifications.Select(v => new HostVerificationOutputDTO
            {
                Id = v.Id,
                HostId = v.HostId,
                HostName = $"{v.Host.User.FirstName} {v.Host.User.LastName}",
                Status = v.Status,

                VerificationDocumentUrl1 = v.DocumentUrl1,
                VerificationDocumentUrl2 = v.DocumentUrl2,
                SubmittedAt = v.SubmittedAt
            }).ToList();
            
            return Ok(dtos);
        }

        [HttpGet("{verificationId}")]
        public async Task<IActionResult> GetVerificationById(int verificationId)
        {
            var verification = await _hostVerificationRepository.GetVerificationByIdAsync(verificationId);
            if (verification == null)
                return NotFound();
            var dto = new HostVerificationOutputDTO
            {
                Id = verification.Id,
                HostId = verification.HostId,
                HostName = $"{verification.Host.User.FirstName} {verification.Host.User.LastName}",
                Status = verification.Status,
                VerificationDocumentUrl1 = verification.DocumentUrl1,
                VerificationDocumentUrl2 = verification.DocumentUrl2,
                SubmittedAt = verification.SubmittedAt
            };
            return Ok(dto);
        }

        [HttpGet("GetVerificationsForCurrentHost")]
        public async Task<IActionResult> GetVerificationsByHostId()
        {
            var hostid= GetCurrentUserId();
            var verification = await _hostVerificationRepository.GetVerificationByhostsAsync(hostid);
            if (verification == null)
                return NotFound();
            var dto = new HostVerificationOutputDTO
            {
                Id = verification.Id,
                HostId = verification.HostId,
                HostName = $"{verification.Host.User.FirstName} {verification.Host.User.LastName}",
                Status = verification.Status,
                VerificationDocumentUrl1 = verification.DocumentUrl1,
                VerificationDocumentUrl2 = verification.DocumentUrl2,
                SubmittedAt = verification.SubmittedAt
            };
            return Ok(dto);
        }

        [HttpGet("GetVerificationsByHostId/{hostId}")]
        public async Task<IActionResult> GetVerificationsByHostId(int hostId)
        {
            var verification = await _hostVerificationRepository.GetVerificationByhostsAsync(hostId);
            if (verification == null)
                return NotFound();
            var dto = new HostVerificationOutputDTO
            {
                Id = verification.Id,
                HostId = verification.HostId,
                HostName = $"{verification.Host.User.FirstName} {verification.Host.User.LastName}",
                Status = verification.Status,
                VerificationDocumentUrl1 = verification.DocumentUrl1,
                VerificationDocumentUrl2 = verification.DocumentUrl2,
                SubmittedAt = verification.SubmittedAt
            };
            return Ok(dto);
        }


        [HttpPost]
        [Route("CreateVerification")]
        public async Task<IActionResult> CreateVerification(List<IFormFile> files)
        {

            try
            {
                var verification = await _hostVerificationRepository.CreateVerificationWithImagesAsync(GetCurrentUserId(), files);
                if (verification == null)
                    return BadRequest("Failed to create verification.");
                var host = await _hostVerificationRepository.GetHostByIdAsync(verification.HostId);
                var notification = new Notification
                {
                    UserId = GetCurrentUserId(),
                    Message = $"Your verification request has been submitted successfully.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                await _notificationRepository.CreateNotificationAsync(notification);
                var dto = new HostVerificationOutputDTO
                {
                    Id = verification.Id,
                    HostId = verification.HostId,
                    HostName = $"{host.User.FirstName} {host.User.LastName}",
                    Status = verification.Status,
                    VerificationDocumentUrl1 = verification.DocumentUrl1,
                    VerificationDocumentUrl2 = verification.DocumentUrl2,
                    SubmittedAt = verification.SubmittedAt
                };
                return CreatedAtAction(nameof(GetVerificationById), new { verificationId = verification.Id }, dto);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating verification: {ex.Message}");
            }
        }

        [HttpDelete("{verificationId}")]
        public async Task<IActionResult> DeleteVerification(int verificationId)
        {
            var verification = await _hostVerificationRepository.GetVerificationByIdAsync(verificationId);
            if (verification == null)
                return NotFound();
            await _hostVerificationRepository.DeleteAsync(verification.Id);
            return NoContent();

        }
        [HttpGet("get-verification-status")]
        [Authorize]
        public async Task<IActionResult> GetVerificationStatus()
        {

            var hostId = GetCurrentUserId();
            var verification = await _hostVerificationRepository.GetVerificationByhostsAsync(hostId);
            if (verification == null)
                return NotFound(new { message = "No verification found for this host." });
            return Ok(new { status = verification.Status });
        }
    }
}
