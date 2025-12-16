using MediatR;
using OrderService.Models;

namespace OrderService.Application.Commands
{
    // Command: Represents the intent to create an order
    public record CreateOrderCommand(string UserId, List<OrderItemDto> Items) : IRequest<Order>;

    public record OrderItemDto(int ProductId, int Quantity, decimal UnitPrice);
}
