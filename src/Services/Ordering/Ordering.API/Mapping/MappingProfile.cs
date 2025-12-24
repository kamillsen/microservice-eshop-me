using AutoMapper;
using Ordering.API.Dtos;
using Ordering.API.Entities;
using Ordering.API.Features.Orders.Commands.CreateOrder;
using BuildingBlocks.Messaging.Events;

namespace Ordering.API.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        
        CreateMap<OrderItem, OrderItemDto>().ReverseMap();

        CreateMap<CreateOrderCommand, Order>()
            .ForMember(dest => dest.Items, opt => opt.Ignore());

        CreateMap<OrderItemDto, OrderItem>();

        CreateMap<BasketCheckoutItem, OrderItemDto>();

        CreateMap<BasketCheckoutEvent, CreateOrderCommand>();
    }
}
 
