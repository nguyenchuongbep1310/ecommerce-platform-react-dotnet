using System.Net;
using System.Net.Sockets;
using Consul;
using Microsoft.EntityFrameworkCore;
using ProductCatalogService.Data;

var builder = WebApplication.CreateBuilder(args);
var registrationId = $"{builder.Environment.ApplicationName}-{builder.Environment.EnvironmentName}";

// 1. Database Setup
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ProductDbContext>(options => options.UseNpgsql(connectionString));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks(); // Add Health Checks

// Add Consul service registration
builder.Services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
{
    consulConfig.Address = new Uri("http://consul:8500");
}));

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
        Name = "productservice", // ⬅️ The key name Ocelot will look up
        Address = ipAddress,
        Port = 8080, // Internal container port
        Tags = new[] { "productservice", "catalog" },
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health"); // Expose Health Check Endpoint

// Apply Migrations on Startup and Seed Data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    context.Database.Migrate();
}

app.Run();
