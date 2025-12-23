using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ProductCatalogService.Application.Common.Constants;
using ProductCatalogService.Application.Common.Interfaces;
using ProductCatalogService.Application.Common.Models;
using ProductCatalogService.Domain.Interfaces;

namespace ProductCatalogService.Application.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetProductByIdQueryHandler> _logger;

    public GetProductByIdQueryHandler(
        IProductRepository repository,
        ICacheService cacheService,
        IMapper mapper,
        ILogger<GetProductByIdQueryHandler> logger)
    {
        _repository = repository;
        _cacheService = cacheService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Product(request.Id);

        var cachedProduct = await _cacheService.GetAsync<ProductDto>(cacheKey, cancellationToken);
        
        if (cachedProduct != null)
        {
            _logger.LogInformation("Cache hit for product ID: {ProductId}", request.Id);
            return cachedProduct;
        }

        _logger.LogInformation("Cache miss for product ID: {ProductId}. Querying database.", request.Id);

        var product = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (product != null)
        {
            var productDto = _mapper.Map<ProductDto>(product);

            // Cache the product
            await _cacheService.SetAsync(
                cacheKey,
                productDto,
                CacheKeys.Expiration.Product,
                cancellationToken);
            
            _logger.LogInformation("Cached product ID: {ProductId}", request.Id);
            return productDto;
        }

        _logger.LogWarning("Product not found: {ProductId}", request.Id);
        return null;
    }
}
