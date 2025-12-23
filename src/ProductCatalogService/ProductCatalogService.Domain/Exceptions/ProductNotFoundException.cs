namespace ProductCatalogService.Domain.Exceptions;

/// <summary>
/// Exception thrown when a product is not found
/// </summary>
public class ProductNotFoundException : Exception
{
    public int ProductId { get; }

    public ProductNotFoundException(int productId)
        : base($"Product with ID {productId} was not found.")
    {
        ProductId = productId;
    }

    public ProductNotFoundException(int productId, string message)
        : base(message)
    {
        ProductId = productId;
    }

    public ProductNotFoundException(int productId, string message, Exception innerException)
        : base(message, innerException)
    {
        ProductId = productId;
    }
}
