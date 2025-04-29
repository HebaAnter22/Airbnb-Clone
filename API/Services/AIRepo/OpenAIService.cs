using OpenAI;
using OpenAI.Chat;
using OpenAI.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs.AI;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using API.Data;
using System.IO;

namespace API.Services.AIRepo
{
    public class OpenAIService : IOpenAIService
    {
        private readonly ChatClient _chatClient;
        private readonly AudioClient _transcriptionClient;
        private readonly AudioClient _speechClient;
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
            
            // Initialize different clients for different purposes
            _chatClient = new ChatClient("gpt-4o-mini", _config.ApiKey);
            _transcriptionClient = new AudioClient("whisper-1", _config.ApiKey);
            _speechClient = new AudioClient("tts-1", _config.ApiKey);
        }

        public async Task<AIResponseDTO> GetAIResponseAsync(AIRequestDTO request)
        {
            try
            {
                // Fetch relevant data from database based on request type
                string dataContext = await FetchDataBasedOnRequestType(request.RequestType, request.Query);
                
                // Create a more directive prompt that encourages including specific property data
                string enhancedQuery = $"Database results: {dataContext}\n\nUser Query: {request.Query}\n\nIMPORTANT: Always include specific property listings with their details in your response when available from the database results.";

                var chatMessages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are a professional AI assistant for an Airbnb clone application. Your role is to provide accurate, helpful, and detailed responses to user queries regarding property listings, booking assistance, availability insights, and general information about our platform. ALWAYS include specific property listings from the database in your responses when available. Format them clearly and present the actual properties with their details rather than generic advice. When users ask about properties in specific locations or with specific features, show them the actual listings from our database."),
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
                    return await FetchBookingData(userQuery);
                case "availability":
                    return await FetchAvailabilityData(userQuery);
                default:
                    return await FetchContextualData(userQuery);
            }
        }

        private async Task<string> FetchPropertyDetails(string userQuery)
        {
            // Extract location from query - prioritize this extraction
            string locationFilter = ExtractLocation(userQuery);
            
            // If no explicit location found using keywords, try to find any location mentions
            if (string.IsNullOrEmpty(locationFilter))
            {
                // Common city/country names that might appear in queries
                var commonLocations = new[] { "cairo", "egypt", "london", "paris", "new york", "tokyo", "dubai", "rome", "berlin" };
                foreach (var location in commonLocations)
                {
                    if (userQuery.ToLower().Contains(location))
                    {
                        locationFilter = location;
                        break;
                    }
                }
            }

            // Extract potential amenity keywords
            var amenityKeywords = new[] { "pool", "wifi", "kitchen", "parking", "ac", "air conditioning", "balcony", "gym", "beach" };
            var mentionedAmenities = amenityKeywords.Where(a => userQuery.ToLower().Contains(a)).ToList();

            // Build query based on extracted filters
            var query = _dbContext.Properties
                .Include(p => p.Host)
                .Include(p => p.Amenities)
                .Include(p => p.PropertyImages)
                .Include(p => p.Category)
                .Include(p => p.CancellationPolicy)
                .Where(p => p.Status == "Active")
                .AsQueryable();

            // Apply location filter if found
            if (!string.IsNullOrEmpty(locationFilter))
            {
                query = query.Where(p => p.City.Contains(locationFilter) || 
                                       p.Country.Contains(locationFilter) || 
                                       p.Address.Contains(locationFilter));
                
                Console.WriteLine($"Filtering properties by location: {locationFilter}");
            }

            // Apply amenity filters if mentioned
            if (mentionedAmenities.Any())
            {
                foreach (var amenity in mentionedAmenities)
                {
                    query = query.Where(p => p.Amenities.Any(a => a.Name.Contains(amenity)));
                }
            }

            // Limit results but ensure we return some properties
            var properties = await query.Take(5).ToListAsync();

            // If no properties found with filters, return some properties anyway
            if (!properties.Any())
            {
                Console.WriteLine("No properties found with filters, returning general properties");
                properties = await _dbContext.Properties
                    .Include(p => p.Host)
                    .Include(p => p.Amenities)
                    .Include(p => p.PropertyImages)
                    .Include(p => p.Category)
                    .Where(p => p.Status == "Active")
                    .Take(5)
                    .ToListAsync();
            }

            if (!properties.Any())
            {
                return "No properties found in our database. Please try a different search.";
            }

            var propertyDetails = new List<string>();
            foreach (var property in properties)
            {
                var hostName = property.Host != null ? $" hosted by {property.Host.HostId}" : "";
                var amenities = property.Amenities != null && property.Amenities.Any() 
                    ? $" with amenities: {string.Join(", ", property.Amenities.Select(a => a.Name).Take(5))}" 
                    : "";
                var category = property.Category != null ? $", type: {property.Category.Name}" : "";
                var cancellation = property.CancellationPolicy != null ? $", cancellation policy: {property.CancellationPolicy.Name}" : "";

                // Add image URL if available
                var imageUrl = property.PropertyImages != null && property.PropertyImages.Any() && property.PropertyImages.FirstOrDefault(i => i.IsPrimary) != null
                    ? property.PropertyImages.FirstOrDefault(i => i.IsPrimary).ImageUrl
                    : (property.PropertyImages != null && property.PropertyImages.Any() ? property.PropertyImages.First().ImageUrl : "No image available");

                propertyDetails.Add($"PROPERTY LISTING: {property.Title}{hostName} located in {property.Address}, {property.City}, {property.Country}{category}" +
                    $" priced at {property.PricePerNight} {property.Currency} per night{amenities}{cancellation}. " +
                    $"Max guests: {property.MaxGuests}, Bedrooms: {property.Bedrooms}, Bathrooms: {property.Bathrooms}. " +
                    $"Image: {imageUrl}");
            }

            return string.Join("\n\n", propertyDetails);
        }

        private string ExtractLocation(string userQuery)
        {
            var locationKeywords = new[] { "in", "near", "at", "around", "close to" };
            string locationFilter = "";
            
            foreach (var keyword in locationKeywords)
            {
                var pattern = $"{keyword} ([\\w\\s]+)";
                var match = System.Text.RegularExpressions.Regex.Match(userQuery.ToLower(), pattern);
                if (match.Success)
                {
                    locationFilter = match.Groups[1].Value.Trim();
                    break;
                }
            }
            
            return locationFilter;
        }

        private async Task<string> FetchBookingData(string userQuery)
        {
            // Look for booking-related information based on query
            try
            {
                // For demonstration, we'll get recent bookings for context
                var recentBookings = await _dbContext.Bookings
                    .Include(b => b.Property)
                    .Include(b => b.Guest)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(5)
                    .Select(b => new
                    {
                        BookingId = b.Id,
                        PropertyName = b.Property.Title,
                        Location = $"{b.Property.City}, {b.Property.Country}",
                        StartDate = b.StartDate.ToString("yyyy-MM-dd"),
                        EndDate = b.EndDate.ToString("yyyy-MM-dd"),
                        Status = b.Status,
                        GuestName = $"{b.Guest.FirstName} {b.Guest.LastName}",
                        TotalAmount = b.TotalAmount
                    })
                    .ToListAsync();

                if (!recentBookings.Any())
                {
                    return "No recent booking information found.";
                }

                var bookingInfo = new List<string>();
                foreach (var booking in recentBookings)
                {
                    bookingInfo.Add($"Booking #{booking.BookingId} for {booking.PropertyName} in {booking.Location} " +
                        $"from {booking.StartDate} to {booking.EndDate}. Status: {booking.Status}. " +
                        $"Guest: {booking.GuestName}. Total: {booking.TotalAmount}.");
                }

                return "Recent booking information:\n\n" + string.Join("\n\n", bookingInfo);
            }
            catch
            {
                return "Unable to retrieve booking information due to an error.";
            }
        }

        private async Task<string> FetchAvailabilityData(string userQuery)
        {
            try
            {
                // Extract potential date ranges from query
                DateTime? startDate = null;
                DateTime? endDate = null;

                // Simple date extraction - could be enhanced with more sophisticated NLP
                var datePattern = @"(\d{1,2}[\/\-\.]\d{1,2}[\/\-\.]\d{2,4})";
                var dateMatches = System.Text.RegularExpressions.Regex.Matches(userQuery, datePattern);
                
                if (dateMatches.Count >= 1)
                {
                    if (DateTime.TryParse(dateMatches[0].Value, out var parsedDate))
                        startDate = parsedDate;
                    
                    if (dateMatches.Count >= 2 && DateTime.TryParse(dateMatches[1].Value, out parsedDate))
                        endDate = parsedDate;
                }

                // Extract location if mentioned
                string locationFilter = "";
                var locationKeywords = new[] { "in", "near", "at", "around" };
                
                foreach (var keyword in locationKeywords)
                {
                    var pattern = $"{keyword} ([\\w\\s]+)";
                    var match = System.Text.RegularExpressions.Regex.Match(userQuery.ToLower(), pattern);
                    if (match.Success)
                    {
                        locationFilter = match.Groups[1].Value.Trim();
                        break;
                    }
                }

                // Query available properties
                var query = _dbContext.Properties
                    .Include(p => p.Availabilities)
                    .Where(p => p.Status == "Active")
                    .AsQueryable();

                // Apply location filter if found
                if (!string.IsNullOrEmpty(locationFilter))
                {
                    query = query.Where(p => p.City.Contains(locationFilter) || 
                                            p.Country.Contains(locationFilter));
                }

                var properties = await query.Take(10).ToListAsync();
                
                var availabilityData = new List<string>();
                foreach (var property in properties)
                {
                    var availabilityDates = property.Availabilities
                        .Where(a => a.IsAvailable)
                        .Where(a => startDate == null || a.Date >= startDate)
                        .Where(a => endDate == null || a.Date <= endDate)
                        .OrderBy(a => a.Date)
                        .Take(7)
                        .ToList();

                    if (availabilityDates.Any())
                    {
                        var dates = string.Join(", ", availabilityDates.Select(a => a.Date.ToString("yyyy-MM-dd")));
                        availabilityData.Add($"{property.Title} in {property.City}, {property.Country} is available on: {dates} " +
                            $"at {property.PricePerNight} {property.Currency} per night.");
                    }
                }

                if (!availabilityData.Any())
                {
                    return startDate != null || endDate != null || !string.IsNullOrEmpty(locationFilter) 
                        ? "No available properties found matching your criteria." 
                        : "Here are some general availability insights:\n\n" + await FetchGeneralAvailabilityInsights();
                }

                return string.Join("\n\n", availabilityData);
            }
            catch
            {
                return "Unable to retrieve availability information due to an error.";
            }
        }

        private async Task<string> FetchGeneralAvailabilityInsights()
        {
            // Provide general insights on property availability
            try
            {
                // Count properties by city
                var topCities = await _dbContext.Properties
                    .Where(p => p.Status == "Active")
                    .GroupBy(p => p.City)
                    .Select(g => new { City = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToListAsync();

                // Get average price by city
                var avgPriceByCity = await _dbContext.Properties
                    .Where(p => p.Status == "Active")
                    .GroupBy(p => p.City)
                    .Select(g => new { City = g.Key, AvgPrice = g.Average(p => p.PricePerNight) })
                    .OrderByDescending(x => x.AvgPrice)
                    .Take(5)
                    .ToListAsync();

                var insights = new List<string>();
                
                if (topCities.Any())
                {
                    insights.Add($"Top cities by property count: {string.Join(", ", topCities.Select(c => $"{c.City} ({c.Count})"))}");
                }
                
                if (avgPriceByCity.Any())
                {
                    insights.Add($"Average prices by city: {string.Join(", ", avgPriceByCity.Select(c => $"{c.City} (${c.AvgPrice:F2})"))}");
                }

                // Add more insights as needed
                return string.Join("\n\n", insights);
            }
            catch
            {
                return "General availability insights not available at the moment.";
            }
        }

        private async Task<string> FetchContextualData(string userQuery)
        {
            // For general queries, try to provide some context from multiple tables
            var context = new List<string>();
            
            try
            {
                // Get popular property types
                var propertyTypes = await _dbContext.Properties
                    .Where(p => p.Status == "Active")
                    .GroupBy(p => p.PropertyType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(3)
                    .ToListAsync();
                    
                if (propertyTypes.Any())
                {
                    context.Add($"Popular property types: {string.Join(", ", propertyTypes.Select(t => t.Type))}");
                }
                
                // Get top amenities
                var amenities = await _dbContext.Amenities
                    .Take(5)
                    .Select(a => a.Name)
                    .ToListAsync();
                    
                if (amenities.Any())
                {
                    context.Add($"Popular amenities: {string.Join(", ", amenities)}");
                }
                
                // Get cancellation policies
                var policies = await _dbContext.CancellationPolicies
                    .Take(3)
                    .Select(p => $"{p.Name} ({p.RefundPercentage}% refund)")
                    .ToListAsync();
                    
                if (policies.Any())
                {
                    context.Add($"Available cancellation policies: {string.Join(", ", policies)}");
                }
                
                return string.Join("\n\n", context);
            }
            catch
            {
                return "Unable to retrieve contextual information.";
            }
        }

        public async Task<AIResponseDTO> TranscribeAudioAsync(byte[] audioData, string fileName)
        {
            try
            {
                Console.WriteLine($"Starting audio transcription for file: {fileName}");
                using var stream = new MemoryStream(audioData);
                
                try
                {
                    // Create AudioTranscriptionOptions
                    var options = new AudioTranscriptionOptions
                    {
                        Language = "en"
                    };
                    
                    // Call the transcription API using the dedicated transcription client
                    var result = await _transcriptionClient.TranscribeAudioAsync(stream, fileName, options);
                    var response = result.Value;
                    
                    if (!string.IsNullOrEmpty(response.Text))
                    {
                        Console.WriteLine($"Successfully transcribed audio: {response.Text.Substring(0, Math.Min(30, response.Text.Length))}...");
                        
                        return new AIResponseDTO
                        {
                            Response = response.Text,
                            RequestId = Guid.NewGuid().ToString(),
                            Timestamp = DateTime.UtcNow,
                            Success = true
                        };
                    }
                    else
                    {
                        throw new Exception("No transcription returned from OpenAI API");
                    }
                }
                catch (Exception innerEx)
                {
                    Console.WriteLine($"OpenAI API Error in transcription: {innerEx.Message}");
                    throw new Exception($"OpenAI API Error in transcription: {innerEx.Message}", innerEx);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in audio transcription: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                return new AIResponseDTO
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        public async Task<AIResponseDTO> TextToSpeechAsync(string text)
        {
            try
            {
                Console.WriteLine($"Starting text-to-speech for text: {text.Substring(0, Math.Min(30, text.Length))}...");
                
                // Define voice - using Alloy which is one of the most reliable voices
                var voice = GeneratedSpeechVoice.Alloy;
                
                // Call OpenAI's text-to-speech API with minimal options
                var options = new SpeechGenerationOptions
                {
                    SpeedRatio = 1.0f
                };

                try
                {
                    // Use the dedicated speech client
                    Console.WriteLine("Calling OpenAI GenerateSpeechAsync API...");
                    var result = await _speechClient.GenerateSpeechAsync(text, voice, options);
                    
                    if (result == null || result.Value == null)
                    {
                        throw new Exception("OpenAI returned null response for text-to-speech");
                    }
                    
                    var audioData = result.Value;
                    Console.WriteLine($"Received audio data byte length: {audioData.ToArray().Length}");
                    
                    // Convert to base64 for response
                    string base64Audio = Convert.ToBase64String(audioData.ToArray());
                    Console.WriteLine($"Base64 encoded data length: {base64Audio.Length}");
                    
                    // Verify the base64 data is valid
                    try
                    {
                        // Attempt to decode a small portion to verify it's valid base64
                        var testDecode = Convert.FromBase64String(base64Audio.Substring(0, Math.Min(100, base64Audio.Length)));
                        Console.WriteLine("Base64 validation successful");
                    }
                    catch (FormatException ex)
                    {
                        Console.WriteLine($"Invalid base64 format: {ex.Message}");
                        throw new Exception("Generated audio could not be properly encoded as base64", ex);
                    }
                    
                    return new AIResponseDTO
                    {
                        Response = base64Audio,
                        RequestId = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.UtcNow,
                        Success = true
                    };
                }
                catch (Exception innerEx)
                {
                    Console.WriteLine($"OpenAI API Error in text-to-speech: {innerEx.Message}");
                    Console.WriteLine($"Inner exception stack trace: {innerEx.StackTrace}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in text-to-speech: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                return new AIResponseDTO
                {
                    Success = false,
                    ErrorMessage = $"Text-to-speech error: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    RequestId = Guid.NewGuid().ToString()
                };
            }
        }
    }
}
