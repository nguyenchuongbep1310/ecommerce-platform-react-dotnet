using MediatR;
using ProductCatalogService.Application.DTOs;

namespace ProductCatalogService.Application.Queries.GetProducts;

public record GetProductsQuery(
    string? Search,
    string? Category,
    decimal? MinPrice,
    decimal? MaxPrice
) : IRequest<List<ProductDto>>;
