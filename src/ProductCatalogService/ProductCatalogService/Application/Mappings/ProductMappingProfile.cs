using AutoMapper;
using ProductCatalogService.Models;
using ProductCatalogService.Application.DTOs;

namespace ProductCatalogService.Application.Mappings
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
