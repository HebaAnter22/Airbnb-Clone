using API.Models;

namespace API.DTOs
{
    public class EditProfileDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string ProfilePictureUrl { get; set; }


        public string? AboutMe { get; set; }
        public string? Work { get; set; }
        public string Education { get; set; }
        public string Languages { get; set; }
        public string? LivesIn { get; set; }
        public string? DreamDestination { get; set; }
        public string? FunFact { get; set; }
        public string? Pets { get; set; }
        public string? ObsessedWith { get; set; }
        public string? SpecialAbout { get; set; }
    }
}
