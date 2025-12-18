using MassTransit;
using Shared.Messages.Commands;
using Shared.Messages.Events;
using Stripe;

namespace PaymentService.Consumers
{
    public class ProcessPaymentConsumer : IConsumer<IProcessPaymentCommand>
    {
        private readonly ILogger<ProcessPaymentConsumer> _logger;
        private readonly IConfiguration _configuration;

        public ProcessPaymentConsumer(ILogger<ProcessPaymentConsumer> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task Consume(ConsumeContext<IProcessPaymentCommand> context)
        {
            _logger.LogInformation("Processing payment for Order {OrderId}, Amount: {Amount}", context.Message.OrderId, context.Message.Amount);

            try
            {
                var secretKey = _configuration["StripeSettings:SecretKey"];
                
                // Mock success for development or specific key
                if (string.IsNullOrEmpty(secretKey) || secretKey == "sk_test_placeholder")
                {
                    _logger.LogInformation("Using mock payment for Order {OrderId}", context.Message.OrderId);
                    await Task.Delay(500); // Simulate network
                }
                else
                {
                    StripeConfiguration.ApiKey = secretKey;
                    var service = new PaymentIntentService();
                    var options = new PaymentIntentCreateOptions
                    {
                        Amount = (long)(context.Message.Amount * 100),
                        Currency = "usd",
                        PaymentMethod = context.Message.PaymentMethodId,
                        Confirm = true,
                        AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                        {
                            Enabled = true,
                            AllowRedirects = "never"
                        }
                    };
                    
                    var intent = await service.CreateAsync(options);
                    if (intent.Status != "succeeded")
                    {
                        throw new Exception($"Payment failed with status: {intent.Status}");
                    }
                }

                _logger.LogInformation("Payment completed for Order {OrderId}", context.Message.OrderId);
                await context.Publish<IPaymentCompletedEvent>(new
                {
                    OrderId = context.Message.OrderId,
                    TransactionId = Guid.NewGuid().ToString() // In real app, use Stripe Intent ID
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment failed for Order {OrderId}", context.Message.OrderId);
                await context.Publish<IPaymentFailedEvent>(new
                {
                    OrderId = context.Message.OrderId,
                    Reason = ex.Message
                });
            }
        }
    }
}
