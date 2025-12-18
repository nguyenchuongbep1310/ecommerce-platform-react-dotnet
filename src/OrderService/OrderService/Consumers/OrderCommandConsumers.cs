using MassTransit;
using OrderService.Data;
using Shared.Messages.Commands;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Consumers
{
    public class OrderCommandConsumers : IConsumer<ICompleteOrderCommand>, IConsumer<ICancelOrderCommand>
    {
        private readonly OrderDbContext _context;
        private readonly ILogger<OrderCommandConsumers> _logger;

        public OrderCommandConsumers(OrderDbContext context, ILogger<OrderCommandConsumers> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ICompleteOrderCommand> context)
        {
            _logger.LogInformation("Completing Order with CorrelationId {OrderId}", context.Message.OrderId);
            
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.CorrelationId == context.Message.OrderId);
            if (order != null)
            {
                order.Status = "Completed";
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order {OrderId} status updated to Completed", order.Id);
            }
        }

        public async Task Consume(ConsumeContext<ICancelOrderCommand> context)
        {
            _logger.LogWarning("Cancelling Order with CorrelationId {OrderId}. Reason: {Reason}", context.Message.OrderId, context.Message.Reason);
            
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.CorrelationId == context.Message.OrderId);
            if (order != null)
            {
                order.Status = "Cancelled";
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order {OrderId} status updated to Cancelled", order.Id);
            }
        }
    }
}
