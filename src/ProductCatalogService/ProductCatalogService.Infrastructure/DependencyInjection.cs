using Elastic.Clients.Elasticsearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductCatalogService.Application.Common.Interfaces;
using ProductCatalogService.Domain.Interfaces;
using ProductCatalogService.Infrastructure.Persistence;
using ProductCatalogService.Infrastructure.Persistence.Repositories;
using ProductCatalogService.Infrastructure.Services;
using StackExchange.Redis;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ProductCatalogService.Infrastructure;

/// <summary>
/// Dependency injection configuration for Infrastructure layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ProductDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Repositories
        services.AddScoped<IProductRepository, ProductRepository>();

        // Services
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IElasticsearchService, ElasticsearchService>();

        // Redis Cache
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        
        services.AddSingleton<IConnectionMultiplexer>(sp => 
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "ProductCatalog_";
        });

        // Elasticsearch
        var elasticsearchUrl = configuration.GetConnectionString("Elasticsearch") 
            ?? "http://elasticsearch:9200";
        services.AddSingleton(sp =>
        {
            var settings = new ElasticsearchClientSettings(new Uri(elasticsearchUrl))
                .DefaultIndex("products");
            return new ElasticsearchClient(settings);
        });

        return services;
    }
}
