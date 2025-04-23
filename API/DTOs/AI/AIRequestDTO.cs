namespace API.DTOs.AI
{
    public class AIRequestDTO
    {
        public string Query { get; set; }
        //public string? Context { get; set; }
        //public string? UserId { get; set; }
        public string RequestType { get; set; } = "chat"; // Default to "chat" or another default type    
    }
    }
