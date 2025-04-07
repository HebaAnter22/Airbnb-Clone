namespace API.Models
{
    public class CancellationPolicy
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal RefundPercentage { get; set; }

        // Navigation Properties
        public virtual ICollection<Property> Properties { get; set; }= new List<Property>();
    }
}