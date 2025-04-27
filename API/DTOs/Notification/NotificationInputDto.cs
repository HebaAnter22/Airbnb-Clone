namespace API.DTOs.Notification
{
    public class NotificationInputDto
    {
        public int UserId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
