# Faz 5.1 - Basket.API Projesi Oluştur Notları

> Bu dosya, Faz 5.1 (Basket.API Projesi Oluştur) adım adım yaparken öğrendiklerimi not aldığım dosyadır.
> 
> **İçerik:**
> - Adım 1: Basket klasör yapısını oluştur
> - Adım 2: Web API projesi oluştur
> - Adım 3: Projeyi solution'a ekle
> - Adım 4: NuGet paketlerini ekle
> - Adım 5: Project References ekle
> - Adım 6: Klasör yapısını oluştur
> - Adım 7: ShoppingCart ve ShoppingCartItem Entity'lerini oluştur
> - Adım 8: appsettings.json'a connection string ve ayarları ekle

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

### Sonraki Adım:
**Faz 5.2: Basket Redis Repository**
- IBasketRepository interface oluştur
- BasketRepository implementation oluştur
- Program.cs'de Redis ve Repository kaydı

---

**Tarih:** Aralık 2024  
**Faz:** Faz 5.1 - Basket.API Projesi Oluştur  
**Durum:** ✅ Tamamlandı

