namespace ProductCatalogService.Middleware;

/// <summary>
/// Middleware for API versioning headers
/// </summary>
public class ApiVersionMiddleware
{
    private readonly RequestDelegate _next;
    private const string ApiVersionHeader = "X-API-Version";
    private const string CurrentVersion = "1.0";

    public ApiVersionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add API version to response headers
        context.Response.Headers.Append(ApiVersionHeader, CurrentVersion);

        // Add deprecation warning if using old version
        var requestedVersion = context.Request.Headers[ApiVersionHeader].FirstOrDefault();
        if (!string.IsNullOrEmpty(requestedVersion) && requestedVersion != CurrentVersion)
        {
            context.Response.Headers.Append("X-API-Deprecated", "true");
            context.Response.Headers.Append("X-API-Deprecation-Info", 
                $"Version {requestedVersion} is deprecated. Please upgrade to version {CurrentVersion}");
        }

        await _next(context);
    }
}
