using AutoMapper;
using Basket.API.Data;
using BuildingBlocks.Messaging.Events;
using MassTransit;
using MediatR;
using BasketCheckoutItem = BuildingBlocks.Messaging.Events.BasketCheckoutItem;

namespace Basket.API.Features.Basket.Commands.CheckoutBasket;

/// <summary>
/// CHECKOUTBASKET HANDLER - COMMAND (Yazma İşlemi + Event Publishing)
/// 
/// NE ZAMAN ÇALIŞIR:
/// - Kullanıcı "Siparişi Tamamla" butonuna bastığında
/// - Ödeme sayfasında sipariş onaylandığında
/// - Frontend: POST /api/baskets/checkout endpoint'i çağrıldığında
/// 
/// NE YAPAR:
/// - Sepeti Redis/PostgreSQL'den okur
/// - BasketCheckoutEvent oluşturur (sipariş bilgileri ile)
/// - Event'i RabbitMQ'ya gönderir (Ordering Service bu event'i dinler ve sipariş oluşturur)
/// - Sepeti siler (checkout edildiği için artık gerekli değil)
/// 
/// ÖNEMLİ: 
/// - Bu handler VERİ DEĞİŞTİRİR (sepet silinir)
/// - RabbitMQ'ya event gönderir (Ordering Service için)
/// - Microservice mimarisinde servisler arası iletişim için event-driven pattern kullanılır
/// </summary>
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
        var basket = await _repository.GetBasket(request.UserName);
        if (basket == null)
        {
            _logger.LogWarning("Basket not found for {UserName}", request.UserName);
            return false;
        }

        if (basket.Items == null || basket.Items.Count == 0)
        {
            _logger.LogWarning("Basket has no items for {UserName}", request.UserName);
            return false;
        }

        var eventMessage = _mapper.Map<BasketCheckoutEvent>(request);
        var items = _mapper.Map<List<BasketCheckoutItem>>(basket.Items);

        eventMessage = eventMessage with
        {
            TotalPrice = basket.TotalPrice,
            Items = items
        };

        await _publishEndpoint.Publish(eventMessage, cancellationToken);

        _logger.LogInformation(
            "BasketCheckoutEvent published for {UserName}. TotalPrice: {TotalPrice}, ItemCount: {ItemCount}",
            request.UserName, eventMessage.TotalPrice, eventMessage.Items.Count);

        await _repository.DeleteBasket(request.UserName);

        return true;
    }
}

