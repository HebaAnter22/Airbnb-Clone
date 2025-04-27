using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class CreateViolationDto
    {
        [Required]
        public string ViolationType { get; set; }
        
        [Required]
        public string Description { get; set; }
        
        public int? ReportedPropertyId { get; set; }
        
        public int? ReportedHostId { get; set; }
    }

    public class ViolationResponseDto
    {
        public int Id { get; set; }
        public int ReportedById { get; set; }
        public string ReporterName { get; set; }
        public int? ReportedPropertyId { get; set; }
        public string ReportedPropertyTitle { get; set; }
        public int? ReportedHostId { get; set; }
        public string ReportedHostName { get; set; }
        public string ViolationType { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public class UpdateViolationStatusDto
    {
        [Required]
        public string Status { get; set; }
        
        public string AdminNotes { get; set; }
    }
} 