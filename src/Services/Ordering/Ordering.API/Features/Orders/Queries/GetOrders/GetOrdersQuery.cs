using MediatR;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Queries.GetOrders;

public record GetOrdersQuery : IRequest<IEnumerable<OrderDto>>;

