namespace API.Models
{
    public class HostVerification
    {
        public int Id { get; set; }
        public int HostId { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; }
        public string Status { get; set; } = "pending";
        public string DocumentUrl { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? VerifiedAt { get; set; }

        // Navigation Property
        public virtual  Host Host{ get; set; }
    }
}