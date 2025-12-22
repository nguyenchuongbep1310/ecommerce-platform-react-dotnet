using AutoMapper;
using OrderService.Models;
using OrderService.DTOs;

namespace OrderService.Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for Order-related mappings
    /// </summary>
    public class OrderMappingProfile : Profile
    {
        public OrderMappingProfile()
        {
            // Order -> OrderDto
            CreateMap<Order, OrderDto>();

            // OrderItem -> OrderItemDto
            CreateMap<OrderItem, OrderItemDto>();

            // Reverse mappings if needed
            CreateMap<OrderDto, Order>();
            CreateMap<OrderItemDto, OrderItem>();
        }
    }
}
