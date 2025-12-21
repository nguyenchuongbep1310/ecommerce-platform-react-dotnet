using System.Diagnostics;

namespace ProductCatalogService.Middleware;

/// <summary>
/// Middleware for tracking API performance metrics
/// </summary>
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private readonly int _slowRequestThresholdMs;

    public PerformanceMonitoringMiddleware(
        RequestDelegate next,
        ILogger<PerformanceMonitoringMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _slowRequestThresholdMs = configuration.GetValue<int>("Performance:SlowRequestThresholdMs", 1000);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            // Log slow requests
            if (elapsedMs > _slowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "SLOW REQUEST: {Method} {Path} took {ElapsedMs}ms (threshold: {ThresholdMs}ms). Status: {StatusCode}",
                    method,
                    path,
                    elapsedMs,
                    _slowRequestThresholdMs,
                    context.Response.StatusCode);
            }

            // Add performance header
            context.Response.Headers.Append("X-Response-Time-Ms", elapsedMs.ToString());

            // Log metrics (could be sent to metrics collection system)
            LogMetrics(method, path, context.Response.StatusCode, elapsedMs);
        }
    }

    private void LogMetrics(string method, string path, int statusCode, long elapsedMs)
    {
        // This could be extended to send metrics to Prometheus, Application Insights, etc.
        _logger.LogDebug(
            "API Metrics - Method: {Method}, Path: {Path}, Status: {StatusCode}, Duration: {DurationMs}ms",
            method,
            path,
            statusCode,
            elapsedMs);
    }
}
