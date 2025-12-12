using MassTransit;
using NotificationService.Consumers;

Console.WriteLine("--- STARTING NOTIFICATION SERVICE ---");
var builder = WebApplication.CreateBuilder(args);

// Add MassTransit configuration
builder.Services.AddMassTransit(x =>
{
    // 1. Add the Consumer to the service collection
    x.AddConsumer<OrderPlacedConsumer>();

    // 2. Configure RabbitMQ connection and endpoint
    x.UsingRabbitMq((context, cfg) =>
    {
        // Connection to the RabbitMQ service defined in Docker Compose
        cfg.Host("rabbitmq", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Use standard endpoint configuration
        cfg.ConfigureEndpoints(context);
    });
});

// We keep the API boilerplate (optional for a pure consumer service)
builder.Services.AddControllers();

var app = builder.Build();

// ... standard pipeline
app.MapControllers();
app.Run();