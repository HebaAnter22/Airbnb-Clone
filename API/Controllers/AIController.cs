using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using API.Services.AIRepo;
using API.DTOs.AI;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace AirbnbClone.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIController : ControllerBase
    {
        private readonly IOpenAIService _openAIService;

        public AIController(IOpenAIService openAIService)
        {
            _openAIService = openAIService;
        }

        //[HttpPost("chat")]
        //public async Task<IActionResult> Chat([FromBody] AIRequestDTO request)
        //{
        //    if (string.IsNullOrEmpty(request.Query))
        //    {
        //        return BadRequest(new { Error = "User input cannot be empty." });
        //    }

        //    var response = await _openAIService.GetAIResponseAsync(request);
        //    return Ok(response);
        //}

            [HttpPost]
            public async Task<IActionResult> ProcessAIRequest([FromBody] AIRequestDTO request)
            {
                if (request == null)
                    return BadRequest(new AIResponseDTO
                    {
                        Success = false,
                        ErrorMessage = "Request body cannot be null.",
                        Timestamp = DateTime.UtcNow,
                        RequestId = Guid.NewGuid().ToString()
                    });
                if (string.IsNullOrEmpty(request.Query))
                    return BadRequest(new AIResponseDTO
                    {
                        Success = false,
                        ErrorMessage = "User input cannot be empty.",
                        Timestamp = DateTime.UtcNow,
                        RequestId = Guid.NewGuid().ToString()
                    });
                if (string.IsNullOrEmpty(request.RequestType))
                    return BadRequest(new AIResponseDTO
                    {
                        Success = false,
                        ErrorMessage = "Request type cannot be empty.",
                        Timestamp = DateTime.UtcNow,
                        RequestId = Guid.NewGuid().ToString()
                    });

                //if (string.IsNullOrEmpty(request.UserId))
                //    return BadRequest(new AIResponseDTO
                //    {
                //        Success = false,
                //        ErrorMessage = "UserId cannot be empty.",
                //        Timestamp = DateTime.UtcNow,
                //        RequestId = Guid.NewGuid().ToString()
                //    });

                // Validate UserId against JWT claim
                //var jwtUserId = User.FindFirst("")?.Value;
                //if (jwtUserId != request.UserId)
                //    return Unauthorized(new AIResponseDTO
                //    {
                //        Success = false,
                //        ErrorMessage = "UserId does not match authenticated user.",
                //        Timestamp = DateTime.UtcNow,
                //        RequestId = Guid.NewGuid().ToString()
                //    });

                try
                {
                    Console.WriteLine($"Received request: {JsonSerializer.Serialize(request)}");
                    AIResponseDTO response = request.RequestType.ToLower() switch
                    {
                        "chat" => await _openAIService.GetAIResponseAsync(request),
                        "property" => await _openAIService.GetPropertyRecommendationsAsync(request.Query),
                        "booking" => await _openAIService.GetBookingAssistanceAsync(request.Query),
                        "availability" => await _openAIService.GetAvailabilityInsightsAsync(request.Query),
                        _ => await _openAIService.GetAIResponseAsync(request),
                        //=> new AIResponseDTO
                        //{
                        //    Success = false,
                        //    ErrorMessage = "Invalid request type. Supported types: chat, property, booking, availability.",
                        //    Timestamp = DateTime.UtcNow,
                        //    RequestId = Guid.NewGuid().ToString()
                        //}
                    };

                    Console.WriteLine($"Response: {JsonSerializer.Serialize(response)}");
                    if (!response.Success)
                        return BadRequest(response);

                    return Ok(response);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing AI request: {ex.Message}");
                    return StatusCode(500, new AIResponseDTO
                    {
                        Success = false,
                        ErrorMessage = $"Server error: {ex.Message}",
                        Timestamp = DateTime.UtcNow,
                        RequestId = Guid.NewGuid().ToString()
                    });
                }
            }
        }
}

