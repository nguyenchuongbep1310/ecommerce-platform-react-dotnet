using MassTransit;
using Shared.Messages.Events;
using ShoppingCartService.Data;
using Microsoft.EntityFrameworkCore;

namespace ShoppingCartService.Consumers
{
    public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
    {
        private readonly CartDbContext _context;
        private readonly ILogger<OrderPlacedConsumer> _logger;

        public OrderPlacedConsumer(CartDbContext context, ILogger<OrderPlacedConsumer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
        {
            var userId = context.Message.UserId;
            _logger.LogInformation("Received OrderPlacedEvent. Clearing cart for UserId: {UserId}", userId);

            // Find the user's cart
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart != null)
            {
                // Clear the cart
                _context.Carts.Remove(cart); // Or just clear items: _context.CartItems.RemoveRange(cart.Items);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cart cleared for UserId: {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("No cart found for UserId: {UserId} to clear.", userId);
            }
        }
    }
}
