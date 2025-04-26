using System.Security.Claims;
using API.DTOs.Notification;
using API.Services.NotificationRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationRepository _notificationRepository;
        public NotificationController(INotificationRepository notificationRepository)
        {
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

        [HttpGet("user-notifications")]
        [Authorize]
        public async Task<IActionResult> GetUserNotifications()
        {
            var notifications = await _notificationRepository.GetNotificationsForUserAsync(GetCurrentUserId());
            if (notifications == null || !notifications.Any())
            {
                return NotFound("No notifications found for this user.");
            }
            var dtos = notifications.Select(n => new NotificationOutputDto
            {
                Id = n.Id,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                SenderName = n.Sender != null ? $"{n.Sender.FirstName} {n.Sender.LastName}" : string.Empty
            }).ToList();
            return Ok(notifications);
        }

        [HttpGet("unread-notifications")]
        [Authorize]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            var userId = GetCurrentUserId();
            var notifications = await _notificationRepository.GetUnreadNotificationsForUserAsync(userId);
            if (notifications == null || !notifications.Any())
            {
                return NotFound("No unread notifications found for this user.");
            }

            var dtos = notifications.Select(n => new NotificationOutputDto
            {
                Id = n.Id,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                SenderName = n.Sender != null ? $"{n.Sender.FirstName} {n.Sender.LastName}" : string.Empty
            }).ToList();

            return Ok(dtos);
        }

        [HttpPost("mark-as-read/{notificationId}")]
        [Authorize]
        public async Task<IActionResult> MarkNotificationAsRead(int notificationId)
        {
            var userId = GetCurrentUserId();
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null || notification.UserId != userId)
            {
                return NotFound("Notification not found.");
            }
            await _notificationRepository.MarkNotificationAsReadAsync(notificationId);
            return NoContent();
        }

        [HttpPost("mark-all-as-read")]
        [Authorize]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var userId = GetCurrentUserId();
            await _notificationRepository.MarkAllNotificationsAsReadAsync(userId);
            return NoContent();
        }

        [HttpGet("unread-count")]
        [Authorize]
        public async Task<IActionResult> GetUnreadNotificationCount()
        {
            var userId = GetCurrentUserId();
            var count = await _notificationRepository.GetUnreadNotificationCountAsync(userId);
            return Ok(new { UnreadCount = count });
        }


    }
}
