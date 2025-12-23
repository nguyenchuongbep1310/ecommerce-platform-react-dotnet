using MediatR;
using ProductCatalogService.Domain.Entities;

namespace ProductCatalogService.Application.Products.Queries.SearchProducts;

public record SearchProductsQuery(
    string? Query,
    string? Category,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool? InStock,
    int Page = 1,
    int PageSize = 20
) : IRequest<SearchResponseDto>;

public class SearchResponseDto
{
    public List<ProductDocument> Products { get; set; } = new();
    public long TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public string? Query { get; set; }
    public SearchFiltersDto Filters { get; set; } = new();
}

public class SearchFiltersDto
{
    public string? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? InStockOnly { get; set; }
}
