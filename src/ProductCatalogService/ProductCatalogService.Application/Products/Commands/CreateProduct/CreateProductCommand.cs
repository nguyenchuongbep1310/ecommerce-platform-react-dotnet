using MediatR;
using ProductCatalogService.Application.Common.Models;

namespace ProductCatalogService.Application.Products.Commands.CreateProduct;

/// <summary>
/// Command to create a new product
/// </summary>
public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    string Category,
    int StockQuantity
) : IRequest<ProductDto>;
