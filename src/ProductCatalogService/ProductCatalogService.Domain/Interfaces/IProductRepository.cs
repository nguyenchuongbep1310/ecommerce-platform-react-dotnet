using ProductCatalogService.Domain.Entities;

namespace ProductCatalogService.Domain.Interfaces;

/// <summary>
/// Repository interface for Product entity
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> SearchAsync(string? search, string? category, decimal? minPrice, decimal? maxPrice, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<(string Category, int Count, decimal AvgPrice, decimal MinPrice, decimal MaxPrice)>> GetCategoriesStatsAsync(CancellationToken cancellationToken = default);
}
