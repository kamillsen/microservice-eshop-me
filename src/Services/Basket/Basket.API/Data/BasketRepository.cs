using System.Text.Json;
using Basket.API.Entities;
using StackExchange.Redis;

namespace Basket.API.Data;

public class BasketRepository : IBasketRepository
{
    private readonly IDatabase _database;
    private readonly ILogger<BasketRepository> _logger;

    public BasketRepository(IConnectionMultiplexer redis, ILogger<BasketRepository> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<ShoppingCart?> GetBasket(string userName)
    {
        var basket = await _database.StringGetAsync($"basket:{userName}");
        
        if (basket.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<ShoppingCart>(basket!);
    }

    public async Task<ShoppingCart> SaveBasket(ShoppingCart basket)
    {
        var serializedBasket = JsonSerializer.Serialize(basket);
        await _database.StringSetAsync($"basket:{basket.UserName}", serializedBasket);
        
        _logger.LogInformation("Basket saved for {UserName}", basket.UserName);
        
        return await GetBasket(basket.UserName) ?? basket;
    }

    public async Task<bool> DeleteBasket(string userName)
    {
        var deleted = await _database.KeyDeleteAsync($"basket:{userName}");
        
        if (deleted)
            _logger.LogInformation("Basket deleted for {UserName}", userName);
        
        return deleted;
    }
}

