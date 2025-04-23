namespace API.DTOs.Profile
{
    public class EmailUpdateDto
    {
        public string NewEmail { get; set; }

    }
    public class EmailVerificationDto
    {
        public bool IsVerified { get; set; }
    }
}
