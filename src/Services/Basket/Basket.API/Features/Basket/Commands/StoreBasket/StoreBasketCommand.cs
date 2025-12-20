using Basket.API.Dtos;
using MediatR;

namespace Basket.API.Features.Basket.Commands.StoreBasket;

public record StoreBasketCommand(ShoppingCartDto Basket) : IRequest<ShoppingCartDto>;

