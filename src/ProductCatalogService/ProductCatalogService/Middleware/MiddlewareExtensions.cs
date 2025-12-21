namespace ProductCatalogService.Middleware;

/// <summary>
/// Extension methods for registering middleware
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Adds correlation ID middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }

    /// <summary>
    /// Adds request logging middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }

    /// <summary>
    /// Adds global exception handling middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }

    /// <summary>
    /// Adds rate limiting middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }

    /// <summary>
    /// Adds security headers middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }

    /// <summary>
    /// Adds performance monitoring middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PerformanceMonitoringMiddleware>();
    }

    /// <summary>
    /// Adds detailed request/response logging middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
    }

    /// <summary>
    /// Adds API version middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseApiVersioning(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiVersionMiddleware>();
    }

    /// <summary>
    /// Adds all custom middleware to the pipeline in the recommended order
    /// </summary>
    public static IApplicationBuilder UseCustomMiddleware(this IApplicationBuilder builder)
    {
        // Order matters! These are applied in sequence
        
        // 1. Correlation ID - should be first to track requests
        builder.UseCorrelationId();
        
        // 2. Exception handling - catch all exceptions
        builder.UseGlobalExceptionHandler();
        
        // 3. Security headers - add security headers early
        builder.UseSecurityHeaders();
        
        // 4. Performance monitoring - track performance
        builder.UsePerformanceMonitoring();
        
        // 5. Rate limiting - prevent abuse
        builder.UseRateLimiting();
        
        // 6. Request logging - log requests
        builder.UseRequestLogging();
        
        // 7. API versioning - add version headers
        builder.UseApiVersioning();
        
        // 8. Detailed logging (optional, only in development)
        // builder.UseRequestResponseLogging();
        
        return builder;
    }
}
