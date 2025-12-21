# Faz 5 - Basket Service (Redis + gRPC Client)

## Servis Hakkında

**Ne İşe Yarar?**
- Kullanıcıların alışveriş sepetini yönetir
- Sepete ürün ekleme, çıkarma, güncelleme
- Sepeti görüntüleme (toplam fiyat, indirimler dahil)
- **Checkout (Ödeme):** Sepeti siparişe dönüştürme (RabbitMQ event gönderir)

**Örnek Kullanım Senaryosu:**
```
1. Kullanıcı: "iPhone 15'i sepete ekle, adet: 2"
   → Basket Service: Sepeti Redis'e kaydet

2. Kullanıcı: "Sepetimi göster"
   → Basket Service: 
      - Redis'ten sepeti al
      - Discount Service'e gRPC ile bağlan → İndirim var mı?
      - Toplam fiyatı hesapla (indirim dahil)
      - Response: { items: [...], totalPrice: 95000, discount: 5000 }

3. Kullanıcı: "Ödeme yap" (Checkout)
   → Basket Service:
      - BasketCheckoutEvent oluştur
      - RabbitMQ'ya event gönder (Ordering Service dinleyecek)
      - Sepeti Redis'ten sil
```

**Neden şimdi?** 
- ✅ Catalog hazır (ürün ID'leri var)
- ✅ Discount hazır (gRPC client kullanılacak)
- ✅ Artık sepet işlemleri yapılabilir

**Neden Redis + PostgreSQL? (Cache-aside Pattern)**
- **Redis (Cache):** Hızlı okuma/yazma için (kullanıcı deneyimi)
- **PostgreSQL (Source of Truth):** Veri kalıcılığı için (veri kaybı riski düşük)
- **Cache-aside Pattern:** Önce Redis'e bak, yoksa PostgreSQL'den al ve cache'le
- Redis down olsa bile PostgreSQL'den okur (yavaş ama çalışır)
- Sepet geçmişi tutulabilir (analiz için)
- Gerçek dünyada production sistemlerde genellikle bu yaklaşım kullanılır

**Neden gRPC Client?**
- Discount Service'e sürekli indirim sorgusu yapılacak
- gRPC çok hızlı (binary format, HTTP/2)
- Internal servis iletişimi için ideal

**Neden RabbitMQ?**
- Checkout işlemi asenkron olmalı (Ordering Service'e event gönderir)
- Fire & Forget pattern (event gönder, bekleme)
- Decoupling (Basket Service, Ordering Service'i bilmez)

---

## 5.1 Basket.API Projesi Oluştur

**Hedef:** Redis + PostgreSQL kullanan REST API projesi (Cache-aside pattern)

### Görevler:

#### Basket klasör yapısını oluştur
**Ne işe yarar:** Basket servisi için klasör oluşturur.

```bash
cd src/Services
mkdir Basket
cd Basket
```

**Açıklama:**
- `src/Services/Basket/` klasörü oluşturulur
- Catalog ve Discount gibi aynı yapıda olacak

#### Web API projesi oluştur
**Ne işe yarar:** Basket Service için REST API projesi oluşturur.

```bash
cd src/Services/Basket
dotnet new webapi -n Basket.API
```

**Açıklama:**
- `webapi` template'i ile proje oluşturulur
- Otomatik olarak `Controllers/`, `Program.cs`, `appsettings.json` oluşturulur
- Swagger konfigürasyonu hazır gelir

#### Projeyi solution'a ekle
**Ne işe yarar:** Projeyi solution'a ekler, böylece diğer projeler referans verebilir.

```bash
cd ../../..
dotnet sln add src/Services/Basket/Basket.API/Basket.API.csproj
```

#### NuGet paketlerini ekle
**Ne işe yarar:** Redis, PostgreSQL, gRPC Client, MediatR, MassTransit ve diğer gerekli paketleri ekler.

```bash
cd src/Services/Basket/Basket.API
dotnet add package MediatR
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
dotnet add package AutoMapper
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add package StackExchange.Redis
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Grpc.Net.Client
dotnet add package Google.Protobuf
dotnet add package Grpc.Tools
dotnet add package MassTransit
dotnet add package MassTransit.RabbitMQ
dotnet add package AspNetCore.HealthChecks.Redis
dotnet add package AspNetCore.HealthChecks.NpgSql
```

**Paketler:**
- `MediatR` → CQRS pattern için
- `FluentValidation` → Request validation için
- `AutoMapper` → Object mapping için
- `StackExchange.Redis` → Redis client
- `Microsoft.EntityFrameworkCore` → EF Core (PostgreSQL için)
- `Microsoft.EntityFrameworkCore.Design` → EF Core migrations için
- `Npgsql.EntityFrameworkCore.PostgreSQL` → PostgreSQL provider
- `Grpc.Net.Client` → gRPC client (Discount Service'e bağlanmak için)
- `Google.Protobuf` → Protocol Buffers
- `Grpc.Tools` → Proto dosyasından C# kodları generate etmek için
- `MassTransit` → RabbitMQ abstraction
- `MassTransit.RabbitMQ` → RabbitMQ provider
- `AspNetCore.HealthChecks.Redis` → Redis health check
- `AspNetCore.HealthChecks.NpgSql` → PostgreSQL health check

**Not:** `Grpc.Tools` paketi `PrivateAssets="All"` olarak eklenmelidir (sadece build time'da kullanılır).

#### Project References ekle
**Ne işe yarar:** BuildingBlocks projelerini referans olarak ekler.

```bash
dotnet add reference ../../../BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj
dotnet add reference ../../../BuildingBlocks/BuildingBlocks.Behaviors/BuildingBlocks.Behaviors.csproj
dotnet add reference ../../../BuildingBlocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj
```

**Açıklama:**
- `BuildingBlocks.Exceptions` → Exception handling için
- `BuildingBlocks.Behaviors` → MediatR pipeline behaviors (Validation, Logging)
- `BuildingBlocks.Messaging` → BasketCheckoutEvent için

#### Klasör yapısını oluştur
**Ne işe yarar:** Entity, Data, Features, GrpcServices için klasör yapısını oluşturur.

```bash
mkdir -p Entities
mkdir -p Data
mkdir -p Features/Basket
mkdir -p Features/Basket/Commands
mkdir -p Features/Basket/Queries
mkdir -p GrpcServices
mkdir -p Protos
mkdir -p Dtos
mkdir -p Mapping
```

**Klasör Açıklamaları:**

1. **`Entities/`** → Sepet entity'leri
   - **`ShoppingCart.cs`**: Sepet entity (UserName, Items, TotalPrice)
   - **`ShoppingCartItem.cs`**: Sepet item entity (ProductId, ProductName, Quantity, Price)
   - **Neden?**: Redis'e serialize/deserialize için

2. **`Data/`** → Redis repository
   - **`IBasketRepository.cs`**: Repository interface
   - **`BasketRepository.cs`**: Redis implementation (JSON serialize/deserialize)
   - **Neden?**: Redis işlemlerini soyutlamak için

3. **`Features/Basket/`** → CQRS (Vertical Slice)
   - **`Commands/`**: Yazma işlemleri (StoreBasket, DeleteBasket, CheckoutBasket)
   - **`Queries/`**: Okuma işlemleri (GetBasket)
   - **Neden?**: CQRS pattern, her feature kendi klasöründe

4. **`GrpcServices/`** → gRPC client wrapper
   - **`DiscountGrpcService.cs`**: Discount Service'e gRPC ile bağlanan wrapper class
   - **Neden?**: gRPC client'ı soyutlamak için

5. **`Protos/`** → Proto dosyaları
   - **`discount.proto`**: Discount.Grpc'tan kopyalanacak
   - **Neden?**: gRPC client için proto dosyası gerekli

6. **`Dtos/`** → Data Transfer Objects
   - **`ShoppingCartDto.cs`**: Sepet DTO
   - **`ShoppingCartItemDto.cs`**: Sepet item DTO
   - **Neden?**: API response'ları için

7. **`Mapping/`** → AutoMapper profiles
   - **`MappingProfile.cs`**: Entity ↔ DTO mapping
   - **Neden?**: Object mapping için

#### ShoppingCart ve ShoppingCartItem Entity'lerini oluştur
**Ne işe yarar:** Sepet verilerini temsil eden entity class'larını oluşturur (PostgreSQL için).

**Entities/ShoppingCart.cs:**

```csharp
namespace Basket.API.Entities;

public class ShoppingCart
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = default!;
    public List<ShoppingCartItem> Items { get; set; } = new();
    
    public decimal TotalPrice
    {
        get
        {
            return Items.Sum(item => item.Price * item.Quantity);
        }
    }
}
```

**Entities/ShoppingCartItem.cs:**

```csharp
namespace Basket.API.Entities;

public class ShoppingCartItem
{
    public Guid Id { get; set; }
    public Guid ShoppingCartId { get; set; }
    // Navigation property yok - Referans projeye göre best practice (döngüsel referans sorunu olmaz)
    
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

**Açıklama:**
- `ShoppingCart` → Sepet (Id, UserName, Items navigation property, TotalPrice calculated property)
- `ShoppingCartItem` → Sepet item'ı (Id, ShoppingCartId FK, **Navigation property YOK** - best practice, ProductId, ProductName, Quantity, Price)
- `TotalPrice` → Calculated property (Items'ların toplamı)
- **Önemli:** `ShoppingCart` navigation property **YOK** (best practice - döngüsel referans sorunu olmaz)
- EF Core relationship `BasketDbContext.OnModelCreating` içinde `.WithOne()` ile tanımlanır (navigation property olmadan)

**Neden calculated property?**
- PostgreSQL'de sadece Items tutulur
- TotalPrice runtime'da hesaplanır (indirim dahil)

#### appsettings.json'a connection string ve ayarları ekle
**Ne işe yarar:** Redis, PostgreSQL, gRPC ve RabbitMQ bağlantı bilgilerini ekler.

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379",
    "Database": "Host=localhost;Port=5437;Database=BasketDb;Username=postgres;Password=postgres"
  },
  "GrpcSettings": {
    "DiscountUrl": "http://localhost:5152"
  },
  "MessageBroker": {
    "Host": "amqp://guest:guest@localhost:5673"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**Açıklama:**
- `ConnectionStrings:Redis` → Redis bağlantı string'i (localhost:6379)
- `ConnectionStrings:Database` → PostgreSQL bağlantı string'i (localhost:5437 - host port)
- `GrpcSettings:DiscountUrl` → Discount gRPC servis URL'i (localhost:5152 - Discount.Grpc port)
- `MessageBroker:Host` → RabbitMQ bağlantı string'i (localhost:5673 - host port)

**Not:** 
- Localhost'tan bağlanırken: `localhost:6379`, `localhost:5437`, `localhost:5152`, `localhost:5673`
- Container network içinde: `basketdb:6379`, `basketpostgres:5432`, `http://discount.grpc:8080`, `amqp://guest:guest@messagebroker:5672`

### Kontrol:
- Proje build oluyor mu? (`dotnet build`)
- Solution'da görünüyor mu? (`dotnet sln list`)
- Tüm paketler eklendi mi? (`cat Basket.API.csproj`)

---

## 5.2 Basket Redis + PostgreSQL Repository

**Hedef:** Redis + PostgreSQL ile sepet işlemleri (Cache-aside pattern)

### Görevler:

#### IBasketRepository interface oluştur
**Ne işe yarar:** Repository pattern için interface tanımlar.

**Data/IBasketRepository.cs:**

```csharp
using Basket.API.Entities;

namespace Basket.API.Data;

public interface IBasketRepository
{
    Task<ShoppingCart?> GetBasket(string userName);
    Task<ShoppingCart> SaveBasket(ShoppingCart basket);
    Task<bool> DeleteBasket(string userName);
}
```

**Açıklama:**
- `GetBasket` → Redis'ten sepeti getirir (yoksa null döner)
- `SaveBasket` → Redis'e sepeti kaydeder (yoksa oluşturur, varsa günceller)
- `DeleteBasket` → Redis'ten sepeti siler

#### BasketDbContext oluştur
**Ne işe yarar:** EF Core ile PostgreSQL veritabanı bağlantısını sağlar.

**Data/BasketDbContext.cs:**

```csharp
using Basket.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Basket.API.Data;

public class BasketDbContext : DbContext
{
    public BasketDbContext(DbContextOptions<BasketDbContext> options) : base(options)
    {
    }

    public DbSet<ShoppingCart> ShoppingCarts { get; set; }
    public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShoppingCart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserName).IsUnique();
            // Navigation property olmadan relationship tanımı (best practice - döngüsel referans yok)
            entity.HasMany(e => e.Items)
                .WithOne() // Navigation property yok
                .HasForeignKey(e => e.ShoppingCartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShoppingCartItem>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        base.OnModelCreating(modelBuilder);
    }
}
```

#### BasketRepository implementation oluştur (Cache-aside Pattern)
**Ne işe yarar:** Redis + PostgreSQL ile sepet işlemlerini implement eder (Cache-aside pattern).

**Data/BasketRepository.cs:**

```csharp
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
            // Mevcut item'ları sil (ExecuteDelete - direkt SQL DELETE, tracking sorunu yok)
            await _context.ShoppingCartItems
                .Where(x => x.ShoppingCartId == existing.Id)
                .ExecuteDeleteAsync();
            
            // Yeni item'ları ekle (AutoMapper kullan - DRY prensibi, maintenance kolay)
            var newItems = basket.Items.Select(item =>
            {
                var entity = _mapper.Map<ShoppingCartItem>(item);
                entity.Id = Guid.NewGuid();
                entity.ShoppingCartId = existing.Id;
                return entity;
            }).ToList();
            
            _context.ShoppingCartItems.AddRange(newItems);
        }

        await _context.SaveChangesAsync();

        // 2. Redis'e yaz (cache) - SaveChanges sonrası existing'i yeniden yükle (Items için)
        try
        {
            ShoppingCart savedBasket;
            if (existing != null)
            {
                // ExecuteDelete sonrası existing.Items boş olabilir, yeniden yükle
                savedBasket = await ReloadBasketWithItems(existing.Id) ?? existing;
            }
            else
            {
                savedBasket = basket;
            }
            
            var json = JsonSerializer.Serialize(savedBasket);
            await _redis.StringSetAsync(GetRedisKey(basket.UserName), json, CacheExpiration);
            _logger.LogInformation("Basket saved to database and cached for {UserName}", basket.UserName);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable, basket saved to database only for {UserName}", basket.UserName);
        }

        // Return için de existing'i yeniden yükle
        if (existing != null)
        {
            return await ReloadBasketWithItems(existing.Id) ?? existing;
        }
        
        return basket;
    }
    
    // Helper methods (DRY prensibi)
    private string GetRedisKey(string userName) => $"{RedisKeyPrefix}{userName}";
    
    private async Task<ShoppingCart?> ReloadBasketWithItems(Guid basketId)
    {
        return await _context.ShoppingCarts
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == basketId);
    }
    
    // Constants (DRY prensibi)
    private const string RedisKeyPrefix = "basket:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24);

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
```

**Açıklama:**
- **Cache-aside Pattern:**
  - `GetBasket`: Önce Redis'e bak, yoksa PostgreSQL'den al ve cache'le
  - `SaveBasket`: PostgreSQL'e yaz (source of truth), Redis'e cache'le
  - `DeleteBasket`: PostgreSQL'den sil, Redis'ten sil
- **Redis down olursa:** Sadece PostgreSQL kullanılır (yavaş ama çalışır)
- **PostgreSQL = Source of Truth:** Gerçek veri kaynağı
- **Redis = Cache:** Hız için

**Önemli İyileştirmeler (Best Practices):**
- **ExecuteDeleteAsync()**: EF Core 7+ özelliği, direkt SQL DELETE çalıştırır (tracking sorunu yok, daha performanslı)
- **AddRange()**: Toplu ekleme, daha performanslı
- **AutoMapper**: Manuel mapping yerine AutoMapper kullanımı (DRY prensibi, maintenance kolay)
- **Helper Methods**: `GetRedisKey()` ve `ReloadBasketWithItems()` - kod tekrarını önler (DRY)
- **Constants**: `RedisKeyPrefix` ve `CacheExpiration` - magic string/values yerine constant kullanımı (DRY)
- **ReloadBasketWithItems()**: `ExecuteDeleteAsync()` sonrası `existing.Items` boş olabileceği için yeniden yükleme

**Önemli Noktalar:**
- JSON serialize/deserialize → Redis string olarak tutar
- Key naming: `basket:{userName}` → Her kullanıcı için ayrı key (helper method ile)
- Null check → GetBasket null dönebilir (sepet yoksa)
- EF Core Include → Navigation property'leri yükler

#### Program.cs'de PostgreSQL, Redis ve Repository kaydı
**Ne işe yarar:** PostgreSQL DbContext, Redis connection ve repository'yi DI container'a kaydeder.

**Program.cs güncellemesi:**

```csharp
using Basket.API.Data;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL
builder.Services.AddDbContext<BasketDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(configuration!);
});

// Repository
builder.Services.AddScoped<IBasketRepository, BasketRepository>();

// ... diğer servisler

var app = builder.Build();

// Migration
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BasketDbContext>();
    await context.Database.MigrateAsync();
}

// Health Checks
builder.Services.AddHealthChecks()
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!)
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!);
```

**Açıklama:**
- `AddDbContext<BasketDbContext>` → PostgreSQL DbContext scoped (her request için yeni)
- `AddSingleton<IConnectionMultiplexer>` → Redis connection singleton (tek instance, tüm request'ler paylaşır)
- `ConnectionMultiplexer.Connect` → Redis'e bağlanır
- `AddScoped<IBasketRepository>` → Repository scoped (her request için yeni instance)
- `MigrateAsync()` → Migration'ları otomatik uygular (container başladığında)
- `AddHealthChecks().AddRedis().AddNpgSql()` → Redis ve PostgreSQL health check

**Neden Singleton (Redis)?**
- Redis connection pahalı (network connection)
- Tüm request'ler aynı connection'ı kullanabilir
- Thread-safe

**Neden Scoped (DbContext, Repository)?**
- DbContext ve Repository her request için yeni instance
- Request bitince dispose edilir

### Test:
- PostgreSQL'e bağlanıyor mu? (Migration uygulandı mı? Tablolar oluştu mu?)
- Redis'e bağlanıyor mu? (`dotnet run` ile servisi başlat, logları kontrol et)
- Sepet kaydediliyor mu? (PostgreSQL ve Redis'te kontrol et)
- Cache-aside pattern çalışıyor mu? (Redis'te yoksa PostgreSQL'den alıyor mu?)

---

## 5.3 Basket gRPC Client (Discount)

**Hedef:** Discount servisine gRPC ile bağlan

### Görevler:

#### discount.proto dosyasını kopyala
**Ne işe yarar:** Discount.Grpc'tan proto dosyasını kopyalar (gRPC client için gerekli).

```bash
cp ../Discount/Discount.Grpc/Protos/discount.proto src/Services/Basket/Basket.API/Protos/discount.proto
```

**Açıklama:**
- Proto dosyası aynı olmalı (server ve client aynı contract'ı kullanır)
- `Protos/` klasörüne kopyalanır

#### .csproj dosyasına proto reference ekle
**Ne işe yarar:** Proto dosyasından C# client kodları generate edilmesini sağlar.

**Basket.API.csproj güncellemesi:**

```xml
<ItemGroup>
  <Protobuf Include="Protos\discount.proto" GrpcServices="Client" />
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Grpc.Tools" PrivateAssets="All" />
</ItemGroup>
```

**Açıklama:**
- `GrpcServices="Client"` → Client kodları generate edilir (server değil)
- `PrivateAssets="All"` → Grpc.Tools sadece build time'da kullanılır

**Önemli:** Proto dosyası build sırasında C# kodlarına dönüştürülür (`obj/Debug/net9.0/Protos/` klasöründe).

#### DiscountGrpcService oluştur
**Ne işe yarar:** gRPC client'ı wrapper class ile soyutlar.

**GrpcServices/DiscountGrpcService.cs:**

```csharp
using Basket.API.Protos;
using Grpc.Net.Client;

namespace Basket.API.GrpcServices;

public class DiscountGrpcService
{
    private readonly DiscountProtoService.DiscountProtoServiceClient _client;
    private readonly ILogger<DiscountGrpcService> _logger;

    public DiscountGrpcService(
        DiscountProtoService.DiscountProtoServiceClient client,
        ILogger<DiscountGrpcService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<CouponModel> GetDiscount(string productName)
    {
        try
        {
            _logger.LogInformation("Getting discount for {ProductName}", productName);
            
            var request = new GetDiscountRequest { ProductName = productName };
            var response = await _client.GetDiscountAsync(request);
            
            _logger.LogInformation("Discount retrieved for {ProductName}: {Amount}", 
                productName, response.Amount);
            
            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Error getting discount for {ProductName}", productName);
            
            // İndirim yoksa null döndür (hata fırlatma)
            if (ex.StatusCode == StatusCode.NotFound)
                return new CouponModel { Amount = 0 };
            
            throw;
        }
    }
}
```

**Açıklama:**
- `DiscountProtoService.DiscountProtoServiceClient` → Proto dosyasından generate edilen client
- `GetDiscountAsync` → gRPC async metod çağrısı
- `RpcException` → gRPC hataları (NotFound, vb.)
- NotFound durumunda → Amount = 0 döndürür (hata fırlatmaz)

**Neden wrapper class?**
- gRPC client'ı soyutlar
- Error handling merkezi yapılır
- Test edilebilirlik artar

#### Program.cs'de gRPC client konfigürasyonu
**Ne işe yarar:** gRPC client'ı DI container'a kaydeder.

**Program.cs güncellemesi:**

```csharp
using Basket.API.GrpcServices;
using Basket.API.Protos;

// gRPC Client
builder.Services.AddGrpcClient<DiscountProtoService.DiscountProtoServiceClient>(options =>
{
    options.Address = new Uri(builder.Configuration["GrpcSettings:DiscountUrl"]!);
});

// DiscountGrpcService
builder.Services.AddScoped<DiscountGrpcService>();
```

**Açıklama:**
- `AddGrpcClient<T>` → gRPC client'ı DI'ya kaydeder
- `Address` → Discount gRPC servis URL'i (appsettings.json'dan)
- `AddScoped<DiscountGrpcService>` → Wrapper service scoped

**Önemli:** gRPC client HTTP/2 kullanır, `GrpcSettings:DiscountUrl` doğru olmalı.

### Test:
- gRPC client Discount'a bağlanıyor mu? (Discount.Grpc servisi çalışırken test et)
- İndirim bilgisi alınabiliyor mu? (GetDiscount metodu test et)
- NotFound durumunda hata fırlatmıyor mu? (Olmayan ürün için test et)

---

## 5.4 Basket CQRS - Commands & Queries

**Hedef:** Sepet işlemleri (CQRS + MediatR)

### Görevler:

#### MediatR, FluentValidation, AutoMapper konfigürasyonu
**Ne işe yarar:** CQRS pattern için gerekli servisleri kaydeder.

**Program.cs güncellemesi:**

```csharp
using BuildingBlocks.Behaviors.Behaviors;
using FluentValidation;
using MediatR;

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);
```

**Açıklama:**
- `RegisterServicesFromAssembly` → Handler'ları otomatik bulur
- `AddBehavior` → Pipeline behaviors ekler (Logging, Validation)
- `AddValidatorsFromAssembly` → Validator'ları otomatik bulur
- `AddAutoMapper` → AutoMapper profile'ları otomatik bulur

#### GetBasketQuery + GetBasketHandler
**Ne işe yarar:** Sepeti getirir (Redis'ten + indirim hesaplar).

**Features/Basket/Queries/GetBasket/GetBasketQuery.cs:**

```csharp
using Basket.API.Dtos;
using MediatR;

namespace Basket.API.Features.Basket.Queries.GetBasket;

public record GetBasketQuery(string UserName) : IRequest<ShoppingCartDto>;
```

**Features/Basket/Queries/GetBasket/GetBasketHandler.cs:**

```csharp
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
```

**Açıklama:**
- `GetBasket` → Redis'ten sepeti alır
- Her item için indirim sorgular (gRPC)
- Toplam indirimi hesaplar
- DTO'ya map eder ve döndürür

**Önemli Noktalar:**
- Sepet yoksa → Boş sepet döndürür (hata fırlatmaz)
- İndirim hesaplama → Her item için ayrı sorgu (gRPC hızlı)
- TotalPrice → Basket.TotalPrice - Discount

#### StoreBasketCommand + StoreBasketHandler + StoreBasketValidator
**Ne işe yarar:** Sepeti kaydeder/günceller (Redis'e).

**Features/Basket/Commands/StoreBasket/StoreBasketCommand.cs:**

```csharp
using Basket.API.Dtos;
using MediatR;

namespace Basket.API.Features.Basket.Commands.StoreBasket;

public record StoreBasketCommand(ShoppingCartDto Basket) : IRequest<ShoppingCartDto>;
```

**Features/Basket/Commands/StoreBasket/StoreBasketValidator.cs:**

```csharp
using FluentValidation;

namespace Basket.API.Features.Basket.Commands.StoreBasket;

public class StoreBasketValidator : AbstractValidator<StoreBasketCommand>
{
    public StoreBasketValidator()
    {
        RuleFor(x => x.Basket.UserName)
            .NotEmpty().WithMessage("UserName boş olamaz");

        RuleFor(x => x.Basket.Items)
            .NotNull().WithMessage("Items null olamaz");

        RuleForEach(x => x.Basket.Items)
            .SetValidator(new ShoppingCartItemValidator());
    }
}

public class ShoppingCartItemValidator : AbstractValidator<ShoppingCartItemDto>
{
    public ShoppingCartItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId boş olamaz");

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("ProductName boş olamaz");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity 0'dan büyük olmalı");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price 0'dan büyük olmalı");
    }
}
```

**Features/Basket/Commands/StoreBasket/StoreBasketHandler.cs:**

```csharp
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
```

**Açıklama:**
- `StoreBasket` → Sepeti Redis'e kaydeder (yoksa oluşturur, varsa günceller)
- Validation → FluentValidation ile doğrulama
- Mapping → AutoMapper ile DTO ↔ Entity

#### DeleteBasketCommand + DeleteBasketHandler
**Ne işe yarar:** Sepeti siler (Redis'ten).

**Features/Basket/Commands/DeleteBasket/DeleteBasketCommand.cs:**

```csharp
using MediatR;

namespace Basket.API.Features.Basket.Commands.DeleteBasket;

public record DeleteBasketCommand(string UserName) : IRequest<bool>;
```

**Features/Basket/Commands/DeleteBasket/DeleteBasketHandler.cs:**

```csharp
using Basket.API.Data;
using MediatR;

namespace Basket.API.Features.Basket.Commands.DeleteBasket;

public class DeleteBasketHandler : IRequestHandler<DeleteBasketCommand, bool>
{
    private readonly IBasketRepository _repository;
    private readonly ILogger<DeleteBasketHandler> _logger;

    public DeleteBasketHandler(
        IBasketRepository repository,
        ILogger<DeleteBasketHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteBasketCommand request, CancellationToken cancellationToken)
    {
        var deleted = await _repository.DeleteBasket(request.UserName);
        
        if (deleted)
            _logger.LogInformation("Basket deleted for {UserName}", request.UserName);
        else
            _logger.LogWarning("Basket not found for {UserName}", request.UserName);

        return deleted;
    }
}
```

#### CheckoutBasketCommand + CheckoutBasketHandler
**Ne işe yarar:** Sepeti checkout eder (RabbitMQ event gönderir).

**Features/Basket/Commands/CheckoutBasket/CheckoutBasketCommand.cs:**

```csharp
using BuildingBlocks.Messaging.Events;
using MediatR;

namespace Basket.API.Features.Basket.Commands.CheckoutBasket;

public record CheckoutBasketCommand(
    string UserName,
    string FirstName,
    string LastName,
    string EmailAddress,
    string AddressLine,
    string Country,
    string State,
    string ZipCode,
    string CardName,
    string CardNumber,
    string Expiration,
    string CVV,
    int PaymentMethod
) : IRequest<bool>;
```

**Features/Basket/Commands/CheckoutBasket/CheckoutBasketValidator.cs:**

```csharp
using FluentValidation;

namespace Basket.API.Features.Basket.Commands.CheckoutBasket;

public class CheckoutBasketValidator : AbstractValidator<CheckoutBasketCommand>
{
    public CheckoutBasketValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("UserName boş olamaz");

        RuleFor(x => x.EmailAddress)
            .NotEmpty().EmailAddress().WithMessage("Geçerli email adresi gerekli");

        RuleFor(x => x.AddressLine)
            .NotEmpty().WithMessage("Adres boş olamaz");

        RuleFor(x => x.CardNumber)
            .NotEmpty().WithMessage("Kart numarası boş olamaz")
            .Length(16).WithMessage("Kart numarası 16 haneli olmalı");

        RuleFor(x => x.CVV)
            .NotEmpty().WithMessage("CVV boş olamaz")
            .Length(3).WithMessage("CVV 3 haneli olmalı");
    }
}
```

**Features/Basket/Commands/CheckoutBasket/CheckoutBasketHandler.cs:**

```csharp
using AutoMapper;
using Basket.API.Data;
using BuildingBlocks.Messaging.Events;
using MassTransit;
using MediatR;

namespace Basket.API.Features.Basket.Commands.CheckoutBasket;

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
        // 1. Sepeti Redis'ten al
        var basket = await _repository.GetBasket(request.UserName);
        if (basket == null)
        {
            _logger.LogWarning("Basket not found for {UserName}", request.UserName);
            return false;
        }

        // 2. Event oluştur
        var eventMessage = _mapper.Map<BasketCheckoutEvent>(request);
        eventMessage.TotalPrice = basket.TotalPrice;

        // 3. RabbitMQ'ya gönder
        await _publishEndpoint.Publish(eventMessage, cancellationToken);

        _logger.LogInformation("BasketCheckoutEvent published for {UserName}. TotalPrice: {TotalPrice}",
            request.UserName, eventMessage.TotalPrice);

        // 4. Sepeti sil
        await _repository.DeleteBasket(request.UserName);

        return true;
    }
}
```

**Açıklama:**
- `CheckoutBasket` → Sepeti checkout eder
- `BasketCheckoutEvent` → RabbitMQ'ya gönderilir (Ordering Service dinleyecek)
- Sepet silinir → Checkout sonrası sepet temizlenir

**Önemli Noktalar:**
- `IPublishEndpoint` → MassTransit ile event publish
- Event mapping → Command'den Event'e AutoMapper ile
- TotalPrice → Basket'ten alınır

#### AutoMapper MappingProfile oluştur
**Ne işe yarar:** Entity ↔ DTO ve Command ↔ Event mapping'lerini tanımlar.

**Mapping/MappingProfile.cs:**

```csharp
using AutoMapper;
using Basket.API.Dtos;
using Basket.API.Entities;
using Basket.API.Features.Basket.Commands.CheckoutBasket;
using BuildingBlocks.Messaging.Events;

namespace Basket.API.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Entity ↔ DTO
        CreateMap<ShoppingCart, ShoppingCartDto>().ReverseMap();
        CreateMap<ShoppingCartItem, ShoppingCartItemDto>().ReverseMap();

        // Command → Event
        CreateMap<CheckoutBasketCommand, BasketCheckoutEvent>();
    }
}
```

#### MassTransit konfigürasyonu
**Ne işe yarar:** RabbitMQ ile event publish için MassTransit'i yapılandırır.

**Program.cs güncellemesi:**

```csharp
using MassTransit;

// MassTransit
builder.Services.AddMassTransit(config =>
{
    config.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["MessageBroker:Host"], "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});
```

**Açıklama:**
- `AddMassTransit` → MassTransit'i DI'ya kaydeder
- `UsingRabbitMq` → RabbitMQ provider kullanır
- `Host` → RabbitMQ connection string (appsettings.json'dan)

### Test:
- Sepet CRUD çalışıyor mu? (GetBasket, StoreBasket, DeleteBasket)
- Checkout event RabbitMQ'ya gidiyor mu? (RabbitMQ Management UI'da kontrol et)
- Validation çalışıyor mu? (Geçersiz request gönder)

---

## 5.5 Basket Controller & Entegrasyon

**Hedef:** REST API endpoint'leri

### Görevler:

#### BasketsController oluştur
**Ne işe yarar:** REST API endpoint'lerini oluşturur (MediatR ile).

**Controllers/BasketsController.cs:**

```csharp
using Basket.API.Dtos;
using Basket.API.Features.Basket.Commands.CheckoutBasket;
using Basket.API.Features.Basket.Commands.DeleteBasket;
using Basket.API.Features.Basket.Commands.StoreBasket;
using Basket.API.Features.Basket.Queries.GetBasket;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Basket.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BasketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BasketsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{userName}")]
    [ProducesResponseType(typeof(ShoppingCartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShoppingCartDto>> GetBasket(string userName)
    {
        var basket = await _mediator.Send(new GetBasketQuery(userName));
        return Ok(basket);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ShoppingCartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShoppingCartDto>> StoreBasket([FromBody] ShoppingCartDto basket)
    {
        var result = await _mediator.Send(new StoreBasketCommand(basket));
        return Ok(result);
    }

    [HttpDelete("{userName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteBasket(string userName)
    {
        await _mediator.Send(new DeleteBasketCommand(userName));
        return NoContent();
    }

    [HttpPost("checkout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> CheckoutBasket([FromBody] CheckoutBasketCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
```

**Açıklama:**
- `GET /api/baskets/{userName}` → Sepeti getirir
- `POST /api/baskets` → Sepeti kaydeder/günceller
- `DELETE /api/baskets/{userName}` → Sepeti siler
- `POST /api/baskets/checkout` → Checkout yapar (RabbitMQ event)

#### Exception middleware ekle
**Ne işe yarar:** Global exception handling ekler.

**Program.cs güncellemesi:**

```csharp
using BuildingBlocks.Exceptions.Handler;

var app = builder.Build();

// Exception Middleware
app.UseExceptionHandler();

// ... diğer middleware'ler
```

**Açıklama:**
- `UseExceptionHandler` → BuildingBlocks.Exceptions'dan gelen middleware
- Tüm hataları yakalar ve ProblemDetails formatında döner

#### Health Checks ekle
**Ne işe yarar:** Redis ve PostgreSQL bağlantılarını kontrol eder.

**Program.cs güncellemesi:**

```csharp
// Health Checks
builder.Services.AddHealthChecks()
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!)
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!);

// ...

app.MapHealthChecks("/health");
```

**Açıklama:**
- `AddRedis` → Redis health check ekler
- `AddNpgSql` → PostgreSQL health check ekler
- `/health` endpoint'i → Redis ve PostgreSQL bağlantılarını kontrol eder

#### Swagger konfigürasyonu
**Ne işe yarar:** Swagger UI'ı yapılandırır.

**Program.cs güncellemesi:**

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ...

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

### Test:
- Swagger açılıyor mu? (http://localhost:5278/swagger)
- Endpoint'ler çalışıyor mu?
  - `GET /api/baskets/user1` → Sepeti getirir
  - `POST /api/baskets` → Sepeti kaydeder
  - `DELETE /api/baskets/user1` → Sepeti siler
  - `POST /api/baskets/checkout` → Checkout yapar
- Health check çalışıyor mu? (http://localhost:5278/health)
- Checkout event RabbitMQ'ya gidiyor mu? (RabbitMQ Management UI'da kontrol et)

**Sonuç:** ✅ Basket Service çalışıyor (Port 5278 - launchSettings.json'da tanımlı)

---

## Özet: Faz 5 adımlar sırası

1. Basket klasörünü oluştur
2. Basket.API Web API projesi oluştur (`dotnet new webapi`)
3. Projeyi solution'a ekle
4. NuGet paketlerini ekle (MediatR, Redis, gRPC Client, MassTransit, vb.)
5. Project References ekle (BuildingBlocks)
6. Klasör yapısını oluştur (Entities, Data, Features, GrpcServices, Protos, Dtos, Mapping)
7. ShoppingCart ve ShoppingCartItem Entity'lerini oluştur
8. appsettings.json'a connection string ve ayarları ekle (Redis, gRPC, RabbitMQ)
9. IBasketRepository interface oluştur
10. BasketRepository implementation oluştur (Redis)
11. Program.cs'de Redis ve Repository kaydı
12. discount.proto dosyasını kopyala (Discount.Grpc'tan)
13. .csproj dosyasına proto reference ekle (GrpcServices="Client")
14. DiscountGrpcService oluştur (gRPC client wrapper)
15. Program.cs'de gRPC client konfigürasyonu
16. MediatR, FluentValidation, AutoMapper konfigürasyonu
17. GetBasketQuery + GetBasketHandler oluştur
18. StoreBasketCommand + StoreBasketHandler + StoreBasketValidator oluştur
19. DeleteBasketCommand + DeleteBasketHandler oluştur
20. CheckoutBasketCommand + CheckoutBasketHandler + CheckoutBasketValidator oluştur
21. AutoMapper MappingProfile oluştur
22. MassTransit konfigürasyonu
23. BasketsController oluştur
24. Exception middleware ekle
25. Health Checks ekle
26. Swagger konfigürasyonu
27. Tüm endpoint'leri test et
28. Checkout event'i RabbitMQ'da kontrol et

**Bu adımlar tamamlandıktan sonra Faz 6'ya (Ordering Service) geçilebilir.**

**Not:** Ordering Service, Basket Service'ten gelen BasketCheckoutEvent'i dinleyecek, bu yüzden Basket Service'in çalışır durumda olması önemlidir.

