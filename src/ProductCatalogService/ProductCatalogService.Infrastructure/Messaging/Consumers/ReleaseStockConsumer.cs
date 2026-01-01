using MassTransit;
using Microsoft.Extensions.Logging;
using ProductCatalogService.Infrastructure.Persistence;
using Shared.Messages.Commands;

namespace ProductCatalogService.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumer for releasing reserved stock when payment fails (compensating transaction)
/// </summary>
public class ReleaseStockConsumer : IConsumer<IReleaseStockCommand>
{
    private readonly ProductDbContext _context;
    private readonly ILogger<ReleaseStockConsumer> _logger;

    public ReleaseStockConsumer(ProductDbContext context, ILogger<ReleaseStockConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IReleaseStockCommand> context)
    {
        _logger.LogInformation(
            "Releasing reserved stock for Order {OrderId} (Compensating Transaction)", 
            context.Message.OrderId);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in context.Message.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    // Restore the stock quantity
                    product.StockQuantity += item.Quantity;
                    
                    _logger.LogInformation(
                        "Restored {Quantity} units of Product {ProductId} (New Stock: {NewStock})",
                        item.Quantity,
                        item.ProductId,
                        product.StockQuantity);
                }
                else
                {
                    _logger.LogWarning(
                        "Product {ProductId} not found during stock release for Order {OrderId}",
                        item.ProductId,
                        context.Message.OrderId);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Successfully released stock for Order {OrderId}", 
                context.Message.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Error releasing stock for Order {OrderId}. Stock may not be restored.", 
                context.Message.OrderId);
            
            await transaction.RollbackAsync();
            
            // Don't throw - this is a compensating transaction
            // Log the error for manual intervention if needed
            _logger.LogCritical(
                "MANUAL INTERVENTION REQUIRED: Failed to release stock for Order {OrderId}. " +
                "Stock quantities may be incorrect. Items: {Items}",
                context.Message.OrderId,
                System.Text.Json.JsonSerializer.Serialize(context.Message.Items));
        }
    }
}
