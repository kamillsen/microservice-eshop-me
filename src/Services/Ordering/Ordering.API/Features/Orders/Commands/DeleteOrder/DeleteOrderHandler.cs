using MediatR;
using Ordering.API.Data;
using Ordering.API.Entities;

namespace Ordering.API.Features.Orders.Commands.DeleteOrder;

public class DeleteOrderHandler : IRequestHandler<DeleteOrderCommand, bool>
{
    private readonly OrderingDbContext _context;
    private readonly ILogger<DeleteOrderHandler> _logger;

    public DeleteOrderHandler(
        OrderingDbContext context,
        ILogger<DeleteOrderHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (order == null)
        {
            _logger.LogWarning("Order not found. OrderId: {OrderId}", request.Id);
            return false;
        }

        // Siparişi silme, sadece iptal et
        order.Status = OrderStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order cancelled. OrderId: {OrderId}", request.Id);

        return true;
    }
}

