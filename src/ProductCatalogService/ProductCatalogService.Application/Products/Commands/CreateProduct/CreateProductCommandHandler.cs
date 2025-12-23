using AutoMapper;
using MediatR;
using ProductCatalogService.Application.Common.Models;
using ProductCatalogService.Domain.Entities;
using ProductCatalogService.Domain.Interfaces;

namespace ProductCatalogService.Application.Products.Commands.CreateProduct;

/// <summary>
/// Handler for CreateProductCommand
/// </summary>
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;

    public CreateProductCommandHandler(IProductRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Category = request.Category,
            StockQuantity = request.StockQuantity
        };

        var createdProduct = await _repository.AddAsync(product, cancellationToken);

        return _mapper.Map<ProductDto>(createdProduct);
    }
}
