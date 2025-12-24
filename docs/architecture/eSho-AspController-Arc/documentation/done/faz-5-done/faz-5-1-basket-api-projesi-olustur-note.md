# Faz 5.1, 5.2, 5.3, 5.4 & 5.5 - Basket.API Projesi, Redis Repository, gRPC Client, CQRS ve Controller Notları

> Bu dosya, Faz 5.1 (Basket.API Projesi Oluştur), Faz 5.2 (Basket Redis Repository), Faz 5.3 (Basket gRPC Client), Faz 5.4 (Basket CQRS - Commands & Queries) ve Faz 5.5 (Basket Controller & Entegrasyon) adım adım yaparken öğrendiklerimi not aldığım dosyadır.
> 
> **İçerik:**
> - Faz 5.1: Basket.API Projesi Oluştur
>   - Adım 1: Basket klasör yapısını oluştur
>   - Adım 2: Web API projesi oluştur
>   - Adım 3: Projeyi solution'a ekle
>   - Adım 4: NuGet paketlerini ekle
>   - Adım 5: Project References ekle
>   - Adım 6: Klasör yapısını oluştur
>   - Adım 7: ShoppingCart ve ShoppingCartItem Entity'lerini oluştur
>   - Adım 8: appsettings.json'a connection string ve ayarları ekle
> - Faz 5.2: Basket Redis Repository
>   - IBasketRepository interface oluştur
>   - BasketRepository implementation oluştur
>   - Program.cs'de Redis ve Repository kaydı
> - Faz 5.3: Basket gRPC Client (Discount)
>   - discount.proto dosyasını kopyala
>   - .csproj dosyasına proto reference ekle
>   - DiscountGrpcService oluştur
>   - Program.cs'de gRPC client konfigürasyonu
> - Faz 5.4: Basket CQRS - Commands & Queries
>   - Adım 1: MediatR, FluentValidation, AutoMapper konfigürasyonu
>   - Adım 2: DTO'ları oluştur
>   - Adım 3: AutoMapper Profile oluştur
>   - Adım 4: GetBasketQuery + GetBasketHandler
>   - Adım 5: StoreBasketCommand + StoreBasketHandler + StoreBasketValidator
>   - Adım 6: DeleteBasketCommand + DeleteBasketHandler
>   - Adım 7: CheckoutBasketCommand + CheckoutBasketHandler + CheckoutBasketValidator
>   - Adım 8: MassTransit konfigürasyonu
> - Faz 5.5: Basket Controller & Entegrasyon
>   - BasketsController oluştur
>   - Exception Handler Middleware ekle
>   - Health Checks ekle

---

## Basket Service Nedir?

**Basket Service**, kullanıcıların alışveriş sepetini yönetir. Sepete ürün ekleme, çıkarma, güncelleme ve ödeme işlemlerini yapar.

### Temel İşlevler:
- Sepete ürün ekleme
- Sepetten ürün çıkarma
- Sepetteki ürün miktarını güncelleme
- Sepeti görüntüleme (toplam fiyat, indirimler dahil)
- Sepeti temizleme
- **Checkout (Ödeme):** Sepeti siparişe dönüştürme (RabbitMQ event gönderir)

### Neden şimdi?
- ✅ Catalog hazır (ürün ID'leri var)
- ✅ Discount hazır (gRPC client kullanılacak)
- ✅ Artık sepet işlemleri yapılabilir

### Neden Redis?
- Sepet geçici veri (kullanıcı çıkış yapınca silinebilir)
- Çok hızlı okuma/yazma gerekiyor
- Key-Value yapısı sepet için ideal (`basket:user1` → JSON)

### Neden gRPC Client?
- Discount Service'e sürekli indirim sorgusu yapılacak
- gRPC çok hızlı (binary format, HTTP/2)
- Internal servis iletişimi için ideal

### Neden RabbitMQ?
- Checkout işlemi asenkron olmalı (Ordering Service'e event gönderir)
- Fire & Forget pattern (event gönder, bekleme)
- Decoupling (Basket Service, Ordering Service'i bilmez)

---

## Adım 1: Basket Klasör Yapısını Oluştur

**Komut:**
```bash
cd src/Services
mkdir Basket
cd Basket
```

**Açıklamalar:**
- `cd src/Services` → Services klasörüne geç
- `mkdir Basket` → Basket klasörü oluştur
- `cd Basket` → Basket klasörüne geç

**Ne işe yarar:**
- Basket servisi için klasör oluşturur
- Catalog ve Discount gibi aynı yapıda olacak
- Sonra bu klasöre Web API projesi ekleyeceğiz

**Sonuç:**
- `src/Services/Basket/` klasörü oluşturuldu

**Kontrol:**
```bash
ls -la src/Services/
# Basket klasörünü görmeli
```

---

## Adım 2: Web API Projesi Oluştur

**Komut:**
```bash
cd src/Services/Basket
dotnet new webapi -n Basket.API
```

**Açıklamalar:**
- `dotnet new webapi` → Web API template'i ile proje oluştur
- `-n Basket.API` → Proje adı
- Template otomatik olarak:
  - `Controllers/` klasörü oluşturur
  - `Program.cs` dosyası oluşturur
  - `appsettings.json` dosyası oluşturur
  - Swagger konfigürasyonu hazır gelir

**Ne işe yarar:**
- Basket Service için REST API projesi oluşturur
- Web API projesi = REST endpoint'leri sağlar
- Controller-based API kullanıyoruz (Minimal API değil)

**Neden Web API?**
- REST endpoint'leri için standart ASP.NET Core projesi
- Controller pattern kullanıyoruz (daha organize)
- Swagger desteği otomatik gelir

**Sonuç:**
- `src/Services/Basket/Basket.API/` klasörü oluşturuldu
- `Basket.API.csproj` dosyası oluşturuldu
- `Program.cs`, `appsettings.json` gibi temel dosyalar oluşturuldu

---

## Adım 3: Projeyi Solution'a Ekle

**Komut:**
```bash
cd ../../..
dotnet sln add src/Services/Basket/Basket.API/Basket.API.csproj
```

**Açıklamalar:**
- `cd ../../..` → Proje root dizinine dön (3 seviye yukarı: Basket.API → Basket → Services → src → root)
- `dotnet sln add` → Solution'a proje ekle
- `src/Services/Basket/Basket.API/Basket.API.csproj` → Eklenecek proje dosyasının yolu

**Ne işe yarar:**
- Projeyi solution'a ekler
- `dotnet sln list` ile görülebilir
- Diğer projeler bu projeyi referans edebilir
- IDE'lerde (VS Code, Visual Studio) solution içinde görünür

**Kontrol:**
```bash
dotnet sln list
```

**Beklenen çıktı:**
```
Project(s)
----------
...
src/Services/Basket/Basket.API/Basket.API.csproj
```

**Sonuç:** ✅ Proje solution'a eklendi

---

## Adım 4: NuGet Paketlerini Ekle

**Komutlar:**
```bash
cd src/Services/Basket/Basket.API
dotnet add package MediatR
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
dotnet add package AutoMapper
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add package StackExchange.Redis
dotnet add package Grpc.Net.Client
dotnet add package Google.Protobuf
dotnet add package Grpc.Tools
dotnet add package MassTransit
dotnet add package MassTransit.RabbitMQ
dotnet add package AspNetCore.HealthChecks.Redis
```

**Açıklamalar:**
- `MediatR` → CQRS pattern için
- `FluentValidation` → Request validation için
- `AutoMapper` → Object mapping için
- `StackExchange.Redis` → Redis client
- `Grpc.Net.Client` → gRPC client (Discount Service'e bağlanmak için)
- `Google.Protobuf` → Protocol Buffers
- `Grpc.Tools` → Proto dosyasından C# kodları generate etmek için
- `MassTransit` → RabbitMQ abstraction
- `MassTransit.RabbitMQ` → RabbitMQ provider
- `AspNetCore.HealthChecks.Redis` → Redis health check

**Not:** `Grpc.Tools` paketi `PrivateAssets="All"` olarak eklenmelidir (sadece build time'da kullanılır).

**Ne işe yarar:**
- CQRS pattern için gerekli paketler
- Redis işlemleri için gerekli paketler
- gRPC client için gerekli paketler
- RabbitMQ event publish için gerekli paketler
- Health check için gerekli paketler

**Central Package Management:**
- Paketler `Directory.Packages.props`'tan versiyon alır
- `.csproj` dosyasında versiyon belirtilmez
- Tüm projeler aynı versiyonu kullanır

**Yapılan İşlemler:**
1. `Directory.Packages.props` dosyasına eksik paket versiyonları eklendi:
   - `StackExchange.Redis` (2.8.16)
   - `Grpc.Net.Client` (2.64.0)
   - `Google.Protobuf` (3.27.3)
   - `Grpc.Tools` (2.64.0) - PrivateAssets="All" ile
   - `AspNetCore.HealthChecks.Redis` (9.0.0)

2. `Basket.API.csproj` dosyasına paket referansları eklendi (versiyonlar olmadan):
   ```xml
   <ItemGroup>
     <PackageReference Include="MediatR" />
     <PackageReference Include="FluentValidation" />
     <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
     <PackageReference Include="AutoMapper" />
     <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" />
     <PackageReference Include="StackExchange.Redis" />
     <PackageReference Include="Grpc.Net.Client" />
     <PackageReference Include="Google.Protobuf" />
     <PackageReference Include="Grpc.Tools">
       <PrivateAssets>All</PrivateAssets>
     </PackageReference>
     <PackageReference Include="MassTransit" />
     <PackageReference Include="MassTransit.RabbitMQ" />
     <PackageReference Include="AspNetCore.HealthChecks.Redis" />
     <PackageReference Include="Swashbuckle.AspNetCore" />
   </ItemGroup>
   ```

**Sorun:**
- Template'den gelen `Program.cs` dosyasında `AddOpenApi()` ve `MapOpenApi()` metodları kullanılıyordu
- Ancak projede `Swashbuckle.AspNetCore` paketi kullanılıyor
- Hata: `error CS1061: 'IServiceCollection' does not contain a definition for 'AddOpenApi'`

**Çözüm:**
- `Program.cs` dosyası Catalog.API'deki gibi Swashbuckle kullanacak şekilde güncellendi
- `AddOpenApi()` → `AddSwaggerGen()` olarak değiştirildi
- `MapOpenApi()` → `UseSwagger()` ve `UseSwaggerUI()` olarak değiştirildi
- Detaylar için: `docs/architecture/eSho-AspController-Arc/documentation/troubleshooting/webapi-template-openapi-swashbuckle-conflict.md`

**Sonuç:**
- Tüm paketler eklendi
- `Basket.API.csproj` dosyası güncellendi
- `Directory.Packages.props` dosyası güncellendi (yeni paketler eklendi)
- `Program.cs` düzeltildi

**Kontrol:**
```bash
dotnet restore src/Services/Basket/Basket.API/Basket.API.csproj
dotnet build src/Services/Basket/Basket.API/Basket.API.csproj
# Build başarılı olmalı (0 hata, 0 uyarı)
```

---

## Adım 5: Project References Ekle

**Komutlar:**
```bash
cd src/Services/Basket/Basket.API
dotnet add reference ../../../BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj
dotnet add reference ../../../BuildingBlocks/BuildingBlocks.Behaviors/BuildingBlocks.Behaviors.csproj
dotnet add reference ../../../BuildingBlocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj
```

**Açıklamalar:**
- `BuildingBlocks.Exceptions` → Exception handling için
- `BuildingBlocks.Behaviors` → MediatR pipeline behaviors (Validation, Logging)
- `BuildingBlocks.Messaging` → BasketCheckoutEvent için

**Ne işe yarar:**
- BuildingBlocks projelerindeki class'ları kullanabiliriz
- Exception handling, validation, logging gibi ortak işlevler
- BasketCheckoutEvent gibi messaging event'leri

**Sonuç:**
- `Basket.API.csproj` dosyasına project references eklendi:
  ```xml
  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\BuildingBlocks.Exceptions\BuildingBlocks.Exceptions.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\BuildingBlocks.Behaviors\BuildingBlocks.Behaviors.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\BuildingBlocks.Messaging\BuildingBlocks.Messaging.csproj" />
  </ItemGroup>
  ```

**Kontrol:**
```bash
cat Basket.API.csproj
# ProjectReference'ları görmeli
```

---

## Adım 6: Klasör Yapısını Oluştur

**Komut:**
```bash
cd src/Services/Basket/Basket.API
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

**Açıklamalar:**
- `mkdir -p` → Klasör oluştur (üst klasörler yoksa onları da oluşturur)
- Her klasör belirli bir sorumluluğa sahip

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
     - **`StoreBasket/`**: StoreBasketCommand, StoreBasketHandler, StoreBasketValidator
     - **`DeleteBasket/`**: DeleteBasketCommand, DeleteBasketHandler
     - **`CheckoutBasket/`**: CheckoutBasketCommand, CheckoutBasketHandler, CheckoutBasketValidator
   - **`Queries/`**: Okuma işlemleri (GetBasket)
     - **`GetBasket/`**: GetBasketQuery, GetBasketHandler
   - **Neden?**: CQRS pattern, her feature kendi klasöründe
   - **Validator'lar:** ValidationBehavior tarafından otomatik uygulanır (AddValidatorsFromAssembly ile kayıtlı)

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

**Sonuç:**
- Tüm klasörler oluşturuldu
- Klasör yapısı hazır

**Kontrol:**
```bash
tree -L 3 -d || find . -type d -maxdepth 3 | sort
# Tüm klasörleri görmeli
```

---

## Adım 7: ShoppingCart ve ShoppingCartItem Entity'lerini Oluştur

**Dosyalar:**
- `Entities/ShoppingCart.cs`
- `Entities/ShoppingCartItem.cs`

**ShoppingCart.cs:**
```csharp
namespace Basket.API.Entities;

public class ShoppingCart
{
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

**ShoppingCartItem.cs:**
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

**Önemli Not:**
- `ShoppingCart` navigation property **YOK** (best practice - döngüsel referans sorunu olmaz)
- EF Core relationship `BasketDbContext.OnModelCreating` içinde `.WithOne()` ile tanımlanır (navigation property olmadan)

**Açıklamalar:**
- `ShoppingCart` → Sepet (UserName key, Items listesi, TotalPrice hesaplanan property)
- `ShoppingCartItem` → Sepet item'ı (ProductId, ProductName, Quantity, Price)
- `TotalPrice` → Calculated property (Items'ların toplamı)

**Neden calculated property?**
- Redis'te sadece Items tutulur
- TotalPrice runtime'da hesaplanır (indirim dahil)
- Her seferinde hesaplanır, veritabanında tutulmaz

**Ne işe yarar:**
- Sepet verilerini temsil eden entity class'ları
- Redis'e serialize/deserialize için kullanılır
- JSON formatında Redis'te saklanır

**Sonuç:**
- `Entities/ShoppingCart.cs` oluşturuldu
- `Entities/ShoppingCartItem.cs` oluşturuldu

**Kontrol:**
```bash
dotnet build src/Services/Basket/Basket.API/Basket.API.csproj
# Build başarılı olmalı (0 hata, 0 uyarı)
```

---

## Adım 8: appsettings.json'a Connection String ve Ayarları Ekle

**Dosya:** `appsettings.json`

**İçerik:**
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
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

**Açıklamalar:**
- `ConnectionStrings:Redis` → Redis bağlantı string'i (localhost:6379)
- `GrpcSettings:DiscountUrl` → Discount gRPC servis URL'i (localhost:5152 - Discount.Grpc port)
- `MessageBroker:Host` → RabbitMQ bağlantı string'i (localhost:5673 - host port)

**Not:** 
- **Localhost'tan bağlanırken:** `localhost:6379`, `localhost:5152`, `localhost:5673`
- **Container network içinde:** `basketdb:6379`, `http://discount.grpc:8080`, `amqp://guest:guest@messagebroker:5672`

**Ne işe yarar:**
- Redis bağlantısı için gerekli
- gRPC client için Discount Service URL'i
- RabbitMQ event publish için gerekli

**Sonuç:**
- `appsettings.json` dosyası güncellendi
- Tüm connection string'ler eklendi

**Kontrol:**
```bash
cat appsettings.json
# ConnectionStrings, GrpcSettings, MessageBroker bölümlerini görmeli
```

---

## Faz 5.1 Özet

### Tamamlanan Adımlar:
1. ✅ Basket klasör yapısı oluşturuldu
2. ✅ Web API projesi oluşturuldu (`Basket.API`)
3. ✅ Proje solution'a eklendi
4. ✅ NuGet paketleri eklendi (Directory.Packages.props + .csproj)
5. ✅ Project References eklendi (BuildingBlocks.Exceptions, Behaviors, Messaging)
6. ✅ Klasör yapısı oluşturuldu (Entities, Data, Features, GrpcServices, Protos, Dtos, Mapping)
7. ✅ Entity'ler oluşturuldu (`ShoppingCart.cs`, `ShoppingCartItem.cs`)
8. ✅ appsettings.json güncellendi (Redis, gRPC, RabbitMQ connection strings)

### Kontrol Sonuçları:
- ✅ Build başarılı (0 hata, 0 uyarı)
- ✅ Tüm klasörler oluşturuldu
- ✅ Entity'ler oluşturuldu ve derlendi
- ✅ appsettings.json güncellendi

---

## Faz 5.2: Basket Redis + PostgreSQL Repository

**Hedef:** Redis + PostgreSQL ile sepet işlemleri (Cache-aside pattern)

### Redis + PostgreSQL (Cache-aside Pattern)

**Redis** (Remote Dictionary Server), in-memory (bellekte) çalışan bir key-value veritabanıdır.

**PostgreSQL**, ilişkisel veritabanıdır (source of truth).

**Cache-aside Pattern:**
- **GetBasket:** Önce Redis'e bak (cache), yoksa PostgreSQL'den al ve cache'le
- **SaveBasket:** PostgreSQL'e yaz (source of truth), Redis'e yaz (cache)
- **DeleteBasket:** PostgreSQL'den sil, Redis'ten sil

**Neden Redis + PostgreSQL?**
- **Redis (Cache):** Hızlı okuma/yazma için (kullanıcı deneyimi)
- **PostgreSQL (Source of Truth):** Veri kalıcılığı için (veri kaybı riski düşük)
- **Redis down olsa bile:** PostgreSQL'den okur (yavaş ama çalışır)
- **Sepet geçmişi:** Tutulabilir (analiz için)
- **Gerçek dünyada:** Production sistemlerde genellikle bu yaklaşım kullanılır

---

## Adım 1: IBasketRepository Interface Oluştur

**Dosya:** `Data/IBasketRepository.cs`

**Kod:**
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

**Açıklamalar:**

### Interface Nedir?

**Interface** = Sözleşme (Contract)
- Hangi metodların olması gerektiğini belirtir
- Metodların imzalarını (parametreler, dönüş tipleri) tanımlar
- Nasıl implement edileceğini belirtmez (sadece ne yapılacağını)

**Örnek:**
```csharp
// Interface: "Sepet getir" diyor, ama nasıl getirileceğini söylemiyor
public interface IBasketRepository
{
    Task<ShoppingCart?> GetBasket(string userName);  // ← Sadece imza
}

// Implementation: "Redis'ten getir" diyor
public class BasketRepository : IBasketRepository
{
    public async Task<ShoppingCart?> GetBasket(string userName)
    {
        // Redis işlemleri burada
    }
}
```

### Metodların Açıklamaları

#### 1. `GetBasket(string userName)`
- **Ne yapar:** Redis'ten kullanıcının sepetini getirir
- **Dönüş tipi:** `Task<ShoppingCart?>` → Nullable (sepet yoksa null)
- **Neden nullable?** Sepet yoksa null döner, hata fırlatmaz
- **Kullanım:** Sepeti görüntüleme işlemlerinde

#### 2. `SaveBasket(ShoppingCart basket)`
- **Ne yapar:** Sepeti Redis'e kaydeder (yoksa oluşturur, varsa günceller)
- **Dönüş tipi:** `Task<ShoppingCart>` → Kaydedilen sepet
- **Neden ShoppingCart döner?** Kaydedilen sepeti döndürür (doğrulama için)
- **Kullanım:** Sepete ürün ekleme/güncelleme işlemlerinde

#### 3. `DeleteBasket(string userName)`
- **Ne yapar:** Redis'ten kullanıcının sepetini siler
- **Dönüş tipi:** `Task<bool>` → Başarılı/başarısız
- **Neden bool?** Silme işleminin başarılı olup olmadığını belirtir
- **Kullanım:** Sepeti temizleme, checkout sonrası silme işlemlerinde

### Neden Interface Kullanıyoruz?

#### 1. Repository Pattern
- Veri erişimini soyutlar
- İş mantığı, veri kaynağından bağımsız olur
- İleride Redis yerine başka bir depolama kullanılabilir

#### 2. Dependency Inversion Principle (SOLID)
- Handler'lar `IBasketRepository`'ye bağlıdır, `BasketRepository`'ye değil
- Kod üst seviyede kalır, alt seviye detaylara bağımlı olmaz

#### 3. Test Edilebilirlik
- Mock repository ile unit test yazılabilir
- Gerçek Redis'e ihtiyaç duyulmaz

#### 4. Esneklik
- Farklı implementasyonlar eklenebilir (ör. InMemory, MongoDB)
- Kod değişmeden implementasyon değiştirilebilir

**Sonuç:**
- `Data/IBasketRepository.cs` oluşturuldu
- 3 metod tanımlandı: GetBasket, SaveBasket, DeleteBasket

**Kontrol:**
```bash
dotnet build src/Services/Basket/Basket.API/Basket.API.csproj
# Build başarılı olmalı (0 hata, 0 uyarı)
```

---

## Adım 2: BasketRepository Implementation Oluştur (Redis + PostgreSQL)

**Dosya:** `Data/BasketRepository.cs`

**Kod:**
```csharp
using System.Text.Json;
using AutoMapper;
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

    // ... metodlar (Cache-aside pattern)
}
```

**Önemli Değişiklikler:**
- `BasketDbContext` dependency eklendi (PostgreSQL için)
- `IMapper` dependency eklendi (AutoMapper - DRY prensibi için)
- `_redis` ve `_context` field'ları var
- Cache-aside pattern implementasyonu
- **Helper methods**: `GetRedisKey()` ve `ReloadBasketWithItems()` (DRY prensibi)
- **Constants**: `RedisKeyPrefix` ve `CacheExpiration` (DRY prensibi)

### IConnectionMultiplexer Nedir?

**IConnectionMultiplexer** = Redis bağlantı yöneticisi

**Ne işe yarar?**
- Redis sunucusuna bağlantı yönetir
- Birden fazla Redis database'ine erişim sağlar
- Connection pooling yapar (bağlantı havuzu)
- Thread-safe (birden fazla thread aynı anda kullanabilir)

**Nasıl çalışır?**
```
IConnectionMultiplexer
    ↓
Redis Server'a bağlantı (localhost:6379)
    ↓
GetDatabase() → IDatabase instance'ı alır
```

**Özellikler:**
- **Singleton:** Tüm uygulama boyunca tek instance
- **Thread-safe:** Birden fazla thread aynı anda kullanabilir
- **Connection pooling:** Bağlantıları yeniden kullanır
- **Automatic reconnection:** Bağlantı koparsa otomatik yeniden bağlanır

### IDatabase Nedir?

**IDatabase** = Redis database instance'ı

**Ne işe yarar?**
- Redis'teki belirli bir database'e (0-15 arası) erişim sağlar
- Redis komutlarını çalıştırır (GET, SET, DELETE, vb.)
- String, List, Set, Hash gibi veri tipleriyle çalışır

**Nasıl çalışır?**
```csharp
IConnectionMultiplexer redis = ...;
IDatabase database = redis.GetDatabase();  // Default: database 0
```

**Metodlar:**
- `StringGetAsync(key)` → Redis'ten string değer alır
- `StringSetAsync(key, value)` → Redis'e string değer kaydeder
- `KeyDeleteAsync(key)` → Redis'ten key siler

**Neden GetDatabase()?**
- Redis'te 16 database var (0-15)
- Her database ayrı namespace (key'ler birbirinden bağımsız)
- Default: database 0
- `GetDatabase(1)` → database 1'e erişir

### Constructor Açıklaması

```csharp
public BasketRepository(IConnectionMultiplexer redis, ILogger<BasketRepository> logger)
{
    _database = redis.GetDatabase();
    _logger = logger;
}
```

**Ne yapıyor?**
1. `IConnectionMultiplexer redis` → DI'dan gelir (Program.cs'de kayıtlı)
2. `redis.GetDatabase()` → Redis database instance'ı alır (default: database 0)
3. `_database` field'ına atar → Tüm metodlarda kullanılır
4. `ILogger` → Loglama için

**Neden field olarak tutuyoruz?**
- Her metodda `redis.GetDatabase()` çağırmak yerine
- Bir kez alıp field'da tutuyoruz (daha verimli)

### GetBasket Metodu (Cache-aside Pattern)

```csharp
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
```

**Adım adım ne yapıyor? (Cache-aside Pattern)**

1. **Önce Redis'e bak (cache)**
   - `StringGetAsync($"basket:{userName}")` → Redis'ten key'i getirir
   - Cache'te varsa → Direkt döner (hızlı)

2. **Redis'te yoksa PostgreSQL'den al**
   - `_context.ShoppingCarts.Include(x => x.Items).FirstOrDefaultAsync(...)` → PostgreSQL'den alır
   - `Include(x => x.Items)` → Navigation property'leri yükler

3. **PostgreSQL'den aldıktan sonra Redis'e yaz (cache)**
   - `StringSetAsync(..., TimeSpan.FromHours(24))` → Redis'e cache'ler (24 saat TTL)
   - Sonraki isteklerde Redis'ten okunur (hızlı)

4. **Redis down olursa**
   - `RedisConnectionException` yakalanır
   - Sadece PostgreSQL'den okur (yavaş ama çalışır)

**Örnek Akış:**
```
GetBasket("user1")
    ↓
1. Redis'e bak → Cache'te VAR → Direkt döner (hızlı) ✅

GetBasket("user2")
    ↓
1. Redis'e bak → Cache'te YOK
    ↓
2. PostgreSQL'den al → Sepet bulundu
    ↓
3. Redis'e cache'le (24 saat TTL)
    ↓
4. Return: ShoppingCart { UserName = "user2", Items = [...] }
```

### SaveBasket Metodu (Cache-aside Pattern)

**Not:** Bu metod EF Core 7+ best practice'lerini kullanır: `ExecuteDeleteAsync()` ve `AddRange()`, ayrıca AutoMapper ile DRY prensibine uygun.

```csharp
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
        // Redis down olursa sadece log'la, PostgreSQL'e yazıldı zaten
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
```

**Önemli İyileştirmeler:**
- **ExecuteDeleteAsync()**: EF Core 7+ özelliği, direkt SQL DELETE çalıştırır (tracking sorunu yok, daha performanslı)
- **AddRange()**: Toplu ekleme, daha performanslı
- **AutoMapper**: Manuel mapping yerine AutoMapper kullanımı (DRY prensibi, maintenance kolay)
- **Helper Methods**: `GetRedisKey()` ve `ReloadBasketWithItems()` - kod tekrarını önler (DRY)
- **Constants**: `RedisKeyPrefix` ve `CacheExpiration` - magic string/values yerine constant kullanımı (DRY)
- **ReloadBasketWithItems()**: `ExecuteDeleteAsync()` sonrası `existing.Items` boş olabileceği için yeniden yükleme

**Adım adım ne yapıyor? (Cache-aside Pattern)**

1. **PostgreSQL'e yaz (source of truth)**
   - Mevcut sepet var mı kontrol et
   - Yoksa → Yeni sepet ekle (`Add`)
   - Varsa → Mevcut item'ları sil, yeni item'ları ekle (`Update`)
   - `SaveChangesAsync()` → PostgreSQL'e kaydet

2. **Redis'e yaz (cache)**
   - `StringSetAsync(..., TimeSpan.FromHours(24))` → Redis'e cache'le (24 saat TTL)
   - Redis down olursa → Sadece log'la, PostgreSQL'e yazıldı zaten

**Örnek Akış:**
```
SaveBasket(basket)
    ↓
1. PostgreSQL'e yaz → Kaydedildi ✅
    ↓
2. Redis'e cache'le → Cache'lendi ✅
    ↓
Return: Saved ShoppingCart
```

**Neden önce PostgreSQL?**
- PostgreSQL = Source of truth (gerçek veri kaynağı)
- Redis = Cache (hız için)
- PostgreSQL'e yazmak kritik, Redis'e yazmak opsiyonel

### DeleteBasket Metodu (Cache-aside Pattern)

```csharp
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
```

**Adım adım ne yapıyor? (Cache-aside Pattern)**

1. **PostgreSQL'den sil**
   - `FirstOrDefaultAsync(...)` → Sepeti bul
   - Varsa → `Remove()` ve `SaveChangesAsync()` → PostgreSQL'den sil

2. **Redis'ten sil**
   - `KeyDeleteAsync(...)` → Redis'ten key'i sil
   - Redis down olursa → Sadece log'la, PostgreSQL'den silindi zaten

3. **Return**
   - PostgreSQL'den silindi mi kontrol et
   - Redis'ten silindi mi kontrol et
   - Herhangi biri başarılıysa → `true` döner

**Örnek Akış:**
```
DeleteBasket("user1")
    ↓
1. PostgreSQL'den sil → Silindi ✅
    ↓
2. Redis'ten sil → Silindi ✅
    ↓
Return: true
```

### Önemli Noktalar

#### 1. Key Naming Convention
- **Format:** `basket:{userName}`
- **Örnek:** `basket:user1`, `basket:john_doe`
- **Neden?** Her kullanıcı için ayrı key, çakışma olmaz

#### 2. JSON Serialize/Deserialize
- Redis string olarak tutar
- C# objelerini JSON'a çeviririz
- `System.Text.Json` kullanıyoruz (.NET'in built-in kütüphanesi)

#### 3. Null Safety
- `GetBasket` nullable döner (`ShoppingCart?`)
- Sepet yoksa null, hata fırlatmaz
- Caller null kontrolü yapmalı

#### 4. Async/Await
- Tüm metodlar async
- Redis işlemleri I/O bound (network)
- Async kullanmak performansı artırır

**Sonuç:**
- `Data/BasketRepository.cs` oluşturuldu
- Tüm metodlar implement edildi
- Redis entegrasyonu tamamlandı

**Kontrol:**
```bash
dotnet build src/Services/Basket/Basket.API/Basket.API.csproj
# Build başarılı olmalı (0 hata, 0 uyarı)
```

---

## Adım 3: Program.cs'de PostgreSQL, Redis ve Repository Kaydı

**Dosya:** `Program.cs`

**Kod:**
```csharp
using Basket.API.Data;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

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

// ...

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

### Redis Connection Kaydı

#### AddSingleton Nedir?

**Singleton** = Uygulama boyunca tek instance

**Ne demek?**
- İlk istekte oluşturulur
- Tüm request'ler aynı instance'ı kullanır
- Uygulama kapanana kadar yaşar

**Örnek:**
```csharp
// İlk request
IConnectionMultiplexer instance1 = ...;  // Oluşturuldu

// İkinci request
IConnectionMultiplexer instance2 = ...;  // Aynı instance (instance1 == instance2)

// Üçüncü request
IConnectionMultiplexer instance3 = ...;  // Aynı instance (instance1 == instance3)
```

#### Neden Singleton?

**1. Redis Connection Pahalı**
- Network connection açmak pahalı (TCP socket)
- Her request'te yeni connection açmak yavaş
- Connection pooling yapmak gerekiyor

**2. Thread-Safe**
- `IConnectionMultiplexer` thread-safe
- Birden fazla thread aynı anda kullanabilir
- Güvenli paylaşım

**3. Connection Pooling**
- `IConnectionMultiplexer` içinde connection pool var
- Birden fazla işlem aynı connection'ı kullanabilir
- Performans artışı

**4. Automatic Reconnection**
- Bağlantı koparsa otomatik yeniden bağlanır
- Uygulama restart olmadan devam eder

**Alternatif (Yanlış):**
```csharp
// ❌ YANLIŞ: Her request'te yeni connection
builder.Services.AddScoped<IConnectionMultiplexer>(sp =>
{
    return ConnectionMultiplexer.Connect(...);  // Her request'te yeni connection!
});
```

**Neden yanlış?**
- Her request'te yeni TCP connection açılır
- Çok yavaş (network latency)
- Connection limit'ine takılabilir
- Gereksiz kaynak kullanımı

#### ConnectionMultiplexer.Connect Nedir?

**ConnectionMultiplexer.Connect** = Redis'e bağlanır

**Ne yapıyor?**
- Redis sunucusuna TCP bağlantı açar
- Connection string'i parse eder
- Connection pool oluşturur
- Bağlantıyı yönetir

**Connection String Format:**
```
localhost:6379
```

**Örnek:**
```csharp
var connection = ConnectionMultiplexer.Connect("localhost:6379");
// Redis'e bağlandı
// Connection pool oluşturuldu
// Hazır kullanıma
```

### Repository Kaydı

#### AddScoped Nedir?

**Scoped** = Her HTTP request için yeni instance

**Ne demek?**
- Her request'te yeni instance oluşturulur
- Request bitince dispose edilir
- Aynı request içinde aynı instance kullanılır

**Örnek:**
```csharp
// Request 1
IBasketRepository repo1 = ...;  // Yeni instance

// Request 2
IBasketRepository repo2 = ...;  // Yeni instance (repo1 != repo2)

// Request 3
IBasketRepository repo3 = ...;  // Yeni instance (repo1 != repo3)
```

#### Neden Scoped?

**1. Request Isolation**
- Her request kendi repository instance'ını kullanır
- Request'ler birbirini etkilemez
- Thread-safe değil ama request bazında izole

**2. Dispose Pattern**
- Request bitince repository dispose edilir
- Kaynaklar temizlenir
- Memory leak önlenir

**3. State Management**
- Repository'de state varsa (field'lar)
- Her request için temiz state
- Önceki request'in state'i görünmez

**Alternatif (Yanlış):**
```csharp
// ❌ YANLIŞ: Tüm request'ler aynı instance
builder.Services.AddSingleton<IBasketRepository, BasketRepository>();
```

**Neden yanlış?**
- Tüm request'ler aynı repository instance'ını kullanır
- State karışabilir (eğer field'lar varsa)
- Thread-safe olmayabilir

**Not:** `BasketRepository`'de state yok (sadece `_database` ve `_logger`), ama best practice olarak Scoped kullanıyoruz.

### DI Container Akışı

```
HTTP Request Geldi
    ↓
DI Container: "IBasketRepository lazım"
    ↓
Scoped: Yeni BasketRepository instance oluştur
    ↓
Constructor: IConnectionMultiplexer lazım
    ↓
Singleton: Mevcut IConnectionMultiplexer instance'ı kullan
    ↓
BasketRepository oluşturuldu
    ↓
Handler'a inject edilir
    ↓
Request bitti → BasketRepository dispose edilir
    ↓
IConnectionMultiplexer hala yaşıyor (Singleton)
```

**Sonuç:**
- Redis connection Singleton olarak kaydedildi
- Repository Scoped olarak kaydedildi
- DI container yapılandırması tamamlandı

**Kontrol:**
```bash
dotnet build src/Services/Basket/Basket.API/Basket.API.csproj
# Build başarılı olmalı (0 hata, 0 uyarı)
```

---

## Faz 5.3: Basket gRPC Client (Discount)

**Hedef:** Discount servisine gRPC ile bağlan

### gRPC Nedir?

**gRPC** (gRPC Remote Procedure Call) = Google'ın geliştirdiği yüksek performanslı RPC framework'ü.

**Özellikler:**
- HTTP/2 protokolü kullanır
- Protocol Buffers (binary format) kullanır
- Çok hızlı (JSON'dan daha hızlı)
- Streaming desteği
- Strong typing (güçlü tip kontrolü)

**Neden gRPC?**
- Basket Service sürekli indirim sorgusu yapıyor (her sepet işleminde)
- gRPC çok hızlı (binary format, HTTP/2)
- Internal servis iletişimi için ideal
- Yüksek performans gerekiyor

**REST vs gRPC:**
| Özellik | REST | gRPC |
|---------|------|------|
| Format | JSON (text) | Protocol Buffers (binary) |
| Hız | Yavaş | Çok hızlı |
| Payload | Büyük | Küçük |
| Protokol | HTTP/1.1 | HTTP/2 |
| Kullanım | Dışarıya açık API'ler | Internal servis iletişimi |

---

## Adım 1: discount.proto Dosyasını Kopyala

**Komut:**
```bash
cp src/Services/Discount/Discount.Grpc/Protos/discount.proto src/Services/Basket/Basket.API/Protos/discount.proto
```

**Açıklamalar:**
- Proto dosyası aynı olmalı (server ve client aynı contract'ı kullanır)
- `Protos/` klasörüne kopyalanır
- Server (Discount.Grpc) ve Client (Basket.API) aynı proto dosyasını kullanır

**Ne işe yarar?**
- gRPC client için proto dosyası gerekli
- Proto dosyasından C# client kodları generate edilir
- Server ve client aynı contract'ı kullanır (uyumluluk)

**Proto Dosyası İçeriği:**
```protobuf
syntax = "proto3";

option csharp_namespace = "Discount.Grpc.Protos";

service DiscountProtoService {
  rpc GetDiscount (GetDiscountRequest) returns (CouponModel);
  // ...
}

message GetDiscountRequest {
  string productName = 1;
}

message CouponModel {
  int32 id = 1;
  string productName = 2;
  string description = 3;
  int32 amount = 4;
}
```

**Sonuç:**
- `Protos/discount.proto` kopyalandı
- Server ve client aynı contract'ı kullanıyor

---

## Adım 2: .csproj Dosyasına Proto Reference Ekle

**Dosya:** `Basket.API.csproj`

**Kod:**
```xml
<ItemGroup>
  <Protobuf Include="Protos\discount.proto" GrpcServices="Client" />
</ItemGroup>
```

**Açıklamalar:**

### Protobuf Item Nedir?

**Protobuf Item** = Proto dosyasını build sürecine dahil eder

**Ne yapıyor?**
- Build sırasında proto dosyasını C# kodlarına dönüştürür
- `GrpcServices="Client"` → Client kodları generate edilir
- `obj/Debug/net9.0/Protos/` klasöründe C# dosyaları oluşur

### GrpcServices="Client" Nedir?

**GrpcServices="Client"** = Client kodları generate et

**Ne demek?**
- Proto dosyasından client kodları oluşturulur
- `DiscountProtoService.DiscountProtoServiceClient` class'ı oluşur
- Server kodları oluşturulmaz (sadece client)

**Alternatif:**
- `GrpcServices="Server"` → Server kodları (Discount.Grpc'ta kullanılıyor)
- `GrpcServices="Both"` → Hem client hem server kodları

### Generate Edilen Kodlar

**Proto dosyasından şunlar generate edilir:**
- `DiscountProtoService.DiscountProtoServiceClient` → gRPC client class
- `GetDiscountRequest` → Request message class
- `CouponModel` → Response message class
- `GetDiscountAsync` → Async metod

**Nerede?**
- `obj/Debug/net9.0/Protos/Discount.cs` → Generate edilen C# dosyası
- Build sırasında otomatik oluşturulur
- Source control'a eklenmez (`.gitignore`'da)

**Sonuç:**
- `.csproj` dosyasına proto reference eklendi
- Build sırasında client kodları generate edilecek

**Kontrol:**
```bash
dotnet build src/Services/Basket/Basket.API/Basket.API.csproj
# Build başarılı olmalı
# obj/Debug/net9.0/Protos/Discount.cs dosyası oluşmalı
```

---

## Adım 3: DiscountGrpcService Oluştur

**Dosya:** `GrpcServices/DiscountGrpcService.cs`

**Kod:**
```csharp
using Discount.Grpc.Protos;
using Grpc.Core;
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

### DiscountProtoService.DiscountProtoServiceClient Nedir?

**DiscountProtoService.DiscountProtoServiceClient** = Proto dosyasından generate edilen gRPC client class

**Ne işe yarar?**
- Discount Service'e gRPC çağrıları yapar
- Proto dosyasındaki service metodlarını expose eder
- `GetDiscountAsync`, `CreateDiscountAsync` gibi metodlar içerir

**Nasıl çalışır?**
```
DiscountProtoService.DiscountProtoServiceClient
    ↓
GetDiscountAsync(request)
    ↓
gRPC çağrısı (HTTP/2, Protocol Buffers)
    ↓
Discount.Grpc Server
    ↓
Response (CouponModel)
```

### GetDiscount Metodu

**Adım adım ne yapıyor?**

1. **Request oluştur**
   ```csharp
   var request = new GetDiscountRequest { ProductName = productName };
   ```
   - Proto dosyasından generate edilen request class
   - `ProductName` property'si set edilir

2. **gRPC çağrısı**
   ```csharp
   var response = await _client.GetDiscountAsync(request);
   ```
   - `GetDiscountAsync` → Async gRPC metod çağrısı
   - Discount Service'e istek gönderilir
   - Response beklenir

3. **Response döndür**
   ```csharp
   return response;  // CouponModel
   ```
   - `CouponModel` → Proto dosyasından generate edilen response class
   - `Id`, `ProductName`, `Description`, `Amount` property'leri içerir

### Error Handling

#### RpcException Nedir?

**RpcException** = gRPC hataları için exception

**Ne işe yarar?**
- gRPC çağrısı başarısız olursa fırlatılır
- `StatusCode` property'si ile hata tipi belirlenir
- `NotFound`, `Internal`, `Unavailable` gibi status code'lar

#### StatusCode.NotFound Nedir?

**StatusCode.NotFound** = Kayıt bulunamadı

**Ne demek?**
- Discount Service'te ürün için indirim yok
- Normal durum (hata değil)
- `Amount = 0` döndürürüz (hata fırlatmıyoruz)

**Neden hata fırlatmıyoruz?**
- İndirim yoksa normal bir durum
- Sepet işlemleri devam edebilir (indirim olmadan)
- Kullanıcı deneyimini bozmaz

**Kod:**
```csharp
catch (RpcException ex)
{
    if (ex.StatusCode == StatusCode.NotFound)
        return new CouponModel { Amount = 0 };  // İndirim yok
    
    throw;  // Diğer hatalar için fırlat
}
```

### Neden Wrapper Class?

**1. Abstraction (Soyutlama)**
- gRPC client'ı soyutlar
- Handler'lar `DiscountGrpcService`'i kullanır, `DiscountProtoServiceClient`'ı değil

**2. Error Handling (Hata Yönetimi)**
- Merkezi error handling
- NotFound durumunda `Amount = 0` döndürür
- Diğer hatalar için throw eder

**3. Test Edilebilirlik**
- Mock'lanabilir
- Unit test yazılabilir
- Gerçek gRPC servisine ihtiyaç duyulmaz

**4. Logging (Loglama)**
- Her işlem loglanır
- Debugging kolaylaşır

**Sonuç:**
- `GrpcServices/DiscountGrpcService.cs` oluşturuldu
- GetDiscount metodu implement edildi
- Error handling eklendi

**Kontrol:**
```bash
dotnet build src/Services/Basket/Basket.API/Basket.API.csproj
# Build başarılı olmalı (0 hata, 0 uyarı)
```

---

## Adım 4: Program.cs'de gRPC Client Konfigürasyonu

**Dosya:** `Program.cs`

**Kod:**
```csharp
using Basket.API.GrpcServices;
using Discount.Grpc.Protos;
using Grpc.Net.Client;

// gRPC Client
builder.Services.AddSingleton<DiscountProtoService.DiscountProtoServiceClient>(sp =>
{
    var address = builder.Configuration["GrpcSettings:DiscountUrl"]!;
    var channel = GrpcChannel.ForAddress(address);
    return new DiscountProtoService.DiscountProtoServiceClient(channel);
});

// DiscountGrpcService
builder.Services.AddScoped<DiscountGrpcService>();
```

### GrpcChannel Nedir?

**GrpcChannel** = gRPC bağlantı kanalı

**Ne işe yarar?**
- gRPC servisine bağlantı yönetir
- HTTP/2 protokolü kullanır
- Connection pooling yapar
- Automatic reconnection (otomatik yeniden bağlanma)

**Nasıl çalışır?**
```csharp
var channel = GrpcChannel.ForAddress("http://localhost:5152");
// gRPC channel oluşturuldu
// HTTP/2 bağlantısı hazır
```

### GrpcChannel.ForAddress Nedir?

**GrpcChannel.ForAddress** = URL'den gRPC channel oluşturur

**Ne yapıyor?**
- URL'den gRPC channel oluşturur
- HTTP/2 bağlantısı açar
- Channel'ı yönetir

**Örnek:**
```csharp
var channel = GrpcChannel.ForAddress("http://localhost:5152");
// Discount.Grpc servisine bağlanır
// Channel hazır
```

### Neden Singleton?

**gRPC Client Singleton olarak kaydedilir**

**Neden?**
- gRPC channel pahalı (HTTP/2 connection)
- Tüm request'ler aynı channel'ı kullanabilir
- Thread-safe
- Connection pooling yapar

**Alternatif (Yanlış):**
```csharp
// ❌ YANLIŞ: Her request'te yeni channel
builder.Services.AddScoped<DiscountProtoService.DiscountProtoServiceClient>(sp =>
{
    var channel = GrpcChannel.ForAddress(...);  // Her request'te yeni channel!
    return new DiscountProtoService.DiscountProtoServiceClient(channel);
});
```

**Neden yanlış?**
- Her request'te yeni HTTP/2 connection açılır
- Çok yavaş (network latency)
- Connection limit'ine takılabilir
- Gereksiz kaynak kullanımı

### DiscountGrpcService Scoped

**DiscountGrpcService Scoped olarak kaydedilir**

**Neden?**
- Her request için yeni instance
- Request bitince dispose edilir
- gRPC client Singleton (paylaşılır)
- Wrapper service Scoped (her request için yeni)

**Akış:**
```
Request 1:
  DiscountGrpcService instance1 → gRPC Client (Singleton, paylaşılan)
  
Request 2:
  DiscountGrpcService instance2 → gRPC Client (Singleton, paylaşılan)
  
Request 3:
  DiscountGrpcService instance3 → gRPC Client (Singleton, paylaşılan)
```

### appsettings.json Konfigürasyonu

**Dosya:** `appsettings.json`

**İçerik:**
```json
{
  "GrpcSettings": {
    "DiscountUrl": "http://localhost:5152"
  }
}
```

**Açıklamalar:**
- `GrpcSettings:DiscountUrl` → Discount gRPC servis URL'i
- Localhost: `http://localhost:5152` (Discount.Grpc port)
- Container network: `http://discount.grpc:8080`

**Neden HTTP?**
- gRPC HTTP/2 kullanır
- URL format: `http://` veya `https://`
- Port: Discount.Grpc'ın dinlediği port

**Sonuç:**
- gRPC client Singleton olarak kaydedildi
- DiscountGrpcService Scoped olarak kaydedildi
- appsettings.json'dan URL okunuyor

**Kontrol:**
```bash
dotnet build src/Services/Basket/Basket.API/Basket.API.csproj
# Build başarılı olmalı (0 hata, 0 uyarı)
```

---

## Faz 5.1, 5.2 & 5.3 Özet

### Faz 5.1 Tamamlanan Adımlar:
1. ✅ Basket klasör yapısı oluşturuldu
2. ✅ Web API projesi oluşturuldu (`Basket.API`)
3. ✅ Proje solution'a eklendi
4. ✅ NuGet paketleri eklendi (Directory.Packages.props + .csproj)
5. ✅ Project References eklendi (BuildingBlocks.Exceptions, Behaviors, Messaging)
6. ✅ Klasör yapısı oluşturuldu (Entities, Data, Features, GrpcServices, Protos, Dtos, Mapping)
7. ✅ Entity'ler oluşturuldu (`ShoppingCart.cs`, `ShoppingCartItem.cs`)
8. ✅ appsettings.json güncellendi (Redis, gRPC, RabbitMQ connection strings)

### Faz 5.2 Tamamlanan Adımlar:
1. ✅ IBasketRepository interface oluşturuldu
2. ✅ BasketRepository implementation oluşturuldu (Redis + JSON)
3. ✅ Program.cs'de Redis ve Repository kaydı yapıldı

### Faz 5.3 Tamamlanan Adımlar:
1. ✅ discount.proto dosyası kopyalandı
2. ✅ .csproj dosyasına proto reference eklendi
3. ✅ DiscountGrpcService oluşturuldu
4. ✅ Program.cs'de gRPC client konfigürasyonu yapıldı

### Önemli Kavramlar:

#### Redis:
- **IConnectionMultiplexer:** Redis bağlantı yöneticisi (Singleton)
- **IDatabase:** Redis database instance'ı (GetDatabase() ile alınır)
- **StringGetAsync:** Redis'ten string değer alır
- **StringSetAsync:** Redis'e string değer kaydeder
- **KeyDeleteAsync:** Redis'ten key siler

#### gRPC:
- **GrpcChannel:** gRPC bağlantı kanalı (HTTP/2)
- **DiscountProtoServiceClient:** Proto dosyasından generate edilen client
- **RpcException:** gRPC hataları için exception
- **StatusCode.NotFound:** Kayıt bulunamadı durumu

#### DI Container:
- **Singleton:** Uygulama boyunca tek instance (IConnectionMultiplexer, gRPC Client)
- **Scoped:** Her request için yeni instance (Repository, DiscountGrpcService)

---

## Faz 5.4: Basket CQRS - Commands & Queries

**Hedef:** Sepet işlemleri (CQRS + MediatR)

### CQRS Pattern Nedir?

**CQRS** (Command Query Responsibility Segregation) = Komut ve sorgu sorumluluklarını ayırma

**Ne demek?**
- **Command** → Yazma işlemleri (Create, Update, Delete)
- **Query** → Okuma işlemleri (Read, Get)
- Her işlem için ayrı handler (Single Responsibility)

**Neden CQRS?**
- Kod organizasyonu (her feature kendi klasöründe)
- Test edilebilirlik (her handler ayrı test edilir)
- Esneklik (farklı veri kaynakları kullanılabilir)
- MediatR ile kolay implementasyon

---

## Adım 1: MediatR, FluentValidation, AutoMapper Konfigürasyonu

**Dosya:** `Program.cs`

**Kod:**
```csharp
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

**Açıklamalar:**

### MediatR Konfigürasyonu

#### `RegisterServicesFromAssembly(typeof(Program).Assembly)`
- **Ne yapar:** `Basket.API` assembly'sini tarar
- **Ne bulur:** `IRequestHandler<TRequest, TResponse>` implement eden tüm class'ları
- **Ne kaydeder:** Handler'ları DI container'a kaydeder (Transient)

**Örnek:**
- `GetBasketHandler : IRequestHandler<GetBasketQuery, ShoppingCartDto>` → Bulunur ve kaydedilir
- `StoreBasketHandler : IRequestHandler<StoreBasketCommand, ShoppingCartDto>` → Bulunur ve kaydedilir

#### `AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))`
- **Ne yapar:** Pipeline'a `LoggingBehavior` ekler
- **Ne işe yarar:** Her request öncesi ve sonrası loglama yapar
- **Akış:** Request → LoggingBehavior → Handler → LoggingBehavior → Response

#### `AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>))`
- **Ne yapar:** Pipeline'a `ValidationBehavior` ekler
- **Ne işe yarar:** Her request öncesi FluentValidation çalıştırır
- **Akış:** Request → ValidationBehavior → Handler → Response
- **Hata durumu:** Validation hatası varsa → `ValidationException` fırlatır, Handler'a gitmez

### FluentValidation Konfigürasyonu

#### `AddValidatorsFromAssembly(typeof(Program).Assembly)`
- **Ne yapar:** `Basket.API` assembly'sini tarar
- **Ne bulur:** `AbstractValidator<T>` implement eden tüm class'ları
- **Ne kaydeder:** Validator'ları DI container'a kaydeder (Scoped)

**Örnek:**
- `StoreBasketValidator : AbstractValidator<StoreBasketCommand>` → Bulunur ve kaydedilir
- `CheckoutBasketValidator : AbstractValidator<CheckoutBasketCommand>` → Bulunur ve kaydedilir

### AutoMapper Konfigürasyonu

#### `AddAutoMapper(typeof(Program).Assembly)`
- **Ne yapar:** `Basket.API` assembly'sini tarar
- **Ne bulur:** `Profile` class'ından inherit eden tüm class'ları
- **Ne kaydeder:** AutoMapper'ı DI container'a kaydeder (Singleton)

**Örnek:**
- `MappingProfile : Profile` → Bulunur ve kaydedilir

### MediatR Pipeline Akışı

```
Controller → MediatR.Send(GetBasketQuery)
    ↓
┌─────────────────────────────────────┐
│  MediatR Pipeline Başlar           │
└─────────────────────────────────────┘
    ↓
┌─────────────────────────────────────┐
│  1. LoggingBehavior                 │
│     - "Handling GetBasketQuery..."  │
│     - Request'i loglar              │
└─────────────────────────────────────┘
    ↓
┌─────────────────────────────────────┐
│  2. ValidationBehavior              │
│     - Request tipine göre validator'ları bul │
│     - Örnek: StoreBasketCommand → StoreBasketValidator │
│     - Örnek: CheckoutBasketCommand → CheckoutBasketValidator │
│     - Tüm validator'ları çalıştır (ValidateAsync) │
│     - Hata varsa: ValidationException fırlat │
│     - Hata yoksa: Handler'a geçer │
└─────────────────────────────────────┘
    ↓
┌─────────────────────────────────────┐
│  3. GetBasketHandler.Handle()       │
│     - Repository'den sepeti al      │
│     - gRPC ile indirim sorgula      │
│     - DTO'ya map et                 │
│     - Return ShoppingCartDto        │
└─────────────────────────────────────┘
    ↓
┌─────────────────────────────────────┐
│  4. LoggingBehavior                │
│     - "Handled GetBasketQuery: {...}"│
│     - Response'u loglar             │
└─────────────────────────────────────┘
    ↓
Controller'a response döner
```

### ValidationBehavior Nasıl Çalışır?

**ValidationBehavior** = Her request için otomatik validation yapar

**Nasıl çalışır?**

1. **DI'dan Validator'ları Alır**
   ```csharp
   private readonly IEnumerable<IValidator<TRequest>> _validators;
   ```
   - DI container'dan request tipine göre tüm validator'ları alır
   - Örnek: `StoreBasketCommand` için → `StoreBasketValidator` bulunur
   - Örnek: `CheckoutBasketCommand` için → `CheckoutBasketValidator` bulunur

2. **Validator Var mı Kontrol Eder**
   ```csharp
   if (_validators.Any())
   ```
   - Request için validator var mı kontrol eder
   - Validator yoksa → Direkt handler'a geçer

3. **Tüm Validator'ları Çalıştırır**
   ```csharp
   var validationResults = await Task.WhenAll(
       _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
   ```
   - Her validator için `ValidateAsync()` çağrılır
   - Paralel çalıştırılır (Task.WhenAll)
   - Tüm sonuçlar toplanır

4. **Hata Kontrolü**
   ```csharp
   if (failures.Any())
   {
       throw new ValidationException(failures);
   }
   ```
   - Hata varsa → `ValidationException` fırlatır
   - Handler'a gitmez
   - Hata yoksa → Handler'a geçer

### Örnek: StoreBasketCommand Validation Akışı

```
Controller → MediatR.Send(StoreBasketCommand)
    ↓
ValidationBehavior çalışır
    ↓
DI Container: "StoreBasketCommand için validator var mı?"
    ↓
StoreBasketValidator bulunur (AddValidatorsFromAssembly ile kayıtlı)
    ↓
StoreBasketValidator.ValidateAsync() çağrılır
    ↓
Validation Kuralları:
  - Basket.UserName → NotEmpty
  - Basket.Items → NotNull
  - Her Item için ShoppingCartItemValidator çalışır
    ↓
Hata varsa → ValidationException fırlatır (400 Bad Request)
Hata yoksa → StoreBasketHandler'a geçer
```

### Örnek: CheckoutBasketCommand Validation Akışı

```
Controller → MediatR.Send(CheckoutBasketCommand)
    ↓
ValidationBehavior çalışır
    ↓
DI Container: "CheckoutBasketCommand için validator var mı?"
    ↓
CheckoutBasketValidator bulunur (AddValidatorsFromAssembly ile kayıtlı)
    ↓
CheckoutBasketValidator.ValidateAsync() çağrılır
    ↓
Validation Kuralları:
  - UserName → NotEmpty
  - EmailAddress → NotEmpty + EmailAddress format
  - AddressLine → NotEmpty
  - CardNumber → NotEmpty + Length(16)
  - CVV → NotEmpty + Length(3)
    ↓
Hata varsa → ValidationException fırlatır (400 Bad Request)
Hata yoksa → CheckoutBasketHandler'a geçer
```

### Neden Otomatik Çalışıyor?

**AddValidatorsFromAssembly** = Assembly'deki tüm validator'ları otomatik bulur ve kaydeder

**Ne yapar?**
- `Basket.API` assembly'sini tarar
- `AbstractValidator<T>` implement eden tüm class'ları bulur
- DI container'a kaydeder

**Örnek:**
- `StoreBasketValidator : AbstractValidator<StoreBasketCommand>` → Bulunur ve kaydedilir
- `CheckoutBasketValidator : AbstractValidator<CheckoutBasketCommand>` → Bulunur ve kaydedilir
- `ShoppingCartItemValidator : AbstractValidator<ShoppingCartItemDto>` → Bulunur ve kaydedilir

**DI Container'da Ne Oluyor?**
```csharp
// AddValidatorsFromAssembly şunları kaydeder:
- IValidator<StoreBasketCommand> → StoreBasketValidator (Scoped)
- IValidator<CheckoutBasketCommand> → CheckoutBasketValidator (Scoped)
- IValidator<ShoppingCartItemDto> → ShoppingCartItemValidator (Scoped)
```

**ValidationBehavior Constructor'da:**
```csharp
public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
{
    _validators = validators;  // ← DI'dan tüm validator'lar gelir
}
```

**Örnek: StoreBasketCommand geldiğinde:**
```csharp
// DI Container şunları sağlar:
IEnumerable<IValidator<StoreBasketCommand>> validators = [
    StoreBasketValidator instance
];
```

**Sonuç:**
- MediatR, FluentValidation, AutoMapper konfigürasyonu tamamlandı
- Pipeline behaviors eklendi (Logging, Validation)
- Handler'lar ve Validator'lar otomatik bulunacak
- **Kendi yazdığımız validator'lar (StoreBasketValidator, CheckoutBasketValidator) otomatik olarak ValidationBehavior tarafından uygulanır**

**Kontrol:**
```bash
dotnet build src/Services/Basket/Basket.API/Basket.API.csproj
# Build başarılı olmalı (0 hata, 0 uyarı)
```

---

## Adım 2: DTO'ları Oluştur

**Klasör:** `Dtos/`

### ShoppingCartDto.cs

**Dosya:** `Dtos/ShoppingCartDto.cs`

**Ne işe yarar:** API response'ları için sepet DTO'su

**Özellikler:**
- `UserName` → Kullanıcı adı
- `Items` → Sepet item'ları listesi (`List<ShoppingCartItemDto>`)
- `TotalPrice` → Toplam fiyat (indirim sonrası)
- `Discount` → Toplam indirim miktarı

**Neden DTO?**
- Entity'ler internal (Redis'te tutulur)
- DTO'lar external (API'ye döner)
- Entity'den farklı property'ler olabilir (Discount gibi)

### ShoppingCartItemDto.cs

**Dosya:** `Dtos/ShoppingCartItemDto.cs`

**Ne işe yarar:** API response'ları için sepet item DTO'su

**Özellikler:**
- `ProductId` → Ürün ID'si
- `ProductName` → Ürün adı
- `Quantity` → Miktar
- `Price` → Birim fiyat

**Sonuç:**
- `Dtos/ShoppingCartDto.cs` oluşturuldu
- `Dtos/ShoppingCartItemDto.cs` oluşturuldu

---

## Adım 3: AutoMapper Profile Oluştur

**Dosya:** `Mapping/MappingProfile.cs`

**Ne işe yarar:** Entity ↔ DTO ve Command ↔ Event mapping'lerini tanımlar

**Mapping'ler:**
1. **ShoppingCart → ShoppingCartDto**
   - `Discount` property'si ignore edilir (manuel hesaplanacak)
2. **ShoppingCartDto → ShoppingCart** (Reverse mapping)
   - StoreBasket için kullanılır
3. **ShoppingCartItem → ShoppingCartItemDto**
4. **ShoppingCartItemDto → ShoppingCartItem** (Reverse mapping)
5. **CheckoutBasketCommand → BasketCheckoutEvent**
   - Checkout işlemi için kullanılır
   - `TotalPrice` ignore edilir (basket'ten alınacak)

**Neden AutoMapper?**
- Manuel mapping kodları yazmaya gerek yok
- Type-safe mapping
- Kolay bakım

**Sonuç:**
- `Mapping/MappingProfile.cs` oluşturuldu
- Tüm mapping'ler tanımlandı

---

## Adım 4: GetBasketQuery + GetBasketHandler

**Klasör:** `Features/Basket/Queries/GetBasket/`

> **📌 ÖNEMLİ: Handler'lar Ne Zaman Çalışır?**
> 
> Bu bölümde handler'ların ne zaman çalıştığını ve hangi işlem sırasında kullanıldığını öğreneceksiniz. Bu bilgiler, handler'ların pratik kullanım senaryolarını anlamanıza yardımcı olacaktır.

### GetBasketQuery.cs

**Dosya:** `Features/Basket/Queries/GetBasket/GetBasketQuery.cs`

**Ne işe yarar:** Sepeti getirmek için query (MediatR request)

**Özellikler:**
- `record GetBasketQuery(string UserName) : IRequest<ShoppingCartDto>`
- `IRequest<TResponse>` → MediatR request interface'i
- `TResponse` → `ShoppingCartDto` (dönüş tipi)

**Neden record?**
- Immutable (değiştirilemez)
- Value equality (değer bazlı karşılaştırma)
- Daha az kod (constructor, equality otomatik)

### GetBasketHandler.cs

**Dosya:** `Features/Basket/Queries/GetBasket/GetBasketHandler.cs`

**Ne işe yarar:** GetBasketQuery'yi işler (Redis'ten sepeti alır, indirim hesaplar)

**NE ZAMAN ÇALIŞIR:**
- Kullanıcı sepete bakmak istediğinde
- Sepet sayfası yüklendiğinde
- Frontend: `GET /api/baskets/{userName}` endpoint'i çağrıldığında
- Kullanıcı "Sepetimi Göster" butonuna bastığında
- Sepet özeti gösterilirken

**HANGİ İŞLEM SIRASINDA KULLANILIR:**
- **Sepet görüntüleme işlemi:** Kullanıcı sepete bakmak istediğinde
- **Sepet özeti:** Checkout sayfasında sepet özeti gösterilirken
- **Sepet güncelleme sonrası:** Ürün eklendikten sonra güncel sepeti göstermek için
- **İndirim hesaplama:** Her ürün için Discount gRPC servisinden indirim sorgulanır

**Tipik Kullanıcı Akışı:**
```
1. Kullanıcı sepete bakmak ister
   ↓
2. Frontend: GET /api/baskets/{userName} çağrılır
   ↓
3. GetBasketHandler çalışır
   ↓
4. Sepet + İndirimler gösterilir
```

**Dependencies:**
- `IBasketRepository` → Redis'ten sepeti almak için
- `DiscountGrpcService` → İndirim sorgulamak için
- `IMapper` → Entity'den DTO'ya map etmek için
- `ILogger` → Loglama için

**Adım adım ne yapıyor?**

1. **Redis'ten sepeti al**
   ```csharp
   var basket = await _repository.GetBasket(request.UserName);
   ```
   - Sepet yoksa → Boş sepet döndürür (hata fırlatmaz)

2. **Her item için indirim sorgula (gRPC)**
   ```csharp
   foreach (var item in basket.Items)
   {
       var coupon = await _discountGrpcService.GetDiscount(item.ProductName);
       if (coupon.Amount > 0)
       {
           totalDiscount += coupon.Amount * item.Quantity;
       }
   }
   ```
   - Her ürün için ayrı gRPC sorgusu
   - Toplam indirimi hesaplar

3. **DTO'ya map et**
   ```csharp
   var basketDto = _mapper.Map<ShoppingCartDto>(basket);
   basketDto.Discount = totalDiscount;
   basketDto.TotalPrice = basket.TotalPrice - totalDiscount;
   ```
   - Entity'den DTO'ya map eder
   - Discount ve TotalPrice'ı hesaplar

**Önemli Noktalar:**
- Sepet yoksa → Boş sepet döndürür (hata fırlatmaz)
- İndirim hesaplama → Her item için ayrı sorgu (gRPC hızlı)
- TotalPrice → Basket.TotalPrice - Discount

**Handler'lar Arası İlişki:**
- GetBasketHandler → Sadece okuma yapar (Query), veri değiştirmez
- StoreBasketHandler → Sepet kaydedilirken kullanılır, sonra GetBasketHandler ile görüntülenir
- CheckoutBasketHandler → Sipariş tamamlanırken kullanılır, sonra sepet silinir

**Sonuç:**
- `Features/Basket/Queries/GetBasket/GetBasketQuery.cs` oluşturuldu
- `Features/Basket/Queries/GetBasket/GetBasketHandler.cs` oluşturuldu

---

## Adım 5: StoreBasketCommand + StoreBasketHandler + StoreBasketValidator

**Klasör:** `Features/Basket/Commands/StoreBasket/`

> **📌 ÖNEMLİ: Handler'lar Ne Zaman Çalışır?**
> 
> StoreBasketHandler, kullanıcı sepete ürün eklediğinde veya sepeti güncellediğinde çalışır. Bu handler, sepet verilerini hem PostgreSQL'e (source of truth) hem Redis'e (cache) kaydeder.

### StoreBasketCommand.cs

**Dosya:** `Features/Basket/Commands/StoreBasket/StoreBasketCommand.cs`

**Ne işe yarar:** Sepeti kaydetmek/güncellemek için command (MediatR request)

**Özellikler:**
- `record StoreBasketCommand(ShoppingCartDto Basket) : IRequest<ShoppingCartDto>`
- `Basket` → Kaydedilecek sepet DTO'su
- `IRequest<TResponse>` → MediatR request interface'i

### StoreBasketValidator.cs

**Dosya:** `Features/Basket/Commands/StoreBasket/StoreBasketValidator.cs`

**Ne işe yarar:** StoreBasketCommand için validation kuralları

**İki Validator Class:**

#### 1. StoreBasketValidator
- `Basket` → NotNull
- `UserName` → NotEmpty
- `Items` → NotNull
- Her `Item` için → `ShoppingCartItemValidator` kullanır

#### 2. ShoppingCartItemValidator
- `ProductId` → NotEmpty
- `ProductName` → NotEmpty
- `Quantity` → GreaterThan(0)
- `Price` → GreaterThan(0)

**Validation Akışı:**
```
StoreBasketCommand geldi
    ↓
ValidationBehavior çalışır
    ↓
StoreBasketValidator.ValidateAsync() çağrılır
    ↓
Hata varsa → ValidationException fırlatır
Hata yoksa → Handler'a geçer
```

### StoreBasketHandler.cs

**Dosya:** `Features/Basket/Commands/StoreBasket/StoreBasketHandler.cs`

**Ne işe yarar:** StoreBasketCommand'yi işler (Redis'e sepeti kaydeder)

**NE ZAMAN ÇALIŞIR:**
- Kullanıcı sepete ürün eklediğinde
- Sepetteki ürün miktarını değiştirdiğinde
- Sepet güncellendiğinde
- Frontend: `POST /api/baskets` endpoint'i çağrıldığında
- Kullanıcı "Sepete Ekle" butonuna bastığında
- Ürün miktarı değiştirildiğinde

**HANGİ İŞLEM SIRASINDA KULLANILIR:**
- **Sepete ürün ekleme:** Yeni ürün sepete eklendiğinde
- **Sepet güncelleme:** Mevcut ürünün miktarı değiştirildiğinde
- **Sepet senkronizasyonu:** Frontend'den sepet verisi gönderildiğinde
- **Sepet kaydetme:** Sepet hem PostgreSQL'e (source of truth) hem Redis'e (cache) kaydedilir

**Tipik Kullanıcı Akışı:**
```
1. Kullanıcı ürün ekler
   ↓
2. Frontend: POST /api/baskets çağrılır
   ↓
3. StoreBasketHandler çalışır
   ↓
4. Sepet kaydedilir (PostgreSQL + Redis)
   ↓
5. Güncel sepet döner
```

**Dependencies:**
- `IBasketRepository` → Redis'e kaydetmek için
- `IMapper` → DTO ↔ Entity mapping için
- `ILogger` → Loglama için

**Adım adım ne yapıyor?**

1. **DTO'dan Entity'ye map et**
   ```csharp
   var basket = _mapper.Map<ShoppingCart>(request.Basket);
   ```

2. **Redis'e kaydet**
   ```csharp
   var savedBasket = await _repository.SaveBasket(basket);
   ```
   - Key yoksa oluşturur, varsa günceller (upsert)

3. **Entity'den DTO'ya map et**
   ```csharp
   var basketDto = _mapper.Map<ShoppingCartDto>(savedBasket);
   ```

**Handler'lar Arası İlişki:**
- StoreBasketHandler → Sepet kaydedilir/güncellenir (Command), veri değiştirir
- GetBasketHandler → Kaydedilen sepet görüntülenirken kullanılır
- CheckoutBasketHandler → Sepet checkout edilirken kullanılır

**Sonuç:**
- `Features/Basket/Commands/StoreBasket/StoreBasketCommand.cs` oluşturuldu
- `Features/Basket/Commands/StoreBasket/StoreBasketHandler.cs` oluşturuldu
- `Features/Basket/Commands/StoreBasket/StoreBasketValidator.cs` oluşturuldu

---

## Adım 6: DeleteBasketCommand + DeleteBasketHandler

**Klasör:** `Features/Basket/Commands/DeleteBasket/`

> **📌 ÖNEMLİ: Handler'lar Ne Zaman Çalışır?**
> 
> DeleteBasketHandler, kullanıcı sepeti manuel olarak silmek istediğinde çalışır. CheckoutBasketHandler içinde de sepet silinir, ama bu handler manuel silme için kullanılır.

### DeleteBasketCommand.cs

**Dosya:** `Features/Basket/Commands/DeleteBasket/DeleteBasketCommand.cs`

**Ne işe yarar:** Sepeti silmek için command (MediatR request)

**Özellikler:**
- `record DeleteBasketCommand(string UserName) : IRequest<bool>`
- `UserName` → Silinecek sepetin kullanıcı adı
- `IRequest<bool>` → Başarılı/başarısız döner

### DeleteBasketHandler.cs

**Dosya:** `Features/Basket/Commands/DeleteBasket/DeleteBasketHandler.cs`

**Ne işe yarar:** DeleteBasketCommand'yi işler (Redis'ten sepeti siler)

**NE ZAMAN ÇALIŞIR:**
- Kullanıcı sepeti manuel olarak silmek istediğinde
- Admin panelinden sepet silindiğinde
- Frontend: `DELETE /api/baskets/{userName}` endpoint'i çağrıldığında
- Kullanıcı "Sepeti Temizle" butonuna bastığında

**HANGİ İŞLEM SIRASINDA KULLANILIR:**
- **Manuel sepet silme:** Kullanıcı sepeti temizlemek istediğinde
- **Admin işlemleri:** Admin panelinden sepet silindiğinde
- **NOT:** CheckoutBasketHandler içinde de sepet silinir, ama bu handler manuel silme için

**Tipik Kullanıcı Akışı:**
```
1. Kullanıcı sepeti temizlemek ister
   ↓
2. Frontend: DELETE /api/baskets/{userName} çağrılır
   ↓
3. DeleteBasketHandler çalışır
   ↓
4. Sepet silinir (PostgreSQL + Redis)
```

**Dependencies:**
- `IBasketRepository` → Redis'ten silmek için
- `ILogger` → Loglama için

**Adım adım ne yapıyor?**

1. **Redis'ten sil**
   ```csharp
   var deleted = await _repository.DeleteBasket(request.UserName);
   ```

2. **Loglama**
   - Başarılıysa → Information log
   - Başarısızsa → Warning log (sepet yoksa)

3. **Return**
   ```csharp
   return deleted;  // true veya false
   ```

**Handler'lar Arası İlişki:**
- DeleteBasketHandler → Manuel sepet silme için kullanılır
- CheckoutBasketHandler → Checkout sonrası sepet silinir (otomatik)
- GetBasketHandler → Silinen sepet görüntülenemez (boş sepet döner)

**Sonuç:**
- `Features/Basket/Commands/DeleteBasket/DeleteBasketCommand.cs` oluşturuldu
- `Features/Basket/Commands/DeleteBasket/DeleteBasketHandler.cs` oluşturuldu

---

## Adım 7: CheckoutBasketCommand + CheckoutBasketHandler + CheckoutBasketValidator

**Klasör:** `Features/Basket/Commands/CheckoutBasket/`

> **📌 ÖNEMLİ: Handler'lar Ne Zaman Çalışır?**
> 
> CheckoutBasketHandler, kullanıcı "Siparişi Tamamla" butonuna bastığında çalışır. Bu handler, RabbitMQ'ya event gönderir (Ordering Service bu event'i dinler ve sipariş oluşturur) ve sepeti siler. Bu, microservice mimarisinde event-driven pattern kullanımının önemli bir örneğidir.

### CheckoutBasketCommand.cs

**Dosya:** `Features/Basket/Commands/CheckoutBasket/CheckoutBasketCommand.cs`

**Ne işe yarar:** Sepeti checkout etmek için command (RabbitMQ event gönderir)

**Özellikler:**
- `record CheckoutBasketCommand(...) : IRequest<bool>`
- **Shipping Address:** FirstName, LastName, EmailAddress, AddressLine, Country, State, ZipCode
- **Payment Info:** CardName, CardNumber, Expiration, CVV, PaymentMethod
- `UserName` → Checkout edilecek sepetin kullanıcı adı
- `IRequest<bool>` → Başarılı/başarısız döner

### CheckoutBasketValidator.cs

**Dosya:** `Features/Basket/Commands/CheckoutBasket/CheckoutBasketValidator.cs`

**Ne işe yarar:** CheckoutBasketCommand için validation kuralları

**Validation Kuralları:**
- `UserName` → NotEmpty
- `EmailAddress` → NotEmpty + EmailAddress format kontrolü
- `AddressLine` → NotEmpty
- `CardNumber` → NotEmpty + Length(16)
- `CVV` → NotEmpty + Length(3)

### CheckoutBasketHandler.cs

**Dosya:** `Features/Basket/Commands/CheckoutBasket/CheckoutBasketHandler.cs`

**Ne işe yarar:** CheckoutBasketCommand'yi işler (RabbitMQ'ya event gönderir, sepeti siler)

**NE ZAMAN ÇALIŞIR:**
- Kullanıcı "Siparişi Tamamla" butonuna bastığında
- Ödeme sayfasında sipariş onaylandığında
- Frontend: `POST /api/baskets/checkout` endpoint'i çağrıldığında
- Sipariş işlemi başlatıldığında

**HANGİ İŞLEM SIRASINDA KULLANILIR:**
- **Sipariş tamamlama:** Kullanıcı siparişi onayladığında
- **Event publishing:** RabbitMQ'ya BasketCheckoutEvent gönderilir (Ordering Service bu event'i dinler ve sipariş oluşturur)
- **Sepet temizleme:** Checkout tamamlandığı için sepet silinir
- **Microservice iletişimi:** Basket Service, Ordering Service'e event gönderir (loosely coupled)

**Tipik Kullanıcı Akışı:**
```
1. Kullanıcı "Siparişi Tamamla" der
   ↓
2. Frontend: POST /api/baskets/checkout çağrılır
   ↓
3. CheckoutBasketHandler çalışır
   ↓
4. Event RabbitMQ'ya gönderilir (Ordering Service dinler)
   ↓
5. Sepet silinir
   ↓
6. Ordering Service event'i alır ve sipariş oluşturur
```

**ÖNEMLİ NOTLAR:**
- RabbitMQ'ya event gönderilir → Ordering Service bu event'i dinler ve sipariş oluşturur
- Sepet silinir → Checkout tamamlandığı için sepet artık gerekli değil
- Event-driven pattern → Microservice mimarisinde servisler arası iletişim için kullanılır

**Dependencies:**
- `IBasketRepository` → Sepeti almak ve silmek için
- `IPublishEndpoint` → RabbitMQ'ya event göndermek için (MassTransit)
- `IMapper` → Command'den Event'e map etmek için
- `ILogger` → Loglama için

**Adım adım ne yapıyor?**

1. **Sepeti Redis'ten al**
   ```csharp
   var basket = await _repository.GetBasket(request.UserName);
   ```
   - Sepet yoksa → `false` döner (hata fırlatmaz)

2. **Event oluştur**
   ```csharp
   var eventMessage = _mapper.Map<BasketCheckoutEvent>(request);
   eventMessage = eventMessage with { TotalPrice = basket.TotalPrice };
   ```
   - Command'den Event'e map eder
   - `TotalPrice`'ı basket'ten alır (record with expression)

3. **RabbitMQ'ya gönder**
   ```csharp
   await _publishEndpoint.Publish(eventMessage, cancellationToken);
   ```
   - Asenkron event gönderir (fire & forget)
   - Ordering Service dinleyecek

4. **Sepeti sil**
   ```csharp
   await _repository.DeleteBasket(request.UserName);
   ```
   - Checkout sonrası sepet temizlenir

**Önemli Noktalar:**
- `IPublishEndpoint` → MassTransit ile event publish
- Event mapping → Command'den Event'e AutoMapper ile
- TotalPrice → Basket'ten alınır (command'de yok)
- Sepet silinir → Checkout sonrası sepet temizlenir

**Handler'lar Arası İlişki:**
- CheckoutBasketHandler → Sipariş tamamlanırken kullanılır (Command), RabbitMQ'ya event gönderir
- GetBasketHandler → Checkout öncesi sepet görüntülenirken kullanılır
- StoreBasketHandler → Checkout öncesi sepet güncellenirken kullanılır
- DeleteBasketHandler → Checkout sonrası sepet silinir (CheckoutBasketHandler içinde)

**Tipik E-ticaret Alışveriş Akışı:**
```
1. Kullanıcı ürün ekler
   ↓ StoreBasketHandler çalışır (sepet kaydedilir)

2. Kullanıcı sepete bakar
   ↓ GetBasketHandler çalışır (sepet + indirimler gösterilir)

3. Kullanıcı tekrar ürün ekler
   ↓ StoreBasketHandler çalışır (sepet güncellenir)

4. Kullanıcı sepete tekrar bakar
   ↓ GetBasketHandler çalışır (güncel sepet gösterilir)

5. Kullanıcı "Siparişi Tamamla" der
   ↓ CheckoutBasketHandler çalışır (event gönderilir, sepet silinir)
   ↓ RabbitMQ → Ordering Service event'i alır ve sipariş oluşturur
```

**Sonuç:**
- `Features/Basket/Commands/CheckoutBasket/CheckoutBasketCommand.cs` oluşturuldu
- `Features/Basket/Commands/CheckoutBasket/CheckoutBasketHandler.cs` oluşturuldu
- `Features/Basket/Commands/CheckoutBasket/CheckoutBasketValidator.cs` oluşturuldu

---

## Adım 8: MassTransit Konfigürasyonu

**Dosya:** `Program.cs`

**Kod:**
```csharp
using MassTransit;

// MassTransit
builder.Services.AddMassTransit(config =>
{
    config.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["MessageBroker:Host"]);
    });
});
```

**Açıklamalar:**

### AddMassTransit Nedir?

**AddMassTransit** = MassTransit'i DI container'a kaydeder

**Ne yapıyor?**
- `IBusControl` → RabbitMQ bağlantısını yönetir (Singleton)
- `IPublishEndpoint` → Event publish için (Scoped)
- `ISendEndpointProvider` → Message send için (Scoped)

### UsingRabbitMq Nedir?

**UsingRabbitMq** = RabbitMQ provider'ını seçer

**Ne yapıyor?**
- RabbitMQ bağlantısını yapılandırır
- Connection string'i parse eder
- Bağlantıyı yönetir

### Host Konfigürasyonu

**cfg.Host(...)**
- **Connection String:** `appsettings.json`'dan `MessageBroker:Host` okunur
- **VHost:** `/` (default virtual host)
- **Username/Password:** `guest/guest` (default RabbitMQ credentials)

**Connection String Format:**
```
amqp://guest:guest@localhost:5673
```

### DI Container'da Ne Oluyor?

```csharp
// MassTransit şunları kaydeder:
- IBusControl → RabbitMqBusControl (Singleton) - RabbitMQ bağlantısını yönetir
- IPublishEndpoint → PublishEndpoint (Scoped) - Event publish için
- ISendEndpointProvider → SendEndpointProvider (Scoped) - Message send için
```

### RabbitMQ Bağlantı Akışı

```
Uygulama Başlatılıyor
    ↓
AddMassTransit() çağrılır
    ↓
IBusControl oluşturulur (Singleton)
    ↓
RabbitMQ'ya bağlanır (amqp://guest:guest@localhost:5673)
    ↓
Connection açılır (TCP socket)
    ↓
Uygulama çalışırken bağlantı açık kalır
    ↓
IPublishEndpoint.Publish() → RabbitMQ'ya mesaj gönderir
    ↓
Uygulama kapanınca → Bağlantı kapanır
```

### Neden Singleton?

**IBusControl Singleton olarak kaydedilir**

**Neden?**
- RabbitMQ bağlantısı pahalı (TCP socket)
- Tüm request'ler aynı bağlantıyı kullanabilir
- Thread-safe
- Connection pooling yapar

**Sonuç:**
- MassTransit konfigürasyonu tamamlandı
- RabbitMQ bağlantısı hazır
- Event publish/subscribe için hazır

**Kontrol:**
```bash
dotnet build src/Services/Basket/Basket.API/Basket.API.csproj
# Build başarılı olmalı (0 hata, 0 uyarı)
```

---

## Handler'lar Ne Zaman Çalışır? - Özet Tablo

| Handler | Ne Zaman Çalışır | Hangi İşlem | Sıklık | Veri Değişikliği |
|---------|------------------|-------------|--------|------------------|
| **GetBasketHandler** | Sepet görüntülenirken | Okuma (Query) | Çok sık | ❌ Hayır |
| **StoreBasketHandler** | Ürün ekleme/güncelleme | Yazma (Command) | Sık | ✅ Evet |
| **DeleteBasketHandler** | Manuel sepet silme | Yazma (Command) | Az | ✅ Evet |
| **CheckoutBasketHandler** | Sipariş tamamlanırken | Yazma + Event (Command) | Az | ✅ Evet |

### Handler'lar Arası İlişkiler:

```
Kullanıcı Sepete Ürün Ekler
    ↓
StoreBasketHandler (sepet kaydedilir)
    ↓
Kullanıcı Sepete Bakar
    ↓
GetBasketHandler (sepet + indirimler gösterilir)
    ↓
Kullanıcı Siparişi Tamamlar
    ↓
CheckoutBasketHandler (event gönderilir, sepet silinir)
    ↓
RabbitMQ → Ordering Service (sipariş oluşturulur)
```

### Önemli Notlar:

1. **GetBasketHandler (Query):**
   - Sadece okuma yapar, veri değiştirmez
   - Her sepet görüntüleme işleminde çalışır
   - İndirim hesaplama yapar (gRPC ile)

2. **StoreBasketHandler (Command):**
   - Veri değiştirir (sepet kaydedilir/güncellenir)
   - Hem PostgreSQL'e hem Redis'e yazar
   - Ürün ekleme/güncelleme işlemlerinde çalışır

3. **CheckoutBasketHandler (Command):**
   - Veri değiştirir (sepet silinir)
   - RabbitMQ'ya event gönderir (Ordering Service için)
   - Sipariş tamamlama işleminde çalışır

4. **DeleteBasketHandler (Command):**
   - Veri değiştirir (sepet silinir)
   - Manuel sepet silme için kullanılır
   - CheckoutBasketHandler içinde de sepet silinir

---

## Faz 5.4 Özet

### Tamamlanan Adımlar:
1. ✅ MediatR, FluentValidation, AutoMapper konfigürasyonu
2. ✅ DTO'lar oluşturuldu (`ShoppingCartDto`, `ShoppingCartItemDto`)
3. ✅ AutoMapper Profile oluşturuldu (`MappingProfile.cs`)
4. ✅ GetBasketQuery + GetBasketHandler oluşturuldu
5. ✅ StoreBasketCommand + StoreBasketHandler + StoreBasketValidator oluşturuldu
6. ✅ DeleteBasketCommand + DeleteBasketHandler oluşturuldu
7. ✅ CheckoutBasketCommand + CheckoutBasketHandler + CheckoutBasketValidator oluşturuldu
8. ✅ MassTransit konfigürasyonu eklendi

### Oluşturulan Klasör ve Dosyalar:

#### Dtos/
- **ShoppingCartDto.cs** → Sepet DTO (API response için)
- **ShoppingCartItemDto.cs** → Sepet item DTO (API response için)

#### Mapping/
- **MappingProfile.cs** → Entity ↔ DTO ve Command ↔ Event mapping'leri

#### Features/Basket/Queries/GetBasket/
- **GetBasketQuery.cs** → Sepeti getirmek için query
- **GetBasketHandler.cs** → GetBasketQuery'yi işler (Redis + gRPC indirim)

#### Features/Basket/Commands/StoreBasket/
- **StoreBasketCommand.cs** → Sepeti kaydetmek için command
- **StoreBasketHandler.cs** → StoreBasketCommand'yi işler (Redis'e kaydet)
- **StoreBasketValidator.cs** → StoreBasketCommand validation kuralları

#### Features/Basket/Commands/DeleteBasket/
- **DeleteBasketCommand.cs** → Sepeti silmek için command
- **DeleteBasketHandler.cs** → DeleteBasketCommand'yi işler (Redis'ten sil)

#### Features/Basket/Commands/CheckoutBasket/
- **CheckoutBasketCommand.cs** → Sepeti checkout etmek için command
- **CheckoutBasketHandler.cs** → CheckoutBasketCommand'yi işler (RabbitMQ event + sepet sil)
- **CheckoutBasketValidator.cs** → CheckoutBasketCommand validation kuralları

### Önemli Kavramlar:

#### MediatR:
- **Pipeline Behavior:** LoggingBehavior, ValidationBehavior
- **Handler Registration:** Otomatik (RegisterServicesFromAssembly)
- **Request/Response:** IRequest<TResponse>, IRequestHandler<TRequest, TResponse>

#### FluentValidation:
- **Validator Registration:** Otomatik (AddValidatorsFromAssembly)
- **Validation Behavior:** Pipeline'da otomatik çalışır
- **Error Handling:** ValidationException fırlatır

#### AutoMapper:
- **Profile Registration:** Otomatik (AddAutoMapper)
- **Mapping Types:** Entity ↔ DTO, Command → Event
- **Ignore Members:** Manuel hesaplanacak property'ler ignore edilir

#### MassTransit:
- **IBusControl:** RabbitMQ bağlantısını yönetir (Singleton)
- **IPublishEndpoint:** Event publish için (Scoped)
- **Connection:** RabbitMQ'ya otomatik bağlanır

---

## Faz 5.5: Basket Controller & Entegrasyon

**Hedef:** REST API endpoint'leri ve entegrasyon

### Controller Pattern Nedir?

**Controller** = HTTP request'leri karşılayan ve response dönen class'lar

**Ne işe yarar?**
- REST API endpoint'lerini tanımlar
- HTTP metodlarını (GET, POST, DELETE) map eder
- MediatR'a command/query gönderir
- Response döner

**Neden Controller?**
- ASP.NET Core'un standart pattern'i
- Route'ları organize eder
- Swagger dokümantasyonu otomatik oluşur

---

## Adım 1: BasketsController Oluştur

**Dosya:** `Controllers/BasketsController.cs`

**Ne işe yarar:** REST API endpoint'lerini oluşturur (MediatR ile)

**Kod:**
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

    // ... endpoint'ler
}
```

**Açıklamalar:**

### ApiController Attribute

**`[ApiController]`** = API Controller olduğunu belirtir

**Ne işe yarar?**
- Automatic model validation (otomatik model doğrulama)
- ProblemDetails response formatı
- 400 Bad Request otomatik döner (validation hatası varsa)

### Route Attribute

**`[Route("api/[controller]")]`** = Route pattern tanımlar

**Ne demek?**
- `[controller]` → Controller adı (BasketsController → "Baskets")
- Base route: `/api/baskets`
- Her metod kendi route'unu ekler

**Örnek:**
- `[HttpGet("{userName}")]` → `/api/baskets/{userName}`
- `[HttpPost]` → `/api/baskets`
- `[HttpPost("checkout")]` → `/api/baskets/checkout`

### IMediator Dependency

**`IMediator`** = MediatR mediator interface'i

**Ne işe yarar?**
- Command/Query'leri handler'lara gönderir
- Pipeline'ı çalıştırır (Logging, Validation)
- Response döner

**Neden Controller'da IMediator?**
- Controller direkt handler'a bağlı değil
- MediatR pattern kullanılıyor
- Decoupling (Controller, Handler'ı bilmez)

### Endpoint'ler

#### 1. GetBasket Endpoint

**`[HttpGet("{userName}")]`**

**Ne yapar:**
- `GET /api/baskets/{userName}` → Sepeti getirir
- `GetBasketQuery` oluşturur ve MediatR'a gönderir
- `ShoppingCartDto` döner

**NE ZAMAN ÇALIŞIR:**
- Kullanıcı sepete bakmak istediğinde
- Sepet sayfası yüklendiğinde
- Frontend: `GET /api/baskets/{userName}` endpoint'i çağrıldığında

**HANGİ HANDLER ÇALIŞIR:**
- `GetBasketHandler` → Sepeti Redis'ten alır, indirim hesaplar, DTO'ya map eder

**Response:**
- `200 OK` → Sepet bulundu
- `200 OK` (boş sepet) → Sepet yoksa boş sepet döner

**Kod:**
```csharp
[HttpGet("{userName}")]
[ProducesResponseType(typeof(ShoppingCartDto), StatusCodes.Status200OK)]
public async Task<ActionResult<ShoppingCartDto>> GetBasket(string userName)
{
    var basket = await _mediator.Send(new GetBasketQuery(userName));
    return Ok(basket);
}
```

#### 2. StoreBasket Endpoint

**`[HttpPost]`**

**Ne yapar:**
- `POST /api/baskets` → Sepeti kaydeder/günceller
- Request body'den `ShoppingCartDto` alır
- `StoreBasketCommand` oluşturur ve MediatR'a gönderir
- `ShoppingCartDto` döner

**NE ZAMAN ÇALIŞIR:**
- Kullanıcı sepete ürün eklediğinde
- Sepetteki ürün miktarını değiştirdiğinde
- Sepet güncellendiğinde
- Frontend: `POST /api/baskets` endpoint'i çağrıldığında

**HANGİ HANDLER ÇALIŞIR:**
- `StoreBasketHandler` → Sepeti PostgreSQL'e kaydeder, Redis'e cache'ler

**Response:**
- `200 OK` → Sepet kaydedildi/güncellendi
- `400 Bad Request` → Validation hatası (FluentValidation)

**Kod:**
```csharp
[HttpPost]
[ProducesResponseType(typeof(ShoppingCartDto), StatusCodes.Status200OK)]
public async Task<ActionResult<ShoppingCartDto>> StoreBasket([FromBody] ShoppingCartDto basket)
{
    var result = await _mediator.Send(new StoreBasketCommand(basket));
    return Ok(result);
}
```

#### 3. DeleteBasket Endpoint

**`[HttpDelete("{userName}")]`**

**Ne yapar:**
- `DELETE /api/baskets/{userName}` → Sepeti siler
- `DeleteBasketCommand` oluşturur ve MediatR'a gönderir
- `bool` döner (başarılı/başarısız)

**NE ZAMAN ÇALIŞIR:**
- Kullanıcı sepeti manuel olarak silmek istediğinde
- Admin panelinden sepet silindiğinde
- Frontend: `DELETE /api/baskets/{userName}` endpoint'i çağrıldığında

**HANGİ HANDLER ÇALIŞIR:**
- `DeleteBasketHandler` → Sepeti PostgreSQL'den ve Redis'ten siler

**Response:**
- `204 No Content` → Sepet silindi
- `404 Not Found` → Sepet bulunamadı

**Kod:**
```csharp
[HttpDelete("{userName}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
public async Task<IActionResult> DeleteBasket(string userName)
{
    var deleted = await _mediator.Send(new DeleteBasketCommand(userName));
    if (!deleted)
        return NotFound();
    
    return NoContent();
}
```

#### 4. CheckoutBasket Endpoint

**`[HttpPost("checkout")]`**

**Ne yapar:**
- `POST /api/baskets/checkout` → Checkout yapar (RabbitMQ event)
- Request body'den `CheckoutBasketCommand` alır
- MediatR'a gönderir
- `bool` döner (başarılı/başarısız)

**NE ZAMAN ÇALIŞIR:**
- Kullanıcı "Siparişi Tamamla" butonuna bastığında
- Ödeme sayfasında sipariş onaylandığında
- Frontend: `POST /api/baskets/checkout` endpoint'i çağrıldığında

**HANGİ HANDLER ÇALIŞIR:**
- `CheckoutBasketHandler` → RabbitMQ'ya event gönderir (Ordering Service için), sepeti siler

**ÖNEMLİ NOT:**
- RabbitMQ'ya event gönderilir → Ordering Service bu event'i dinler ve sipariş oluşturur
- Sepet silinir → Checkout tamamlandığı için sepet artık gerekli değil

**Response:**
- `200 OK` → Checkout başarılı
- `400 Bad Request` → Sepet bulunamadı veya validation hatası

**Kod:**
```csharp
[HttpPost("checkout")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<ActionResult<bool>> CheckoutBasket([FromBody] CheckoutBasketCommand command)
{
    var result = await _mediator.Send(command);
    if (!result)
        return BadRequest("Basket not found");
    
    return Ok(result);
}
```

### ProducesResponseType Attribute

**`[ProducesResponseType]`** = Swagger dokümantasyonu için response tiplerini belirtir

**Ne işe yarar?**
- Swagger UI'da response tipleri gösterilir
- API dokümantasyonu otomatik oluşur
- Client'lar için bilgi sağlar

**Örnek:**
```csharp
[ProducesResponseType(typeof(ShoppingCartDto), StatusCodes.Status200OK)]
```
- Swagger'da: `200 OK` → `ShoppingCartDto` döner

### Request/Response Akışı

```
HTTP Request: GET /api/baskets/user1
    ↓
BasketsController.GetBasket("user1")
    ↓
_mediator.Send(new GetBasketQuery("user1"))
    ↓
MediatR Pipeline:
  - LoggingBehavior
  - ValidationBehavior
  - GetBasketHandler.Handle()
    ↓
Response: ShoppingCartDto
    ↓
HTTP Response: 200 OK + JSON
```

**Sonuç:**
- `Controllers/BasketsController.cs` oluşturuldu
- 4 endpoint eklendi: GetBasket, StoreBasket, DeleteBasket, CheckoutBasket
- MediatR entegrasyonu tamamlandı

---

## Adım 2: Exception Handler Middleware Ekle

**Dosya:** `Program.cs`

**Ne işe yarar:** Global exception handling ekler

**Kod:**
```csharp
using BuildingBlocks.Exceptions.Handler;

// Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ...

var app = builder.Build();

// Exception Handler Middleware
app.UseExceptionHandler();
```

**Açıklamalar:**

### AddExceptionHandler Nedir?

**`AddExceptionHandler<GlobalExceptionHandler>()`** = Exception handler'ı DI container'a kaydeder

**Ne yapar?**
- `GlobalExceptionHandler` class'ını kaydeder
- Exception'ları yakalamak için hazırlar

### AddProblemDetails Nedir?

**`AddProblemDetails()`** = ProblemDetails servisini ekler

**Ne işe yarar?**
- RFC 7807 formatında hata response'ları
- Standart hata formatı (type, title, status, detail)

### UseExceptionHandler Middleware

**`app.UseExceptionHandler()`** = Exception handler middleware'ini ekler

**Ne yapar?**
- Tüm exception'ları yakalar
- `GlobalExceptionHandler.TryHandleAsync()` çağrılır
- ProblemDetails formatında response döner

### GlobalExceptionHandler Nasıl Çalışır?

**Dosya:** `BuildingBlocks.Exceptions/Handler/GlobalExceptionHandler.cs`

**Ne yapar:**
1. Exception yakalanır
2. Exception tipine göre ProblemDetails oluşturulur:
   - `NotFoundException` → 404 Not Found
   - `BadRequestException` → 400 Bad Request
   - `InternalServerException` → 500 Internal Server Error
   - Diğer exception'lar → 500 Internal Server Error
3. ProblemDetails JSON formatında döner

**Exception Akışı:**
```
Handler'da exception fırlatıldı
    ↓
MediatR exception'ı yukarı fırlatır
    ↓
Controller exception'ı yakalamaz
    ↓
UseExceptionHandler middleware yakalar
    ↓
GlobalExceptionHandler.TryHandleAsync() çağrılır
    ↓
ProblemDetails oluşturulur
    ↓
JSON response döner
```

**Örnek Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Not Found",
  "status": 404,
  "detail": "Basket not found for user1",
  "instance": "/api/baskets/user1"
}
```

**Sonuç:**
- Exception Handler Middleware eklendi
- Global exception handling aktif
- ProblemDetails formatında hata response'ları

---

## Adım 3: Health Checks Ekle

**Dosya:** `Program.cs`

**Ne işe yarar:** Redis bağlantısını kontrol eder

**Kod:**
```csharp
// Health Checks
builder.Services.AddHealthChecks()
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

// ...

app.MapHealthChecks("/health");
```

**Açıklamalar:**

### AddHealthChecks Nedir?

**`AddHealthChecks()`** = Health check servislerini ekler

**Ne yapar?**
- Health check sistemini yapılandırır
- Health check provider'ları eklenir

### AddRedis Nedir?

**`AddRedis(...)`** = Redis health check provider'ı ekler

**Ne yapar?**
- Redis bağlantısını kontrol eder
- Connection string'i kullanır
- Redis erişilebilir mi kontrol eder

**Nasıl çalışır?**
```
Health Check Request: GET /health
    ↓
AddRedis health check çalışır
    ↓
Redis'e bağlanmaya çalışır
    ↓
Başarılı → Healthy
Başarısız → Unhealthy
```

### MapHealthChecks Nedir?

**`app.MapHealthChecks("/health")`** = Health check endpoint'ini oluşturur

**Ne yapar?**
- `/health` endpoint'ini oluşturur
- Health check'leri çalıştırır
- Sonuçları döner

**Response Format:**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "Redis": {
      "status": "Healthy",
      "duration": "00:00:00.0012345"
    }
  }
}
```

**Status Değerleri:**
- `Healthy` → Tüm health check'ler başarılı
- `Degraded` → Bazı health check'ler başarısız
- `Unhealthy` → Tüm health check'ler başarısız

**Neden Health Checks?**
- Monitoring için (Kubernetes, Docker, vb.)
- Load balancer'lar için (sağlıklı instance'ları seçer)
- Alerting için (sağlıksız servisleri tespit eder)

**Sonuç:**
- Health Checks eklendi
- Redis health check aktif
- `/health` endpoint'i hazır

---

## Faz 5.5 Özet

### Tamamlanan Adımlar:
1. ✅ BasketsController oluşturuldu
2. ✅ Exception Handler Middleware eklendi
3. ✅ Health Checks eklendi

### Oluşturulan Dosyalar:

#### Controllers/
- **BasketsController.cs** → REST API endpoint'leri
  - `GET /api/baskets/{userName}` → GetBasket
  - `POST /api/baskets` → StoreBasket
  - `DELETE /api/baskets/{userName}` → DeleteBasket
  - `POST /api/baskets/checkout` → CheckoutBasket

### Program.cs Güncellemeleri:

#### Exception Handler:
- `AddExceptionHandler<GlobalExceptionHandler>()` → Exception handler kaydı
- `AddProblemDetails()` → ProblemDetails servisi
- `app.UseExceptionHandler()` → Exception middleware

#### Health Checks:
- `AddHealthChecks().AddRedis(...)` → Redis health check
- `app.MapHealthChecks("/health")` → Health check endpoint'i

### REST API Endpoint'leri:

| Method | Endpoint | Açıklama | Handler | Ne Zaman Çalışır | Response |
|--------|----------|----------|---------|------------------|----------|
| GET | `/api/baskets/{userName}` | Sepeti getirir | `GetBasketHandler` | Sepet görüntülenirken | `200 OK` + ShoppingCartDto |
| POST | `/api/baskets` | Sepeti kaydeder/günceller | `StoreBasketHandler` | Ürün ekleme/güncelleme | `200 OK` + ShoppingCartDto |
| DELETE | `/api/baskets/{userName}` | Sepeti siler | `DeleteBasketHandler` | Manuel sepet silme | `204 No Content` veya `404 Not Found` |
| POST | `/api/baskets/checkout` | Checkout yapar | `CheckoutBasketHandler` | Sipariş tamamlanırken | `200 OK` veya `400 Bad Request` |

**Handler Açıklamaları:**
- **GetBasketHandler:** Sepeti Redis'ten alır, indirim hesaplar, DTO'ya map eder (Query - Okuma)
- **StoreBasketHandler:** Sepeti PostgreSQL'e kaydeder, Redis'e cache'ler (Command - Yazma)
- **DeleteBasketHandler:** Sepeti PostgreSQL'den ve Redis'ten siler (Command - Yazma)
- **CheckoutBasketHandler:** RabbitMQ'ya event gönderir, sepeti siler (Command - Yazma + Event)

### Önemli Kavramlar:

#### Controller Pattern:
- **ApiController:** Automatic model validation
- **Route:** Endpoint pattern tanımlama
- **IMediator:** MediatR entegrasyonu
- **ProducesResponseType:** Swagger dokümantasyonu

#### Exception Handling:
- **GlobalExceptionHandler:** Tüm exception'ları yakalar
- **ProblemDetails:** RFC 7807 formatında hata response'ları
- **UseExceptionHandler:** Exception middleware

#### Health Checks:
- **AddHealthChecks:** Health check sistemi
- **AddRedis:** Redis bağlantı kontrolü
- **MapHealthChecks:** Health check endpoint'i

---

## Faz 5 Tamamlandı - Genel Özet

### Tamamlanan Tüm Fazlar:
1. ✅ Faz 5.1: Basket.API Projesi Oluştur
2. ✅ Faz 5.2: Basket Redis Repository
3. ✅ Faz 5.3: Basket gRPC Client (Discount)
4. ✅ Faz 5.4: Basket CQRS (Commands & Queries)
5. ✅ Faz 5.5: Basket Controller & Entegrasyon

### Basket Service Özellikleri:

#### Teknolojiler:
- **Redis:** Sepet verilerini saklar (in-memory, hızlı)
- **gRPC:** Discount Service'e bağlanır (hızlı, binary)
- **RabbitMQ:** Checkout event'lerini gönderir (async)
- **MediatR:** CQRS pattern implementasyonu
- **FluentValidation:** Request validation
- **AutoMapper:** Object mapping

#### Endpoint'ler:
- `GET /api/baskets/{userName}` → Sepeti getirir (indirim hesaplar)
- `POST /api/baskets` → Sepeti kaydeder/günceller
- `DELETE /api/baskets/{userName}` → Sepeti siler
- `POST /api/baskets/checkout` → Checkout yapar (RabbitMQ event)

#### Özellikler:
- ✅ Sepet CRUD işlemleri
- ✅ İndirim hesaplama (gRPC ile)
- ✅ Checkout işlemi (RabbitMQ event)
- ✅ Global exception handling
- ✅ Health checks (Redis)
- ✅ Swagger dokümantasyonu

---

**Tarih:** Aralık 2024  
**Faz:** Faz 5.1, 5.2, 5.3, 5.4 & 5.5 - Basket.API Projesi, Redis Repository, gRPC Client, CQRS ve Controller  
**Durum:** ✅ Tamamlandı

