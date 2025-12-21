using System.Collections.Concurrent;
using System.Net;

namespace ProductCatalogService.Middleware;

/// <summary>
/// Rate limiting middleware to prevent abuse
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, ClientRequestInfo> _clients = new();
    
    private readonly int _requestLimit;
    private readonly TimeSpan _timeWindow;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        
        // Read from configuration or use defaults
        _requestLimit = configuration.GetValue<int>("RateLimiting:RequestLimit", 100);
        _timeWindow = TimeSpan.FromMinutes(configuration.GetValue<int>("RateLimiting:TimeWindowMinutes", 1));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for health checks and Hangfire dashboard
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/hangfire"))
        {
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        var clientInfo = _clients.GetOrAdd(clientId, _ => new ClientRequestInfo());

        lock (clientInfo)
        {
            var now = DateTime.UtcNow;
            
            // Remove old requests outside the time window
            clientInfo.RequestTimestamps.RemoveAll(timestamp => 
                now - timestamp > _timeWindow);

            if (clientInfo.RequestTimestamps.Count >= _requestLimit)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for client {ClientId}. Limit: {Limit} requests per {TimeWindow}",
                    clientId,
                    _requestLimit,
                    _timeWindow);

                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers.Append("Retry-After", _timeWindow.TotalSeconds.ToString());
                
                await context.Response.WriteAsJsonAsync(new
                {
                    statusCode = 429,
                    message = "Rate limit exceeded. Please try again later.",
                    retryAfter = _timeWindow.TotalSeconds
                });
                
                return;
            }

            clientInfo.RequestTimestamps.Add(now);
        }

        // Add rate limit headers
        var remainingRequests = _requestLimit - clientInfo.RequestTimestamps.Count;
        context.Response.Headers.Append("X-RateLimit-Limit", _requestLimit.ToString());
        context.Response.Headers.Append("X-RateLimit-Remaining", remainingRequests.ToString());
        context.Response.Headers.Append("X-RateLimit-Reset", 
            DateTime.UtcNow.Add(_timeWindow).ToString("o"));

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get client IP from X-Forwarded-For header (for proxies/load balancers)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Fall back to remote IP address
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    // Cleanup old entries periodically (could be moved to a background service)
    public static void CleanupOldEntries()
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-1);
        var keysToRemove = _clients
            .Where(kvp => kvp.Value.RequestTimestamps.All(t => t < cutoffTime))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _clients.TryRemove(key, out _);
        }
    }
}

public class ClientRequestInfo
{
    public List<DateTime> RequestTimestamps { get; set; } = new();
}
