using AutoMapper;
using Basket.API.Dtos;
using Basket.API.Entities;
using Basket.API.Features.Basket.Commands.CheckoutBasket;
using BuildingBlocks.Messaging.Events;

namespace Basket.API.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ShoppingCart, ShoppingCartDto>()
            .ForMember(dest => dest.Discount, opt => opt.Ignore());

        CreateMap<ShoppingCartDto, ShoppingCart>();

        CreateMap<ShoppingCartItem, ShoppingCartItemDto>().ReverseMap();

        CreateMap<ShoppingCartItem, BasketCheckoutItem>();

        CreateMap<CheckoutBasketCommand, BasketCheckoutEvent>()
            .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
            .ForMember(dest => dest.Items, opt => opt.Ignore());
    }
}

