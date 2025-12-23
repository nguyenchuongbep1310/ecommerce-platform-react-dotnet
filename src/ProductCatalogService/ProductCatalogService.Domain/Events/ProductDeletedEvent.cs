namespace ProductCatalogService.Domain.Events;

/// <summary>
/// Domain event raised when a product is deleted
/// </summary>
public class ProductDeletedEvent
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}
