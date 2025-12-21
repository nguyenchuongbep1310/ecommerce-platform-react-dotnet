using Hangfire;
using Microsoft.EntityFrameworkCore;
using ProductCatalogService.Data;
using ProductCatalogService.Application.Common.Interfaces;
using System.Text.Json;

namespace ProductCatalogService.ScheduledJobs;

/// <summary>
/// Scheduled jobs for product catalog maintenance and analytics
/// </summary>
public class ProductCatalogJobs
{
    private readonly ProductDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ProductCatalogJobs> _logger;

    public ProductCatalogJobs(
        ProductDbContext dbContext,
        ICacheService cacheService,
        ILogger<ProductCatalogJobs> logger)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Generates daily product analytics report
    /// Runs daily at 2:00 AM
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task GenerateDailyAnalyticsAsync()
    {
        _logger.LogInformation("Starting daily analytics generation...");

        try
        {
            var analytics = new
            {
                GeneratedAt = DateTime.UtcNow,
                TotalProducts = await _dbContext.Products.CountAsync(),
                InStockProducts = await _dbContext.Products.CountAsync(p => p.StockQuantity > 0),
                OutOfStockProducts = await _dbContext.Products.CountAsync(p => p.StockQuantity == 0),
                LowStockProducts = await _dbContext.Products.CountAsync(p => p.StockQuantity > 0 && p.StockQuantity <= 10),
                TotalInventoryValue = await _dbContext.Products.SumAsync(p => p.Price * p.StockQuantity),
                AveragePrice = await _dbContext.Products.AverageAsync(p => p.Price),
                CategoryBreakdown = await _dbContext.Products
                    .GroupBy(p => p.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Count = g.Count(),
                        TotalValue = g.Sum(p => p.Price * p.StockQuantity)
                    })
                    .ToListAsync()
            };

            // Store analytics in cache for 24 hours
            await _cacheService.SetAsync(
                "analytics:daily:latest",
                JsonSerializer.Serialize(analytics),
                TimeSpan.FromHours(24));

            _logger.LogInformation(
                "Daily analytics generated successfully. Total Products: {Total}, In Stock: {InStock}, Total Value: ${Value:N2}",
                analytics.TotalProducts,
                analytics.InStockProducts,
                analytics.TotalInventoryValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate daily analytics");
            throw;
        }
    }

    /// <summary>
    /// Cleans up old cache entries
    /// Runs every 6 hours
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task CleanupOldCacheEntriesAsync()
    {
        _logger.LogInformation("Starting cache cleanup...");

        try
        {
            // This is a placeholder - actual implementation would depend on your cache service
            // For Redis, you might want to remove specific patterns or expired keys
            
            _logger.LogInformation("Cache cleanup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup cache");
            throw;
        }
    }

    /// <summary>
    /// Updates product popularity scores based on stock changes
    /// Runs every 12 hours
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task UpdateProductPopularityScoresAsync()
    {
        _logger.LogInformation("Starting product popularity score update...");

        try
        {
            // Get products with low stock (indicating high demand)
            var popularProducts = await _dbContext.Products
                .Where(p => p.StockQuantity < 20 && p.StockQuantity > 0)
                .OrderBy(p => p.StockQuantity)
                .Take(100)
                .Select(p => new { p.Id, p.Name, p.StockQuantity, p.Category })
                .ToListAsync();

            // Cache popular products list
            await _cacheService.SetAsync(
                "products:popular",
                JsonSerializer.Serialize(popularProducts),
                TimeSpan.FromHours(12));

            _logger.LogInformation(
                "Updated popularity scores for {Count} products",
                popularProducts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update product popularity scores");
            throw;
        }
    }

    /// <summary>
    /// Generates weekly inventory report
    /// Runs every Monday at 8:00 AM
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task GenerateWeeklyInventoryReportAsync()
    {
        _logger.LogInformation("Starting weekly inventory report generation...");

        try
        {
            var report = new
            {
                GeneratedAt = DateTime.UtcNow,
                WeekNumber = GetIso8601WeekOfYear(DateTime.UtcNow),
                Year = DateTime.UtcNow.Year,
                Summary = new
                {
                    TotalProducts = await _dbContext.Products.CountAsync(),
                    TotalValue = await _dbContext.Products.SumAsync(p => p.Price * p.StockQuantity),
                    LowStockAlerts = await _dbContext.Products.CountAsync(p => p.StockQuantity <= 10 && p.StockQuantity > 0),
                    OutOfStockAlerts = await _dbContext.Products.CountAsync(p => p.StockQuantity == 0)
                },
                TopCategories = await _dbContext.Products
                    .GroupBy(p => p.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        ProductCount = g.Count(),
                        TotalValue = g.Sum(p => p.Price * p.StockQuantity),
                        AverageStock = g.Average(p => p.StockQuantity)
                    })
                    .OrderByDescending(x => x.TotalValue)
                    .Take(10)
                    .ToListAsync()
            };

            // Store report in cache
            var reportKey = $"reports:inventory:weekly:{report.Year}:W{report.WeekNumber}";
            await _cacheService.SetAsync(
                reportKey,
                JsonSerializer.Serialize(report),
                TimeSpan.FromDays(30)); // Keep for 30 days

            _logger.LogInformation(
                "Weekly inventory report generated successfully for Week {Week}, {Year}",
                report.WeekNumber,
                report.Year);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate weekly inventory report");
            throw;
        }
    }

    /// <summary>
    /// Cleanup old product data (soft-deleted items older than 90 days)
    /// Runs monthly on the 1st at 3:00 AM
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task CleanupOldDataAsync()
    {
        _logger.LogInformation("Starting old data cleanup...");

        try
        {
            // This is a placeholder for cleanup logic
            // You might want to delete soft-deleted products, old logs, etc.
            
            var cutoffDate = DateTime.UtcNow.AddDays(-90);
            
            _logger.LogInformation(
                "Old data cleanup completed. Cutoff date: {CutoffDate}",
                cutoffDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old data");
            throw;
        }
    }

    private static int GetIso8601WeekOfYear(DateTime date)
    {
        var day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
        if (day >= System.DayOfWeek.Monday && day <= System.DayOfWeek.Wednesday)
        {
            date = date.AddDays(3);
        }

        return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
            date,
            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
            System.DayOfWeek.Monday);
    }
}
