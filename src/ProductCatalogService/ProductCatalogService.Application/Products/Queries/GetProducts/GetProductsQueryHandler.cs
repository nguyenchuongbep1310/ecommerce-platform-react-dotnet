using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ProductCatalogService.Application.Common.Constants;
using ProductCatalogService.Application.Common.Interfaces;
using ProductCatalogService.Application.Common.Models;
using ProductCatalogService.Domain.Interfaces;

namespace ProductCatalogService.Application.Products.Queries.GetProducts;

/// <summary>
/// Handler for GetProductsQuery
/// </summary>
public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, ProductListDto>
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetProductsQueryHandler> _logger;

    public GetProductsQueryHandler(
        IProductRepository repository, 
        ICacheService cacheService,
        IMapper mapper,
        ILogger<GetProductsQueryHandler> logger)
    {
        _repository = repository;
        _cacheService = cacheService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ProductListDto> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Products(request.Search, request.Category, request.MinPrice, request.MaxPrice);

        var productDtos = await _cacheService.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                _logger.LogInformation("Cache miss for products. Querying database.");
                
                var products = await _repository.SearchAsync(
                    request.Search, 
                    request.Category, 
                    request.MinPrice, 
                    request.MaxPrice, 
                    cancellationToken);

                var list = products.ToList();
                _logger.LogInformation("Retrieved {Count} products from database", list.Count);
                
                return _mapper.Map<List<ProductDto>>(list);
            },
            CacheKeys.Expiration.ProductList,
            cancellationToken
        );

        return new ProductListDto(productDtos.Count, productDtos);
    }
}
