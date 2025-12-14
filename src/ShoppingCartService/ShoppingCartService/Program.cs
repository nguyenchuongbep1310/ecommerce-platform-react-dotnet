using System.Net;
using System.Net.Sockets;
using Consul;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using ShoppingCartService.Data;

var builder = WebApplication.CreateBuilder(args);
var registrationId = $"{builder.Environment.ApplicationName}-{builder.Environment.EnvironmentName}";

builder.Services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
{
    consulConfig.Address = new Uri("http://consul:8500");
}));

// 1. Database Setup
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CartDbContext>(options => options.UseNpgsql(connectionString));

// 2. HttpClientFactory Setup for Inter-Service Communication
// This allows the Cart Service to reliably call the Product Catalog Service.
builder.Services.AddHttpClient(
    "ProductClient",
    client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ProductCatalogServiceUrl"]!);
    }
);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CartDbContext>(); // Add Health Checks

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ShoppingCartService.Consumers.OrderPlacedConsumer>();

    x.UsingRabbitMq(
        (context, cfg) =>
        {
            cfg.Host(
                "rabbitmq",
                h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                }
            );

            // Configures the consumer endpoints automatically
            cfg.ConfigureEndpoints(context);
        }
    );
});

var app = builder.Build();

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
var consulClient = app.Services.GetRequiredService<IConsulClient>();

lifetime.ApplicationStarted.Register(() =>
{
    var hostName = Dns.GetHostName();
    var ipAddress =
        Dns.GetHostEntry(hostName)
            .AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork)
            ?.ToString()
        ?? "localhost";

    var registration = new AgentServiceRegistration()
    {
        ID = registrationId,
        Name = "cartservice", // ⬅️ The key name Ocelot will look up
        Address = ipAddress,
        Port = 8080, // Internal container port
        Tags = new[] { "cartservice", "shopping" },
        Check = new AgentServiceCheck()
        {
            HTTP = $"http://{ipAddress}:8080/health",
            Interval = TimeSpan.FromSeconds(10),
            DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30),
            Timeout = TimeSpan.FromSeconds(5),
        },
    };
    consulClient.Agent.ServiceRegister(registration).Wait();
});

lifetime.ApplicationStopped.Register(() =>
{
    consulClient.Agent.ServiceDeregister(registrationId).Wait();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health"); // Expose Health Check Endpoint

// Apply Migrations on Startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CartDbContext>();
    context.Database.Migrate();
}

app.Run();
