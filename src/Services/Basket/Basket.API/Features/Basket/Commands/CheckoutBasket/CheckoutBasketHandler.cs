using AutoMapper;
using Basket.API.Data;
using BuildingBlocks.Messaging.Events;
using MassTransit;
using MediatR;

namespace Basket.API.Features.Basket.Commands.CheckoutBasket;

public class CheckoutBasketHandler : IRequestHandler<CheckoutBasketCommand, bool>
{
    private readonly IBasketRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMapper _mapper;
    private readonly ILogger<CheckoutBasketHandler> _logger;

    public CheckoutBasketHandler(
        IBasketRepository repository,
        IPublishEndpoint publishEndpoint,
        IMapper mapper,
        ILogger<CheckoutBasketHandler> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<bool> Handle(CheckoutBasketCommand request, CancellationToken cancellationToken)
    {
        // 1. Sepeti Redis'ten al
        var basket = await _repository.GetBasket(request.UserName);
        if (basket == null)
        {
            _logger.LogWarning("Basket not found for {UserName}", request.UserName);
            return false;
        }

        // 2. Event oluştur
        var eventMessage = _mapper.Map<BasketCheckoutEvent>(request);
        eventMessage = eventMessage with { TotalPrice = basket.TotalPrice };

        // 3. RabbitMQ'ya gönder
        await _publishEndpoint.Publish(eventMessage, cancellationToken);

        _logger.LogInformation("BasketCheckoutEvent published for {UserName}. TotalPrice: {TotalPrice}",
            request.UserName, eventMessage.TotalPrice);

        // 4. Sepeti sil
        await _repository.DeleteBasket(request.UserName);

        return true;
    }
}

