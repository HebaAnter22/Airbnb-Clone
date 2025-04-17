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

        public string? LivesIn { get; set; }
        public string? DreamDestination { get; set; }
        public string? FunFact { get; set; }
        public string? Pets { get; set; }
        public string? ObsessedWith { get; set; }
        public string? SpecialAbout { get; set; }


        // Navigation Properties
        public virtual User User { get; set; }
        public virtual ICollection<Property> Properties { get; set; }
        public virtual ICollection<HostVerification> Verifications { get; set; }
    }
}
