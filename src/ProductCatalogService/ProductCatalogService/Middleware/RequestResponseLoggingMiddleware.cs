using System.Text;

namespace ProductCatalogService.Middleware;

/// <summary>
/// Middleware for logging detailed request and response information
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly bool _logRequestBody;
    private readonly bool _logResponseBody;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _logRequestBody = configuration.GetValue<bool>("Logging:LogRequestBody", false);
        _logResponseBody = configuration.GetValue<bool>("Logging:LogResponseBody", false);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for certain paths
        if (ShouldSkipLogging(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Log request
        await LogRequestAsync(context.Request);

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);

            // Log response
            await LogResponseAsync(context.Response);

            // Copy response back to original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task LogRequestAsync(HttpRequest request)
    {
        var requestInfo = new StringBuilder();
        requestInfo.AppendLine($"Request: {request.Method} {request.Path}{request.QueryString}");
        requestInfo.AppendLine($"Headers: {string.Join(", ", request.Headers.Select(h => $"{h.Key}={h.Value}"))}");

        if (_logRequestBody && request.ContentLength > 0)
        {
            request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];
            await request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length));
            var bodyAsText = Encoding.UTF8.GetString(buffer);
            requestInfo.AppendLine($"Body: {bodyAsText}");
            request.Body.Position = 0;
        }

        _logger.LogDebug(requestInfo.ToString());
    }

    private async Task LogResponseAsync(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        var bodyAsText = await new StreamReader(response.Body).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);

        var responseInfo = new StringBuilder();
        responseInfo.AppendLine($"Response: Status {response.StatusCode}");
        responseInfo.AppendLine($"Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={h.Value}"))}");

        if (_logResponseBody && !string.IsNullOrEmpty(bodyAsText))
        {
            responseInfo.AppendLine($"Body: {bodyAsText}");
        }

        _logger.LogDebug(responseInfo.ToString());
    }

    private bool ShouldSkipLogging(PathString path)
    {
        // Skip logging for health checks, metrics, and static files
        return path.StartsWithSegments("/health") ||
               path.StartsWithSegments("/metrics") ||
               path.StartsWithSegments("/hangfire") ||
               path.Value?.Contains("swagger") == true;
    }
}
