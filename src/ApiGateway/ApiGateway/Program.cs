using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Serilog;
using OpenTelemetry.Metrics;

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
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder
            .WithOrigins("https://media-only-develop-prep.trycloudflare.com", "https://ecommerce-platform-react-dotnet.vercel.app")
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

app.UseCors("CorsPolicy"); // 1. Enable CORS before Ocelot

app.UseOpenTelemetryPrometheusScrapingEndpoint(); // Exposes the metrics at /metrics
app.MapHealthChecks("/health");

// 1. Map Controllers (optional, but good practice if gateway has its own endpoints)
app.MapControllers();

// 2. Add Ocelot middleware to the pipeline
await app.UseOcelot();

app.Run();