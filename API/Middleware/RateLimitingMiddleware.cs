using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace API.Middleware
{
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RateLimitMiddleware> _logger;
        private readonly int _maxRequests;
        private readonly TimeSpan _window;

        public RateLimitMiddleware(
            RequestDelegate next,
            IMemoryCache cache,
            ILogger<RateLimitMiddleware> logger,
            int maxRequests = 100,
            int windowInSeconds = 60)
        {
            _next = next;
            _cache = cache;
            _logger = logger;
            _maxRequests = maxRequests;
            _window = TimeSpan.FromSeconds(windowInSeconds);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Get client identifier
            var clientId = GetClientIdentifier(context);
            var cacheKey = $"RateLimit_{clientId}";
            var path = context.Request.Path;

            _logger.LogDebug("Rate limit check for client {ClientId} on path {Path}", clientId, path);

            // Try to get client's request count from cache
            if (!_cache.TryGetValue<ClientStatistics>(cacheKey, out var stats))
            {
                _logger.LogInformation("New rate limit window started for client {ClientId}", clientId);

                // First request from this client
                stats = new ClientStatistics
                {
                    RequestCount = 1,
                    FirstRequestTime = DateTime.UtcNow
                };

                _cache.Set(cacheKey, stats, _window);
                await _next(context);
                return;
            }

            // Check if the time window has expired
            var timeSinceFirstRequest = DateTime.UtcNow - stats.FirstRequestTime;
            if (timeSinceFirstRequest > _window)
            {
                _logger.LogInformation("Rate limit window expired for client {ClientId}. Resetting counter.", clientId);

                // Reset for a new window
                stats.RequestCount = 1;
                stats.FirstRequestTime = DateTime.UtcNow;
                _cache.Set(cacheKey, stats, _window);
                await _next(context);
                return;
            }

            // Increment request count
            stats.RequestCount++;
            var remainingWindow = _window - timeSinceFirstRequest;
            _cache.Set(cacheKey, stats, remainingWindow);

            _logger.LogDebug("Request #{RequestCount} from client {ClientId} in current window. {RemainingSeconds}s remaining.",
                stats.RequestCount, clientId, remainingWindow.TotalSeconds);

            // Check if max requests exceeded
            if (stats.RequestCount > _maxRequests)
            {
                var retryAfter = remainingWindow;
                _logger.LogWarning("Rate limit exceeded for client {ClientId}. {RequestCount} requests in {ElapsedSeconds}s. Blocking for {RetryAfter}s.",
                    clientId, stats.RequestCount, timeSinceFirstRequest.TotalSeconds, retryAfter.TotalSeconds);

                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers.Add("Retry-After", ((int)retryAfter.TotalSeconds).ToString());
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsync(
                    $"{{\"error\": \"Too many requests. Try again after {(int)retryAfter.TotalSeconds} seconds.\"}}");
                return;
            }

            await _next(context);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // Simple example using IP address
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // For API key authentication, you might use:
            // var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
            // return apiKey ?? ipAddress;

            return ipAddress;
        }
    }

    public class ClientStatistics
    {
        public int RequestCount { get; set; }
        public DateTime FirstRequestTime { get; set; }
    }

    public static class RateLimitExtensions
    {
        public static IServiceCollection AddRateLimiting(this IServiceCollection services)
        {
            services.AddMemoryCache();
            return services;
        }

        public static IApplicationBuilder UseRateLimiting(
            this IApplicationBuilder app,
            int maxRequests = 100,
            int windowInSeconds = 60)
        {
            return app.UseMiddleware<RateLimitMiddleware>(
                app.ApplicationServices.GetRequiredService<IMemoryCache>(),
                app.ApplicationServices.GetRequiredService<ILogger<RateLimitMiddleware>>(),
                maxRequests,
                windowInSeconds);
        }
    }
}