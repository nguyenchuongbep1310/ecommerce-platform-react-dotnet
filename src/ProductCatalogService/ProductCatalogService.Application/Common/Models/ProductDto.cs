namespace ProductCatalogService.Application.Common.Models;

public record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    string Category,
    int StockQuantity
);

public record ProductListDto(
    int TotalCount,
    List<ProductDto> Products
);
