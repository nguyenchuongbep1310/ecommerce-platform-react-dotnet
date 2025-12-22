# AutoMapper Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        E-Commerce Platform                               │
│                     AutoMapper Implementation                            │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                          OrderService                                    │
├─────────────────────────────────────────────────────────────────────────┤
│  Program.cs                                                              │
│  └─ builder.Services.AddAutoMapper(typeof(Program).Assembly)            │
│                                                                          │
│  Application/Mappings/OrderMappingProfile.cs                            │
│  ├─ Order → OrderDto                                                    │
│  └─ OrderItem → OrderItemDto                                            │
│                                                                          │
│  Controllers/OrdersController.cs                                        │
│  └─ _mapper.Map<List<OrderDto>>(orders)                                │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                     ProductCatalogService                                │
├─────────────────────────────────────────────────────────────────────────┤
│  Program.cs                                                              │
│  └─ builder.Services.AddAutoMapper(typeof(Program).Assembly)            │
│                                                                          │
│  Application/Mappings/ProductMappingProfile.cs                          │
│  └─ Product → ProductDto                                                │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                          UserService                                     │
├─────────────────────────────────────────────────────────────────────────┤
│  Program.cs                                                              │
│  └─ builder.Services.AddAutoMapper(typeof(Program).Assembly)            │
│                                                                          │
│  Mappings/UserMappingProfile.cs                                         │
│  ├─ ApplicationUser → UserProfileDto                                    │
│  └─ RegisterRequestDto → ApplicationUser                                │
│                                                                          │
│  Controller/AuthController.cs                                           │
│  └─ _mapper.Map<UserProfileDto>(user)                                  │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                     ShoppingCartService                                  │
├─────────────────────────────────────────────────────────────────────────┤
│  Program.cs                                                              │
│  └─ builder.Services.AddAutoMapper(typeof(Program).Assembly)            │
│                                                                          │
│  Mappings/CartMappingProfile.cs                                         │
│  ├─ Cart → CartResponseDto                                              │
│  └─ CartItem → CartItemDto                                              │
│                                                                          │
│  Controllers/CartController.cs                                          │
│  └─ _mapper.Map<CartResponseDto>(cart)                                 │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                      AutoMapper Flow                                     │
└─────────────────────────────────────────────────────────────────────────┘

    1. Registration (Program.cs)
       ↓
    2. Profile Discovery (Automatic)
       ↓
    3. Configuration Validation (Optional)
       ↓
    4. Dependency Injection (IMapper)
       ↓
    5. Runtime Mapping (Controllers/Services)


┌─────────────────────────────────────────────────────────────────────────┐
│                    Mapping Profile Structure                             │
└─────────────────────────────────────────────────────────────────────────┘

    public class OrderMappingProfile : Profile
    {
        public OrderMappingProfile()
        {
            // Simple mapping (properties match by name)
            CreateMap<Order, OrderDto>();

            // Complex mapping (custom configuration)
            CreateMap<ApplicationUser, UserProfileDto>()
                .ForMember(dest => dest.UserName,
                          opt => opt.MapFrom(src => src.UserName ?? string.Empty));

            // Reverse mapping
            CreateMap<OrderDto, Order>()
                .ReverseMap();
        }
    }


┌─────────────────────────────────────────────────────────────────────────┐
│                      Usage Patterns                                      │
└─────────────────────────────────────────────────────────────────────────┘

    ┌──────────────────┐
    │  Single Object   │
    └──────────────────┘
    var dto = _mapper.Map<OrderDto>(order);

    ┌──────────────────┐
    │   Collection     │
    └──────────────────┘
    var dtos = _mapper.Map<List<OrderDto>>(orders);

    ┌──────────────────┐
    │  EF Core Query   │
    └──────────────────┘
    var dtos = await _context.Orders
        .ProjectTo<OrderDto>(_mapper.ConfigurationProvider)
        .ToListAsync();

    ┌──────────────────┐
    │  Update Existing │
    └──────────────────┘
    _mapper.Map(source, destination);


┌─────────────────────────────────────────────────────────────────────────┐
│                         Benefits                                         │
└─────────────────────────────────────────────────────────────────────────┘

    ✅ Reduced Code        ~40+ lines of boilerplate eliminated
    ✅ Type Safety         Compile-time checking
    ✅ Maintainability     Centralized configuration
    ✅ Performance         Compiled expressions (fast)
    ✅ Testability         Unit test mapping logic
    ✅ Consistency         Uniform approach across services
    ✅ Documentation       Clear object relationships


┌─────────────────────────────────────────────────────────────────────────┐
│                    Package Dependencies                                  │
└─────────────────────────────────────────────────────────────────────────┘

    AutoMapper (v12.0.1)
    └─ Core mapping functionality

    AutoMapper.Extensions.Microsoft.DependencyInjection (v12.0.1)
    └─ ASP.NET Core integration
       └─ Automatic profile discovery
       └─ IMapper registration
       └─ Lifetime management
```
