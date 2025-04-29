namespace API.DTOs.Auth
{
    public class PasswordResetNotificationDto
    {
        public string Email { get; set; }
        public DateTime ResetTime { get; set; }
    }
}
