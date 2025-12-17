using MassTransit;
using Shared.Messages.Events;
using Microsoft.Extensions.Logging;

using NotificationService.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Consumers
{
    // MassTransit looks for classes implementing IConsumer<T>
    public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
    {
        private readonly ILogger<OrderPlacedConsumer> _logger;
        private readonly IHubContext<OrderHub> _hubContext;

        public OrderPlacedConsumer(ILogger<OrderPlacedConsumer> logger, IHubContext<OrderHub> hubContext)
        {
            _logger = logger;
            _hubContext = hubContext;
        }

        // The Consume method executes whenever a new message (event) is received
        public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
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
            // Send real-time notification
            await _hubContext.Clients.Group(message.UserId).SendAsync("ReceiveOrderNotification", new 
            {
                OrderId = message.OrderId,
                Status = "Order Placed Successfully!",
                TotalAmount = message.TotalAmount
            });

            // Returning implicitly in async Task method
        }
    }
}