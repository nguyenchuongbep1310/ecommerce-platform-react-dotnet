namespace ProductCatalogService.Domain.Entities;

/// <summary>
/// Elasticsearch document model for product search
/// </summary>
public class ProductDocument
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool InStock => StockQuantity > 0;
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Create ProductDocument from Product entity
    /// </summary>
    public static ProductDocument FromProduct(Product product)
    {
        return new ProductDocument
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            Category = product.Category,
            IndexedAt = DateTime.UtcNow
        };
    }
}
