using MediatR;

namespace ProductCatalogService.Application.Products.Queries.GetCategoryStats;

public record GetCategoryStatsQuery : IRequest<Dictionary<string, long>>;
