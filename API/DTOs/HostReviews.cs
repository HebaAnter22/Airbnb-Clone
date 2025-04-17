namespace API.DTOs
{
    public class HostReviewDto
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public string PropertyTitle { get; set; }
        public string GuestName { get; set; }
        public string GuestAvatar { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
