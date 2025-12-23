using MediatR;

namespace ProductCatalogService.Application.Products.Queries.GetSuggestions;

public record GetSuggestionsQuery(string Prefix, int Limit = 10) : IRequest<List<string>>;
