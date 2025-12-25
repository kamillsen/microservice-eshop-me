using MediatR;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Commands.CreateOrder;

public record CreateOrderCommand(
    string UserName,
    decimal TotalPrice,
    decimal Discount,
    List<OrderItemDto> Items,
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
) : IRequest<Guid>;

