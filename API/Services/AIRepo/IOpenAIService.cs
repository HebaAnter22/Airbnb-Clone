using API.DTOs.AI;

namespace API.Services.AIRepo
{
    public interface IOpenAIService
    {
        public Task<AIResponseDTO> GetAIResponseAsync(AIRequestDTO request);
        public Task<AIResponseDTO> GetPropertyRecommendationsAsync(string userQuery);
        public Task<AIResponseDTO> GetBookingAssistanceAsync(string userQuery);
        public Task<AIResponseDTO> GetAvailabilityInsightsAsync(string userQuery);
        //public Task<AIResponseDTO> FetchPropertyDetails(string userQuery);
    }
}
