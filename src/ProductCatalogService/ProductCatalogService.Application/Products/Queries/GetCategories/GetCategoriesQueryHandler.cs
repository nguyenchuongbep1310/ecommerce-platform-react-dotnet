using MediatR;
using Microsoft.Extensions.Logging;
using ProductCatalogService.Application.Common.Constants;
using ProductCatalogService.Application.Common.Interfaces;
using ProductCatalogService.Domain.Interfaces;

namespace ProductCatalogService.Application.Products.Queries.GetCategories;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetCategoriesQueryHandler> _logger;

    public GetCategoriesQueryHandler(
        IProductRepository repository,
        ICacheService cacheService,
        ILogger<GetCategoriesQueryHandler> logger)
    {
        _repository = repository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.AllCategories;

        var categories = await _cacheService.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                _logger.LogInformation("Cache miss for categories. Querying database.");

                var stats = await _repository.GetCategoriesStatsAsync(cancellationToken);
                
                var result = stats.Select(s => new CategoryDto(
                    s.Category,
                    s.Count,
                    s.AvgPrice,
                    s.MinPrice,
                    s.MaxPrice
                )).ToList();

                _logger.LogInformation("Retrieved {Count} categories from database", result.Count);
                return result;
            },
            CacheKeys.Expiration.Categories,
            cancellationToken
        );

        _logger.LogInformation("Returning {Count} categories", categories.Count);
        return categories;
    }
}
