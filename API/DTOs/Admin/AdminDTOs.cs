namespace API.DTOs.Admin
{
    public class BlockUserDto
    {
        public bool IsBlocked { get; set; }
    }

    public class ApprovePropertyDto
    {
        public bool IsApproved { get; set; }
    }

    public class SuspendPropertyDTO
    {
        public bool IsSuspended { get; set; }
    }

    public class UpdateBookingStatusDto
    {
        public string Status { get; set; }
    }


    public class GuestDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public string AccountStatus { get; set; }
        public string Role { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int BookingsCount { get; set; }
        public decimal TotalSpent { get; set; }

    }

    public class HostDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string Role { get; set; }
        public DateTime StartDate { get; set; }
        public bool IsVerified { get; set; }
        public decimal Rating { get; set; }
        public int TotalReviews { get; set; }
        public int PropertiesCount { get; set; }
        public decimal TotalIncome { get; set; }
    }
}
