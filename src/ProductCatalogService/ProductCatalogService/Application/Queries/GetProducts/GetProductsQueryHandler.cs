using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using ProductCatalogService.Application.DTOs;
using ProductCatalogService.Data;
using System.Text.Json;

namespace ProductCatalogService.Application.Queries.GetProducts;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, List<ProductDto>>
{
    private readonly ProductDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<GetProductsQueryHandler> _logger;

    public GetProductsQueryHandler(
        ProductDbContext context,
        IDistributedCache cache,
        ILogger<GetProductsQueryHandler> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Create a unique cache key based on all parameters
        string cacheKey = $"products_{request.Search}_{request.Category}_{request.MinPrice}_{request.MaxPrice}";

        // 1. Check Cache
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedData))
        {
            _logger.LogInformation("Cache hit for key: {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<List<ProductDto>>(cachedData)!;
        }

        _logger.LogInformation("Cache miss for key: {CacheKey}. Querying database.", cacheKey);

        // 2. Build Query
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(p => p.Name.ToLower().Contains(request.Search.ToLower()) 
                                  || p.Description.ToLower().Contains(request.Search.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(p => p.Category == request.Category);
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= request.MaxPrice.Value);
        }

        // 3. Execute Query and Map to DTO
        var products = await query
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.Category,
                p.StockQuantity
            ))
            .ToListAsync(cancellationToken);

        // 4. Save to Cache (60 seconds expiration)
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
        };
        var serializedData = JsonSerializer.Serialize(products);
        await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions, cancellationToken);

        _logger.LogInformation("Cached {Count} products with key: {CacheKey}", products.Count, cacheKey);

        return products;
    }
}
