using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;

var builder = WebApplication.CreateBuilder(args);

// Configure the builder to use ocelot.json configuration file
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Standard services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Ocelot services
builder.Services.AddOcelot().AddConsul();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Swagger UI is not typically run on the gateway in production, but useful for dev.
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 1. Map Controllers (optional, but good practice if gateway has its own endpoints)
app.MapControllers(); 

// 2. Add Ocelot middleware to the pipeline
await app.UseOcelot();

app.Run();