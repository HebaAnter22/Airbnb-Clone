namespace API.DTOs
{


    public class GoogleAuthRequest
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string IdToken { get; set; }
    }
}
