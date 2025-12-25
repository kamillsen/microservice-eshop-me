using AutoMapper;
using Basket.API.Data;
using Basket.API.Dtos;
using Basket.API.Entities;
using Basket.API.GrpcServices;
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
    private readonly DiscountGrpcService _discountGrpcService;
    private readonly IMapper _mapper;
    private readonly ILogger<StoreBasketHandler> _logger;

    public StoreBasketHandler(
        IBasketRepository repository,
        DiscountGrpcService discountGrpcService,
        IMapper mapper,
        ILogger<StoreBasketHandler> logger)
    {
        _repository = repository;
        _discountGrpcService = discountGrpcService;
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

        // ADIM 4: Her ürün için Discount gRPC servisinden indirim sorgula
        decimal totalDiscount = 0;
        foreach (var item in savedBasket.Items)
        {
            var coupon = await _discountGrpcService.GetDiscount(item.ProductName);
            if (coupon.Amount > 0)
            {
                // İndirim miktarı × ürün adedi = toplam indirim
                totalDiscount += coupon.Amount * item.Quantity;
            }
        }

        // ADIM 5: Discount'ı DTO'ya ekle
        basketDto.Discount = totalDiscount;
        basketDto.TotalPrice = savedBasket.TotalPrice - totalDiscount; // İndirimli fiyatı hesapla

        _logger.LogInformation("Basket stored for {UserName}: TotalPrice={TotalPrice}, Discount={Discount}",
            request.Basket.UserName, basketDto.TotalPrice, basketDto.Discount);

        return basketDto;
    }
}

