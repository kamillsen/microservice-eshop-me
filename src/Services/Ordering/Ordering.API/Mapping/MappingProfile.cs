using AutoMapper;
using Ordering.API.Dtos;
using Ordering.API.Entities;
using Ordering.API.Features.Orders.Commands.CreateOrder;

namespace Ordering.API.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Entity ↔ DTO
        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        CreateMap<OrderItem, OrderItemDto>().ReverseMap();

        // Command → Entity
        CreateMap<CreateOrderCommand, Order>()
            .ForMember(dest => dest.Items, opt => opt.Ignore()); // Items manuel eklenir

        CreateMap<OrderItemDto, OrderItem>();
    }
}

