using MediatR;
using ProductCatalogService.Application.Common.Interfaces;

namespace ProductCatalogService.Application.Products.Queries.GetCategoryStats;

public class GetCategoryStatsQueryHandler : IRequestHandler<GetCategoryStatsQuery, Dictionary<string, long>>
{
    private readonly IElasticsearchService _elasticsearchService;

    public GetCategoryStatsQueryHandler(IElasticsearchService elasticsearchService)
    {
        _elasticsearchService = elasticsearchService;
    }

    public async Task<Dictionary<string, long>> Handle(GetCategoryStatsQuery request, CancellationToken cancellationToken)
    {
        return await _elasticsearchService.GetCategoryAggregationsAsync(cancellationToken);
    }
}
