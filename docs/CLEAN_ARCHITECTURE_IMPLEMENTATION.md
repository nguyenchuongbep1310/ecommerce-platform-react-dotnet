# Clean Architecture Implementation Guide

## Step-by-Step Implementation

This guide provides detailed steps to restructure the ProductCatalogService to Clean Architecture. Once completed, this pattern can be replicated for other services.

## Phase 1: Create Project Structure

### Step 1: Create Solution and Projects

```bash
cd /Users/chuongnguyen/Downloads/Coding/ecommerce-platform/src/ProductCatalogService

# Create solution file
dotnet new sln -n ProductCatalogService

# Create Domain project (Class Library)
dotnet new classlib -n ProductCatalogService.Domain -f net10.0

# Create Application project (Class Library)
dotnet new classlib -n ProductCatalogService.Application -f net10.0

# Create Infrastructure project (Class Library)
dotnet new classlib -n ProductCatalogService.Infrastructure -f net10.0

# Rename existing project to API
mv ProductCatalogService ProductCatalogService.API

# Add projects to solution
dotnet sln add ProductCatalogService.Domain/ProductCatalogService.Domain.csproj
dotnet sln add ProductCatalogService.Application/ProductCatalogService.Application.csproj
dotnet sln add ProductCatalogService.Infrastructure/ProductCatalogService.Infrastructure.csproj
dotnet sln add ProductCatalogService.API/ProductCatalogService.csproj
```

### Step 2: Set Up Project References

```bash
# Application depends on Domain
cd ProductCatalogService.Application
dotnet add reference ../ProductCatalogService.Domain/ProductCatalogService.Domain.csproj

# Infrastructure depends on Application and Domain
cd ../ProductCatalogService.Infrastructure
dotnet add reference ../ProductCatalogService.Domain/ProductCatalogService.Domain.csproj
dotnet add reference ../ProductCatalogService.Application/ProductCatalogService.Application.csproj

# API depends on Application and Infrastructure
cd ../ProductCatalogService.API
dotnet add reference ../ProductCatalogService.Application/ProductCatalogService.Application.csproj
dotnet add reference ../ProductCatalogService.Infrastructure/ProductCatalogService.Infrastructure.csproj
```

## Phase 2: Domain Layer

### Files to Move to Domain Project

```
ProductCatalogService.Domain/
├── Entities/
│   ├── Product.cs                    # FROM: Models/Product.cs
│   └── ProductDocument.cs            # FROM: Models/ProductDocument.cs
├── Events/
│   ├── ProductCreatedEvent.cs        # NEW
│   ├── ProductUpdatedEvent.cs        # NEW
│   └── ProductDeletedEvent.cs        # NEW
├── Exceptions/
│   ├── ProductNotFoundException.cs   # NEW
│   └── InvalidProductException.cs    # NEW
├── Interfaces/
│   └── IProductRepository.cs         # NEW (extract from current code)
└── Common/
    └── BaseEntity.cs                 # NEW (if needed)
```

### Domain Layer Rules

- ✅ No dependencies on other projects
- ✅ Pure C# code (no framework dependencies)
- ✅ Contains business entities and rules
- ✅ Defines interfaces (repositories, services)
- ❌ No database concerns
- ❌ No HTTP concerns
- ❌ No external service dependencies

## Phase 3: Application Layer

### Files to Move to Application Project

```
ProductCatalogService.Application/
├── Common/
│   ├── Interfaces/
│   │   ├── ICacheService.cs          # FROM: Application/Common/Interfaces/
│   │   ├── IElasticsearchService.cs  # NEW (extract interface)
│   │   └── IProductDbContext.cs      # NEW (extract interface)
│   ├── Mappings/
│   │   └── ProductMappingProfile.cs  # FROM: Application/Mappings/
│   ├── Models/
│   │   └── ProductDto.cs             # FROM: Application/DTOs/
│   └── Behaviors/
│       ├── LoggingBehavior.cs        # FROM: Application/Behaviors/
│       ├── ValidationBehavior.cs     # FROM: Application/Behaviors/
│       └── PerformanceBehavior.cs    # FROM: Application/Behaviors/
├── Products/
│   ├── Commands/
│   │   ├── CreateProduct/
│   │   │   ├── CreateProductCommand.cs         # FROM: Application/Commands/
│   │   │   ├── CreateProductCommandHandler.cs  # FROM: Application/Handlers/
│   │   │   └── CreateProductCommandValidator.cs # FROM: Application/Validators/
│   │   ├── UpdateProduct/
│   │   │   ├── UpdateProductCommand.cs
│   │   │   ├── UpdateProductCommandHandler.cs
│   │   │   └── UpdateProductCommandValidator.cs
│   │   └── DeleteProduct/
│   │       ├── DeleteProductCommand.cs
│   │       ├── DeleteProductCommandHandler.cs
│   │       └── DeleteProductCommandValidator.cs
│   └── Queries/
│       ├── GetProducts/
│       │   ├── GetProductsQuery.cs             # FROM: Application/Queries/
│       │   └── GetProductsQueryHandler.cs      # FROM: Application/Handlers/
│       ├── GetProductById/
│       │   ├── GetProductByIdQuery.cs
│       │   └── GetProductByIdQueryHandler.cs
│       └── SearchProducts/
│           ├── SearchProductsQuery.cs
│           └── SearchProductsQueryHandler.cs
└── DependencyInjection.cs            # NEW (register Application services)
```

### Application Layer Rules

- ✅ Depends only on Domain
- ✅ Contains use cases (commands/queries)
- ✅ Contains DTOs
- ✅ Contains application interfaces
- ✅ Contains AutoMapper profiles
- ✅ Contains MediatR behaviors
- ❌ No infrastructure implementation details
- ❌ No database implementation
- ❌ No external service implementation

## Phase 4: Infrastructure Layer

### Files to Move to Infrastructure Project

```
ProductCatalogService.Infrastructure/
├── Persistence/
│   ├── Configurations/
│   │   └── ProductConfiguration.cs   # FROM: Data/ (EF Core config)
│   ├── Migrations/                   # FROM: Migrations/
│   ├── Repositories/
│   │   └── ProductRepository.cs      # NEW (implement IProductRepository)
│   └── ProductDbContext.cs           # FROM: Data/ProductDbContext.cs
├── Services/
│   ├── ElasticsearchService.cs       # FROM: Services/
│   ├── RedisCacheService.cs          # FROM: Infrastructure/Caching/
│   └── DateTimeService.cs            # NEW (if needed)
├── BackgroundJobs/
│   ├── ProductCatalogJobs.cs         # FROM: ScheduledJobs/
│   └── HangfireJobScheduler.cs       # FROM: ScheduledJobs/
├── BackgroundServices/
│   ├── ElasticsearchSyncService.cs   # FROM: BackgroundServices/
│   ├── CacheWarmingService.cs        # FROM: BackgroundServices/
│   └── InventoryMonitoringService.cs # FROM: BackgroundServices/
├── Consumers/
│   └── ReserveStockConsumer.cs       # FROM: Consumers/
├── HealthChecks/
│   ├── DatabaseHealthCheck.cs        # FROM: Infrastructure/HealthChecks/
│   └── RedisCacheHealthCheck.cs      # FROM: Infrastructure/HealthChecks/
└── DependencyInjection.cs            # NEW (register Infrastructure services)
```

### Infrastructure Layer Rules

- ✅ Depends on Application and Domain
- ✅ Implements application interfaces
- ✅ Contains EF Core DbContext
- ✅ Contains repository implementations
- ✅ Contains external service implementations
- ✅ Contains background jobs
- ✅ Contains message consumers
- ❌ No HTTP/API concerns
- ❌ No controllers

## Phase 5: API/Presentation Layer

### Files to Keep in API Project

```
ProductCatalogService.API/
├── Controllers/
│   ├── V1/
│   │   └── ProductsController.cs     # KEEP (update namespaces)
│   ├── V2/
│   │   └── ProductsController.cs     # KEEP (update namespaces)
│   ├── SearchController.cs           # KEEP
│   └── BackgroundJobsController.cs   # KEEP
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs # FROM: Middleware/
│   ├── RequestLoggingMiddleware.cs    # FROM: Middleware/
│   ├── CorrelationIdMiddleware.cs     # FROM: Middleware/
│   └── Extensions/
│       └── MiddlewareExtensions.cs    # FROM: Middleware/
├── Filters/
│   └── HangfireAuthorizationFilter.cs # FROM: HangfireAuthorizationFilter.cs
├── Program.cs                         # UPDATE (register all layers)
├── appsettings.json                   # KEEP
├── appsettings.Development.json       # KEEP
└── Dockerfile                         # UPDATE (multi-project build)
```

### API Layer Rules

- ✅ Depends on Application and Infrastructure
- ✅ Contains controllers
- ✅ Contains middleware
- ✅ Contains filters
- ✅ Contains Program.cs
- ✅ Configures dependency injection
- ❌ No business logic
- ❌ No data access logic

## Phase 6: Update Dependency Injection

### Domain/DependencyInjection.cs

```csharp
// Domain has no dependencies, no DI needed
```

### Application/DependencyInjection.cs

```csharp
using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ProductCatalogService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        // FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Pipeline Behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        return services;
    }
}
```

### Infrastructure/DependencyInjection.cs

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductCatalogService.Application.Common.Interfaces;
using ProductCatalogService.Domain.Interfaces;
using ProductCatalogService.Infrastructure.Persistence;
using ProductCatalogService.Infrastructure.Persistence.Repositories;
using ProductCatalogService.Infrastructure.Services;

namespace ProductCatalogService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ProductDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Repositories
        services.AddScoped<IProductRepository, ProductRepository>();

        // Services
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IElasticsearchService, ElasticsearchService>();

        // Redis
        var redisConnection = configuration.GetConnectionString("Redis");
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection ?? "localhost:6379";
            options.InstanceName = "ProductCatalog_";
        });

        // Elasticsearch
        var elasticsearchUrl = configuration.GetConnectionString("Elasticsearch")
            ?? "http://elasticsearch:9200";
        services.AddSingleton(sp =>
        {
            var settings = new Elastic.Clients.Elasticsearch.ElasticsearchClientSettings(
                new Uri(elasticsearchUrl))
                .DefaultIndex("products");
            return new Elastic.Clients.Elasticsearch.ElasticsearchClient(settings);
        });

        // Background Services
        services.AddHostedService<ElasticsearchSyncService>();
        services.AddHostedService<CacheWarmingService>();
        services.AddHostedService<InventoryMonitoringService>();

        // Hangfire
        services.AddHangfire(config =>
        {
            config.UsePostgreSqlStorage(options =>
            {
                options.UseNpgsqlConnection(connectionString);
            });
        });
        services.AddHangfireServer();
        services.AddScoped<ProductCatalogJobs>();

        return services;
    }
}
```

### API/Program.cs (Updated)

```csharp
using ProductCatalogService.Application;
using ProductCatalogService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Clean Architecture layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ... rest of configuration

var app = builder.Build();

// ... middleware pipeline

app.Run();
```

## Phase 7: Update Dockerfiles

### Multi-Stage Dockerfile for API Project

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["ProductCatalogService.Domain/ProductCatalogService.Domain.csproj", "ProductCatalogService.Domain/"]
COPY ["ProductCatalogService.Application/ProductCatalogService.Application.csproj", "ProductCatalogService.Application/"]
COPY ["ProductCatalogService.Infrastructure/ProductCatalogService.Infrastructure.csproj", "ProductCatalogService.Infrastructure/"]
COPY ["ProductCatalogService.API/ProductCatalogService.csproj", "ProductCatalogService.API/"]
COPY ["../Shared.Messages/Shared.Messages.csproj", "Shared.Messages/"]

# Restore dependencies
RUN dotnet restore "ProductCatalogService.API/ProductCatalogService.csproj"

# Copy source code
COPY ProductCatalogService.Domain/ ProductCatalogService.Domain/
COPY ProductCatalogService.Application/ ProductCatalogService.Application/
COPY ProductCatalogService.Infrastructure/ ProductCatalogService.Infrastructure/
COPY ProductCatalogService.API/ ProductCatalogService.API/
COPY ../Shared.Messages/ Shared.Messages/

# Build
WORKDIR "/src/ProductCatalogService.API"
RUN dotnet build "ProductCatalogService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProductCatalogService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductCatalogService.dll"]
```

## Testing Strategy

### Unit Tests Structure

```
ProductCatalogService.Tests/
├── Domain.Tests/
│   ├── Entities/
│   │   └── ProductTests.cs
│   └── Specifications/
│       └── ProductSpecificationsTests.cs
├── Application.Tests/
│   ├── Products/
│   │   ├── Commands/
│   │   │   └── CreateProductCommandHandlerTests.cs
│   │   └── Queries/
│   │       └── GetProductsQueryHandlerTests.cs
│   └── Mappings/
│       └── ProductMappingProfileTests.cs
└── Infrastructure.Tests/
    ├── Repositories/
    │   └── ProductRepositoryTests.cs
    └── Services/
        └── ElasticsearchServiceTests.cs
```

## Migration Checklist

- [ ] Create new project structure
- [ ] Set up project references
- [ ] Move Domain entities
- [ ] Create Domain interfaces
- [ ] Move Application use cases
- [ ] Move Application DTOs
- [ ] Move AutoMapper profiles
- [ ] Create Application DI
- [ ] Move Infrastructure implementations
- [ ] Create Infrastructure DI
- [ ] Update API controllers
- [ ] Update Program.cs
- [ ] Update Dockerfile
- [ ] Update docker-compose.yml
- [ ] Test build locally
- [ ] Run unit tests
- [ ] Test API endpoints
- [ ] Update documentation

## Common Issues & Solutions

### Issue: Circular Dependencies

**Solution**: Ensure dependency flow is correct (API → Infrastructure → Application → Domain)

### Issue: Namespace Conflicts

**Solution**: Use consistent naming: `{ServiceName}.{Layer}.{Feature}`

### Issue: Missing Dependencies

**Solution**: Check project references and NuGet packages in each layer

### Issue: Docker Build Fails

**Solution**: Update Dockerfile to copy all projects in correct order

## Next Steps

1. Start with ProductCatalogService
2. Follow this guide step-by-step
3. Test thoroughly after each phase
4. Document any deviations or learnings
5. Apply pattern to other services

## Expected Timeline

- **Project Setup**: 1 hour
- **Domain Layer**: 2 hours
- **Application Layer**: 3 hours
- **Infrastructure Layer**: 4 hours
- **API Layer**: 2 hours
- **Testing & Fixes**: 3 hours
- **Total**: ~15 hours per service
