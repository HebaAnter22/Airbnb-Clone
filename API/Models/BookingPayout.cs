namespace API.Models
{
    public class BookingPayout
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } 
        public DateTime CreatedAt { get; set; }

        public virtual Booking Booking { get; set; }
    }
}
