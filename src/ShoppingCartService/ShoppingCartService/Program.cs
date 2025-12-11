using Microsoft.EntityFrameworkCore;
using ShoppingCartService.Data;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Setup
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CartDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. HttpClientFactory Setup for Inter-Service Communication
// This allows the Cart Service to reliably call the Product Catalog Service.
builder.Services.AddHttpClient("ProductClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ProductCatalogServiceUrl"]!);
});


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ShoppingCartService.Consumers.OrderPlacedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Configures the consumer endpoints automatically
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// Apply Migrations on Startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CartDbContext>();
    context.Database.Migrate();
}

app.Run();