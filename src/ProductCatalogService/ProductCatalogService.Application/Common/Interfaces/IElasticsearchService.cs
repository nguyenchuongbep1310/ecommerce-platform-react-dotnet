using ProductCatalogService.Domain.Entities;

namespace ProductCatalogService.Application.Common.Interfaces;

/// <summary>
/// Interface for Elasticsearch service
/// </summary>
public interface IElasticsearchService
{
    Task InitializeIndexAsync(CancellationToken cancellationToken = default);
    Task IndexProductAsync(Product product, CancellationToken cancellationToken = default);
    Task BulkIndexProductsAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default);
    Task<(List<ProductDocument> Products, long TotalCount)> SearchProductsAsync(
        string? searchTerm,
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        bool? inStockOnly,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
    Task<List<string>> GetSuggestionsAsync(string prefix, int limit = 10, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(int productId, CancellationToken cancellationToken = default);
    Task UpdateProductAsync(Product product, CancellationToken cancellationToken = default);
    Task<Dictionary<string, long>> GetCategoryAggregationsAsync(CancellationToken cancellationToken = default);
}
