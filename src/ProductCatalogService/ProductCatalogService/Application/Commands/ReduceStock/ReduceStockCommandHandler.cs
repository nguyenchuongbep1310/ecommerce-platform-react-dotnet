using MediatR;
using ProductCatalogService.Data;

namespace ProductCatalogService.Application.Commands.ReduceStock;

public class ReduceStockCommandHandler : IRequestHandler<ReduceStockCommand, bool>
{
    private readonly ProductDbContext _context;
    private readonly ILogger<ReduceStockCommandHandler> _logger;

    public ReduceStockCommandHandler(
        ProductDbContext context,
        ILogger<ReduceStockCommandHandler> logger)
    {
        _context = context;
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

        return true;
    }
}
