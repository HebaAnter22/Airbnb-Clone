using API.DTOs.AI;
using System.Threading.Tasks;

namespace API.Services.AIRepo
{
    public interface IOpenAIService
    {
        public Task<AIResponseDTO> GetAIResponseAsync(AIRequestDTO request);
        public Task<AIResponseDTO> GetPropertyRecommendationsAsync(string userQuery);
        public Task<AIResponseDTO> GetBookingAssistanceAsync(string userQuery);
        public Task<AIResponseDTO> GetAvailabilityInsightsAsync(string userQuery);
        public Task<AIResponseDTO> TranscribeAudioAsync(byte[] audioData, string fileName);
        public Task<AIResponseDTO> TextToSpeechAsync(string text);
        //public Task<AIResponseDTO> FetchPropertyDetails(string userQuery);
    }
}
