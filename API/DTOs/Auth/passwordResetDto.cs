namespace API.DTOs.Auth
{
    public class ForgotPasswordDto
    {
        public string Email { get; set; }
    }

    public class ValidateResetTokenDto
    {
        public string Email { get; set; }
        public string Token { get; set; }
    }

    public class ResetPasswordDto
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }

    public class PasswordResetResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
