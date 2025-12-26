using Serilog;
using MassTransit;
using NotificationService.Consumers;
using NotificationService.Hubs;
using NotificationService.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

Console.WriteLine("--- STARTING NOTIFICATION SERVICE ---");

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Serilog.Debugging.SelfLog.Enable(Console.Error);
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Seq("http://seq_server") 
    .CreateLogger();

builder.Host.UseSerilog();

// Add SignalR
builder.Services.AddSignalR();

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

        // For SignalR: Get token from query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
}
else
{
    Console.WriteLine("Warning: JwtSettings:SecretKey is missing configuration.");
}

// Configure CORS for SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder
            .AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowed((host) => true) // Allow any origin for development
            .AllowCredentials();
    });
});

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
builder.Services.AddHealthChecks();

var app = builder.Build();

// Middleware pipeline order is important!
// 1. Exception handling (should be first to catch all errors)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. Request logging
app.UseMiddleware<RequestLoggingMiddleware>();

// ... standard pipeline
app.UseCors("CorsPolicy");
app.MapHealthChecks("/health");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<OrderHub>("/notificationHub");
app.Run();