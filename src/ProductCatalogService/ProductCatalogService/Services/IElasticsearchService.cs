using ProductCatalogService.Models;

namespace ProductCatalogService.Services;

public interface IElasticsearchService
{
    /// <summary>
    /// Initialize Elasticsearch index with mappings
    /// </summary>
    Task InitializeIndexAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Index a single product
    /// </summary>
    Task IndexProductAsync(Product product, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Index multiple products in bulk
    /// </summary>
    Task BulkIndexProductsAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Search products with full-text search
    /// </summary>
    Task<(List<ProductDocument> Products, long TotalCount)> SearchProductsAsync(
        string? searchTerm,
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        bool? inStockOnly,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get product suggestions for autocomplete
    /// </summary>
    Task<List<string>> GetSuggestionsAsync(string prefix, int limit = 10, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete a product from index
    /// </summary>
    Task DeleteProductAsync(int productId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update a product in index
    /// </summary>
    Task UpdateProductAsync(Product product, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get aggregated category statistics
    /// </summary>
    Task<Dictionary<string, long>> GetCategoryAggregationsAsync(CancellationToken cancellationToken = default);
}
