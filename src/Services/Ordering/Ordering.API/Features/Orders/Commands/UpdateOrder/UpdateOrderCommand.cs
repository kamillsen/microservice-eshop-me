using MediatR;
using Ordering.API.Entities;

namespace Ordering.API.Features.Orders.Commands.UpdateOrder;

public record UpdateOrderCommand(
    Guid Id,
    OrderStatus Status
) : IRequest<bool>;

