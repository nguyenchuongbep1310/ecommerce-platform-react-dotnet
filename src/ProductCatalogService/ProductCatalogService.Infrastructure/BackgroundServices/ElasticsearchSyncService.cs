using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProductCatalogService.Application.Common.Interfaces;
using ProductCatalogService.Infrastructure.Persistence;

namespace ProductCatalogService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that periodically syncs products from database to Elasticsearch
/// </summary>
public class ElasticsearchSyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ElasticsearchSyncService> _logger;
    private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(30); // Sync every 30 minutes

    public ElasticsearchSyncService(
        IServiceProvider serviceProvider,
        ILogger<ElasticsearchSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Elasticsearch Sync Service is starting");

        // Wait for 1 minute before first sync to allow services to initialize
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncProductsToElasticsearchAsync(stoppingToken);
                
                _logger.LogInformation(
                    "Next Elasticsearch sync scheduled in {Minutes} minutes",
                    _syncInterval.TotalMinutes);

                await Task.Delay(_syncInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Elasticsearch Sync Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during Elasticsearch sync");
                
                // Wait a bit before retrying on error
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task SyncProductsToElasticsearchAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        var elasticsearchService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();

        _logger.LogInformation("Starting Elasticsearch sync...");

        try
        {
            // Get all products that were updated since last sync
            var products = await dbContext.Products
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (products.Any())
            {
                await elasticsearchService.BulkIndexProductsAsync(products, cancellationToken);
                _logger.LogInformation(
                    "Successfully synced {Count} products to Elasticsearch",
                    products.Count);
            }
            else
            {
                _logger.LogInformation("No products to sync");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync products to Elasticsearch");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Elasticsearch Sync Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}
