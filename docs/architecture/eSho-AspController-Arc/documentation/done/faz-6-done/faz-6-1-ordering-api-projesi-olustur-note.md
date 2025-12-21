# Faz 6.1 - Ordering.API Projesi Oluştur Notları

> Bu dosya, Faz 6.1 (Ordering.API Projesi Oluştur) adım adım yaparken öğrendiklerimi not aldığım dosyadır.
> 
> **İçerik:**
> - Adım 1: Ordering klasör yapısını oluştur
> - Adım 2: Web API projesi oluştur
> - Adım 3: Projeyi solution'a ekle
> - Adım 4-6: NuGet paketlerini ekle
> - Adım 7: Project References ekle
> - Adım 8: Klasör yapısını oluştur
> - Adım 9-10: Order ve OrderItem Entity'lerini oluştur
> - Adım 11-12: appsettings.json dosyalarını yapılandır

---

## Ordering Service Nedir?

**Ordering Service**, kullanıcıların siparişlerini yönetir. Basket Service'ten gelen checkout event'ini dinleyerek otomatik sipariş oluşturur.

### Temel İşlevler:
- **Otomatik sipariş oluşturma:** Basket checkout event'ini dinler (RabbitMQ)
- Sipariş listesini gösterme
- Sipariş detaylarını gösterme
- Kullanıcıya göre sipariş arama
- Sipariş durumunu güncelleme (Admin)
- Sipariş iptal etme

### Neden şimdi?
- ✅ Basket hazır (checkout event gönderiyor)
- ✅ RabbitMQ hazır (event queue çalışıyor)
- ✅ Artık sipariş işlemleri yapılabilir
- ✅ En karmaşık servis (Consumer + CQRS + MediatR)

### Neden RabbitMQ Consumer?
- Basket Service checkout yaptığında asenkron olarak sipariş oluşturulmalı
- Decoupling (Basket Service, Ordering Service'i bilmez)
- Event-driven architecture (loosely coupled)
- Retry mekanizması (event başarısız olursa tekrar dener)
- Scalability (birden fazla Ordering Service instance çalışabilir)

### Neden PostgreSQL?
- Siparişler kalıcı veri (silinmemeli)
- İlişkisel yapı gerekli (Order → OrderItems)
- Sorgulama ve raporlama için ideal
- ACID garantisi (transaction güvenliği)

---

## Adım 1: Ordering Klasör Yapısını Oluştur

**Komut:**
```bash
cd src/Services
mkdir Ordering
cd Ordering
```

**Açıklamalar:**
- `cd src/Services` → Services klasörüne geç
- `mkdir Ordering` → Ordering klasörü oluştur
- `cd Ordering` → Ordering klasörüne geç

**Ne işe yarar:**
- Ordering servisi için klasör oluşturur
- Catalog ve Basket gibi aynı yapıda olacak
- Sonra bu klasöre Web API projesi ekleyeceğiz

**Sonuç:**
- `src/Services/Ordering/` klasörü oluşturuldu

**Kontrol:**
```bash
ls -la src/Services/
# Ordering klasörünü görmeli
```

---

## Adım 2: Web API Projesi Oluştur

**Komut:**
```bash
cd src/Services/Ordering
dotnet new webapi -n Ordering.API
```

**Açıklamalar:**
- `dotnet new webapi` → Web API template'i ile proje oluştur
- `-n Ordering.API` → Proje adı
- Template otomatik olarak:
  - `Controllers/` klasörü oluşturur
  - `Program.cs` dosyası oluşturur
  - `appsettings.json` dosyası oluşturur
  - Swagger konfigürasyonu hazır gelir

**Ne işe yarar:**
- Ordering Service için REST API projesi oluşturur
- Web API projesi = REST endpoint'leri sağlar
- Controller-based API kullanıyoruz (Minimal API değil)

**Neden Web API?**
- REST endpoint'leri için standart ASP.NET Core projesi
- Controller pattern kullanıyoruz (daha organize)
- Swagger desteği otomatik gelir

**Sorun:**
- Template'den gelen `Program.cs` dosyasında `AddOpenApi()` ve `MapOpenApi()` metodları kullanılıyordu
- Ancak projede `Swashbuckle.AspNetCore` paketi kullanılıyor (diğer projelerle tutarlılık için)
- Hata: `error NU1008: Projects that use central package version management should not define the version on the PackageReference items: Microsoft.AspNetCore.OpenApi`

**Çözüm:**
1. `Ordering.API.csproj` dosyasından `Microsoft.AspNetCore.OpenApi` paketi kaldırıldı
2. `Swashbuckle.AspNetCore` paketi eklendi (diğer API projeleriyle aynı)
3. `Program.cs` dosyası Catalog.API ve Basket.API'deki gibi Swashbuckle kullanacak şekilde güncellendi:
   - `AddOpenApi()` → `AddSwaggerGen()` olarak değiştirildi
   - `MapOpenApi()` → `UseSwagger()` ve `UseSwaggerUI()` olarak değiştirildi
4. `Directory.Packages.props` dosyasından `Microsoft.AspNetCore.OpenApi` kaldırıldı (artık kullanılmıyor)

**Sonuç:**
- `src/Services/Ordering/Ordering.API/` klasörü oluşturuldu
- `Ordering.API.csproj` dosyası oluşturuldu
- `Program.cs`, `appsettings.json` gibi temel dosyalar oluşturuldu
- Swagger konfigürasyonu Swashbuckle ile yapılandırıldı

---

## Adım 3: Projeyi Solution'a Ekle

**Komut:**
```bash
cd ../../..
dotnet sln add src/Services/Ordering/Ordering.API/Ordering.API.csproj
```

**Açıklamalar:**
- `cd ../../..` → Proje root dizinine dön (3 seviye yukarı: Ordering.API → Ordering → Services → src → root)
- `dotnet sln add` → Solution'a proje ekle
- `src/Services/Ordering/Ordering.API/Ordering.API.csproj` → Eklenecek proje dosyasının yolu

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
src/Services/Ordering/Ordering.API/Ordering.API.csproj
```

**Sonuç:** ✅ Proje solution'a eklendi

---

## Adım 4-6: NuGet Paketlerini Ekle

**Komutlar:**
```bash
cd src/Services/Ordering/Ordering.API

# Core (Adım 4)
dotnet add package MediatR
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
dotnet add package AutoMapper
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection

# EF Core & PostgreSQL (Adım 5)
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package AspNetCore.HealthChecks.NpgSql

# MassTransit & RabbitMQ (Adım 6)
dotnet add package MassTransit
dotnet add package MassTransit.RabbitMQ
dotnet add package AspNetCore.HealthChecks.RabbitMQ
```

**Açıklamalar:**

### Core Paketler (Adım 4):
- `MediatR` → CQRS pattern için
- `FluentValidation` → Request validation için
- `AutoMapper` → Object mapping için

### EF Core & PostgreSQL Paketler (Adım 5):
- `Microsoft.EntityFrameworkCore` → EF Core ORM
- `Microsoft.EntityFrameworkCore.Design` → EF Core migrations için
- `Npgsql.EntityFrameworkCore.PostgreSQL` → PostgreSQL provider
- `AspNetCore.HealthChecks.NpgSql` → PostgreSQL health check

### MassTransit & RabbitMQ Paketler (Adım 6):
- `MassTransit` → RabbitMQ abstraction
- `MassTransit.RabbitMQ` → RabbitMQ provider (Consumer için)
- `AspNetCore.HealthChecks.RabbitMQ` → RabbitMQ health check

**Ne işe yarar:**
- CQRS pattern için gerekli paketler
- PostgreSQL veritabanı işlemleri için gerekli paketler
- RabbitMQ consumer için gerekli paketler
- Health check için gerekli paketler

**Central Package Management:**
- Paketler `Directory.Packages.props`'tan versiyon alır
- `.csproj` dosyasında versiyon belirtilmez
- Tüm projeler aynı versiyonu kullanır

**Yapılan İşlemler:**
1. `Directory.Packages.props` dosyasına eksik paket versiyonları eklendi:
   - `AspNetCore.HealthChecks.RabbitMQ` (9.0.0)
   - `Microsoft.AspNetCore.OpenApi` (9.0.11) - sonra kaldırıldı (kullanılmıyor)

2. `Ordering.API.csproj` dosyasına paket referansları eklendi (versiyonlar olmadan):
   ```xml
   <ItemGroup>
     <PackageReference Include="AspNetCore.HealthChecks.NpgSql" />
     <PackageReference Include="AspNetCore.HealthChecks.RabbitMQ" />
     <PackageReference Include="AutoMapper" />
     <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" />
     <PackageReference Include="FluentValidation" />
     <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
     <PackageReference Include="MassTransit" />
     <PackageReference Include="MassTransit.RabbitMQ" />
     <PackageReference Include="MediatR" />
     <PackageReference Include="Microsoft.EntityFrameworkCore" />
     <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
       <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
       <PrivateAssets>all</PrivateAssets>
     </PackageReference>
     <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
     <PackageReference Include="Swashbuckle.AspNetCore" />
   </ItemGroup>
   ```

**Sonuç:**
- Tüm paketler eklendi
- `Ordering.API.csproj` dosyası güncellendi
- `Directory.Packages.props` dosyası güncellendi (yeni paketler eklendi)

**Kontrol:**
```bash
dotnet restore src/Services/Ordering/Ordering.API/Ordering.API.csproj
dotnet build src/Services/Ordering/Ordering.API/Ordering.API.csproj
# Build başarılı olmalı (0 hata, 0 uyarı)
```

---

## Adım 7: Project References Ekle

**Komutlar:**
```bash
cd src/Services/Ordering/Ordering.API
dotnet add reference ../../../BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj
dotnet add reference ../../../BuildingBlocks/BuildingBlocks.Behaviors/BuildingBlocks.Behaviors.csproj
dotnet add reference ../../../BuildingBlocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj
```

**Açıklamalar:**
- `BuildingBlocks.Exceptions` → Exception handling için
- `BuildingBlocks.Behaviors` → MediatR pipeline behaviors (Validation, Logging)
- `BuildingBlocks.Messaging` → BasketCheckoutEvent için (Consumer'da kullanılacak)

**Ne işe yarar:**
- BuildingBlocks projelerindeki class'ları kullanabiliriz
- Exception handling, validation, logging gibi ortak işlevler
- BasketCheckoutEvent gibi messaging event'leri

**Sonuç:**
- `Ordering.API.csproj` dosyasına project references eklendi:
  ```xml
  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\BuildingBlocks.Exceptions\BuildingBlocks.Exceptions.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\BuildingBlocks.Behaviors\BuildingBlocks.Behaviors.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\BuildingBlocks.Messaging\BuildingBlocks.Messaging.csproj" />
  </ItemGroup>
  ```

**Kontrol:**
```bash
cat Ordering.API.csproj
# ProjectReference'ları görmeli
```

---

## Adım 8: Klasör Yapısını Oluştur

**Komut:**
```bash
cd src/Services/Ordering/Ordering.API
mkdir -p Entities
mkdir -p Data
mkdir -p Features/Orders/Commands
mkdir -p Features/Orders/Queries
mkdir -p EventHandlers
mkdir -p Dtos
mkdir -p Mapping
```

**Açıklamalar:**
- `mkdir -p` → Klasör oluştur (üst klasörler yoksa onları da oluşturur)
- Her klasör belirli bir sorumluluğa sahip

**Klasör Açıklamaları:**

1. **`Entities/`** → Sipariş entity'leri
   - **`Order.cs`**: Sipariş entity (Id, UserName, TotalPrice, OrderDate, Status, Items)
   - **`OrderItem.cs`**: Sipariş item entity (Id, OrderId FK, ProductId, ProductName, Quantity, Price)
   - **Neden?**: PostgreSQL'de saklanacak veri yapısı

2. **`Data/`** → EF Core DbContext
   - **`OrderingDbContext.cs`**: PostgreSQL DbContext
   - **`Migrations/`**: EF Core migrations
   - **Neden?**: Veritabanı işlemleri için

3. **`Features/Orders/`** → CQRS (Vertical Slice)
   - **`Commands/`**: Yazma işlemleri (CreateOrder, UpdateOrder, DeleteOrder)
   - **`Queries/`**: Okuma işlemleri (GetOrders, GetOrderById, GetOrdersByUser)
   - **Neden?**: CQRS pattern, her feature kendi klasöründe

4. **`EventHandlers/`** → RabbitMQ Consumer
   - **`BasketCheckoutConsumer.cs`**: BasketCheckoutEvent'i dinleyen consumer
   - **Neden?**: RabbitMQ event'lerini işlemek için

5. **`Dtos/`** → Data Transfer Objects
   - **`OrderDto.cs`**: Sipariş DTO
   - **`OrderItemDto.cs`**: Sipariş item DTO
   - **Neden?**: API response'ları için

6. **`Mapping/`** → AutoMapper profiles
   - **`MappingProfile.cs`**: Entity ↔ DTO, Event → Command mapping
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

## Adım 9-10: Order ve OrderItem Entity'lerini Oluştur

**Dosyalar:**
- `Entities/Order.cs`
- `Entities/OrderItem.cs`

### Order.cs

**Dosya:** `Entities/Order.cs`

**Kod:**
```csharp
namespace Ordering.API.Entities;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserName { get; set; } = default!;
    public decimal TotalPrice { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    public List<OrderItem> Items { get; set; } = new();
}

public enum OrderStatus
{
    Pending = 0,
    Shipped = 1,
    Delivered = 2,
    Cancelled = 3
}
```

**Açıklamalar:**
- `Order` → Sipariş (Id, UserName, TotalPrice, OrderDate, Status enum, Items navigation property)
- `OrderStatus` → Enum (Pending, Shipped, Delivered, Cancelled)
- `Items` → Navigation property (OrderItem collection)

**Neden enum?**
- Status değerleri sabit (type-safe)
- Veritabanında integer olarak saklanır
- Kod okunabilirliği artar

**Ne işe yarar:**
- Sipariş verilerini temsil eden entity class
- PostgreSQL'de `Orders` tablosu olarak saklanacak
- EF Core ile ilişkisel veritabanı yapısı

### OrderItem.cs

**Dosya:** `Entities/OrderItem.cs`

**Kod:**
```csharp
namespace Ordering.API.Entities;

public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

**Açıklamalar:**
- `OrderItem` → Sipariş item'ı (Id, OrderId FK, Order navigation, ProductId, ProductName, Quantity, Price)
- `OrderId` → Foreign key (Order'a bağlı)
- `Order` → Navigation property (Order entity'ye referans)

**Ne işe yarar:**
- Sipariş item verilerini temsil eden entity class
- PostgreSQL'de `OrderItems` tablosu olarak saklanacak
- Order ile one-to-many ilişki (bir Order'ın birden fazla OrderItem'ı olabilir)

**Sonuç:**
- `Entities/Order.cs` oluşturuldu (Order + OrderStatus enum)
- `Entities/OrderItem.cs` oluşturuldu

**Kontrol:**
```bash
dotnet build src/Services/Ordering/Ordering.API/Ordering.API.csproj
# Build başarılı olmalı (0 hata, 0 uyarı)
```

---

## Adım 11-12: appsettings.json Dosyalarını Yapılandır

**Dosyalar:**
- `appsettings.json`
- `appsettings.Development.json`

### appsettings.json

**Dosya:** `appsettings.json`

**İçerik:**
```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5435;Database=OrderingDb;Username=postgres;Password=postgres"
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
- `ConnectionStrings:Database` → PostgreSQL bağlantı string'i (localhost:5435 - host port)
- `MessageBroker:Host` → RabbitMQ bağlantı string'i (localhost:5673 - host port)

**Not:** 
- **Localhost'tan bağlanırken:** `localhost:5435`, `localhost:5673`
- **Container network içinde:** `orderingdb:5432`, `amqp://guest:guest@messagebroker:5672`

**Port Kontrolü:**
- PostgreSQL: Port 5435 (diğer servislerle çakışmıyor)
  - Catalog: 5436
  - Discount: 5434
  - Basket: 5437
  - Ordering: 5435 ✅
- RabbitMQ: Port 5673 (Basket ile aynı, doğru - aynı RabbitMQ instance'ını kullanıyorlar)

**Ne işe yarar:**
- PostgreSQL bağlantısı için gerekli
- RabbitMQ consumer için gerekli

### appsettings.Development.json

**Dosya:** `appsettings.Development.json`

**İçerik:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

**Açıklamalar:**
- Development ortamı için log ayarları
- `Microsoft.EntityFrameworkCore` → EF Core SQL sorgularını loglar (debug için)

**Ne işe yarar:**
- Development ortamında daha detaylı loglama
- EF Core SQL sorgularını görmek için

**Sonuç:**
- `appsettings.json` dosyası güncellendi (PostgreSQL + RabbitMQ connection strings)
- `appsettings.Development.json` dosyası oluşturuldu (Development log ayarları)

**Kontrol:**
```bash
cat appsettings.json
# ConnectionStrings, MessageBroker bölümlerini görmeli
```

---

## Faz 6.1 Özet

### Tamamlanan Adımlar:
1. ✅ Ordering klasör yapısı oluşturuldu
2. ✅ Web API projesi oluşturuldu (`Ordering.API`)
3. ✅ Proje solution'a eklendi
4. ✅ NuGet paketleri eklendi (Directory.Packages.props + .csproj)
   - Core: MediatR, FluentValidation, AutoMapper
   - EF Core & PostgreSQL: Microsoft.EntityFrameworkCore, Npgsql.EntityFrameworkCore.PostgreSQL
   - MassTransit & RabbitMQ: MassTransit, MassTransit.RabbitMQ
   - Health Checks: AspNetCore.HealthChecks.NpgSql, AspNetCore.HealthChecks.RabbitMQ
5. ✅ Project References eklendi (BuildingBlocks.Exceptions, Behaviors, Messaging)
6. ✅ Klasör yapısı oluşturuldu (Entities, Data, Features, EventHandlers, Dtos, Mapping)
7. ✅ Entity'ler oluşturuldu (`Order.cs`, `OrderItem.cs`)
8. ✅ appsettings.json dosyaları yapılandırıldı (PostgreSQL + RabbitMQ connection strings)

### Kontrol Sonuçları:
- ✅ Build başarılı (0 hata, 0 uyarı)
- ✅ Tüm klasörler oluşturuldu
- ✅ Entity'ler oluşturuldu ve derlendi
- ✅ appsettings.json dosyaları yapılandırıldı
- ✅ Port çakışması yok (PostgreSQL: 5435, RabbitMQ: 5673)
- ✅ Solution build başarılı

### Oluşturulan Klasör ve Dosyalar:

#### Klasörler:
- `Entities/` → Order ve OrderItem entity'leri
- `Data/` → DbContext ve Migrations için
- `Features/Orders/Commands/` → Command handler'ları için
- `Features/Orders/Queries/` → Query handler'ları için
- `EventHandlers/` → RabbitMQ Consumer için
- `Dtos/` → DTO'lar için
- `Mapping/` → AutoMapper profiles için

#### Dosyalar:
- `Entities/Order.cs` → Order entity + OrderStatus enum
- `Entities/OrderItem.cs` → OrderItem entity
- `appsettings.json` → PostgreSQL + RabbitMQ connection strings
- `appsettings.Development.json` → Development log ayarları

### Önemli Notlar:

#### Central Package Management (CPM):
- Tüm paket versiyonları `Directory.Packages.props` dosyasında yönetiliyor
- `.csproj` dosyalarında versiyon belirtilmez
- Yeni paket eklendiğinde `Directory.Packages.props`'a versiyon eklenmeli

#### Swagger Konfigürasyonu:
- Template'den gelen `Microsoft.AspNetCore.OpenApi` yerine `Swashbuckle.AspNetCore` kullanıldı
- Diğer API projeleriyle (Catalog, Basket) tutarlılık için
- `Program.cs` dosyası Catalog.API ve Basket.API'deki gibi güncellendi

#### Port Yönetimi:
- PostgreSQL portları benzersiz (çakışma yok):
  - Catalog: 5436
  - Discount: 5434
  - Basket: 5437
  - Ordering: 5435 ✅
- RabbitMQ portu: 5673 (Basket ve Ordering aynı RabbitMQ instance'ını kullanıyor - doğru)

---

**Tarih:** Aralık 2024  
**Faz:** Faz 6.1 - Ordering.API Projesi Oluştur  
**Durum:** ✅ Tamamlandı

