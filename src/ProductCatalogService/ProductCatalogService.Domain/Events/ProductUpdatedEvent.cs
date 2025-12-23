namespace ProductCatalogService.Domain.Events;

/// <summary>
/// Domain event raised when a product is updated
/// </summary>
public class ProductUpdatedEvent
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
