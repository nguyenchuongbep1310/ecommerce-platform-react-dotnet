using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductCatalogService.Application.Common.Constants;
using ProductCatalogService.Application.Common.Interfaces;
using ProductCatalogService.Application.DTOs;
using ProductCatalogService.Data;

namespace ProductCatalogService.Application.Queries.GetProductById;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly ProductDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetProductByIdQueryHandler> _logger;

    public GetProductByIdQueryHandler(
        ProductDbContext context,
        ICacheService cacheService,
        ILogger<GetProductByIdQueryHandler> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Product(request.Id);

        // Use GetOrCreate pattern with nullable return type
        var cachedProduct = await _cacheService.GetAsync<ProductDto>(cacheKey, cancellationToken);
        
        if (cachedProduct != null)
        {
            _logger.LogInformation("Cache hit for product ID: {ProductId}", request.Id);
            return cachedProduct;
        }

        _logger.LogInformation("Cache miss for product ID: {ProductId}. Querying database.", request.Id);

        var product = await _context.Products
            .Where(p => p.Id == request.Id)
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.Category,
                p.StockQuantity
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (product != null)
        {
            // Cache the product
            await _cacheService.SetAsync(
                cacheKey,
                product,
                CacheKeys.Expiration.Product,
                cancellationToken);
            
            _logger.LogInformation("Cached product ID: {ProductId}", request.Id);
        }
        else
        {
            _logger.LogWarning("Product not found: {ProductId}", request.Id);
        }

        return product;
    }
}
