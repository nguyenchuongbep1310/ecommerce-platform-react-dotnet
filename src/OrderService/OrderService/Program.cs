using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrderService.Sagas;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Setup
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. HttpClientFactory Setup
builder.Services.AddHttpClient("CartClient", client => client.BaseAddress = new Uri(builder.Configuration["CartServiceUrl"]!))
    .AddStandardResilienceHandler();

builder.Services.AddHttpClient("PaymentClient", client => client.BaseAddress = new Uri(builder.Configuration["PaymentServiceUrl"]!))
    .AddStandardResilienceHandler();

builder.Services.AddHttpClient("ProductClient", client => client.BaseAddress = new Uri(builder.Configuration["ProductCatalogServiceUrl"]!))
    .AddStandardResilienceHandler();

// 3. Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderDbContext>();

// --- OBSERVABILITY (OpenTelemetry) ---
const string serviceName = "OrderService";

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
          .AddSource(MassTransit.Logging.DiagnosticHeaders.DefaultListenerName) // MassTransit Tracing
          .AddOtlpExporter(options =>
          {
              options.Endpoint = new Uri("http://jaeger:4317"); // OTLP gRPC port
          });
    });

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// --- AUTHENTICATION & AUTHORIZATION ---
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

// Ensure key is not null before proceeding
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
    // Fallback or log warning if JWT is not configured (prevent startup crash if config is missing)
    Console.WriteLine("Warning: JwtSettings:SecretKey is missing configuration.");
}

builder.Services.AddMassTransit(x =>
{
    // Configure the Transactional Outbox
    x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
    {
        o.QueryDelay = TimeSpan.FromSeconds(1);
        o.UsePostgres();
        o.UseBusOutbox();
    });

    // Register Saga
    x.AddSagaStateMachine<OrderStateMachine, OrderState>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<OrderDbContext>();
            r.UsePostgres();
        });
        
    x.AddConsumer<OrderService.Consumers.OrderCommandConsumers>();

    x.UsingRabbitMq((context, cfg) =>
    {
        // Connection to the RabbitMQ service defined in Docker Compose
        cfg.Host("rabbitmq", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        cfg.ConfigureEndpoints(context); 
    });
});

var app = builder.Build();

// ... standard pipeline configuration (Swagger, Authorization)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Apply Migrations on Startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    context.Database.Migrate();
}

app.Run();