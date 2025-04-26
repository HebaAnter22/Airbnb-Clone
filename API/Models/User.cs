using Stripe;

namespace API.Models
{
    public enum UserRole
    {
        Guest,
        Host,
        Admin
    }

    public enum Account_Status
    {
        Active,
        Pending,
        Blocked
    }

    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string PhoneNumber { get; set; }
        public string AccountStatus { get; set; } = Account_Status.Active.ToString();
        public bool EmailVerified { get; set; } = false;
        public bool PhoneVerified { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public string Role { get; set; } = UserRole.Guest.ToString();
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        // Navigation Properties
        public Host Host { get; set; }
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Conversation> ConversationsAsUser1 { get; set; } = new List<Conversation>();
        public ICollection<Conversation> ConversationsAsUser2 { get; set; } = new List<Conversation>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<Favourite> Favourites { get; set; } = new List<Favourite>();
        public ICollection<UserUsedPromotion> UsedPromotions { get; set; } = new List<UserUsedPromotion>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<Notification> NotificationsSent { get; set; } = new List<Notification>();


    }
}