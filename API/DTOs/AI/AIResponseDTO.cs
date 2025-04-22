namespace API.DTOs.AI
{
    public class AIResponseDTO
    {
        public string Response { get; set; }
        public string RequestId { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
