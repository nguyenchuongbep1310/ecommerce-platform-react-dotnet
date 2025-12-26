using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Serilog;
using OpenTelemetry.Metrics;
using ApiGateway.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Configure the builder to use ocelot.json configuration file
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Standard services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// Add Ocelot services
builder.Services.AddOcelot().AddConsul();

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
    .AddJwtBearer("ApiSecurity", options =>
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
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder
            .WithOrigins(
                "http://localhost:5173",
                "https://elidia-counterpaned-juli.ngrok-free.dev", 
                "https://ecommerce-platform-react-dotnet.vercel.app")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation() // Monitor inbound HTTP requests
               .AddPrometheusExporter(); // Enable the /metrics endpoint
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Swagger UI is not typically run on the gateway in production, but useful for dev.
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware pipeline order is important!
// 1. Exception handling (should be first to catch all errors)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. Correlation ID (early in pipeline to track requests)
app.UseMiddleware<CorrelationIdMiddleware>();

// 3. Request logging (after correlation ID)
app.UseMiddleware<RequestLoggingMiddleware>();

// 4. CORS (before Ocelot)
app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.UseOpenTelemetryPrometheusScrapingEndpoint(); // Exposes the metrics at /metrics
app.MapHealthChecks("/health");

// 1. Map Controllers (optional, but good practice if gateway has its own endpoints)
app.MapControllers();

// 2. Add Ocelot middleware to the pipeline
await app.UseOcelot();

app.Run();