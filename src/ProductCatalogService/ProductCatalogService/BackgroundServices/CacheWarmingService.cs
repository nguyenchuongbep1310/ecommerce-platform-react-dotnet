using Microsoft.EntityFrameworkCore;
using ProductCatalogService.Application.Common.Interfaces;
using ProductCatalogService.Data;
using System.Text.Json;

namespace ProductCatalogService.BackgroundServices;

/// <summary>
/// Background service that warms up cache with frequently accessed data
/// </summary>
public class CacheWarmingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheWarmingService> _logger;
    private readonly TimeSpan _warmupInterval = TimeSpan.FromHours(6); // Warm cache every 6 hours

    public CacheWarmingService(
        IServiceProvider serviceProvider,
        ILogger<CacheWarmingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cache Warming Service is starting");

        // Wait for 2 minutes before first warmup to allow services to initialize
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await WarmupCacheAsync(stoppingToken);
                
                _logger.LogInformation(
                    "Next cache warmup scheduled in {Hours} hours",
                    _warmupInterval.TotalHours);

                await Task.Delay(_warmupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Cache Warming Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during cache warmup");
                
                // Wait before retrying on error
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }

    private async Task WarmupCacheAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        _logger.LogInformation("Starting cache warmup...");

        try
        {
            // 1. Cache popular products (top 50 by stock quantity as a proxy for popularity)
            var popularProducts = await dbContext.Products
                .AsNoTracking()
                .OrderByDescending(p => p.StockQuantity)
                .Take(50)
                .ToListAsync(cancellationToken);

            foreach (var product in popularProducts)
            {
                var cacheKey = $"product:{product.Id}";
                await cacheService.SetAsync(
                    cacheKey,
                    JsonSerializer.Serialize(product),
                    TimeSpan.FromHours(12),
                    cancellationToken);
            }

            _logger.LogInformation("Cached {Count} popular products", popularProducts.Count);

            // 2. Cache category list
            var categories = await dbContext.Products
                .AsNoTracking()
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync(cancellationToken);

            await cacheService.SetAsync(
                "categories:all",
                JsonSerializer.Serialize(categories),
                TimeSpan.FromHours(24),
                cancellationToken);

            _logger.LogInformation("Cached {Count} categories", categories.Count);

            // 3. Cache product count by category
            var categoryCounts = await dbContext.Products
                .AsNoTracking()
                .GroupBy(p => p.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            await cacheService.SetAsync(
                "categories:counts",
                JsonSerializer.Serialize(categoryCounts),
                TimeSpan.FromHours(12),
                cancellationToken);

            _logger.LogInformation("Cached category counts");

            // 4. Cache in-stock products count
            var inStockCount = await dbContext.Products
                .AsNoTracking()
                .CountAsync(p => p.StockQuantity > 0, cancellationToken);

            await cacheService.SetAsync(
                "products:instock:count",
                inStockCount.ToString(),
                TimeSpan.FromHours(6),
                cancellationToken);

            _logger.LogInformation("Cache warmup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to warm up cache");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cache Warming Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}
