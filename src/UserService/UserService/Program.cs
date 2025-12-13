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
    builder.Services.AddHealthChecks(); // Add Health Checks

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

