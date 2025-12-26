using System.Net;
using System.Net.Sockets;
using Consul;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MassTransit;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ProductCatalogService.Infrastructure.Authorization;
using ProductCatalogService.Middleware;

// Clean Architecture Layers
using ProductCatalogService.Application;
using ProductCatalogService.Infrastructure;
using ProductCatalogService.Infrastructure.Persistence;


var builder = WebApplication.CreateBuilder(args);
var registrationId = $"{builder.Environment.ApplicationName}-{builder.Environment.EnvironmentName}";

// ============================================================================
// CLEAN ARCHITECTURE - Dependency Injection
// ============================================================================

// Add Application Layer (MediatR, AutoMapper, FluentValidation, Behaviors)
builder.Services.AddApplication();

// Add Infrastructure Layer (DbContext, Repositories, Services, Cache, Elasticsearch)
builder.Services.AddInfrastructure(builder.Configuration);

// --- AUTHENTICATION & AUTHORIZATION ---
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

if (!string.IsNullOrEmpty(secretKey))
{
    var key = Encoding.UTF8.GetBytes(secretKey);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Only for dev
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ClockSkew = TimeSpan.Zero
        };
    });
}
else
{
    Console.WriteLine("Warning: JwtSettings:SecretKey is missing configuration.");
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// ============================================================================
// API Layer Configuration
// ============================================================================

builder.Services.AddControllers();

// --- API VERSIONING ---
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
        new Asp.Versioning.UrlSegmentApiVersionReader(),
        new Asp.Versioning.HeaderApiVersionReader("X-Api-Version"),
        new Asp.Versioning.QueryStringApiVersionReader("api-version")
    );
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with API versioning
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() 
    {
        Title = "Product Catalog API",
        Version = "v1",
        Description = "Product Catalog Service API - Version 1.0"
    });
    
    options.SwaggerDoc("v2", new() 
    {
        Title = "Product Catalog API",
        Version = "v2",
        Description = "Product Catalog Service API - Version 2.0 (Enhanced with pagination, sorting, and metadata)"
    });
});


// --- HEALTH CHECKS ---
builder.Services.AddHealthChecks()
    // Database health check
    .AddCheck<ProductCatalogService.Infrastructure.HealthChecks.DatabaseHealthCheck>(
        "database",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: new[] { "db", "sql", "postgresql" })
    
    // Redis health check
    .AddCheck<ProductCatalogService.Infrastructure.HealthChecks.RedisCacheHealthCheck>(
        "redis_cache",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
        tags: new[] { "cache", "redis" })
    
    // RabbitMQ health check
    .AddRabbitMQ(
        rabbitConnectionString: builder.Configuration.GetConnectionString("RabbitMQ") ?? "amqp://guest:guest@rabbitmq:5672",
        name: "rabbitmq",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
        tags: new[] { "messaging", "rabbitmq" })
    
    // Elasticsearch health check
    .AddElasticsearch(
        builder.Configuration.GetConnectionString("Elasticsearch") ?? "http://elasticsearch:9200",
        name: "elasticsearch",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
        tags: new[] { "search", "elasticsearch" })
    
    // Self health check (memory, CPU)
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Service is running"), 
        tags: new[] { "self" });

// Health Checks UI
builder.Services.AddHealthChecksUI(options =>
{
    options.SetEvaluationTimeInSeconds(30); // Check every 30 seconds
    options.MaximumHistoryEntriesPerEndpoint(50);
    options.AddHealthCheckEndpoint("ProductCatalogService", "/health");
})
.AddInMemoryStorage();

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
    consulConfig.Address = new Uri(builder.Configuration.GetConnectionString("Consul") ?? "http://consul:8500");
}));


// 3. MassTransit Setup
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ProductCatalogService.Infrastructure.Messaging.Consumers.ReserveStockConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitUri = new Uri(builder.Configuration.GetConnectionString("RabbitMQ") ?? "amqp://guest:guest@rabbitmq:5672");
        cfg.Host(rabbitUri.Host, rabbitUri.Port > 0 ? (ushort)rabbitUri.Port : (ushort)5672, "/", h =>
        {
            var userInfo = rabbitUri.UserInfo.Split(':');
            h.Username(userInfo.Length > 0 ? userInfo[0] : "guest");
            h.Password(userInfo.Length > 1 ? userInfo[1] : "guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});

// 5. Hangfire Setup for Scheduled Tasks
builder.Services.AddHangfire(config =>
{
    config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options =>
        {
            options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"));
        });
});

// Add Hangfire server
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 5; // Number of concurrent jobs
    options.ServerName = $"ProductCatalogService-{Environment.MachineName}";
});

// Register scheduled jobs class
builder.Services.AddScoped<ProductCatalogService.Infrastructure.Jobs.Scheduled.ProductCatalogJobs>();

// 6. Background Services
builder.Services.AddHostedService<ProductCatalogService.Infrastructure.BackgroundServices.ElasticsearchSyncService>();
builder.Services.AddHostedService<ProductCatalogService.Infrastructure.BackgroundServices.CacheWarmingService>();
builder.Services.AddHostedService<ProductCatalogService.Infrastructure.BackgroundServices.InventoryMonitoringService>();


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

// --- CUSTOM MIDDLEWARE PIPELINE ---
// Apply all custom middleware in the recommended order
app.UseCustomMiddleware();

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Catalog API V1");
        options.SwaggerEndpoint("/swagger/v2/swagger.json", "Product Catalog API V2");
        options.DisplayRequestDuration();
    });
}

app.UseAuthentication();
app.UseAuthorization();

// --- HANGFIRE DASHBOARD ---
// Access at: http://localhost:8080/hangfire
app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    DashboardTitle = "Product Catalog - Background Jobs",
    StatsPollingInterval = 2000, // 2 seconds
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Configure recurring jobs
ProductCatalogService.Infrastructure.Jobs.Scheduled.HangfireJobScheduler.ConfigureRecurringJobs();

app.MapControllers();

// --- HEALTH CHECK ENDPOINTS ---
// Detailed health check with JSON response
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse,
    ResultStatusCodes =
    {
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy] = StatusCodes.Status200OK,
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded] = StatusCodes.Status200OK,
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

// Readiness probe (for Kubernetes)
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db") || check.Tags.Contains("cache"),
    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
});

// Liveness probe (for Kubernetes)
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("self"),
    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
});

// Health Checks UI Dashboard
app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui";
    options.ApiPath = "/health-ui-api";
});

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
            // DbInitializer.Initialize(context); // Seed data is now in DbContext.OnModelCreating
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
    
    // Initialize Elasticsearch index and bulk index products
    try
    {
        var elasticsearchService = services.GetRequiredService<ProductCatalogService.Application.Common.Interfaces.IElasticsearchService>();
        
        // Bulk index all existing products
        var products = await context.Products.ToListAsync();
        if (products.Any())
        {
            await elasticsearchService.BulkIndexProductsAsync(products);
            logger.LogInformation("Bulk indexed {Count} products to Elasticsearch.", products.Count);
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to initialize Elasticsearch. Search functionality may be limited.");
    }
}

app.Run();
