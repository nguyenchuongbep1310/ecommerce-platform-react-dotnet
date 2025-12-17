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
                var total = request.Items.Sum(i => i.Quantity * i.UnitPrice);
                var order = new Order { UserId = request.UserId, TotalAmount = total, Status = "Processing" };

                // 1. Prepare Order Items
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

                // 2. Reduce Stock (Product Service Call) - ideally this should be moved to a consumer or saga, keeping it simple here for MVP migration
                var productClient = _clientFactory.CreateClient("ProductClient");
                foreach (var item in request.Items)
                {
                     var stockResponse = await productClient.PostAsJsonAsync($"/api/products/{item.ProductId}/reduce-stock",
                        new { Quantity = item.Quantity }, cancellationToken);

                    if (!stockResponse.IsSuccessStatusCode)
                    {
                        throw new Exception($"Stock update failed for Product ID: {item.ProductId}.");
                    }
                }

                // 3. Process Payment (Stripe)
                if (string.IsNullOrEmpty(request.PaymentMethodId))
                {
                    throw new Exception("Payment Method ID is required for processing.");
                }

                StripeConfiguration.ApiKey = _configuration["StripeSettings:SecretKey"];
                var paymentIntentService = new PaymentIntentService();
                
                try
                {
                    var paymentIntent = await paymentIntentService.CreateAsync(new PaymentIntentCreateOptions
                    {
                        Amount = (long)(total * 100),
                        Currency = "usd",
                        PaymentMethod = request.PaymentMethodId,
                        Confirm = true,
                        ReturnUrl = "https://localhost:5000/order-success",
                        AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                        {
                            Enabled = true,
                            AllowRedirects = "never"
                        }
                    }, cancellationToken: cancellationToken);

                    if (paymentIntent.Status != "succeeded")
                    {
                        throw new Exception($"Payment not successful. Status: {paymentIntent.Status}");
                    }
                }
                catch (StripeException ex)
                {
                    throw new Exception($"Stripe Error: {ex.Message}");
                }

                await transaction.CommitAsync(cancellationToken);

                // 4. Publish Event
                var orderPlacedEvent = new OrderPlacedEvent
                {
                    OrderId = order.Id,
                    UserId = order.UserId,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount,
                    Items = order.Items.Select(oi => new OrderItemEvent
                    {
                        ProductId = oi.ProductId,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice
                    }).ToList()
                };
                await _publishEndpoint.Publish(orderPlacedEvent, cancellationToken);

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
