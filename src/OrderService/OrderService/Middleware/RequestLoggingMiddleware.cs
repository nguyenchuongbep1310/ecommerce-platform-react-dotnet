using System.Diagnostics;
using System.Security.Claims;

namespace OrderService.Middleware;

/// <summary>
/// Middleware for logging HTTP requests and responses with timing information
/// Integrates with CorrelationIdMiddleware for distributed tracing
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private static readonly HashSet<string> SensitivePaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/orders/place"
    };

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for health checks and metrics
        if (ShouldSkipLogging(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        
        // Get correlation ID from context (set by CorrelationIdMiddleware or TraceIdentifier)
        var correlationId = context.TraceIdentifier;
        
        // Extract user information if authenticated
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userEmail = context.User?.FindFirst(ClaimTypes.Email)?.Value;
        
        // Determine if this is a sensitive endpoint
        var isSensitive = IsSensitivePath(context.Request.Path);
        
        // Log request with structured data
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = userId ?? "Anonymous",
            ["UserEmail"] = userEmail ?? "Anonymous",
            ["ClientIp"] = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            ["UserAgent"] = context.Request.Headers["User-Agent"].ToString()
        }))
        {
            _logger.LogInformation(
                "HTTP Request: {Method} {Path}{QueryString} | User: {UserId} | IP: {ClientIp}",
                context.Request.Method,
                context.Request.Path,
                isSensitive ? "" : context.Request.QueryString.ToString(),
                userId ?? "Anonymous",
                context.Connection.RemoteIpAddress?.ToString() ?? "Unknown");

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                
                // Determine log level based on status code
                var logLevel = GetLogLevel(context.Response.StatusCode);
                
                // Log response with structured data
                _logger.Log(
                    logLevel,
                    "HTTP Response: {Method} {Path} | Status: {StatusCode} | Duration: {ElapsedMs}ms | User: {UserId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    userId ?? "Anonymous");
                
                // Log slow requests as warnings
                if (stopwatch.ElapsedMilliseconds > 3000)
                {
                    _logger.LogWarning(
                        "Slow Request Detected: {Method} {Path} took {ElapsedMs}ms | User: {UserId}",
                        context.Request.Method,
                        context.Request.Path,
                        stopwatch.ElapsedMilliseconds,
                        userId ?? "Anonymous");
                }
            }
        }
    }

    private bool ShouldSkipLogging(PathString path)
    {
        return path.StartsWithSegments("/health") ||
               path.StartsWithSegments("/metrics") ||
               path.Value?.Contains("swagger", StringComparison.OrdinalIgnoreCase) == true;
    }

    private bool IsSensitivePath(PathString path)
    {
        return SensitivePaths.Contains(path.Value ?? string.Empty);
    }

    private LogLevel GetLogLevel(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };
    }
}
