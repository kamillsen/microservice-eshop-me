using AutoMapper;
using BuildingBlocks.Messaging.Events;
using MassTransit;
using MediatR;
using Ordering.API.Features.Orders.Commands.CreateOrder;

namespace Ordering.API.EventHandlers;

public class BasketCheckoutConsumer : IConsumer<BasketCheckoutEvent>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger<BasketCheckoutConsumer> _logger;

    public BasketCheckoutConsumer(
        IMediator mediator,
        IMapper mapper,
        ILogger<BasketCheckoutConsumer> logger)
    {
        _mediator = mediator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BasketCheckoutEvent> context)
    {
        _logger.LogInformation("BasketCheckoutEvent consumed. UserName: {UserName}, TotalPrice: {TotalPrice}",
            context.Message.UserName, context.Message.TotalPrice);

        try
        {
            // 1. Event'ten Command oluştur
            var command = _mapper.Map<CreateOrderCommand>(context.Message);

            // 2. MediatR ile CreateOrderHandler'ı çağır
            var orderId = await _mediator.Send(command);

            _logger.LogInformation("Order created from BasketCheckoutEvent. OrderId: {OrderId}, UserName: {UserName}",
                orderId, context.Message.UserName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing BasketCheckoutEvent for UserName: {UserName}",
                context.Message.UserName);
            throw; // MassTransit otomatik retry yapar
        }
    }
}

