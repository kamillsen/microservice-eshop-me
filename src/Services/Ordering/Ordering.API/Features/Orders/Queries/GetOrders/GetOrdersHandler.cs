using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ordering.API.Data;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Queries.GetOrders;

public class GetOrdersHandler : IRequestHandler<GetOrdersQuery, IEnumerable<OrderDto>>
{
    private readonly OrderingDbContext _context;
    private readonly IMapper _mapper;

    public GetOrdersHandler(OrderingDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<OrderDto>>(orders);
    }
}


