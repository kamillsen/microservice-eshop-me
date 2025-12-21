using MediatR;

namespace Ordering.API.Features.Orders.Commands.DeleteOrder;

public record DeleteOrderCommand(Guid Id) : IRequest<bool>;

