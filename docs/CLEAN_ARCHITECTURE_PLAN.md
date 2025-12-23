# Clean Architecture Restructuring Plan

## Overview

This document outlines the plan to restructure the e-commerce microservices platform to follow **Clean Architecture** principles as defined by Robert C. Martin (Uncle Bob).

## Clean Architecture Principles

### Core Concepts

1. **Dependency Rule**: Dependencies point inward. Inner layers know nothing about outer layers.
2. **Independence**: Business logic is independent of frameworks, UI, databases, and external agencies.
3. **Testability**: Business rules can be tested without UI, database, web server, or external elements.
4. **Framework Independence**: Architecture doesn't depend on feature-laden software libraries.

### Layer Structure

```
┌─────────────────────────────────────────────────────────────┐
│                        Presentation                          │
│                    (API, Controllers)                        │
│                   Depends on: Application                    │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                       Application                            │
│           (Use Cases, DTOs, Interfaces)                      │
│                   Depends on: Domain                         │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                         Domain                               │
│              (Entities, Value Objects,                       │
│           Domain Events, Domain Services)                    │
│                   Depends on: Nothing                        │
└─────────────────────────────────────────────────────────────┘
                           ▲
┌──────────────────────────┴──────────────────────────────────┐
│                     Infrastructure                           │
│        (Data Access, External Services,                      │
│         File System, Email, etc.)                            │
│              Depends on: Application, Domain                 │
└─────────────────────────────────────────────────────────────┘
```

## Current vs. Target Structure

### Current Structure (Example: ProductCatalogService)

```
ProductCatalogService/
├── Application/           # Mixed concerns
├── BackgroundServices/    # Infrastructure concern
├── Consumers/            # Infrastructure concern
├── Controllers/          # Presentation concern
├── Data/                 # Infrastructure concern
├── Infrastructure/       # Partial implementation
├── Middleware/           # Infrastructure concern
├── Models/               # Domain entities (mixed with DTOs)
├── Services/             # Application services
└── Program.cs            # Entry point
```

**Issues:**

- ❌ Mixed concerns in Application folder
- ❌ No clear separation between Domain and Application
- ❌ Infrastructure scattered across multiple folders
- ❌ DTOs mixed with domain models
- ❌ Difficult to test business logic in isolation

### Target Structure (Clean Architecture)

```
ProductCatalogService/
├── ProductCatalogService.Domain/          # Core business logic
│   ├── Entities/                          # Domain entities
│   │   ├── Product.cs
│   │   └── ProductDocument.cs
│   ├── ValueObjects/                      # Value objects
│   ├── Events/                            # Domain events
│   │   └── ProductCreatedEvent.cs
│   ├── Exceptions/                        # Domain exceptions
│   │   └── ProductNotFoundException.cs
│   ├── Interfaces/                        # Domain interfaces
│   │   └── IProductRepository.cs
│   └── Specifications/                    # Business rules
│       └── ProductSpecifications.cs
│
├── ProductCatalogService.Application/     # Use cases & orchestration
│   ├── Common/
│   │   ├── Interfaces/                    # Application interfaces
│   │   │   ├── ICacheService.cs
│   │   │   ├── IElasticsearchService.cs
│   │   │   └── ICurrentUserService.cs
│   │   ├── Mappings/                      # AutoMapper profiles
│   │   │   └── ProductMappingProfile.cs
│   │   ├── Models/                        # DTOs
│   │   │   └── ProductDto.cs
│   │   └── Behaviors/                     # MediatR behaviors
│   │       ├── LoggingBehavior.cs
│   │       ├── ValidationBehavior.cs
│   │       └── PerformanceBehavior.cs
│   ├── Products/
│   │   ├── Commands/                      # Write operations
│   │   │   ├── CreateProduct/
│   │   │   │   ├── CreateProductCommand.cs
│   │   │   │   ├── CreateProductCommandHandler.cs
│   │   │   │   └── CreateProductCommandValidator.cs
│   │   │   ├── UpdateProduct/
│   │   │   └── DeleteProduct/
│   │   └── Queries/                       # Read operations
│   │       ├── GetProducts/
│   │       │   ├── GetProductsQuery.cs
│   │       │   └── GetProductsQueryHandler.cs
│   │       ├── GetProductById/
│   │       └── SearchProducts/
│   └── DependencyInjection.cs             # Service registration
│
├── ProductCatalogService.Infrastructure/   # External concerns
│   ├── Persistence/                        # Database
│   │   ├── Configurations/                 # EF Core configurations
│   │   │   └── ProductConfiguration.cs
│   │   ├── Migrations/
│   │   ├── Repositories/
│   │   │   └── ProductRepository.cs
│   │   └── ProductDbContext.cs
│   ├── Services/                           # External services
│   │   ├── ElasticsearchService.cs
│   │   ├── RedisCacheService.cs
│   │   └── DateTimeService.cs
│   ├── BackgroundJobs/                     # Hangfire jobs
│   │   └── ProductCatalogJobs.cs
│   ├── Consumers/                          # Message consumers
│   │   └── ReserveStockConsumer.cs
│   ├── HealthChecks/                       # Health checks
│   │   ├── DatabaseHealthCheck.cs
│   │   └── RedisCacheHealthCheck.cs
│   └── DependencyInjection.cs              # Service registration
│
└── ProductCatalogService.API/              # Presentation layer
    ├── Controllers/                        # API endpoints
    │   ├── V1/
    │   │   └── ProductsController.cs
    │   ├── V2/
    │   │   └── ProductsController.cs
    │   ├── SearchController.cs
    │   └── BackgroundJobsController.cs
    ├── Middleware/                         # HTTP middleware
    │   ├── ExceptionHandlingMiddleware.cs
    │   ├── RequestLoggingMiddleware.cs
    │   └── CorrelationIdMiddleware.cs
    ├── Filters/                            # Action filters
    │   └── HangfireAuthorizationFilter.cs
    ├── Extensions/                         # Extension methods
    │   └── MiddlewareExtensions.cs
    ├── Program.cs                          # Entry point
    ├── appsettings.json
    ├── appsettings.Development.json
    └── Dockerfile
```

## Benefits of Clean Architecture

### 1. **Testability**

- ✅ Domain logic can be tested without any infrastructure
- ✅ Application use cases can be tested with mocked repositories
- ✅ Easy to write unit tests for business rules

### 2. **Maintainability**

- ✅ Clear separation of concerns
- ✅ Easy to locate and modify code
- ✅ Changes in one layer don't affect others

### 3. **Flexibility**

- ✅ Easy to swap out infrastructure (e.g., change from PostgreSQL to MongoDB)
- ✅ Framework independence
- ✅ Can change UI without affecting business logic

### 4. **Scalability**

- ✅ Each layer can be scaled independently
- ✅ Clear boundaries for microservices
- ✅ Easy to add new features

### 5. **Team Collaboration**

- ✅ Different teams can work on different layers
- ✅ Clear contracts between layers (interfaces)
- ✅ Reduced merge conflicts

## Migration Strategy

### Phase 1: Create New Project Structure

1. Create separate projects for each layer
2. Set up project references (following dependency rule)
3. Configure build order

### Phase 2: Move Domain Layer

1. Move entities to Domain project
2. Create domain interfaces
3. Define domain events
4. Add domain exceptions

### Phase 3: Move Application Layer

1. Move use cases (commands/queries)
2. Move DTOs
3. Move AutoMapper profiles
4. Move application interfaces
5. Move MediatR behaviors

### Phase 4: Move Infrastructure Layer

1. Move DbContext and repositories
2. Move external service implementations
3. Move background jobs
4. Move message consumers
5. Move health checks

### Phase 5: Move Presentation Layer

1. Move controllers to API project
2. Move middleware
3. Move filters
4. Update Program.cs
5. Update configuration files

### Phase 6: Update Dependencies

1. Update NuGet package references
2. Update namespace references
3. Update dependency injection registrations

### Phase 7: Testing & Validation

1. Run all unit tests
2. Run integration tests
3. Verify builds
4. Test API endpoints

## Project Structure for Each Service

### OrderService

```
OrderService/
├── OrderService.Domain/
│   ├── Entities/
│   │   ├── Order.cs
│   │   └── OrderItem.cs
│   ├── Events/
│   │   └── OrderPlacedEvent.cs
│   ├── Enums/
│   │   └── OrderStatus.cs
│   └── Interfaces/
│       └── IOrderRepository.cs
│
├── OrderService.Application/
│   ├── Common/
│   │   ├── Interfaces/
│   │   ├── Mappings/
│   │   └── Models/
│   ├── Orders/
│   │   ├── Commands/
│   │   │   ├── CreateOrder/
│   │   │   └── UpdateOrderStatus/
│   │   └── Queries/
│   │       ├── GetOrderById/
│   │       └── GetOrdersByUserId/
│   └── Sagas/
│       └── OrderStateMachine.cs
│
├── OrderService.Infrastructure/
│   ├── Persistence/
│   ├── Services/
│   ├── Consumers/
│   └── Sagas/
│
└── OrderService.API/
    ├── Controllers/
    ├── Middleware/
    └── Program.cs
```

### UserService

```
UserService/
├── UserService.Domain/
│   ├── Entities/
│   │   └── ApplicationUser.cs
│   ├── Events/
│   └── Interfaces/
│
├── UserService.Application/
│   ├── Common/
│   ├── Authentication/
│   │   ├── Commands/
│   │   │   ├── Register/
│   │   │   ├── Login/
│   │   │   └── RefreshToken/
│   │   └── Queries/
│   │       └── GetUserProfile/
│   └── Users/
│
├── UserService.Infrastructure/
│   ├── Identity/
│   │   └── ApplicationDbContext.cs
│   ├── Services/
│   │   └── TokenService.cs
│   └── Persistence/
│
└── UserService.API/
    ├── Controllers/
    ├── Middleware/
    └── Program.cs
```

### ShoppingCartService

```
ShoppingCartService/
├── ShoppingCartService.Domain/
│   ├── Entities/
│   │   ├── Cart.cs
│   │   └── CartItem.cs
│   └── Interfaces/
│
├── ShoppingCartService.Application/
│   ├── Common/
│   ├── Carts/
│   │   ├── Commands/
│   │   │   ├── AddItem/
│   │   │   └── ClearCart/
│   │   └── Queries/
│   │       └── GetCart/
│   └── Consumers/
│
├── ShoppingCartService.Infrastructure/
│   ├── Persistence/
│   ├── Services/
│   └── Consumers/
│
└── ShoppingCartService.API/
    ├── Controllers/
    ├── Middleware/
    └── Program.cs
```

## Dependency Flow

```
API Layer
  ↓ (depends on)
Application Layer
  ↓ (depends on)
Domain Layer
  ↑ (implemented by)
Infrastructure Layer
```

**Key Points:**

- API depends on Application
- Application depends on Domain
- Infrastructure depends on Application and Domain
- Domain depends on nothing (pure business logic)

## NuGet Package Distribution

### Domain Project

```xml
<ItemGroup>
  <!-- No external dependencies - pure C# -->
</ItemGroup>
```

### Application Project

```xml
<ItemGroup>
  <PackageReference Include="AutoMapper" />
  <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" />
  <PackageReference Include="MediatR" />
  <PackageReference Include="FluentValidation" />
  <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
</ItemGroup>
```

### Infrastructure Project

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
  <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" />
  <PackageReference Include="MassTransit.RabbitMQ" />
  <PackageReference Include="Elastic.Clients.Elasticsearch" />
  <PackageReference Include="Hangfire.AspNetCore" />
  <PackageReference Include="Hangfire.PostgreSql" />
</ItemGroup>
```

### API Project

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
  <PackageReference Include="Swashbuckle.AspNetCore" />
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
  <PackageReference Include="Asp.Versioning.Mvc" />
  <PackageReference Include="Asp.Versioning.Mvc.ApiExplorer" />
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" />
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" />
</ItemGroup>
```

## Implementation Checklist

### Per Service

- [ ] Create Domain project
- [ ] Create Application project
- [ ] Create Infrastructure project
- [ ] Create API project
- [ ] Set up project references
- [ ] Move entities to Domain
- [ ] Move use cases to Application
- [ ] Move repositories to Infrastructure
- [ ] Move controllers to API
- [ ] Update namespaces
- [ ] Update dependency injection
- [ ] Test build
- [ ] Run tests
- [ ] Update Docker configuration

## Next Steps

1. **Start with one service** (ProductCatalogService recommended as it's the most complex)
2. **Create the project structure**
3. **Move code systematically** (Domain → Application → Infrastructure → API)
4. **Test thoroughly** after each major move
5. **Repeat for other services** once the pattern is established
6. **Update documentation** to reflect new structure

## Expected Outcomes

After restructuring:

- ✅ Clear separation of concerns
- ✅ Improved testability
- ✅ Better maintainability
- ✅ Framework independence
- ✅ Easier onboarding for new developers
- ✅ Professional, enterprise-grade architecture
- ✅ Aligned with industry best practices

## Timeline Estimate

- **Per Service**: 4-6 hours
- **Total (4 services)**: 16-24 hours
- **Testing & Validation**: 4-6 hours
- **Documentation**: 2-3 hours
- **Total Project**: 22-33 hours

## Risks & Mitigation

| Risk                | Mitigation                                   |
| ------------------- | -------------------------------------------- |
| Breaking changes    | Comprehensive testing after each move        |
| Build errors        | Move incrementally, test frequently          |
| Namespace conflicts | Use consistent naming conventions            |
| Lost functionality  | Create checklist for each service            |
| Docker build issues | Update Dockerfiles to reference new projects |

## Resources

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [.NET Clean Architecture Template](https://github.com/jasontaylordev/CleanArchitecture)
- [Clean Architecture with .NET Core](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
