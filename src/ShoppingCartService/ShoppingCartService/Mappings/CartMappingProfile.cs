using AutoMapper;
using ShoppingCartService.Models;
using ShoppingCartService.DTOs;

namespace ShoppingCartService.Mappings
{
    /// <summary>
    /// AutoMapper profile for Cart-related mappings
    /// </summary>
    public class CartMappingProfile : Profile
    {
        public CartMappingProfile()
        {
            // Cart -> CartResponseDto
            CreateMap<Cart, CartResponseDto>();

            // CartItem -> CartItemDto
            CreateMap<CartItem, CartItemDto>();

            // Reverse mappings if needed
            CreateMap<CartResponseDto, Cart>();
            CreateMap<CartItemDto, CartItem>();
        }
    }
}
