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
        // Command'dan Entity oluştur (basit property mapping)
        var order = _mapper.Map<Order>(request);
        
        // Business Logic: OrderItem'lara OrderId ata (Foreign Key Relationship)
        // SRP: Bu business rule handler'da olmalı, mapping'de değil
        order.Items = request.Items.Select(itemDto =>
        {
            var orderItem = _mapper.Map<OrderItem>(itemDto);
            orderItem.OrderId = order.Id; // Parent Order'ın Id'sini ata
            return orderItem;
        }).ToList();

        // Veritabanına kaydet
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order created. OrderId: {OrderId}, UserName: {UserName}, TotalPrice: {TotalPrice}",
            order.Id, order.UserName, order.TotalPrice);

        return order.Id;
    }
}

