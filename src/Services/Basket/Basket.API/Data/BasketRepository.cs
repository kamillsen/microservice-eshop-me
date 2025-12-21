using System.Text.Json;
using AutoMapper;
using Basket.API.Dtos;
using Basket.API.Entities;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Basket.API.Data;

public class BasketRepository : IBasketRepository
{
    private const string RedisKeyPrefix = "basket:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24);
    
    private readonly IDatabase _redis;
    private readonly BasketDbContext _context;
    private readonly ILogger<BasketRepository> _logger;
    private readonly IMapper _mapper;

    public BasketRepository(
        IConnectionMultiplexer redis,
        BasketDbContext context,
        ILogger<BasketRepository> logger,
        IMapper mapper)
    {
        _redis = redis.GetDatabase();
        _context = context;
        _logger = logger;
        _mapper = mapper;
    }
    
    private string GetRedisKey(string userName) => $"{RedisKeyPrefix}{userName}";
    
    private async Task<ShoppingCart?> ReloadBasketWithItems(Guid basketId)
    {
        return await _context.ShoppingCarts
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == basketId);
    }

    /// <summary>
    /// Kullanıcının sepetini getirir. Kullanıcı sepetini görüntülemek istediğinde çalışır (GET /api/baskets/{userName}).
    /// Önce Redis cache'e bakar, yoksa PostgreSQL'den alır ve cache'e yazar.
    /// </summary>
    public async Task<ShoppingCart?> GetBasket(string userName)
    {
        try
        {
            // 1. Önce Redis'e bak (hızlı)
            var cached = await _redis.StringGetAsync(GetRedisKey(userName));
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
                await _redis.StringSetAsync(GetRedisKey(userName), json, CacheExpiration);
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

    /// <summary>
    /// Sepeti kaydeder veya günceller. Kullanıcı sepete ürün eklediğinde/güncellediğinde çalışır (POST /api/baskets).
    /// Önce PostgreSQL'e yazar (kaynak veri), sonra Redis cache'e yazar (performans için).
    /// </summary>
    public async Task<ShoppingCart> SaveBasket(ShoppingCart basket)
    {
        // 1. PostgreSQL'de kullanıcının mevcut sepetini kontrol et
        //    Varsa existing'e yüklenir, yoksa null döner
        var existing = await _context.ShoppingCarts
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.UserName == basket.UserName);

        // 2. YENİ SEPET İSE: ID'ler atanıp PostgreSQL'e eklenir
        if (existing == null)
        {
            // Sepet ve item'lara benzersiz ID'ler ata
            basket.Id = Guid.NewGuid();
            basket.Items.ForEach(item => item.Id = Guid.NewGuid());
            // Yeni sepeti EF Core'a ekle (henüz DB'ye yazılmadı)
            _context.ShoppingCarts.Add(basket);
        }
        // 3. MEVCUT SEPET İSE: Eski item'lar silinir, yeni item'lar eklenir
        else
        {
            // Eski item'ları PostgreSQL'den direkt sil (ExecuteDeleteAsync tracking yapmaz)
            await _context.ShoppingCartItems
                .Where(x => x.ShoppingCartId == existing.Id)
                .ExecuteDeleteAsync();
            
            // Kullanıcıdan gelen yeni item'ları entity'ye çevir ve hazırla
            var newItems = basket.Items.Select(item =>
            {
                var entity = _mapper.Map<ShoppingCartItem>(item);
                entity.Id = Guid.NewGuid();
                entity.ShoppingCartId = existing.Id; // Mevcut sepete bağla
                return entity;
            }).ToList();
            
            // Yeni item'ları EF Core'a ekle (henüz DB'ye yazılmadı)
            _context.ShoppingCartItems.AddRange(newItems);
        }

        // 4. Tüm değişiklikleri PostgreSQL'e kaydet (INSERT veya DELETE+INSERT)
        await _context.SaveChangesAsync();

        // 5. savedBasket'e güncel veriyi ata (Redis'e yazmak ve döndürmek için)
        ShoppingCart savedBasket;
        if (existing != null)
        {
            // MEVCUT SEPET GÜNCELLENDİYSE:
            // ExecuteDeleteAsync tracking yapmadığı için existing.Items eski veriyi içeriyor
            // Bu yüzden PostgreSQL'den güncel veriyi çek (reload)
            // Eğer reload null dönerse existing'i kullan (fallback)
            savedBasket = await ReloadBasketWithItems(existing.Id) ?? existing;
        }
        else
        {
            // YENİ SEPET OLUŞTURULDUYSA:
            // basket zaten doğru (ID'ler atandı, PostgreSQL'e yazıldı)
            // Reload gerekmez, direkt basket'i kullan
            savedBasket = basket;
        }

        // 6. savedBasket'i Redis cache'e yaz (performans için)
        try
        {
            // Basket'i JSON string'e çevir
            var json = JsonSerializer.Serialize(savedBasket);
            // Redis'e kaydet (key: "basket:username", expiration: 24 saat)
            await _redis.StringSetAsync(GetRedisKey(basket.UserName), json, CacheExpiration);
            _logger.LogInformation("Basket saved to database and cached for {UserName}", basket.UserName);
        }
        catch (RedisConnectionException ex)
        {
            // Redis down olsa bile devam et (PostgreSQL'e yazıldı zaten - source of truth)
            _logger.LogWarning(ex, "Redis unavailable, basket saved to database only for {UserName}", basket.UserName);
        }

        // 7. savedBasket'i döndür (API response olarak)
        return savedBasket;
    }

    /// <summary>
    /// Kullanıcının sepetini siler. Kullanıcı sepetini temizlediğinde çalışır (DELETE /api/baskets/{userName}).
    /// Hem PostgreSQL'den hem de Redis cache'den siler.
    /// </summary>
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
            var deleted = await _redis.KeyDeleteAsync(GetRedisKey(userName));
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

