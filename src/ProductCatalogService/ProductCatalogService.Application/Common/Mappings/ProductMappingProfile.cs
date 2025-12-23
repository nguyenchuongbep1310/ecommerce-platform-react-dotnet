using AutoMapper;
using ProductCatalogService.Domain.Entities;
using ProductCatalogService.Application.Common.Models;

namespace ProductCatalogService.Application.Common.Mappings
{
    /// <summary>
    /// AutoMapper profile for Product-related mappings
    /// </summary>
    public class ProductMappingProfile : Profile
    {
        public ProductMappingProfile()
        {
            // Product -> ProductDto
            CreateMap<Product, ProductDto>();

            // Reverse mapping if needed
            CreateMap<ProductDto, Product>();
        }
    }
}
