using MediatR;
using Ordering.API.Data;
using Ordering.API.Entities;

namespace Ordering.API.Features.Orders.Commands.UpdateOrder;

public class UpdateOrderHandler : IRequestHandler<UpdateOrderCommand, bool>
{
    private readonly OrderingDbContext _context;
    private readonly ILogger<UpdateOrderHandler> _logger;

    public UpdateOrderHandler(
        OrderingDbContext context,
        ILogger<UpdateOrderHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (order == null)
        {
            _logger.LogWarning("Order not found. OrderId: {OrderId}", request.Id);
            return false;
        }

        order.Status = request.Status;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order updated. OrderId: {OrderId}, NewStatus: {Status}",
            request.Id, request.Status);

        return true;
    }
}

