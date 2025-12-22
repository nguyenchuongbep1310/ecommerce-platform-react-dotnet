# AutoMapper Implementation Guide

## Overview

This project uses **AutoMapper** for object-to-object mapping across all microservices. AutoMapper eliminates the need for manual property mapping and reduces boilerplate code, making the codebase cleaner and more maintainable.

## Installation

AutoMapper has been added to all microservices via NuGet packages:

```xml
<PackageReference Include="AutoMapper" Version="13.0.1" />
<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="13.0.1" />
```

## Services with AutoMapper

The following services have AutoMapper configured:

1. **OrderService**
2. **ProductCatalogService**
3. **UserService**
4. **ShoppingCartService**

## Configuration

### 1. Mapping Profiles

Each service has its own mapping profile(s) located in the appropriate namespace:

#### OrderService

**Location:** `OrderService/Application/Mappings/OrderMappingProfile.cs`

```csharp
public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        // Order -> OrderDto
        CreateMap<Order, OrderDto>();

        // OrderItem -> OrderItemDto
        CreateMap<OrderItem, OrderItemDto>();

        // Reverse mappings
        CreateMap<OrderDto, Order>();
        CreateMap<OrderItemDto, OrderItem>();
    }
}
```

#### ProductCatalogService

**Location:** `ProductCatalogService/Application/Mappings/ProductMappingProfile.cs`

```csharp
public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        // Product -> ProductDto
        CreateMap<Product, ProductDto>();

        // Reverse mapping
        CreateMap<ProductDto, Product>();
    }
}
```

#### UserService

**Location:** `UserService/Mappings/UserMappingProfile.cs`

```csharp
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // ApplicationUser -> UserProfileDto
        CreateMap<ApplicationUser, UserProfileDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName ?? string.Empty));

        // RegisterRequestDto -> ApplicationUser
        CreateMap<RegisterRequestDto, ApplicationUser>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email));
    }
}
```

#### ShoppingCartService

**Location:** `ShoppingCartService/Mappings/CartMappingProfile.cs`

```csharp
public class CartMappingProfile : Profile
{
    public CartMappingProfile()
    {
        // Cart -> CartResponseDto
        CreateMap<Cart, CartResponseDto>();

        // CartItem -> CartItemDto
        CreateMap<CartItem, CartItemDto>();

        // Reverse mappings
        CreateMap<CartResponseDto, Cart>();
        CreateMap<CartItemDto, CartItem>();
    }
}
```

### 2. Dependency Injection Registration

AutoMapper is registered in each service's `Program.cs`:

```csharp
// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);
```

This automatically scans the assembly for all classes that inherit from `Profile` and registers them.

## Usage Examples

### Basic Mapping

#### Before (Manual Mapping)

```csharp
var orderDto = new OrderDto
{
    Id = order.Id,
    UserId = order.UserId,
    OrderDate = order.OrderDate,
    TotalAmount = order.TotalAmount,
    Status = order.Status,
    Items = order.Items.Select(i => new OrderItemDto
    {
        ProductId = i.ProductId,
        Quantity = i.Quantity,
        UnitPrice = i.UnitPrice
    }).ToList()
};
```

#### After (AutoMapper)

```csharp
var orderDto = _mapper.Map<OrderDto>(order);
```

### Collection Mapping

#### Before (Manual Mapping)

```csharp
var orderDtos = orders.Select(o => new OrderDto
{
    Id = o.Id,
    UserId = o.UserId,
    OrderDate = o.OrderDate,
    TotalAmount = o.TotalAmount,
    Status = o.Status,
    Items = o.Items.Select(i => new OrderItemDto
    {
        ProductId = i.ProductId,
        Quantity = i.Quantity,
        UnitPrice = i.UnitPrice
    }).ToList()
}).ToList();
```

#### After (AutoMapper)

```csharp
var orderDtos = _mapper.Map<List<OrderDto>>(orders);
```

### Controller Usage

Inject `IMapper` into your controller:

```csharp
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public OrdersController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetOrderHistory()
    {
        var orders = await _mediator.Send(new GetOrdersByUserIdQuery(userId));
        var orderDtos = _mapper.Map<List<OrderDto>>(orders);
        return Ok(orderDtos);
    }
}
```

## Advanced Mapping Scenarios

### Custom Property Mapping

When property names don't match or you need custom logic:

```csharp
CreateMap<ApplicationUser, UserProfileDto>()
    .ForMember(dest => dest.UserName,
               opt => opt.MapFrom(src => src.UserName ?? string.Empty));
```

### Ignoring Properties

To ignore specific properties during mapping:

```csharp
CreateMap<Source, Destination>()
    .ForMember(dest => dest.PropertyToIgnore, opt => opt.Ignore());
```

### Conditional Mapping

Map properties based on conditions:

```csharp
CreateMap<Source, Destination>()
    .ForMember(dest => dest.Status,
               opt => opt.MapFrom(src => src.IsActive ? "Active" : "Inactive"));
```

### Flattening

AutoMapper can automatically flatten nested objects:

```csharp
// Source
public class Order
{
    public Customer Customer { get; set; }
}

public class Customer
{
    public string Name { get; set; }
}

// Destination
public class OrderDto
{
    public string CustomerName { get; set; } // Automatically mapped from Order.Customer.Name
}

// Mapping
CreateMap<Order, OrderDto>();
```

## Best Practices

### 1. Keep Profiles Focused

Create separate profiles for different domains or bounded contexts:

- `OrderMappingProfile` for order-related mappings
- `ProductMappingProfile` for product-related mappings
- `UserMappingProfile` for user-related mappings

### 2. Use Reverse Mappings Wisely

Only create reverse mappings when needed:

```csharp
CreateMap<Order, OrderDto>()
    .ReverseMap(); // Creates both Order -> OrderDto and OrderDto -> Order
```

### 3. Validate Configuration

In development, you can validate AutoMapper configuration at startup:

```csharp
var mapper = serviceProvider.GetRequiredService<IMapper>();
mapper.ConfigurationProvider.AssertConfigurationIsValid();
```

### 4. Use ProjectTo for Queryable Sources

When working with Entity Framework, use `ProjectTo` for better performance:

```csharp
var orderDtos = await _context.Orders
    .Where(o => o.UserId == userId)
    .ProjectTo<OrderDto>(_mapper.ConfigurationProvider)
    .ToListAsync();
```

### 5. Avoid Over-Mapping

Don't map everything. Sometimes manual mapping is more appropriate:

- When you need complex business logic
- When mapping would obscure the intent
- For simple, one-off transformations

## Testing AutoMapper

### Unit Testing Profiles

```csharp
[Fact]
public void OrderMappingProfile_ShouldBeValid()
{
    var configuration = new MapperConfiguration(cfg =>
        cfg.AddProfile<OrderMappingProfile>());

    configuration.AssertConfigurationIsValid();
}

[Fact]
public void Order_ShouldMapTo_OrderDto()
{
    var mapper = new MapperConfiguration(cfg =>
        cfg.AddProfile<OrderMappingProfile>()).CreateMapper();

    var order = new Order
    {
        Id = 1,
        UserId = "user123",
        TotalAmount = 100.50m,
        Status = "Pending"
    };

    var dto = mapper.Map<OrderDto>(order);

    Assert.Equal(order.Id, dto.Id);
    Assert.Equal(order.UserId, dto.UserId);
    Assert.Equal(order.TotalAmount, dto.TotalAmount);
    Assert.Equal(order.Status, dto.Status);
}
```

## Performance Considerations

1. **AutoMapper is fast** - It uses compiled expressions for mapping, making it nearly as fast as manual mapping.

2. **Avoid mapping in loops** - Map collections in one call:

   ```csharp
   // Good
   var dtos = _mapper.Map<List<OrderDto>>(orders);

   // Bad
   var dtos = orders.Select(o => _mapper.Map<OrderDto>(o)).ToList();
   ```

3. **Use ProjectTo with EF Core** - This translates mappings to SQL, reducing data transfer:
   ```csharp
   var dtos = await _context.Orders
       .ProjectTo<OrderDto>(_mapper.ConfigurationProvider)
       .ToListAsync();
   ```

## Troubleshooting

### Common Issues

1. **Missing Mapping Configuration**

   - Error: "Missing map from Source to Destination"
   - Solution: Add the mapping in your Profile class

2. **Ambiguous Mapping**

   - Error: "Ambiguous match found"
   - Solution: Use `ForMember` to explicitly configure the mapping

3. **Null Reference Exceptions**
   - Solution: Configure null handling in your profile:
     ```csharp
     CreateMap<Source, Destination>()
         .ForMember(dest => dest.Property,
                    opt => opt.NullSubstitute("Default Value"));
     ```

## Migration Checklist

When adding AutoMapper to a new service:

- [ ] Add AutoMapper NuGet packages
- [ ] Create mapping profile(s)
- [ ] Register AutoMapper in `Program.cs`
- [ ] Update controllers to inject `IMapper`
- [ ] Replace manual mapping with AutoMapper calls
- [ ] Test the mappings
- [ ] Update documentation

## Resources

- [AutoMapper Documentation](https://docs.automapper.org/)
- [AutoMapper GitHub](https://github.com/AutoMapper/AutoMapper)
- [Best Practices](https://docs.automapper.org/en/stable/Best-practices.html)

## Summary

AutoMapper has been successfully integrated into all microservices, providing:

- ✅ Cleaner, more maintainable code
- ✅ Reduced boilerplate
- ✅ Type-safe mappings
- ✅ Better testability
- ✅ Consistent mapping patterns across services

All manual mapping code has been replaced with AutoMapper calls, making the codebase more professional and easier to maintain.
