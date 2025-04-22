using System;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text.Json;

namespace API.Services.AIRepo
{
    public class GoogleCalendarService
    {
        private readonly CalendarService _calendarService;
        private readonly string _applicationName = "Airbnb Clone Calendar Integration";

        public GoogleCalendarService(IOptions<GoogleCalendarConfiguration> config)
        {
            try
            {
                // Try multiple possible locations for the credentials file
                string[] possiblePaths = new string[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "airbnb-clone-455917-d9e02a79f618.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "airbnb-clone-455917-d9e02a79f618.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "airbnb-clone-455917-d9e02a79f618.json")
                };

                string credentialsJson = null;
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        credentialsJson = File.ReadAllText(path);
                        break;
                    }
                }

                if (string.IsNullOrEmpty(credentialsJson))
                {
                    throw new FileNotFoundException("Google Calendar credentials file not found in any of the expected locations.");
                }
                
                var credentials = GoogleCredential.FromJson(credentialsJson)
                    .CreateScoped(CalendarService.Scope.Calendar);

                _calendarService = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credentials,
                    ApplicationName = _applicationName
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Google Calendar service: {ex.Message}");
                throw new InvalidOperationException("Failed to initialize Google Calendar service. Please check your credentials.", ex);
            }
        }

        public async Task<string> AddBookingToCalendarAsync(string calendarId, string summary, string description, DateTime startDate, DateTime endDate)
        {
            try
            {
                var eventItem = new Event
                {
                    Summary = summary,
                    Description = description,
                    Start = new EventDateTime
                    {
                        DateTime = startDate,
                        TimeZone = "UTC"
                    },
                    End = new EventDateTime
                    {
                        DateTime = endDate,
                        TimeZone = "UTC"
                    }
                };

                var createdEvent = await _calendarService.Events.Insert(eventItem, calendarId).ExecuteAsync();
                return createdEvent.HtmlLink;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding event to Google Calendar: {ex.Message}");
                throw new Exception($"Failed to add event to Google Calendar: {ex.Message}", ex);
            }
        }
    }
} 