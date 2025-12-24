using AutoMapper;
using Basket.API.Data;
using Basket.API.Dtos;
using Basket.API.GrpcServices;
using MediatR;

namespace Basket.API.Features.Basket.Queries.GetBasket;

/// <summary>
/// GETBASKET HANDLER - QUERY (Okuma İşlemi)
/// 
/// NE ZAMAN ÇALIŞIR:
/// - Kullanıcı sepete bakmak istediğinde
/// - Sepet sayfası yüklendiğinde
/// - Frontend: GET /api/baskets/{userName} endpoint'i çağrıldığında
/// 
/// NE YAPAR:
/// - Sepeti Redis'ten okur (hızlı erişim için cache)
/// - Redis'te yoksa PostgreSQL'den okur ve Redis'e cache'ler
/// - Her ürün için Discount gRPC servisinden indirim bilgisi alır
/// - Toplam indirimi hesaplar ve fiyattan düşer
/// - Sepet bilgisini DTO formatında döner
/// 
/// ÖNEMLİ: Bu handler VERİ DEĞİŞTİRMEZ, sadece okur ve gösterir.
/// </summary>
public class GetBasketHandler : IRequestHandler<GetBasketQuery, ShoppingCartDto>
{
    private readonly IBasketRepository _repository;
    private readonly DiscountGrpcService _discountGrpcService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetBasketHandler> _logger;

    public GetBasketHandler(
        IBasketRepository repository,
        DiscountGrpcService discountGrpcService,
        IMapper mapper,
        ILogger<GetBasketHandler> logger)
    {
        _repository = repository;
        _discountGrpcService = discountGrpcService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ShoppingCartDto> Handle(GetBasketQuery request, CancellationToken cancellationToken)
    {
        // ADIM 1: Sepeti Redis'ten almaya çalışır (cache'den hızlı okuma)
        // Redis'te yoksa PostgreSQL'den alır ve Redis'e cache'ler
        var basket = await _repository.GetBasket(request.UserName);
        
        // Sepet yoksa boş sepet döndür (kullanıcı ilk kez sepete bakıyorsa)
        if (basket == null)
        {
            return new ShoppingCartDto
            {
                UserName = request.UserName,
                Items = new List<ShoppingCartItemDto>()
            };
        }

        // ADIM 2: Her ürün için Discount gRPC servisinden indirim sorgula
        // Örnek: "iPhone 15" ürünü için %10 indirim varsa, o indirimi hesapla
        decimal totalDiscount = 0;
        foreach (var item in basket.Items)
        {
            var coupon = await _discountGrpcService.GetDiscount(item.ProductName);
            if (coupon.Amount > 0)
            {
                // İndirim miktarı × ürün adedi = toplam indirim
                totalDiscount += coupon.Amount * item.Quantity;
            }
        }

        // ADIM 3: Entity'yi DTO'ya map et (frontend'e göndermek için)
        var basketDto = _mapper.Map<ShoppingCartDto>(basket);
        basketDto.Discount = totalDiscount; // Toplam indirim miktarını ekle
        basketDto.TotalPrice = basket.TotalPrice - totalDiscount; // İndirimli fiyatı hesapla

        _logger.LogInformation("Basket retrieved for {UserName}: TotalPrice={TotalPrice}, Discount={Discount}",
            request.UserName, basketDto.TotalPrice, basketDto.Discount);

        return basketDto;
    }
}

