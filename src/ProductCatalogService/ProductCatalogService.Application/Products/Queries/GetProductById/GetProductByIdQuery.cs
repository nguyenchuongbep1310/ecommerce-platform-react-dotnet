using MediatR;
using ProductCatalogService.Application.Common.Models;

namespace ProductCatalogService.Application.Products.Queries.GetProductById;

public record GetProductByIdQuery(int Id) : IRequest<ProductDto?>;
