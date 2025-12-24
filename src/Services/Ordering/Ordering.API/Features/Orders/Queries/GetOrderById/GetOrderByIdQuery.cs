using MediatR;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Queries.GetOrderById;

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto?>;

