using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProductCatalogService.Infrastructure.Persistence;

namespace ProductCatalogService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that monitors inventory levels and logs alerts for low stock
/// </summary>
public class InventoryMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InventoryMonitoringService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour
    private const int LowStockThreshold = 10;
    private const int CriticalStockThreshold = 5;

    public InventoryMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<InventoryMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Inventory Monitoring Service is starting");

        // Wait for 5 minutes before first check
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckInventoryLevelsAsync(stoppingToken);
                
                _logger.LogInformation(
                    "Next inventory check scheduled in {Hours} hour(s)",
                    _checkInterval.TotalHours);

                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Inventory Monitoring Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during inventory check");
                
                // Wait before retrying on error
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }

    private async Task CheckInventoryLevelsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        _logger.LogInformation("Starting inventory level check...");

        try
        {
            // Get products with critical stock levels
            var criticalStockProducts = await dbContext.Products
                .AsNoTracking()
                .Where(p => p.StockQuantity > 0 && p.StockQuantity <= CriticalStockThreshold)
                .Select(p => new { p.Id, p.Name, p.StockQuantity, p.Category })
                .ToListAsync(cancellationToken);

            if (criticalStockProducts.Any())
            {
                _logger.LogWarning(
                    "CRITICAL: {Count} products have critically low stock (≤{Threshold} units)",
                    criticalStockProducts.Count,
                    CriticalStockThreshold);

                foreach (var product in criticalStockProducts)
                {
                    _logger.LogWarning(
                        "CRITICAL STOCK: Product '{ProductName}' (ID: {ProductId}, Category: {Category}) has only {Stock} unit(s) remaining",
                        product.Name,
                        product.Id,
                        product.Category,
                        product.StockQuantity);
                }
            }

            // Get products with low stock levels
            var lowStockProducts = await dbContext.Products
                .AsNoTracking()
                .Where(p => p.StockQuantity > CriticalStockThreshold && p.StockQuantity <= LowStockThreshold)
                .Select(p => new { p.Id, p.Name, p.StockQuantity, p.Category })
                .ToListAsync(cancellationToken);

            if (lowStockProducts.Any())
            {
                _logger.LogWarning(
                    "WARNING: {Count} products have low stock (≤{Threshold} units)",
                    lowStockProducts.Count,
                    LowStockThreshold);

                foreach (var product in lowStockProducts)
                {
                    _logger.LogWarning(
                        "LOW STOCK: Product '{ProductName}' (ID: {ProductId}, Category: {Category}) has {Stock} unit(s) remaining",
                        product.Name,
                        product.Id,
                        product.Category,
                        product.StockQuantity);
                }
            }

            // Get out of stock products
            var outOfStockProducts = await dbContext.Products
                .AsNoTracking()
                .Where(p => p.StockQuantity == 0)
                .Select(p => new { p.Id, p.Name, p.Category })
                .ToListAsync(cancellationToken);

            if (outOfStockProducts.Any())
            {
                _logger.LogInformation(
                    "INFO: {Count} products are currently out of stock",
                    outOfStockProducts.Count);
            }

            // Summary
            var totalProducts = await dbContext.Products.CountAsync(cancellationToken);
            var inStockProducts = totalProducts - outOfStockProducts.Count;

            _logger.LogInformation(
                "Inventory check completed. Total: {Total}, In Stock: {InStock}, Out of Stock: {OutOfStock}, Low Stock: {LowStock}, Critical: {Critical}",
                totalProducts,
                inStockProducts,
                outOfStockProducts.Count,
                lowStockProducts.Count,
                criticalStockProducts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check inventory levels");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Inventory Monitoring Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}
