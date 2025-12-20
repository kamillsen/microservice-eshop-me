using AutoMapper;
using Basket.API.Data;
using Basket.API.Dtos;
using Basket.API.GrpcServices;
using MediatR;

namespace Basket.API.Features.Basket.Queries.GetBasket;

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
        // 1. Sepeti Redis'ten al
        var basket = await _repository.GetBasket(request.UserName);
        
        if (basket == null)
        {
            // Sepet yoksa yeni sepet döndür
            return new ShoppingCartDto
            {
                UserName = request.UserName,
                Items = new List<ShoppingCartItemDto>()
            };
        }

        // 2. Her item için indirim sorgula (gRPC)
        decimal totalDiscount = 0;
        foreach (var item in basket.Items)
        {
            var coupon = await _discountGrpcService.GetDiscount(item.ProductName);
            if (coupon.Amount > 0)
            {
                totalDiscount += coupon.Amount * item.Quantity;
            }
        }

        // 3. DTO'ya map et
        var basketDto = _mapper.Map<ShoppingCartDto>(basket);
        basketDto.Discount = totalDiscount;
        basketDto.TotalPrice = basket.TotalPrice - totalDiscount;

        _logger.LogInformation("Basket retrieved for {UserName}: TotalPrice={TotalPrice}, Discount={Discount}",
            request.UserName, basketDto.TotalPrice, basketDto.Discount);

        return basketDto;
    }
}

