using AutoMapper;
using MediatR;
using Ordering.API.Data;
using Ordering.API.Entities;


namespace Ordering.API.Features.Orders.Commands.CreateOrder;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly OrderingDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(
        OrderingDbContext context,
        IMapper mapper,
        ILogger<CreateOrderHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Command'dan Entity oluştur
        var order = _mapper.Map<Order>(request);
        order.Id = Guid.NewGuid();
        order.OrderDate = DateTime.UtcNow;
        order.Status = OrderStatus.Pending;

        // 2. OrderItems'ları ekle
        foreach (var itemDto in request.Items)
        {
            var orderItem = _mapper.Map<OrderItem>(itemDto);
            orderItem.Id = Guid.NewGuid();
            orderItem.OrderId = order.Id;
            order.Items.Add(orderItem);
        }

        // 3. Veritabanına kaydet
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order created. OrderId: {OrderId}, UserName: {UserName}, TotalPrice: {TotalPrice}",
            order.Id, order.UserName, order.TotalPrice);

        return order.Id;
    }
}

