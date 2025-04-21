using API.DTOs.Admin;
using API.Models;
using API.Services.HostVerificationRepo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HostVerificationController : ControllerBase
    {
        private readonly IHostVerificationRepository _hostVerificationRepository;

        public HostVerificationController(IHostVerificationRepository hostVerificationRepository)
        {
            _hostVerificationRepository = hostVerificationRepository;
        }

        [HttpGet]
        [Route("GetAllVerifications")]
        public async Task<IActionResult> GetAllVerifications()
        {
            var verifications = await _hostVerificationRepository.GetAllVerificationsAsync();
            var dtos = verifications.Select(v => new HostVerificationOutputDTO
            {
                Id = v.Id,
                HostId = v.UserId,
                HostName = $"{v.Host.User.FirstName} {v.Host.User.LastName}",
                Status = v.Status,
                VerificationDocumentUrl = v.DocumentUrl,
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
                HostId = verification.UserId,
                HostName = $"{verification.Host.User.FirstName} {verification.Host.User.LastName}",
                Status = verification.Status,
                VerificationDocumentUrl = verification.DocumentUrl,
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
                HostId = verification.UserId,
                HostName = $"{verification.Host.User.FirstName} {verification.Host.User.LastName}",
                Status = verification.Status,
                VerificationDocumentUrl = verification.DocumentUrl,
                SubmittedAt = verification.SubmittedAt
            };
            return Ok(dto);
        }

        [HttpPut("{verificationId}")]
        public async Task<IActionResult> UpdateVerificationStatus(int verificationId, [FromBody] string newStatus)
        {
            if (string.IsNullOrEmpty(newStatus))
                return BadRequest("New status cannot be null or empty.");
            var result = await _hostVerificationRepository.UpdateVerificationStatusAsync(verificationId, newStatus);
            if (!result)
                return NotFound();
            return NoContent();
        }

        [HttpPost]
        [Route("CreateVerification")]
        public async Task<IActionResult> CreateVerification(int hostId, List<IFormFile> files)
        {
            if (hostId <= 0)
                return BadRequest("Invalid host ID.");

            try
            {
                var verification = await _hostVerificationRepository.CreateVerificationWithImagesAsync(hostId, files);
                return CreatedAtAction(nameof(GetVerificationById), new { verificationId = verification.Id }, verification);
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
    }
}