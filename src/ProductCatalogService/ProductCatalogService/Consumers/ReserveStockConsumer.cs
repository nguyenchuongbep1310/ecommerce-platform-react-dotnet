using MassTransit;
using Microsoft.EntityFrameworkCore;
using ProductCatalogService.Data;
using Shared.Messages.Commands;
using Shared.Messages.Events;

namespace ProductCatalogService.Consumers
{
    public class ReserveStockConsumer : IConsumer<IReserveStockCommand>
    {
        private readonly ProductDbContext _context;
        private readonly ILogger<ReserveStockConsumer> _logger;

        public ReserveStockConsumer(ProductDbContext context, ILogger<ReserveStockConsumer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IReserveStockCommand> context)
        {
            _logger.LogInformation("Attempting to reserve stock for Order {OrderId}", context.Message.OrderId);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in context.Message.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null || product.StockQuantity < item.Quantity)
                    {
                        _logger.LogWarning("Insufficient stock for Product {ProductId}", item.ProductId);
                        await context.Publish<IStockReservationFailedEvent>(new
                        {
                            OrderId = context.Message.OrderId,
                            Reason = $"Insufficient stock for Product {item.ProductId}"
                        });
                        return;
                    }

                    product.StockQuantity -= item.Quantity;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Stock reserved for Order {OrderId}", context.Message.OrderId);
                await context.Publish<IStockReservedEvent>(new
                {
                    OrderId = context.Message.OrderId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reserving stock for Order {OrderId}", context.Message.OrderId);
                await transaction.RollbackAsync();
                await context.Publish<IStockReservationFailedEvent>(new
                {
                    OrderId = context.Message.OrderId,
                    Reason = "Internal error during stock reservation"
                });
            }
        }
    }
}
