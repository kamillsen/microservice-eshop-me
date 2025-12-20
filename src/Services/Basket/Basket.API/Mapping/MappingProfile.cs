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
        // ShoppingCart -> ShoppingCartDto
        CreateMap<ShoppingCart, ShoppingCartDto>()
            .ForMember(dest => dest.Discount, opt => opt.Ignore()); // Discount manuel hesaplanacak

        // ShoppingCartDto -> ShoppingCart (Reverse mapping)
        CreateMap<ShoppingCartDto, ShoppingCart>();

        // ShoppingCartItem -> ShoppingCartItemDto
        CreateMap<ShoppingCartItem, ShoppingCartItemDto>();

        // ShoppingCartItemDto -> ShoppingCartItem (Reverse mapping)
        CreateMap<ShoppingCartItemDto, ShoppingCartItem>();

        // Command → Event
        CreateMap<CheckoutBasketCommand, BasketCheckoutEvent>()
            .ForMember(dest => dest.TotalPrice, opt => opt.Ignore()); // TotalPrice basket'ten alınacak
    }
}

