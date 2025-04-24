namespace API.DTOs.Review
{
    public class ReviewOutputDto
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int ReviewerId { get; set; }
        public string ReviewerName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ReviewInputDto
    {
        public int BookingId { get; set; }
        public int Rating { get; set; } 
        public string Comment { get; set; }
    }

    public class ReviewDto
    {
        public int Id { get; set; }
        public int Booking_Id { get; set; }
        public int ReviewerId { get; set; }
        public string ReviewerName { get; set; }
        public string ReviewerImage { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
