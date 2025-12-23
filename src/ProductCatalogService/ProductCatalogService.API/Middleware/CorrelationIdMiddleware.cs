namespace ProductCatalogService.Middleware;

/// <summary>
/// Middleware for adding correlation IDs to track requests across services
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if correlation ID exists in request headers
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();

        // If not, generate a new one
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        // Add to response headers
        context.Response.Headers.Append(CorrelationIdHeader, correlationId);

        // Add to HttpContext items for use in logging and other middleware
        context.Items["CorrelationId"] = correlationId;

        // Add to trace identifier for better logging
        context.TraceIdentifier = correlationId;

        await _next(context);
    }
}
