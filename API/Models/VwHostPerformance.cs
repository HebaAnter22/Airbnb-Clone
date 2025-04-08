namespace API.Models
{
    public class VwHostPerformance
    {
        public int HostId { get; set; }
        public string HostName { get; set; } = null!;
        public int TotalProperties { get; set; }
        public int TotalBookings { get; set; }
        public decimal? AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }
}
