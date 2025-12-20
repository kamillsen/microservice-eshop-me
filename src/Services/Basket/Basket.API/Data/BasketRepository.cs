using System.Text.Json;
using Basket.API.Entities;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Basket.API.Data;

public class BasketRepository : IBasketRepository
{
    private readonly IDatabase _redis;
    private readonly BasketDbContext _context;
    private readonly ILogger<BasketRepository> _logger;

    public BasketRepository(
        IConnectionMultiplexer redis,
        BasketDbContext context,
        ILogger<BasketRepository> logger)
    {
        _redis = redis.GetDatabase();
        _context = context;
        _logger = logger;
    }

    public async Task<ShoppingCart?> GetBasket(string userName)
    {
        try
        {
            // 1. Önce Redis'e bak (hızlı)
            var cached = await _redis.StringGetAsync($"basket:{userName}");
            if (!cached.IsNullOrEmpty)
            {
                _logger.LogInformation("Basket retrieved from cache for {UserName}", userName);
                return JsonSerializer.Deserialize<ShoppingCart>(cached!);
            }

            // 2. Redis'te yoksa PostgreSQL'den al
            var basket = await _context.ShoppingCarts
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.UserName == userName);

            // 3. PostgreSQL'den aldıktan sonra Redis'e yaz (cache)
            if (basket != null)
            {
                var json = JsonSerializer.Serialize(basket);
                await _redis.StringSetAsync($"basket:{userName}", json, TimeSpan.FromHours(24));
                _logger.LogInformation("Basket retrieved from database and cached for {UserName}", userName);
            }

            return basket;
        }
        catch (RedisConnectionException ex)
        {
            // Redis down olursa sadece PostgreSQL'den oku
            _logger.LogWarning(ex, "Redis unavailable, reading from database for {UserName}", userName);
            return await _context.ShoppingCarts
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.UserName == userName);
        }
    }

    public async Task<ShoppingCart> SaveBasket(ShoppingCart basket)
    {
        // 1. PostgreSQL'e yaz (source of truth)
        var existing = await _context.ShoppingCarts
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.UserName == basket.UserName);

        if (existing == null)
        {
            basket.Id = Guid.NewGuid();
            basket.Items.ForEach(item => item.Id = Guid.NewGuid());
            _context.ShoppingCarts.Add(basket);
        }
        else
        {
            // Mevcut item'ları sil
            _context.ShoppingCartItems.RemoveRange(existing.Items);
            // Yeni item'ları ekle
            basket.Items.ForEach(item =>
            {
                item.Id = Guid.NewGuid();
                item.ShoppingCartId = existing.Id;
            });
            existing.Items = basket.Items;
            _context.ShoppingCarts.Update(existing);
        }

        await _context.SaveChangesAsync();

        // 2. Redis'e yaz (cache)
        try
        {
            var savedBasket = existing ?? basket;
            var json = JsonSerializer.Serialize(savedBasket);
            await _redis.StringSetAsync($"basket:{basket.UserName}", json, TimeSpan.FromHours(24));
            _logger.LogInformation("Basket saved to database and cached for {UserName}", basket.UserName);
        }
        catch (RedisConnectionException ex)
        {
            // Redis down olursa sadece log'la, PostgreSQL'e yazıldı zaten
            _logger.LogWarning(ex, "Redis unavailable, basket saved to database only for {UserName}", basket.UserName);
        }

        return existing ?? basket;
    }

    public async Task<bool> DeleteBasket(string userName)
    {
        // 1. PostgreSQL'den sil
        var basket = await _context.ShoppingCarts
            .FirstOrDefaultAsync(x => x.UserName == userName);

        if (basket != null)
        {
            _context.ShoppingCarts.Remove(basket);
            await _context.SaveChangesAsync();
        }

        // 2. Redis'ten sil
        try
        {
            var deleted = await _redis.KeyDeleteAsync($"basket:{userName}");
            if (deleted || basket != null)
            {
                _logger.LogInformation("Basket deleted for {UserName}", userName);
                return true;
            }
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable, basket deleted from database only for {UserName}", userName);
        }

        return basket != null;
    }
}

