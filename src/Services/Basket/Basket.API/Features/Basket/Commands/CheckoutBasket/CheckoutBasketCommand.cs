using BuildingBlocks.Messaging.Events;
using MediatR;

namespace Basket.API.Features.Basket.Commands.CheckoutBasket;

public record CheckoutBasketCommand(
    string UserName,
    string FirstName,
    string LastName,
    string EmailAddress,
    string AddressLine,
    string Country,
    string State,
    string ZipCode,
    string CardName,
    string CardNumber,
    string Expiration,
    string CVV,
    int PaymentMethod
) : IRequest<bool>;

