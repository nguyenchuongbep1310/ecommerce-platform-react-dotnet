using MediatR;
using ProductCatalogService.Application.DTOs;
using ProductCatalogService.Data;

namespace ProductCatalogService.Application.Queries.GetProductById;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly ProductDbContext _context;
    private readonly ILogger<GetProductByIdQueryHandler> _logger;

    public GetProductByIdQueryHandler(
        ProductDbContext context,
        ILogger<GetProductByIdQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching product with ID: {ProductId}", request.Id);

        var product = await _context.Products.FindAsync(new object[] { request.Id }, cancellationToken);

        if (product == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found", request.Id);
            return null;
        }

        return new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Category,
            product.StockQuantity
        );
    }
}
