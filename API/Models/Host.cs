using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace API.Models
{
    public class Host 
    {
        public int HostId { get; set; }
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public string? AboutMe { get; set; }
        public string? Work { get; set; }
        public decimal Rating { get; set; } = 0; 
        public int TotalReviews { get; set; } = 0; 
        public string Education { get; set; }
        public string Languages { get; set; }
        public bool IsVerified { get; set; } = false;

        // Navigation Properties
        public virtual User User { get; set; }
        public virtual ICollection<Property> Properties { get; set; }
        public virtual ICollection<HostVerification> Verifications { get; set; }
    }
}
