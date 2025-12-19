using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductCatalogService.Application.Common.Constants;
using ProductCatalogService.Application.Common.Interfaces;
using ProductCatalogService.Application.DTOs;
using ProductCatalogService.Data;

namespace ProductCatalogService.Application.Queries.GetProducts;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, List<ProductDto>>
{
    private readonly ProductDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetProductsQueryHandler> _logger;

    public GetProductsQueryHandler(
        ProductDbContext context,
        ICacheService cacheService,
        ILogger<GetProductsQueryHandler> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<List<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Create cache key using consistent naming
        var cacheKey = CacheKeys.Products(request.Search, request.Category, request.MinPrice, request.MaxPrice);

        // Use GetOrCreate pattern for cleaner code
        var products = await _cacheService.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                _logger.LogInformation("Cache miss for key: {CacheKey}. Querying database.", cacheKey);
                
                // Build query
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

                // Execute query and map to DTO
                var result = await query
                    .Select(p => new ProductDto(
                        p.Id,
                        p.Name,
                        p.Description,
                        p.Price,
                        p.Category,
                        p.StockQuantity
                    ))
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} products from database", result.Count);
                return result;
            },
            CacheKeys.Expiration.ProductList,
            cancellationToken
        );

        _logger.LogInformation("Returning {Count} products (from cache or database)", products.Count);
        return products;
    }
}
