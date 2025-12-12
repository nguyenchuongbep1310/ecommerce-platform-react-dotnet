using MassTransit;
using Shared.Messages.Events;
using Microsoft.Extensions.Logging;

namespace NotificationService.Consumers
{
    // MassTransit looks for classes implementing IConsumer<T>
    public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
    {
        private readonly ILogger<OrderPlacedConsumer> _logger;

        public OrderPlacedConsumer(ILogger<OrderPlacedConsumer> logger)
        {
            _logger = logger;
        }

        // The Consume method executes whenever a new message (event) is received
        public Task Consume(ConsumeContext<OrderPlacedEvent> context)
        {
            var message = context.Message;

            // Log the received event (simulating sending an email/SMS)
            _logger.LogInformation("--- NOTIFICATION SERVICE ---");
            _logger.LogInformation("Order {OrderId} placed by user {UserId} for a total of {TotalAmount:C} received.",
                message.OrderId, 
                message.UserId, 
                message.TotalAmount);
            _logger.LogInformation("Notifying user: Sending confirmation email/SMS...");
            _logger.LogInformation("Total Items: {ItemCount}", message.Items.Count());
            _logger.LogInformation("----------------------------");

            // Returning Task.CompletedTask signals successful processing to MassTransit, 
            // which then removes the message from the queue.
            return Task.CompletedTask;
        }
    }
}