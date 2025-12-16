using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Queries;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Application.Handlers
{
    public class GetOrderHandlers : 
        IRequestHandler<GetOrderByIdQuery, Order?>,
        IRequestHandler<GetOrdersByUserIdQuery, List<Order>>
    {
        private readonly OrderDbContext _context;

        public GetOrderHandlers(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<Order?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
        }

        public async Task<List<Order>> Handle(GetOrdersByUserIdQuery request, CancellationToken cancellationToken)
        {
            return await _context.Orders
                .Where(o => o.UserId == request.UserId)
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync(cancellationToken);
        }
    }
}
