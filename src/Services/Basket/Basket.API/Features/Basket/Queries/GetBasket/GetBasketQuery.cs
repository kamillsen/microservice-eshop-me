using Basket.API.Dtos;
using MediatR;

namespace Basket.API.Features.Basket.Queries.GetBasket;

public record GetBasketQuery(string UserName) : IRequest<ShoppingCartDto>;

