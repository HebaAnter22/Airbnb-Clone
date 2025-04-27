namespace API.DTOs.Notification
{
    public class NotificationOutputDto
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string SenderName { get; set; } = string.Empty;
    }
}
