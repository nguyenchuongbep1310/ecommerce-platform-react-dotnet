namespace ProductCatalogService.Application.DTOs;

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
