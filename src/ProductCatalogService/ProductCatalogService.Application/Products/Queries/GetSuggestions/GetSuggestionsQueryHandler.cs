using MediatR;
using ProductCatalogService.Application.Common.Interfaces;

namespace ProductCatalogService.Application.Products.Queries.GetSuggestions;

public class GetSuggestionsQueryHandler : IRequestHandler<GetSuggestionsQuery, List<string>>
{
    private readonly IElasticsearchService _elasticsearchService;

    public GetSuggestionsQueryHandler(IElasticsearchService elasticsearchService)
    {
        _elasticsearchService = elasticsearchService;
    }

    public async Task<List<string>> Handle(GetSuggestionsQuery request, CancellationToken cancellationToken)
    {
        return await _elasticsearchService.GetSuggestionsAsync(request.Prefix, request.Limit, cancellationToken);
    }
}
