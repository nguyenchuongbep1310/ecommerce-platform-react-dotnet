using MediatR;
using ProductCatalogService.Application.DTOs;

namespace ProductCatalogService.Application.Queries.GetProductById;

public record GetProductByIdQuery(int Id) : IRequest<ProductDto?>;
