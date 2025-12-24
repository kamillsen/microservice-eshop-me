using AutoMapper;
using Basket.API.Data;
using Basket.API.Dtos;
using Basket.API.Entities;
using MediatR;

namespace Basket.API.Features.Basket.Commands.StoreBasket;

/// <summary>
/// STOREBASKET HANDLER - COMMAND (Yazma İşlemi)
/// 
/// NE ZAMAN ÇALIŞIR:
/// - Kullanıcı sepete ürün eklediğinde
/// - Sepetteki ürün miktarını değiştirdiğinde
/// - Sepet güncellendiğinde
/// - Frontend: POST /api/baskets endpoint'i çağrıldığında
/// 
/// NE YAPAR:
/// - Gelen sepet verisini (DTO) Entity'ye çevirir
/// - Sepeti PostgreSQL'e kaydeder (kalıcı depolama - source of truth)
/// - Sepeti Redis'e cache'ler (hızlı erişim için)
/// - Mevcut sepet varsa günceller, yoksa yeni oluşturur
/// - Kaydedilen sepeti DTO formatında döner
/// 
/// ÖNEMLİ: Bu handler VERİ DEĞİŞTİRİR (sepet kaydedilir/güncellenir)
/// </summary>
public class StoreBasketHandler : IRequestHandler<StoreBasketCommand, ShoppingCartDto>
{
    private readonly IBasketRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<StoreBasketHandler> _logger;

    public StoreBasketHandler(
        IBasketRepository repository,
        IMapper mapper,
        ILogger<StoreBasketHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ShoppingCartDto> Handle(StoreBasketCommand request, CancellationToken cancellationToken)
    {
        // ADIM 1: Frontend'den gelen DTO'yu Entity'ye map et
        // (DTO = Data Transfer Object, API ile veri alışverişi için)
        // (Entity = Veritabanı modeli)
        var basket = _mapper.Map<ShoppingCart>(request.Basket);

        // ADIM 2: Sepeti kaydet
        // Repository içinde:
        // - Önce PostgreSQL'e yazılır (kalıcı depolama)
        // - Sonra Redis'e cache'lenir (hızlı okuma için)
        // - Mevcut sepet varsa güncellenir, yoksa yeni oluşturulur
        var savedBasket = await _repository.SaveBasket(basket);

        // ADIM 3: Kaydedilen Entity'yi DTO'ya map et (frontend'e döndürmek için)
        var basketDto = _mapper.Map<ShoppingCartDto>(savedBasket);

        _logger.LogInformation("Basket stored for {UserName}", request.Basket.UserName);

        return basketDto;
    }
}

