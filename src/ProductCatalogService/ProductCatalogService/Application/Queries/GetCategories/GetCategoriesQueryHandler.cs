using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductCatalogService.Application.Common.Constants;
using ProductCatalogService.Application.Common.Interfaces;
using ProductCatalogService.Data;

namespace ProductCatalogService.Application.Queries.GetCategories;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly ProductDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetCategoriesQueryHandler> _logger;

    public GetCategoriesQueryHandler(
        ProductDbContext context,
        ICacheService cacheService,
        ILogger<GetCategoriesQueryHandler> logger)
    {
        _context = context;
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

                // Get all categories with statistics
                var result = await _context.Products
                    .GroupBy(p => p.Category)
                    .Select(g => new CategoryDto(
                        g.Key,
                        g.Count(),
                        g.Average(p => p.Price),
                        g.Min(p => p.Price),
                        g.Max(p => p.Price)
                    ))
                    .OrderBy(c => c.Name)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} categories from database", result.Count);
                return result;
            },
            CacheKeys.Expiration.Categories, // Cache for 1 hour
            cancellationToken
        );

        _logger.LogInformation("Returning {Count} categories", categories.Count);
        return categories;
    }
}
