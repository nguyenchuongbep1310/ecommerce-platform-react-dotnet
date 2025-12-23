using MediatR;

namespace ProductCatalogService.Application.Products.Queries.GetCategories;

/// <summary>
/// Query to get all product categories with product counts
/// </summary>
public record GetCategoriesQuery : IRequest<List<CategoryDto>>;

/// <summary>
/// Category DTO with product count
/// </summary>
public record CategoryDto(
    string Name,
    int ProductCount,
    decimal? AveragePrice = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null
);
