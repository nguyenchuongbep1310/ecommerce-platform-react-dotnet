using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace ProductCatalogService.Infrastructure.HealthChecks;

/// <summary>
/// Custom health check for Redis cache connectivity and performance
/// </summary>
public class RedisCacheHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheHealthCheck> _logger;

    public RedisCacheHealthCheck(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheHealthCheck> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var startTime = DateTime.UtcNow;
            
            // Test write
            var testKey = "health_check_test";
            await db.StringSetAsync(testKey, "test", TimeSpan.FromSeconds(10));
            
            // Test read
            var value = await db.StringGetAsync(testKey);
            
            // Clean up
            await db.KeyDeleteAsync(testKey);
            
            var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            var data = new Dictionary<string, object>
            {
                { "connected", _redis.IsConnected },
                { "responseTime", $"{responseTime:F2}ms" },
                { "endpoints", string.Join(", ", _redis.GetEndPoints().Select(e => e.ToString())) }
            };

            if (responseTime > 100)
            {
                _logger.LogWarning("Redis response time is slow: {ResponseTime}ms", responseTime);
                return HealthCheckResult.Degraded(
                    $"Redis is responding slowly ({responseTime:F2}ms)",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"Redis is healthy (response time: {responseTime:F2}ms)",
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy(
                "Redis is unavailable",
                ex,
                new Dictionary<string, object>
                {
                    { "connected", _redis.IsConnected },
                    { "error", ex.Message }
                });
        }
    }
}
