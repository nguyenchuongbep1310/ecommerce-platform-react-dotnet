using MediatR;
using OrderService.Models;

namespace OrderService.Application.Queries
{
    // Query: Represents the intent to fetch an order by ID
    public record GetOrderByIdQuery(int Id) : IRequest<Order?>;
    
    // Query: Represents the intent to fetch all orders for a user
    public record GetOrdersByUserIdQuery(string UserId) : IRequest<List<Order>>;
}
