using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs.AI;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using API.Data;

namespace API.Services.AIRepo
{
    public class OpenAIService : IOpenAIService
    {
        private readonly ChatClient _chatClient;
        private readonly AIConfiguration _config;
        private readonly AppDbContext _dbContext;

        public OpenAIService(IOptions<AIConfiguration> config, AppDbContext dbContext)
        {
            _config = config.Value;
            _dbContext = dbContext;
            if (!_config.IsValid)
            {
                throw new InvalidOperationException("Invalid OpenAI API key configuration. Please ensure a valid API key is provided in the application settings.");
            }
            // Log the first few and last few characters of the API key for debugging
            string maskedKey = _config.ApiKey.Length > 10 ? _config.ApiKey.Substring(0, 7) + "****" + _config.ApiKey.Substring(_config.ApiKey.Length - 4) : "Key too short";
            Console.WriteLine($"Initializing OpenAI client with API key: {maskedKey}");
            _chatClient = new ChatClient("gpt-4o-mini", _config.ApiKey);
        }

        public async Task<AIResponseDTO> GetAIResponseAsync(AIRequestDTO request)
        {
            try
            {
                // Fetch relevant data from database based on request type
                string dataContext = await FetchDataBasedOnRequestType(request.RequestType, request.Query);
                string enhancedQuery = $"Context from database: {dataContext}\nUser Query: {request.Query}";

                var chatMessages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are a professional AI assistant for an Airbnb clone application. Your role is to provide accurate, helpful, and detailed responses to user queries regarding property listings, booking assistance, availability insights, and general information about our platform. Always maintain a courteous and professional tone, prioritize user satisfaction, and ensure responses are relevant to the context provided."),
                    new UserChatMessage(enhancedQuery)
                };

                var options = new ChatCompletionOptions
                {
                    MaxOutputTokenCount = _config.MaxTokens,
                    Temperature = _config.Temperature,
                };

                ChatCompletion completion = await _chatClient.CompleteChatAsync(chatMessages, options);

                Console.WriteLine($"OpenAI API response received. Content count: {completion.Content.Count}");
                string responseText = completion.Content.Count > 0 ? completion.Content[0].Text : "No content returned";
                Console.WriteLine($"Response text: {responseText}");

                return new AIResponseDTO
                {
                    Response = responseText,
                    RequestId = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OpenAI API call: {ex.Message}");
                return new AIResponseDTO
                {
                    Success = false,
                    ErrorMessage = ex.Message.Contains("incorrect API key") ? "Invalid OpenAI API key. Please contact the administrator to update the API key." : ex.Message,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        public async Task<AIResponseDTO> GetPropertyRecommendationsAsync(string userQuery)
        {
            var request = new AIRequestDTO
            {
                Query = $"Based on this query: '{userQuery}', what properties would you recommend? Consider location, amenities, and price range.",
                RequestType = "property"
            };
            return await GetAIResponseAsync(request);
        }

        public async Task<AIResponseDTO> GetBookingAssistanceAsync(string userQuery)
        {
            var request = new AIRequestDTO
            {
                Query = $"Regarding this booking query: '{userQuery}', what assistance can you provide? Consider dates, guest count, and special requirements.",
                RequestType = "booking"
            };
            return await GetAIResponseAsync(request);
        }

        public async Task<AIResponseDTO> GetAvailabilityInsightsAsync(string userQuery)
        {
            var request = new AIRequestDTO
            {
                Query = $"For this availability query: '{userQuery}', what insights can you provide? Consider peak seasons, pricing trends, and booking patterns.",
                RequestType = "availability"
            };
            return await GetAIResponseAsync(request);
        }

        private async Task<string> FetchDataBasedOnRequestType(string requestType, string userQuery)
        {
            switch (requestType.ToLower())
            {
                case "property":
                    return await FetchPropertyDetails(userQuery);
                case "booking":
                case "availability":
                    return await FetchAvailabilityData(userQuery);
                default:
                    return "No specific data available for this query.";
            }
        }

        private async Task<string> FetchPropertyDetails(string userQuery)
        {
            var properties = await _dbContext.Properties
                .Take(5)
                .Select(p => $"{p.Title} located in {p.Address} with price range {p.PricePerNight}")
                .ToListAsync();

            return properties.Any() ? string.Join("\n", properties) : "No property details found.";
        }

        private async Task<string> FetchAvailabilityData(string userQuery)
        {
            var availableProperties = await _dbContext.Properties
                .Join(_dbContext.PropertyAvailabilities,
                      p => p.Id,
                      a => a.PropertyId,
                      (p, a) => new { Property = p, Availability = a })
                .Where(pa => pa.Availability.IsAvailable)
                .Select(pa => $"{pa.Property.Title} in {pa.Property.Address} is available on {pa.Availability.Date.ToShortDateString()}")
                .Take(5)
                .ToListAsync();

            return availableProperties.Any() ? string.Join("\n", availableProperties) : "No available properties found.";
        }
    }
}
