# Clean Architecture Quick Reference

## 🎯 The Golden Rule

**Dependencies point INWARD. Inner layers know NOTHING about outer layers.**

```
API → Application → Domain ← Infrastructure
```

## 📁 Where Does This Go?

### Domain Layer (ProductCatalogService.Domain)

**What belongs here:**

- ✅ Entities (Product, Order, User)
- ✅ Value Objects (Money, Address)
- ✅ Domain Events (ProductCreatedEvent)
- ✅ Domain Exceptions (ProductNotFoundException)
- ✅ Repository Interfaces (IProductRepository)
- ✅ Domain Services (if needed)
- ✅ Specifications (business rules)

**What DOESN'T belong here:**

- ❌ DTOs
- ❌ Database code (DbContext, migrations)
- ❌ External service calls
- ❌ HTTP/API concerns
- ❌ Any framework dependencies

**Dependencies:** NONE (pure C#)

**Example:**

```csharp
// ✅ Good - Pure domain entity
namespace ProductCatalogService.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }

        // Domain logic
        public void UpdatePrice(decimal newPrice)
        {
            if (newPrice <= 0)
                throw new InvalidProductException("Price must be positive");
            Price = newPrice;
        }
    }
}
```

---

### Application Layer (ProductCatalogService.Application)

**What belongs here:**

- ✅ Use Cases (Commands/Queries)
- ✅ Command/Query Handlers
- ✅ DTOs (Data Transfer Objects)
- ✅ AutoMapper Profiles
- ✅ Validators (FluentValidation)
- ✅ Application Interfaces (ICacheService, IEmailService)
- ✅ MediatR Behaviors

**What DOESN'T belong here:**

- ❌ Database implementation (DbContext)
- ❌ External service implementation
- ❌ Controllers
- ❌ Middleware
- ❌ Infrastructure details

**Dependencies:** Domain only

**Example:**

```csharp
// ✅ Good - Application use case
namespace ProductCatalogService.Application.Products.Commands.CreateProduct
{
    public record CreateProductCommand(
        string Name,
        decimal Price,
        string Category) : IRequest<ProductDto>;

    public class CreateProductCommandHandler
        : IRequestHandler<CreateProductCommand, ProductDto>
    {
        private readonly IProductRepository _repository;
        private readonly IMapper _mapper;

        public async Task<ProductDto> Handle(
            CreateProductCommand request,
            CancellationToken cancellationToken)
        {
            var product = new Product
            {
                Name = request.Name,
                Price = request.Price,
                Category = request.Category
            };

            await _repository.AddAsync(product, cancellationToken);
            return _mapper.Map<ProductDto>(product);
        }
    }
}
```

---

### Infrastructure Layer (ProductCatalogService.Infrastructure)

**What belongs here:**

- ✅ DbContext
- ✅ Repository Implementations
- ✅ External Service Implementations (Elasticsearch, Redis)
- ✅ Database Migrations
- ✅ EF Core Configurations
- ✅ Background Jobs (Hangfire)
- ✅ Message Consumers (MassTransit)
- ✅ Health Checks

**What DOESN'T belong here:**

- ❌ Controllers
- ❌ Middleware
- ❌ Domain entities (they're in Domain)
- ❌ Use cases (they're in Application)

**Dependencies:** Application + Domain

**Example:**

```csharp
// ✅ Good - Infrastructure implementation
namespace ProductCatalogService.Infrastructure.Persistence.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ProductDbContext _context;

        public ProductRepository(ProductDbContext context)
        {
            _context = context;
        }

        public async Task<Product> AddAsync(
            Product product,
            CancellationToken cancellationToken)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync(cancellationToken);
            return product;
        }
    }
}
```

---

### API/Presentation Layer (ProductCatalogService.API)

**What belongs here:**

- ✅ Controllers
- ✅ Middleware
- ✅ Filters
- ✅ Program.cs
- ✅ Configuration files (appsettings.json)
- ✅ Dockerfile

**What DOESN'T belong here:**

- ❌ Business logic
- ❌ Data access logic
- ❌ Domain entities
- ❌ Repository implementations

**Dependencies:** Application + Infrastructure

**Example:**

```csharp
// ✅ Good - Thin controller
namespace ProductCatalogService.API.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProductsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<ActionResult<ProductDto>> Create(
            CreateProductCommand command)
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
    }
}
```

---

## 🔄 Common Patterns

### Creating a New Feature

**1. Start with Domain (if needed)**

```csharp
// Domain/Entities/Product.cs
public class Product { ... }

// Domain/Interfaces/IProductRepository.cs
public interface IProductRepository { ... }
```

**2. Create Application Use Case**

```csharp
// Application/Products/Commands/CreateProduct/CreateProductCommand.cs
public record CreateProductCommand(...) : IRequest<ProductDto>;

// Application/Products/Commands/CreateProduct/CreateProductCommandHandler.cs
public class CreateProductCommandHandler : IRequestHandler<...> { ... }

// Application/Products/Commands/CreateProduct/CreateProductCommandValidator.cs
public class CreateProductCommandValidator : AbstractValidator<...> { ... }
```

**3. Implement Infrastructure**

```csharp
// Infrastructure/Persistence/Repositories/ProductRepository.cs
public class ProductRepository : IProductRepository { ... }
```

**4. Add API Endpoint**

```csharp
// API/Controllers/ProductsController.cs
[HttpPost]
public async Task<ActionResult> Create(CreateProductCommand command)
{
    return Ok(await _mediator.Send(command));
}
```

---

## 🚫 Common Mistakes

### ❌ WRONG: Application depends on Infrastructure

```csharp
// Application/Products/Commands/CreateProductCommandHandler.cs
public class CreateProductCommandHandler
{
    private readonly ProductDbContext _context;  // ❌ BAD!

    public async Task Handle(...)
    {
        _context.Products.Add(...);  // ❌ Direct database access
    }
}
```

### ✅ CORRECT: Application depends on Interface

```csharp
// Application/Products/Commands/CreateProductCommandHandler.cs
public class CreateProductCommandHandler
{
    private readonly IProductRepository _repository;  // ✅ GOOD!

    public async Task Handle(...)
    {
        await _repository.AddAsync(...);  // ✅ Through interface
    }
}
```

---

### ❌ WRONG: Domain depends on Application

```csharp
// Domain/Entities/Product.cs
public class Product
{
    public ProductDto ToDto()  // ❌ BAD! Domain knows about DTO
    {
        return new ProductDto { ... };
    }
}
```

### ✅ CORRECT: Application maps Domain to DTO

```csharp
// Application/Common/Mappings/ProductMappingProfile.cs
public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        CreateMap<Product, ProductDto>();  // ✅ GOOD!
    }
}
```

---

### ❌ WRONG: Controller has business logic

```csharp
// API/Controllers/ProductsController.cs
[HttpPost]
public async Task<ActionResult> Create(CreateProductRequest request)
{
    // ❌ BAD! Business logic in controller
    if (request.Price <= 0)
        return BadRequest("Invalid price");

    var product = new Product { ... };
    _context.Products.Add(product);
    await _context.SaveChangesAsync();

    return Ok(product);
}
```

### ✅ CORRECT: Controller delegates to use case

```csharp
// API/Controllers/ProductsController.cs
[HttpPost]
public async Task<ActionResult> Create(CreateProductCommand command)
{
    // ✅ GOOD! Just delegate to MediatR
    var result = await _mediator.Send(command);
    return Ok(result);
}
```

---

## 📦 Project References

```
Domain
  └── (no dependencies)

Application
  └── Domain

Infrastructure
  ├── Domain
  └── Application

API
  ├── Application
  └── Infrastructure
```

**Rule:** Never reference upward or sideways!

---

## 🧪 Testing Strategy

### Domain Tests

```csharp
// Test pure business logic
[Fact]
public void UpdatePrice_WithNegativePrice_ThrowsException()
{
    var product = new Product();
    Assert.Throws<InvalidProductException>(() =>
        product.UpdatePrice(-10));
}
```

### Application Tests

```csharp
// Test use cases with mocked dependencies
[Fact]
public async Task CreateProduct_ShouldCallRepository()
{
    var mockRepo = new Mock<IProductRepository>();
    var handler = new CreateProductCommandHandler(mockRepo.Object, ...);

    await handler.Handle(new CreateProductCommand(...), ...);

    mockRepo.Verify(r => r.AddAsync(...), Times.Once);
}
```

### Integration Tests

```csharp
// Test API endpoints
[Fact]
public async Task POST_Products_ReturnsCreated()
{
    var response = await _client.PostAsJsonAsync("/api/products", ...);
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
}
```

---

## 🎨 Naming Conventions

### Commands (Write Operations)

- `CreateProductCommand`
- `UpdateProductCommand`
- `DeleteProductCommand`

### Queries (Read Operations)

- `GetProductsQuery`
- `GetProductByIdQuery`
- `SearchProductsQuery`

### Handlers

- `CreateProductCommandHandler`
- `GetProductsQueryHandler`

### Validators

- `CreateProductCommandValidator`
- `UpdateProductCommandValidator`

### DTOs

- `ProductDto`
- `ProductListDto`
- `CreateProductRequest`

### Repositories

- `IProductRepository` (interface in Domain)
- `ProductRepository` (implementation in Infrastructure)

---

## 🔑 Key Principles

1. **Dependency Inversion**: Depend on abstractions, not concretions
2. **Single Responsibility**: Each class has one reason to change
3. **Open/Closed**: Open for extension, closed for modification
4. **Interface Segregation**: Many specific interfaces > one general interface
5. **Separation of Concerns**: Each layer has distinct responsibilities

---

## 📚 Quick Checklist

When adding new code, ask yourself:

- [ ] Does this belong in the correct layer?
- [ ] Am I depending on interfaces, not implementations?
- [ ] Can I test this without external dependencies?
- [ ] Is the dependency direction correct (inward)?
- [ ] Am I mixing concerns (e.g., business logic in controller)?
- [ ] Would changing the database/framework break my domain logic?

If you answer "no" to any of these, reconsider your design!

---

## 🎯 Remember

**The goal is not perfection, but maintainability.**

Clean Architecture is a guideline, not a strict rule. Use pragmatism:

- ✅ Follow the principles for complex business logic
- ⚠️ Be flexible for simple CRUD operations
- ❌ Don't over-engineer simple features

**When in doubt, ask: "Can I test this easily?"**

If the answer is yes, you're probably on the right track! 🚀
