using System.Net;
using System.Net.Sockets;
using System.Text;
using Consul;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection; // Ensure this is present if using Minimal APIs
using Microsoft.IdentityModel.Tokens;
using UserService.Data;
using UserService.Models;
using Serilog;

// Configure Serilog to read configuration from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
        .Build())
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 1. Database Setup
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

    // 2. Identity Setup
    builder
        .Services.AddIdentity<ApplicationUser, IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

    // 3. JWT Configuration (Keys, Lifetime, Validation)
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var key = Encoding.ASCII.GetBytes(
        jwtSettings["SecretKey"] ?? throw new ArgumentNullException("SecretKey not configured.")
    );

    builder
        .Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; // Set to true in production
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
            };
        });

    // Add services to the container.
    builder.Services.AddControllers();

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(); // Add Health Checks

    // --- RATE LIMITING ---
    builder.Services.AddRateLimiter(rateLimiterOptions =>
    {
        // Global rate limiter - applies to all endpoints unless overridden
        rateLimiterOptions.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var userIdentifier = context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
            
            return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: userIdentifier,
                factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        });

        // Strict rate limiter for authentication endpoints (login, register)
        rateLimiterOptions.AddPolicy("auth", context =>
            System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

        // Token refresh rate limiter - more lenient than auth
        rateLimiterOptions.AddPolicy("refresh", context =>
            System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                    QueueLimit = 2
                }));

        // Sliding window for profile endpoints
        rateLimiterOptions.AddPolicy("profile", context =>
            System.Threading.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: context.User.Identity?.Name ?? "anonymous",
                factory: _ => new System.Threading.RateLimiting.SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 4,
                    QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

        // Token bucket for validation endpoint - allows bursts
        rateLimiterOptions.AddPolicy("validate", context =>
            System.Threading.RateLimiting.RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: _ => new System.Threading.RateLimiting.TokenBucketRateLimiterOptions
                {
                    TokenLimit = 15,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                    TokensPerPeriod = 15,
                    AutoReplenishment = true,
                    QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

        // Concurrency limiter for resource-intensive operations
        rateLimiterOptions.AddPolicy("concurrent", context =>
            System.Threading.RateLimiting.RateLimitPartition.GetConcurrencyLimiter(
                partitionKey: context.User.Identity?.Name ?? "anonymous",
                factory: _ => new System.Threading.RateLimiting.ConcurrencyLimiterOptions
                {
                    PermitLimit = 10,
                    QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                    QueueLimit = 5
                }));

        // Custom rejection response
        rateLimiterOptions.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            
            TimeSpan? retryAfterValue = null;
            if (context.Lease.TryGetMetadata(System.Threading.RateLimiting.MetadataName.RetryAfter, out var retryAfter))
            {
                retryAfterValue = retryAfter;
                context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
            }

            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = "Too many requests",
                message = "Rate limit exceeded. Please try again later.",
                retryAfterSeconds = retryAfterValue.HasValue ? (int)retryAfterValue.Value.TotalSeconds : (int?)null
            }, cancellationToken);
        };
    });

    var registrationId = $"{builder.Environment.ApplicationName}-{builder.Environment.EnvironmentName}";

    // --- Consul Configuration ---
    builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>(); // Needed for Consul

    builder.Services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
    {
        // Point to the Consul container using the Docker service name
        consulConfig.Address = new Uri("http://consul:8500");
    }));

    var app = builder.Build();
    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    var consulClient = app.Services.GetRequiredService<IConsulClient>();

    lifetime.ApplicationStarted.Register(() =>
    {
        // The health check is CRITICAL for Consul to monitor the service
        var httpPort = app.Configuration.GetValue<int>("ASPNETCORE_HTTP_PORTS");
        var registration = new AgentServiceRegistration()
        {
            ID = registrationId,
            Name = "userservice", // This is the service name Ocelot will look up!
            Address = "userservice", // Use the Docker Service Name!
            Port = httpPort, // The internal container port (8080)
            Tags = new[] { "userservice", "auth" },
            Check = new AgentServiceCheck()
            {
                HTTP = $"http://userservice:{httpPort}/health", // Health Check Endpoint
                Interval = TimeSpan.FromSeconds(10),
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30),
                Timeout = TimeSpan.FromSeconds(5),
            },
        };

        // Attempt registration
        consulClient.Agent.ServiceRegister(registration).Wait();
    });

    lifetime.ApplicationStopped.Register(() =>
    {
        // Deregister service upon graceful shutdown
        consulClient.Agent.ServiceDeregister(registrationId).Wait();
    });

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger(c =>
        {
            c.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0;
        });
        app.UseSwaggerUI();
    }

    app.UseRateLimiter(); // Enable rate limiting middleware
    app.UseAuthentication(); // Must be before UseAuthorization
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health"); // Expose Health Check Endpoint

    // Ensure a database is created and migrations applied on startup
    // NOTE: This is fine for development but should be handled by a proper migration tool/job in production.
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "User Service terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

