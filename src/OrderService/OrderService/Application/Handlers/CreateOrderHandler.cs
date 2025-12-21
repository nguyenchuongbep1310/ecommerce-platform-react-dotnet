using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Commands;
using OrderService.Data;
using OrderService.Models;
using Shared.Messages.Events;
using MassTransit;
using Stripe;
using Microsoft.Extensions.Configuration;

namespace OrderService.Application.Handlers
{
    public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Order>
    {
        private readonly OrderDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;

        public CreateOrderHandler(OrderDbContext context, IPublishEndpoint publishEndpoint, IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
            _clientFactory = clientFactory;
            _configuration = configuration;
        }

        public async Task<Order> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var correlationId = Guid.NewGuid();
                var total = request.Items.Sum(i => i.Quantity * i.UnitPrice);
                var order = new Order 
                { 
                    CorrelationId = correlationId,
                    UserId = request.UserId, 
                    TotalAmount = total, 
                    Status = "Pending" 
                };

                foreach (var item in request.Items)
                {
                    order.Items.Add(new OrderItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(cancellationToken);

                // Publish Event within the same transaction - MassTransit Outbox will handle this
                await _publishEndpoint.Publish<IOrderSubmittedEvent>(new
                {
                    OrderId = correlationId,
                    UserId = request.UserId,
                    TotalAmount = total,
                    Items = request.Items.Select(i => new Shared.Messages.Events.OrderItemDto
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList()
                }, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return order;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
