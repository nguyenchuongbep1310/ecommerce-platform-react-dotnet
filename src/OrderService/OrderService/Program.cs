using Microsoft.EntityFrameworkCore;
using OrderService.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Setup
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. HttpClientFactory Setup
builder.Services.AddHttpClient("CartClient", client => client.BaseAddress = new Uri(builder.Configuration["CartServiceUrl"]!));
builder.Services.AddHttpClient("PaymentClient", client => client.BaseAddress = new Uri(builder.Configuration["PaymentServiceUrl"]!));
builder.Services.AddHttpClient("ProductClient", client => client.BaseAddress = new Uri(builder.Configuration["ProductCatalogServiceUrl"]!));


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ... standard pipeline configuration (Swagger, Authorization)

app.MapControllers();

// Apply Migrations on Startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    context.Database.Migrate();
}

app.Run();