using AutoMapper;
using Basket.API.Data;
using Basket.API.Dtos;
using Basket.API.Entities;
using MediatR;

namespace Basket.API.Features.Basket.Commands.StoreBasket;

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
        // 1. DTO'dan Entity'ye map et
        var basket = _mapper.Map<ShoppingCart>(request.Basket);

        // 2. Redis'e kaydet
        var savedBasket = await _repository.SaveBasket(basket);

        // 3. Entity'den DTO'ya map et
        var basketDto = _mapper.Map<ShoppingCartDto>(savedBasket);

        _logger.LogInformation("Basket stored for {UserName}", request.Basket.UserName);

        return basketDto;
    }
}

