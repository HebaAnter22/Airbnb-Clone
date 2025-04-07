namespace API.Models
{
    public class PropertyAvailability
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public DateTime Date { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string BlockedReason { get; set; }
        public decimal Price { get; set; }
        public int MinNights { get; set; } = 1;

        public virtual Property Property { get; set; } = null!;
    }
}
