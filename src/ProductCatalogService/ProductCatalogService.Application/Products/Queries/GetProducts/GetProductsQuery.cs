using MediatR;
using ProductCatalogService.Application.Common.Models;

namespace ProductCatalogService.Application.Products.Queries.GetProducts;

/// <summary>
/// Query to get all products
/// </summary>
public record GetProductsQuery(
    string? Search = null,
    string? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null
) : IRequest<ProductListDto>;
