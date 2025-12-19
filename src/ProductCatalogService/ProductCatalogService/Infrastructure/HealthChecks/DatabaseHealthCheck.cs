using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProductCatalogService.Data;

namespace ProductCatalogService.Infrastructure.HealthChecks;

/// <summary>
/// Custom health check for database connectivity and performance
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ProductDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(
        ProductDbContext context,
        ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Test database connectivity with a simple query
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

            // Get product count as a health indicator
            var productCount = await _context.Products.CountAsync(cancellationToken);
            
            var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            var connectionString = _context.Database.GetConnectionString();
            var serverInfo = connectionString?.Split(';').FirstOrDefault(s => s.Contains("Host")) ?? "unknown";

            var data = new Dictionary<string, object>
            {
                { "responseTime", $"{responseTime:F2}ms" },
                { "productCount", productCount },
                { "server", serverInfo }
            };

            if (responseTime > 500)
            {
                _logger.LogWarning("Database response time is slow: {ResponseTime}ms", responseTime);
                return HealthCheckResult.Degraded(
                    $"Database is responding slowly ({responseTime:F2}ms)",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"Database is healthy (response time: {responseTime:F2}ms, products: {productCount})",
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy(
                "Database is unavailable",
                ex,
                new Dictionary<string, object>
                {
                    { "error", ex.Message }
                });
        }
    }
}
