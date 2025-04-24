namespace API.DTOs.Admin
{
    public class HostVerificationInputDTO
    {
        public int Id { get; set; }
        public int HostId { get; set; }

        public string Status { get; set; }
        public string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsVerified { get; set; }
    }

    public class HostOnboardingDto
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Country { get; set; } // e.g., "US"
    }
    
}
