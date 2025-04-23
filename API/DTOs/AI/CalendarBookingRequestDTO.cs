using System;

namespace API.DTOs.AI
{
    public class CalendarBookingRequestDTO
    {
        public string PropertyTitle { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
} 