using System.Net;
using System.Net.Sockets;
using Consul;
using Microsoft.EntityFrameworkCore;
using ProductCatalogService.Data;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
var registrationId = $"{builder.Environment.ApplicationName}-{builder.Environment.EnvironmentName}";

// 1. Database Setup
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ProductDbContext>(options => options.UseNpgsql(connectionString));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ProductDbContext>(); // Add Health Checks

// --- OBSERVABILITY (OpenTelemetry) ---
const string serviceName = "ProductCatalogService";

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing =>
    {
       tracing
          .AddAspNetCoreInstrumentation()
          .AddHttpClientInstrumentation()
          .AddEntityFrameworkCoreInstrumentation()
          .AddOtlpExporter(options =>
          {
              options.Endpoint = new Uri("http://jaeger:4317"); // OTLP gRPC port
          });
    });

// Add Consul service registration
builder.Services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
{
    consulConfig.Address = new Uri("http://consul:8500");
}));

// 2. Redis Cache Setup
var redisConnection = builder.Configuration.GetConnectionString("Redis");
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection ?? "localhost:6379";
    options.InstanceName = "ProductCatalog_";
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
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<ProductDbContext>();

    // Retry logic for DB connection
    for (int i = 0; i < 5; i++)
    {
        try
        {
            context.Database.Migrate();
            DbInitializer.Initialize(context);
            logger.LogInformation("Database migrated and initialized successfully.");
            break; 
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Database migration failed (Attempt {i + 1}/5): {ex.Message}");
            if (i == 4) throw; // Throw on last attempt
            System.Threading.Thread.Sleep(2000);
        }
    }
}

app.Run();
