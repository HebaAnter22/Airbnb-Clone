using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using API.Services.AIRepo;
using API.DTOs.AI;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using System.IO;
using System;
using Microsoft.AspNetCore.Http;

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

        /// <summary>
        /// Transcribes an audio file to text using OpenAI's audio transcription API
        /// </summary>
        /// <param name="audioFile">The audio file to transcribe (WAV or MP3 format)</param>
        /// <returns>The transcribed text from the audio file</returns>
        /// <response code="200">Returns the transcribed text</response>
        /// <response code="400">If the file is missing or empty</response>
        /// <response code="500">If there was an error processing the file</response>
        [HttpPost("transcribe")]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AIResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AIResponseDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(AIResponseDTO), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> TranscribeAudio(IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
            {
                return BadRequest(new AIResponseDTO
                {
                    Success = false,
                    ErrorMessage = "No audio file provided.",
                    Timestamp = DateTime.UtcNow,
                    RequestId = Guid.NewGuid().ToString()
                });
            }

            using var memoryStream = new MemoryStream();
            await audioFile.CopyToAsync(memoryStream);
            var audioData = memoryStream.ToArray();

            var response = await _openAIService.TranscribeAudioAsync(audioData, audioFile.FileName);
            return Ok(response);
        }

        /// <summary>
        /// Converts text to speech using OpenAI's text-to-speech API
        /// </summary>
        /// <param name="request">The text to convert to speech</param>
        /// <returns>Base64-encoded audio data</returns>
        /// <response code="200">Returns the audio as base64</response>
        /// <response code="400">If the request is invalid</response>
        /// <response code="500">If there was an error processing the request</response>
        [HttpPost("text-to-speech")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AIResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AIResponseDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(AIResponseDTO), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> TextToSpeech([FromBody] AIRequestDTO request)
        {
            Console.WriteLine($"Text-to-speech request received with content type: {Request.ContentType}");
            Console.WriteLine($"Query text: {request?.Query?.Substring(0, Math.Min(30, request?.Query?.Length ?? 0))}...");
            
            if (request == null)
            {
                Console.WriteLine("Request body is null");
                return BadRequest(new AIResponseDTO
                {
                    Success = false,
                    ErrorMessage = "Request body cannot be null.",
                    Timestamp = DateTime.UtcNow,
                    RequestId = Guid.NewGuid().ToString()
                });
            }
            
            if (string.IsNullOrEmpty(request.Query))
            {
                Console.WriteLine("Query text is empty");
                return BadRequest(new AIResponseDTO
                {
                    Success = false,
                    ErrorMessage = "Text cannot be empty.",
                    Timestamp = DateTime.UtcNow,
                    RequestId = Guid.NewGuid().ToString()
                });
            }

            try
            {
                Console.WriteLine("Calling OpenAIService.TextToSpeechAsync");
                var response = await _openAIService.TextToSpeechAsync(request.Query);
                
                if (response.Success)
                {
                    Console.WriteLine($"Text-to-speech successful: {response.Response?.Length ?? 0} bytes returned");
                    return Ok(response);
                }
                else
                {
                    Console.WriteLine($"Text-to-speech failed: {response.ErrorMessage}");
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in TextToSpeech controller: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
                }
                
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

