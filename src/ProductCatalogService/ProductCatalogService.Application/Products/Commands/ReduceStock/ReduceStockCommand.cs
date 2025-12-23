using MediatR;

namespace ProductCatalogService.Application.Products.Commands.ReduceStock;

public record ReduceStockCommand(int ProductId, int Quantity) : IRequest<bool>;
