using MediatR;

namespace Basket.API.Features.Basket.Commands.DeleteBasket;

public record DeleteBasketCommand(string UserName) : IRequest<bool>;



