using MediatR;
using Microsoft.Extensions.Logging;
using ProductCatalogService.Application.Common.Interfaces;

namespace ProductCatalogService.Application.Products.Queries.SearchProducts;

public class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, SearchResponseDto>
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<SearchProductsQueryHandler> _logger;

    public SearchProductsQueryHandler(
        IElasticsearchService elasticsearchService,
        ILogger<SearchProductsQueryHandler> logger)
    {
        _elasticsearchService = elasticsearchService;
        _logger = logger;
    }

    public async Task<SearchResponseDto> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        var (products, totalCount) = await _elasticsearchService.SearchProductsAsync(
            request.Query, 
            request.Category, 
            request.MinPrice, 
            request.MaxPrice, 
            request.InStock, 
            request.Page, 
            request.PageSize, 
            cancellationToken);

        return new SearchResponseDto
        {
            Products = products,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
            Query = request.Query,
            Filters = new SearchFiltersDto
            {
                Category = request.Category,
                MinPrice = request.MinPrice,
                MaxPrice = request.MaxPrice,
                InStockOnly = request.InStock
            }
        };
    }
}
