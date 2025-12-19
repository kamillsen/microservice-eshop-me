# Faz 5.1, 5.2 & 5.3 - Basket.API Projesi, Redis Repository ve gRPC Client Notları

> Bu dosya, Faz 5.1 (Basket.API Projesi Oluştur), Faz 5.2 (Basket Redis Repository) ve Faz 5.3 (Basket gRPC Client) adım adım yaparken öğrendiklerimi not aldığım dosyadır.
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
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

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

## Faz 5.2: Basket Redis Repository

**Hedef:** Redis ile sepet işlemleri (Get, Save, Delete)

### Redis Nedir?

**Redis** (Remote Dictionary Server), in-memory (bellekte) çalışan bir key-value veritabanıdır.

**Özellikler:**
- Çok hızlı (bellekte çalışır)
- Key-Value yapısı (basit veri yapısı)
- String, List, Set, Hash gibi veri tipleri destekler
- Persistence (kalıcılık) desteği (opsiyonel)

**Neden Redis Kullanıyoruz?**
- Sepet geçici veri (kullanıcı çıkış yapınca silinebilir)
- Çok hızlı okuma/yazma gerekiyor (her sepet işleminde)
- Key-Value yapısı sepet için ideal (`basket:user1` → JSON)
- İlişkisel veritabanına göre çok daha hızlı

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

## Adım 2: BasketRepository Implementation Oluştur

**Dosya:** `Data/BasketRepository.cs`

**Kod:**
```csharp
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

    // ... metodlar
}
```

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

### GetBasket Metodu

```csharp
public async Task<ShoppingCart?> GetBasket(string userName)
{
    var basket = await _database.StringGetAsync($"basket:{userName}");
    
    if (basket.IsNullOrEmpty)
        return null;

    return JsonSerializer.Deserialize<ShoppingCart>(basket!);
}
```

**Adım adım ne yapıyor?**

1. **`StringGetAsync($"basket:{userName}")`**
   - Redis'ten key'i getirir
   - Key format: `basket:user1`, `basket:user2` vb.
   - Dönüş: `RedisValue` (string veya null)

2. **`IsNullOrEmpty` kontrolü**
   - Key yoksa veya boşsa → `null` döner
   - Sepet yoksa hata fırlatmaz, sadece null döner

3. **`JsonSerializer.Deserialize<ShoppingCart>(basket!)`**
   - JSON string'i `ShoppingCart` objesine dönüştürür
   - `System.Text.Json` kullanılıyor (.NET'in built-in kütüphanesi)

**Örnek Akış:**
```
Redis'te:
  Key: "basket:user1"
  Value: '{"UserName":"user1","Items":[{"ProductId":"...","ProductName":"iPhone 15","Quantity":2,"Price":50000}]}'
         ↓
StringGetAsync() → JSON string alır
         ↓
JsonSerializer.Deserialize() → ShoppingCart objesi
         ↓
Return: ShoppingCart { UserName = "user1", Items = [...] }
```

### SaveBasket Metodu

```csharp
public async Task<ShoppingCart> SaveBasket(ShoppingCart basket)
{
    var serializedBasket = JsonSerializer.Serialize(basket);
    await _database.StringSetAsync($"basket:{basket.UserName}", serializedBasket);
    
    _logger.LogInformation("Basket saved for {UserName}", basket.UserName);
    
    return await GetBasket(basket.UserName) ?? basket;
}
```

**Adım adım ne yapıyor?**

1. **`JsonSerializer.Serialize(basket)`**
   - `ShoppingCart` objesini JSON string'e dönüştürür
   - Örnek: `{"UserName":"user1","Items":[...]}`

2. **`StringSetAsync($"basket:{basket.UserName}", serializedBasket)`**
   - Redis'e kaydeder
   - Key: `basket:{userName}`
   - Value: JSON string
   - Key yoksa oluşturur, varsa günceller (upsert)

3. **Loglama**
   - İşlem loglanır (debugging için)

4. **`GetBasket` ile doğrulama**
   - Kaydedilen sepeti tekrar Redis'ten alır
   - Doğrulama için (gerçekten kaydedildi mi?)

**Örnek Akış:**
```
ShoppingCart objesi:
  { UserName = "user1", Items = [...] }
         ↓
JsonSerializer.Serialize() → JSON string
         ↓
StringSetAsync() → Redis'e kaydet
         ↓
Redis'te:
  Key: "basket:user1"
  Value: '{"UserName":"user1","Items":[...]}'
```

**Neden GetBasket ile döndürüyoruz?**
- Redis'in gerçekten kaydettiğini doğrulamak için
- Redis'ten okunan değeri döndürmek daha güvenli
- Eğer GetBasket null dönerse → Orijinal basket'i döndürür (`?? basket`)

### DeleteBasket Metodu

```csharp
public async Task<bool> DeleteBasket(string userName)
{
    var deleted = await _database.KeyDeleteAsync($"basket:{userName}");
    
    if (deleted)
        _logger.LogInformation("Basket deleted for {UserName}", userName);
    
    return deleted;
}
```

**Adım adım ne yapıyor?**

1. **`KeyDeleteAsync($"basket:{userName}")`**
   - Redis'ten key'i siler
   - Dönüş: `bool` (true = silindi, false = key yoktu)

2. **Loglama**
   - Başarılıysa log kaydı

3. **Return**
   - Silme işleminin başarılı/başarısız olduğunu döner

**Örnek Akış:**
```
KeyDeleteAsync("basket:user1")
         ↓
Redis'ten key silinir
         ↓
Return: true (başarılı) veya false (key yoksa)
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

## Adım 3: Program.cs'de Redis ve Repository Kaydı

**Dosya:** `Program.cs`

**Kod:**
```csharp
using Basket.API.Data;
using StackExchange.Redis;

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(configuration!);
});

// Repository
builder.Services.AddScoped<IBasketRepository, BasketRepository>();
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

### Sonraki Adım:
**Faz 5.4: Basket CQRS - Commands & Queries**
- GetBasketQuery + GetBasketHandler
- StoreBasketCommand + StoreBasketHandler
- DeleteBasketCommand + DeleteBasketHandler
- CheckoutBasketCommand + CheckoutBasketHandler

---

**Tarih:** Aralık 2024  
**Faz:** Faz 5.1, 5.2 & 5.3 - Basket.API Projesi, Redis Repository ve gRPC Client  
**Durum:** ✅ Tamamlandı

