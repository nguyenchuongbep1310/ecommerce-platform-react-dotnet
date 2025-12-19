using MediatR;

namespace ProductCatalogService.Application.Commands.ReduceStock;

public record ReduceStockCommand(
    int ProductId,
    int Quantity
) : IRequest<bool>;
