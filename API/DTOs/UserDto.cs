using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class RegisterUserDto
    {

        

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }
        [Required]
        public string Role { get; set; }



        [Required]
        [EmailAddress]
        public string Email { get; set; }



        [Required]
        public string Password { get; set; }
        



    }
    public class LoginUserDto
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
