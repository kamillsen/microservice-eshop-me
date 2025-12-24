using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ordering.API.Data;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Queries.GetOrdersByUser;

public class GetOrdersByUserHandler : IRequestHandler<GetOrdersByUserQuery, IEnumerable<OrderDto>>
{
    private readonly OrderingDbContext _context;
    private readonly IMapper _mapper;

    public GetOrdersByUserHandler(OrderingDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<OrderDto>> Handle(GetOrdersByUserQuery request, CancellationToken cancellationToken)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserName == request.UserName)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<OrderDto>>(orders);
    }
}


