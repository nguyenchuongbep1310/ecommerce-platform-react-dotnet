namespace ProductCatalogService.Domain.Exceptions;

/// <summary>
/// Exception thrown when product data is invalid
/// </summary>
public class InvalidProductException : Exception
{
    public InvalidProductException(string message)
        : base(message)
    {
    }

    public InvalidProductException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
