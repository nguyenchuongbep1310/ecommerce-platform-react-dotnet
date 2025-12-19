using MediatR;
using ProductCatalogService.Application.Common.Constants;
using ProductCatalogService.Application.Common.Interfaces;
using ProductCatalogService.Data;

namespace ProductCatalogService.Application.Commands.ReduceStock;

public class ReduceStockCommandHandler : IRequestHandler<ReduceStockCommand, bool>
{
    private readonly ProductDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ReduceStockCommandHandler> _logger;

    public ReduceStockCommandHandler(
        ProductDbContext context,
        ICacheService cacheService,
        ILogger<ReduceStockCommandHandler> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<bool> Handle(ReduceStockCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Reducing stock for Product ID: {ProductId} by {Quantity}", 
            request.ProductId, 
            request.Quantity);

        var product = await _context.Products.FindAsync(
            new object[] { request.ProductId }, 
            cancellationToken);

        if (product == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found", request.ProductId);
            return false;
        }

        if (request.Quantity <= 0)
        {
            _logger.LogWarning("Invalid quantity: {Quantity}", request.Quantity);
            return false;
        }

        if (product.StockQuantity < request.Quantity)
        {
            _logger.LogWarning(
                "Insufficient stock for Product ID {ProductId}. Available: {Available}, Requested: {Requested}",
                request.ProductId,
                product.StockQuantity,
                request.Quantity);
            return false;
        }

        product.StockQuantity -= request.Quantity;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully reduced stock for Product ID: {ProductId}. New stock: {NewStock}",
            request.ProductId,
            product.StockQuantity);

        // Invalidate cache for this product and related caches
        await InvalidateCacheAsync(request.ProductId, cancellationToken);

        return true;
    }

    private async Task InvalidateCacheAsync(int productId, CancellationToken cancellationToken)
    {
        try
        {
            // Invalidate specific product cache
            await _cacheService.RemoveAsync(CacheKeys.Product(productId), cancellationToken);
            
            // Invalidate product lists (all variations with filters)
            await _cacheService.RemoveByPrefixAsync(CacheKeys.ProductsPrefix, cancellationToken);
            
            _logger.LogInformation("Invalidated cache for Product ID: {ProductId}", productId);
        }
        catch (Exception ex)
        {
            // Log but don't fail the operation if cache invalidation fails
            _logger.LogError(ex, "Error invalidating cache for Product ID: {ProductId}", productId);
        }
    }
}
