using AutoMapper;
using Basket.API.Data;
using BuildingBlocks.Messaging.Events;
using MassTransit;
using MediatR;

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
        // ADIM 1: Sepeti Redis'ten al (cache'den hızlı okuma)
        // Redis'te yoksa PostgreSQL'den alır
        var basket = await _repository.GetBasket(request.UserName);
        if (basket == null)
        {
            _logger.LogWarning("Basket not found for {UserName}", request.UserName);
            return false; // Sepet yoksa checkout yapılamaz
        }

        // ADIM 2: BasketCheckoutEvent oluştur
        // Bu event, Ordering Service'in sipariş oluşturması için gerekli tüm bilgileri içerir
        var eventMessage = _mapper.Map<BasketCheckoutEvent>(request);
        eventMessage = eventMessage with { TotalPrice = basket.TotalPrice }; // Sepetten toplam fiyatı ekle

        // ADIM 3: Event'i RabbitMQ'ya gönder Order servisi için
        // RabbitMQ bir message broker'dır (mesaj kuyruğu)
      await _publishEndpoint.Publish(eventMessage, cancellationToken);

        _logger.LogInformation("BasketCheckoutEvent published for {UserName}. TotalPrice: {TotalPrice}",
            request.UserName, eventMessage.TotalPrice);

        // ADIM 4: Sepeti sil
        // Checkout tamamlandığı için sepet artık gerekli değil
        // Hem Redis'ten hem PostgreSQL'den silinir
        await _repository.DeleteBasket(request.UserName);

        return true; // Checkout başarılı
    }
}

