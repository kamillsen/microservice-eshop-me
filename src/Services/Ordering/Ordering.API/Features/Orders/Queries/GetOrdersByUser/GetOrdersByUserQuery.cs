using MediatR;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Queries.GetOrdersByUser;

public record GetOrdersByUserQuery(string UserName) : IRequest<IEnumerable<OrderDto>>;

