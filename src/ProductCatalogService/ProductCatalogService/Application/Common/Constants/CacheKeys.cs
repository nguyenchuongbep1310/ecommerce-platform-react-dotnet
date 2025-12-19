namespace ProductCatalogService.Application.Common.Constants;

/// <summary>
/// Cache key constants for consistent key naming
/// </summary>
public static class CacheKeys
{
    // Prefixes
    public const string ProductPrefix = "product:";
    public const string ProductsPrefix = "products:";
    public const string CategoryPrefix = "category:";
    public const string CategoriesPrefix = "categories:";

    // Individual product cache
    public static string Product(int id) => $"{ProductPrefix}{id}";

    // Product list cache (with filters)
    public static string Products(string? search, string? category, decimal? minPrice, decimal? maxPrice)
        => $"{ProductsPrefix}search:{search ?? "all"}_cat:{category ?? "all"}_min:{minPrice?.ToString() ?? "none"}_max:{maxPrice?.ToString() ?? "none"}";

    // All products cache
    public const string AllProducts = "products:all";

    // Categories cache
    public const string AllCategories = "categories:all";

    // Category with product count
    public static string CategoryWithCount(string category) => $"{CategoryPrefix}{category}:count";

    // Product price cache
    public static string ProductPrice(int id) => $"{ProductPrefix}{id}:price";

    // Stock quantity cache
    public static string ProductStock(int id) => $"{ProductPrefix}{id}:stock";

    // Cache expiration times
    public static class Expiration
    {
        public static readonly TimeSpan Product = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan ProductList = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan Categories = TimeSpan.FromHours(1);
        public static readonly TimeSpan ProductPrice = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan ProductStock = TimeSpan.FromMinutes(2);
    }
}
