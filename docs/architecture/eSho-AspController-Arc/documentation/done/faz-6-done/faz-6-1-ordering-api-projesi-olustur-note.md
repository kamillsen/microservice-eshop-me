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

---

# Faz 6.2 - Ordering Database & Seed Data Notları

> Bu dosya, Faz 6.2 (Ordering Database & Seed Data) adım adım yaparken öğrendiklerimi not aldığım dosyadır.
> 
> **İçerik:**
> - Adım 1: OrderingDbContext oluştur
> - Adım 2: Program.cs'de DbContext kaydı
> - Adım 3: Program.cs'de Migration uygulama kodu
> - Adım 4: EF Core Migration oluştur

---

## Faz 6.2 Nedir?

**Faz 6.2**, Ordering Service için veritabanı yapısını oluşturur. PostgreSQL'de Orders ve OrderItems tablolarını oluşturmak için EF Core DbContext ve Migration'ları hazırlar.

### Temel İşlevler:
- **OrderingDbContext** → EF Core DbContext (PostgreSQL bağlantısı)
- **Entity Configuration** → Order ve OrderItem entity'lerinin veritabanı konfigürasyonu
- **Migration** → Veritabanı şemasını oluşturan migration dosyaları
- **Auto Migration** → Uygulama başladığında otomatik migration uygulama

### Neden şimdi?
- ✅ Entity'ler hazır (Order, OrderItem)
- ✅ PostgreSQL connection string hazır
- ✅ Artık veritabanı yapısını oluşturabiliriz
- ✅ Migration'lar ile veritabanı şeması yönetilebilir

---

## Adım 1: OrderingDbContext Oluştur

**Dosya:** `Data/OrderingDbContext.cs`

**Kod:**
```csharp
using Ordering.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ordering.API.Data;

public class OrderingDbContext : DbContext
{
    public OrderingDbContext(DbContextOptions<OrderingDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserName);
            entity.Property(e => e.Status)
                .HasConversion<int>(); // Enum'u integer'a çevir
            entity.HasMany(e => e.Items)
                .WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        base.OnModelCreating(modelBuilder);
    }
}
```

**Açıklamalar:**

### Order Entity Configuration:
- `HasKey(e => e.Id)` → Primary key tanımı
- `HasIndex(e => e.UserName)` → UserName'e göre arama performansı için index
- `HasConversion<int>()` → OrderStatus enum'u PostgreSQL'de integer olarak saklanır
- `HasMany(e => e.Items).WithOne(e => e.Order)` → One-to-many ilişki (Order → OrderItems)
- `OnDelete(DeleteBehavior.Cascade)` → Order silinince OrderItems da otomatik silinir

### OrderItem Entity Configuration:
- `HasKey(e => e.Id)` → Primary key tanımı
- Foreign key ilişkisi Order entity konfigürasyonunda tanımlı

**Navigation Property:**
- `OrderItem.Order` → Navigation property var (gerçek projelerdeki gibi)
- EF Core'un `Include()` özelliğini kullanmak için gerekli
- DTO'lar kullanıldığı için döngüsel referans sorunu yok

**Ne işe yarar:**
- PostgreSQL veritabanı bağlantısını sağlar
- Entity'lerin veritabanı konfigürasyonunu tanımlar
- İlişkileri (foreign key, cascade delete) yönetir

**Sonuç:**
- `Data/OrderingDbContext.cs` dosyası oluşturuldu
- Order ve OrderItem entity'leri için konfigürasyon yapıldı

**Kontrol:**
```bash
dotnet build src/Services/Ordering/Ordering.API/Ordering.API.csproj
# Build başarılı olmalı (0 hata, 0 uyarı)
```

---

## Adım 2: Program.cs'de DbContext Kaydı

**Dosya:** `Program.cs`

**Eklenen Kod:**
```csharp
using Ordering.API.Data;
using Microsoft.EntityFrameworkCore;

// PostgreSQL
builder.Services.AddDbContext<OrderingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));
```

**Açıklamalar:**
- `AddDbContext<OrderingDbContext>()` → DbContext'i DI container'a kaydeder
- `UseNpgsql()` → PostgreSQL provider kullanılır
- `GetConnectionString("Database")` → `appsettings.json`'dan connection string okunur
- Scoped lifetime → Her HTTP request için yeni DbContext instance

**Ne işe yarar:**
- DbContext'i dependency injection ile kullanılabilir hale getirir
- Handler'larda `OrderingDbContext` inject edilebilir
- Connection string `appsettings.json`'dan okunur

**Sonuç:**
- `Program.cs` dosyasına DbContext kaydı eklendi
- PostgreSQL bağlantısı yapılandırıldı

**Kontrol:**
```bash
dotnet build src/Services/Ordering/Ordering.API/Ordering.API.csproj
# Build başarılı olmalı
```

---

## Adım 3: Program.cs'de Migration Uygulama Kodu

**Dosya:** `Program.cs`

**Eklenen Kod:**
```csharp
var app = builder.Build();

// Migration
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
    await context.Database.MigrateAsync();
}
```

**Açıklamalar:**
- `app.Services.CreateScope()` → Service scope oluşturur (DbContext için gerekli)
- `GetRequiredService<OrderingDbContext>()` → DbContext'i scope'tan alır
- `MigrateAsync()` → Tüm pending migration'ları otomatik uygular
- Uygulama başladığında çalışır (container başladığında)

**Ne işe yarar:**
- Uygulama başladığında migration'ları otomatik uygular
- Manuel `dotnet ef database update` komutuna gerek kalmaz
- Container-based deployment için ideal

**Not:** Seed data yok (siparişler kullanıcı işlemleriyle oluşur)

**Sonuç:**
- `Program.cs` dosyasına migration kodu eklendi
- Otomatik migration yapılandırıldı

**Kontrol:**
```bash
dotnet build src/Services/Ordering/Ordering.API/Ordering.API.csproj
# Build başarılı olmalı
```

---

## Adım 4: EF Core Migration Oluştur

**Komut:**
```bash
cd src/Services/Ordering/Ordering.API
export DOTNET_ROOT=/usr/lib64/dotnet  # Fedora için (troubleshooting)
dotnet ef migrations add InitialCreate --output-dir Data/Migrations
```

**Açıklamalar:**
- `dotnet ef migrations add` → Yeni migration oluşturur
- `InitialCreate` → Migration adı (ilk migration)
- `--output-dir Data/Migrations` → Migration dosyalarının kaydedileceği klasör
- `export DOTNET_ROOT=/usr/lib64/dotnet` → Fedora için gerekli (dotnet-ef tool sorunu)

**Ne işe yarar:**
- Veritabanı şemasını oluşturan migration dosyalarını oluşturur
- Migration dosyaları `Data/Migrations/` klasörüne kaydedilir
- `MigrateAsync()` ile otomatik uygulanır

**Oluşturulan Dosyalar:**
- `20251221183640_InitialCreate.cs` → Migration dosyası (Up/Down metodları)
- `20251221183640_InitialCreate.Designer.cs` → Migration metadata
- `OrderingDbContextModelSnapshot.cs` → Veritabanı model snapshot

**Migration İçeriği:**
- `Orders` tablosu:
  - `Id` (Guid, PK)
  - `UserName` (string, indexed)
  - `TotalPrice` (decimal)
  - `OrderDate` (DateTime)
  - `Status` (int - enum conversion)
- `OrderItems` tablosu:
  - `Id` (Guid, PK)
  - `OrderId` (Guid, FK → Orders.Id, Cascade Delete)
  - `ProductId` (Guid)
  - `ProductName` (string)
  - `Quantity` (int)
  - `Price` (decimal)

**Troubleshooting:**
- **Sorun:** `dotnet-ef tool .NET runtime bulamıyor`
- **Sebep:** DOTNET_ROOT environment variable yanlış ayarlanmış
- **Çözüm:** `export DOTNET_ROOT=/usr/lib64/dotnet` (Fedora için)
- **Detay:** `docs/architecture/eSho-AspController-Arc/documentation/troubleshooting/dotnet-ef-tool-dotnet-runtime-not-found.md`

**Sonuç:**
- Migration dosyaları oluşturuldu
- `Data/Migrations/` klasörüne kaydedildi

**Kontrol:**
```bash
ls -la src/Services/Ordering/Ordering.API/Data/Migrations/
# Migration dosyalarını görmeli
```

---

## Faz 6.2 Özet

### Tamamlanan Adımlar:
1. ✅ OrderingDbContext oluşturuldu (Order ve OrderItem entity konfigürasyonları)
2. ✅ Program.cs'de DbContext kaydı yapıldı (PostgreSQL)
3. ✅ Program.cs'de Migration uygulama kodu eklendi (otomatik migration)
4. ✅ EF Core Migration oluşturuldu (InitialCreate)

### Kontrol Sonuçları:
- ✅ Build başarılı (0 hata, 0 uyarı)
- ✅ DbContext doğru yapılandırıldı
- ✅ Migration dosyaları oluşturuldu
- ✅ Navigation property eklendi (gerçek projelerdeki gibi)

### Oluşturulan Dosyalar:
- `Data/OrderingDbContext.cs` → EF Core DbContext
- `Data/Migrations/20251221183640_InitialCreate.cs` → Migration dosyası
- `Data/Migrations/20251221183640_InitialCreate.Designer.cs` → Migration metadata
- `Data/Migrations/OrderingDbContextModelSnapshot.cs` → Model snapshot

### Önemli Notlar:

#### Navigation Property:
- `OrderItem.Order` navigation property var (gerçek projelerdeki gibi)
- EF Core'un `Include()` özelliğini kullanmak için gerekli
- DTO'lar kullanıldığı için döngüsel referans sorunu yok
- Catalog.API'deki gibi aynı yaklaşım

#### Cascade Delete:
- Order silinince OrderItems da otomatik silinir
- Veri bütünlüğü için önemli

#### Enum Conversion:
- OrderStatus enum'u PostgreSQL'de integer olarak saklanır
- `HasConversion<int>()` ile yapılır

---

# Faz 6.3 - Ordering CQRS - Commands & Queries Notları

> Bu dosya, Faz 6.3 (Ordering CQRS - Commands & Queries) adım adım yaparken öğrendiklerimi not aldığım dosyadır.
> 
> **İçerik:**
> - Adım 1: MediatR, FluentValidation, AutoMapper konfigürasyonu
> - Adım 2: DTO'ları oluştur
> - Adım 3: AutoMapper MappingProfile oluştur
> - Adım 4: CreateOrderCommand + Handler + Validator
> - Adım 5: UpdateOrderCommand + Handler
> - Adım 6: DeleteOrderCommand + Handler
> - Adım 7: GetOrdersQuery + Handler
> - Adım 8: GetOrderByIdQuery + Handler
> - Adım 9: GetOrdersByUserQuery + Handler

---

## Faz 6.3 Nedir?

**Faz 6.3**, Ordering Service için CQRS pattern'ini uygular. MediatR, FluentValidation ve AutoMapper kullanarak Commands (yazma) ve Queries (okuma) işlemlerini oluşturur.

### Temel İşlevler:
- **Commands** → Yazma işlemleri (CreateOrder, UpdateOrder, DeleteOrder)
- **Queries** → Okuma işlemleri (GetOrders, GetOrderById, GetOrdersByUser)
- **Validation** → FluentValidation ile request doğrulama
- **Mapping** → AutoMapper ile Entity ↔ DTO mapping

### Neden şimdi?
- ✅ DbContext hazır
- ✅ Entity'ler hazır
- ✅ Artık CQRS pattern'ini uygulayabiliriz
- ✅ Handler'lar ile business logic'i organize edebiliriz

---

## Adım 1: MediatR, FluentValidation, AutoMapper Konfigürasyonu

**Dosya:** `Program.cs`

**Eklenen Kod:**
```csharp
using MediatR;
using BuildingBlocks.Behaviors.Behaviors;
using FluentValidation;
using BuildingBlocks.Exceptions.Handler;

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

// Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
```

**Ve middleware:**
```csharp
// Exception Handler Middleware
app.UseExceptionHandler();
```

**Açıklamalar:**

### MediatR:
- `RegisterServicesFromAssembly` → Handler'ları otomatik bulur ve kaydeder
- `AddBehavior<LoggingBehavior>` → Tüm handler'larda otomatik logging
- `AddBehavior<ValidationBehavior>` → FluentValidation ile otomatik validation

### FluentValidation:
- `AddValidatorsFromAssembly` → Validator'ları otomatik bulur ve kaydeder
- ValidationBehavior pipeline'da çalışır

### AutoMapper:
- `AddAutoMapper` → MappingProfile'ları otomatik bulur ve kaydeder
- Entity ↔ DTO mapping için

### Exception Handler:
- `GlobalExceptionHandler` → Tüm exception'ları yakalar ve ProblemDetails döner
- `UseExceptionHandler()` → Middleware olarak eklenir

**Ne işe yarar:**
- CQRS pattern için gerekli servisleri kaydeder
- Pipeline behaviors ile otomatik logging ve validation
- Exception handling ile merkezi hata yönetimi

**Sonuç:**
- `Program.cs` dosyasına servis kayıtları eklendi
- CQRS pattern yapılandırıldı

**Kontrol:**
```bash
dotnet build src/Services/Ordering/Ordering.API/Ordering.API.csproj
# Build başarılı olmalı
```

---

## Adım 2: DTO'ları Oluştur

**Dosyalar:**
- `Dtos/OrderDto.cs`
- `Dtos/OrderItemDto.cs`

### OrderDto.cs

**Dosya:** `Dtos/OrderDto.cs`

**Kod:**
```csharp
namespace Ordering.API.Dtos;

public class OrderDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = default!;
    public decimal TotalPrice { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = default!;
    public List<OrderItemDto> Items { get; set; } = new();
}
```

**Açıklamalar:**
- `Status` → string (enum'dan string'e çevrilecek)
- `Items` → OrderItemDto listesi

**Ne işe yarar:**
- API response'ları için DTO
- Entity'ler direkt serialize edilmez (döngüsel referans sorunu olmaz)

### OrderItemDto.cs

**Dosya:** `Dtos/OrderItemDto.cs`

**Kod:**
```csharp
namespace Ordering.API.Dtos;

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

**Açıklamalar:**
- OrderItem entity'sinin DTO versiyonu
- Navigation property yok (Order referansı yok)

**Ne işe yarar:**
- OrderItem verilerini API'de döndürmek için
- OrderDto içinde kullanılır

**Sonuç:**
- `Dtos/OrderDto.cs` oluşturuldu
- `Dtos/OrderItemDto.cs` oluşturuldu

**Kontrol:**
```bash
dotnet build src/Services/Ordering/Ordering.API/Ordering.API.csproj
# Build başarılı olmalı
```

---

## Adım 3: AutoMapper MappingProfile Oluştur

**Dosya:** `Mapping/MappingProfile.cs`

**Kod:**
```csharp
using AutoMapper;
using Ordering.API.Dtos;
using Ordering.API.Entities;
using Ordering.API.Features.Orders.Commands.CreateOrder;

namespace Ordering.API.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Entity ↔ DTO
        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        CreateMap<OrderItem, OrderItemDto>().ReverseMap();

        // Command → Entity
        CreateMap<CreateOrderCommand, Order>()
            .ForMember(dest => dest.Items, opt => opt.Ignore()); // Items manuel eklenir

        CreateMap<OrderItemDto, OrderItem>();
    }
}
```

**Açıklamalar:**
- `Order.Status` → Enum'u string'e çevirir (DTO'da string)
- `OrderItem ↔ OrderItemDto` → ReverseMap ile iki yönlü mapping
- `CreateOrderCommand → Order` → Items ignore edilir (manuel eklenir)
- `OrderItemDto → OrderItem` → OrderItem mapping

**Not:** `BasketCheckoutEvent → CreateOrderCommand` mapping'i 6.4'te eklenecek (RabbitMQ Consumer)

**Ne işe yarar:**
- Entity ↔ DTO mapping
- Command → Entity mapping
- Enum → string conversion

**Sonuç:**
- `Mapping/MappingProfile.cs` oluşturuldu
- Mapping konfigürasyonları yapıldı

**Kontrol:**
```bash
dotnet build src/Services/Ordering/Ordering.API/Ordering.API.csproj
# Build başarılı olmalı
```

---

## Adım 4: CreateOrderCommand + Handler + Validator

### CreateOrderCommand.cs

**Dosya:** `Features/Orders/Commands/CreateOrder/CreateOrderCommand.cs`

**Kod:**
```csharp
using MediatR;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Commands.CreateOrder;

public record CreateOrderCommand(
    string UserName,
    decimal TotalPrice,
    List<OrderItemDto> Items,
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
) : IRequest<Guid>;
```

**Açıklamalar:**
- `IRequest<Guid>` → MediatR command (OrderId döner)
- Tüm sipariş bilgileri (UserName, TotalPrice, Items, Address, Payment)

**Ne işe yarar:**
- Yeni sipariş oluşturmak için command
- RabbitMQ Consumer'da kullanılacak (6.4'te)

### CreateOrderValidator.cs

**Dosya:** `Features/Orders/Commands/CreateOrder/CreateOrderValidator.cs`

**Kod:**
```csharp
using FluentValidation;

namespace Ordering.API.Features.Orders.Commands.CreateOrder;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("UserName boş olamaz");

        RuleFor(x => x.TotalPrice)
            .GreaterThan(0).WithMessage("TotalPrice 0'dan büyük olmalı");

        RuleFor(x => x.Items)
            .NotNull().WithMessage("Items null olamaz")
            .NotEmpty().WithMessage("Items boş olamaz");

        RuleFor(x => x.EmailAddress)
            .NotEmpty().EmailAddress().WithMessage("Geçerli email adresi gerekli");

        RuleFor(x => x.AddressLine)
            .NotEmpty().WithMessage("Adres boş olamaz");
    }
}
```

**Açıklamalar:**
- FluentValidation ile request doğrulama
- ValidationBehavior pipeline'da otomatik çalışır

**Ne işe yarar:**
- Geçersiz request'leri yakalar
- Hata mesajları döner

### CreateOrderHandler.cs

**Dosya:** `Features/Orders/Commands/CreateOrder/CreateOrderHandler.cs`

**Kod:**
```csharp
using AutoMapper;
using MediatR;
using Ordering.API.Data;
using Ordering.API.Entities;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Commands.CreateOrder;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly OrderingDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(
        OrderingDbContext context,
        IMapper mapper,
        ILogger<CreateOrderHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Command'dan Entity oluştur
        var order = _mapper.Map<Order>(request);
        order.Id = Guid.NewGuid();
        order.OrderDate = DateTime.UtcNow;
        order.Status = OrderStatus.Pending;

        // 2. OrderItems'ları ekle
        foreach (var itemDto in request.Items)
        {
            var orderItem = _mapper.Map<OrderItem>(itemDto);
            orderItem.Id = Guid.NewGuid();
            orderItem.OrderId = order.Id;
            order.Items.Add(orderItem);
        }

        // 3. Veritabanına kaydet
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order created. OrderId: {OrderId}, UserName: {UserName}, TotalPrice: {TotalPrice}",
            order.Id, order.UserName, order.TotalPrice);

        return order.Id;
    }
}
```

**Açıklamalar:**
- Command'dan Entity oluşturur (AutoMapper)
- OrderItems'ları manuel ekler (foreach ile)
- Veritabanına kaydeder
- Logging yapar

**Ne işe yarar:**
- Yeni sipariş oluşturur
- OrderItems'ları ekler
- OrderId döner

**Sonuç:**
- `CreateOrderCommand.cs` oluşturuldu
- `CreateOrderValidator.cs` oluşturuldu
- `CreateOrderHandler.cs` oluşturuldu

**Kontrol:**
```bash
dotnet build src/Services/Ordering/Ordering.API/Ordering.API.csproj
# Build başarılı olmalı
```

---

## Adım 5: UpdateOrderCommand + Handler

### UpdateOrderCommand.cs

**Dosya:** `Features/Orders/Commands/UpdateOrder/UpdateOrderCommand.cs`

**Kod:**
```csharp
using MediatR;
using Ordering.API.Entities;

namespace Ordering.API.Features.Orders.Commands.UpdateOrder;

public record UpdateOrderCommand(
    Guid Id,
    OrderStatus Status
) : IRequest<bool>;
```

**Açıklamalar:**
- Sadece Status güncellenir (Admin için)
- `IRequest<bool>` → Başarılı/başarısız döner

**Ne işe yarar:**
- Sipariş durumunu günceller (Pending → Shipped → Delivered)

### UpdateOrderHandler.cs

**Dosya:** `Features/Orders/Commands/UpdateOrder/UpdateOrderHandler.cs`

**Kod:**
```csharp
using MediatR;
using Ordering.API.Data;
using Ordering.API.Entities;

namespace Ordering.API.Features.Orders.Commands.UpdateOrder;

public class UpdateOrderHandler : IRequestHandler<UpdateOrderCommand, bool>
{
    private readonly OrderingDbContext _context;
    private readonly ILogger<UpdateOrderHandler> _logger;

    public UpdateOrderHandler(
        OrderingDbContext context,
        ILogger<UpdateOrderHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (order == null)
        {
            _logger.LogWarning("Order not found. OrderId: {OrderId}", request.Id);
            return false;
        }

        order.Status = request.Status;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order updated. OrderId: {OrderId}, NewStatus: {Status}",
            request.Id, request.Status);

        return true;
    }
}
```

**Açıklamalar:**
- Order'ı bulur
- Status'u günceller
- Logging yapar

**Ne işe yarar:**
- Admin sipariş durumunu günceller

**Sonuç:**
- `UpdateOrderCommand.cs` oluşturuldu
- `UpdateOrderHandler.cs` oluşturuldu

**Kontrol:**
```bash
dotnet build src/Services/Ordering/Ordering.API/Ordering.API.csproj
# Build başarılı olmalı
```

---

## Adım 6: DeleteOrderCommand + Handler

### DeleteOrderCommand.cs

**Dosya:** `Features/Orders/Commands/DeleteOrder/DeleteOrderCommand.cs`

**Kod:**
```csharp
using MediatR;

namespace Ordering.API.Features.Orders.Commands.DeleteOrder;

public record DeleteOrderCommand(Guid Id) : IRequest<bool>;
```

**Açıklamalar:**
- Sipariş ID'si alır
- `IRequest<bool>` → Başarılı/başarısız döner

**Ne işe yarar:**
- Siparişi iptal etmek için command

### DeleteOrderHandler.cs

**Dosya:** `Features/Orders/Commands/DeleteOrder/DeleteOrderHandler.cs`

**Kod:**
```csharp
using MediatR;
using Ordering.API.Data;
using Ordering.API.Entities;

namespace Ordering.API.Features.Orders.Commands.DeleteOrder;

public class DeleteOrderHandler : IRequestHandler<DeleteOrderCommand, bool>
{
    private readonly OrderingDbContext _context;
    private readonly ILogger<DeleteOrderHandler> _logger;

    public DeleteOrderHandler(
        OrderingDbContext context,
        ILogger<DeleteOrderHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (order == null)
        {
            _logger.LogWarning("Order not found. OrderId: {OrderId}", request.Id);
            return false;
        }

        // Siparişi silme, sadece iptal et
        order.Status = OrderStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order cancelled. OrderId: {OrderId}", request.Id);

        return true;
    }
}
```

**Açıklamalar:**
- Siparişi silmez, sadece `Cancelled` status'u verir
- Veri bütünlüğü için (raporlama, analiz)

**Ne işe yarar:**
- Siparişi iptal eder (soft delete)

**Sonuç:**
- `DeleteOrderCommand.cs` oluşturuldu
- `DeleteOrderHandler.cs` oluşturuldu

**Kontrol:**
```bash
dotnet build src/Services/Ordering/Ordering.API/Ordering.API.csproj
# Build başarılı olmalı
```

---

## Adım 7: GetOrdersQuery + Handler

### GetOrdersQuery.cs

**Dosya:** `Features/Orders/Queries/GetOrders/GetOrdersQuery.cs`

**Kod:**
```csharp
using MediatR;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Queries.GetOrders;

public record GetOrdersQuery : IRequest<IEnumerable<OrderDto>>;
```

**Açıklamalar:**
- Parametre yok (tüm siparişler)
- `IRequest<IEnumerable<OrderDto>>` → OrderDto listesi döner

**Ne işe yarar:**
- Tüm siparişleri getirir (Admin için)

### GetOrdersHandler.cs

**Dosya:** `Features/Orders/Queries/GetOrders/GetOrdersHandler.cs`

**Kod:**
```csharp
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ordering.API.Data;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Queries.GetOrders;

public class GetOrdersHandler : IRequestHandler<GetOrdersQuery, IEnumerable<OrderDto>>
{
    private readonly OrderingDbContext _context;
    private readonly IMapper _mapper;

    public GetOrdersHandler(OrderingDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<OrderDto>>(orders);
    }
}
```

**Açıklamalar:**
- `Include(o => o.Items)` → Navigation property ile OrderItems yüklenir
- `OrderByDescending` → En yeni siparişler önce
- AutoMapper ile DTO'ya map edilir

**Ne işe yarar:**
- Tüm siparişleri getirir (Admin için)

**Sonuç:**
- `GetOrdersQuery.cs` oluşturuldu
- `GetOrdersHandler.cs` oluşturuldu

**Kontrol:**
```bash
dotnet build src/Services/Ordering/Ordering.API/Ordering.API.csproj
# Build başarılı olmalı
```

---

## Adım 8: GetOrderByIdQuery + Handler

### GetOrderByIdQuery.cs

**Dosya:** `Features/Orders/Queries/GetOrderById/GetOrderByIdQuery.cs`

**Kod:**
```csharp
using MediatR;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Queries.GetOrderById;

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto?>;
```

**Açıklamalar:**
- Order ID alır
- `IRequest<OrderDto?>` → OrderDto veya null döner

**Ne işe yarar:**
- Belirli bir siparişi getirir

### GetOrderByIdHandler.cs

**Dosya:** `Features/Orders/Queries/GetOrderById/GetOrderByIdHandler.cs`

**Kod:**
```csharp
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ordering.API.Data;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Queries.GetOrderById;

public class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly OrderingDbContext _context;
    private readonly IMapper _mapper;

    public GetOrderByIdHandler(OrderingDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order == null)
            return null;

        return _mapper.Map<OrderDto>(order);
    }
}
```

**Açıklamalar:**
- `Include(o => o.Items)` → Navigation property ile OrderItems yüklenir
- `FirstOrDefaultAsync` → Order bulunamazsa null döner
- AutoMapper ile DTO'ya map edilir

**Ne işe yarar:**
- Belirli bir siparişi getirir

**Sonuç:**
- `GetOrderByIdQuery.cs` oluşturuldu
- `GetOrderByIdHandler.cs` oluşturuldu

**Kontrol:**
```bash
dotnet build src/Services/Ordering/Ordering.API/Ordering.API.csproj
# Build başarılı olmalı
```

---

## Adım 9: GetOrdersByUserQuery + Handler

### GetOrdersByUserQuery.cs

**Dosya:** `Features/Orders/Queries/GetOrdersByUser/GetOrdersByUserQuery.cs`

**Kod:**
```csharp
using MediatR;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Queries.GetOrdersByUser;

public record GetOrdersByUserQuery(string UserName) : IRequest<IEnumerable<OrderDto>>;
```

**Açıklamalar:**
- UserName alır
- `IRequest<IEnumerable<OrderDto>>` → OrderDto listesi döner

**Ne işe yarar:**
- Kullanıcıya ait siparişleri getirir

### GetOrdersByUserHandler.cs

**Dosya:** `Features/Orders/Queries/GetOrdersByUser/GetOrdersByUserHandler.cs`

**Kod:**
```csharp
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ordering.API.Data;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Queries.GetOrdersByUser;

public class GetOrdersByUserHandler : IRequestHandler<GetOrdersByUserQuery, IEnumerable<OrderDto>>
{
    private readonly OrderingDbContext _context;
    private readonly IMapper _mapper;

    public GetOrdersByUserHandler(OrderingDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<OrderDto>> Handle(GetOrdersByUserQuery request, CancellationToken cancellationToken)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserName == request.UserName)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<OrderDto>>(orders);
    }
}
```

**Açıklamalar:**
- `Include(o => o.Items)` → Navigation property ile OrderItems yüklenir
- `Where(o => o.UserName == request.UserName)` → Kullanıcıya göre filtreleme
- `OrderByDescending` → En yeni siparişler önce
- AutoMapper ile DTO'ya map edilir

**Ne işe yarar:**
- Kullanıcıya ait siparişleri getirir

**Sonuç:**
- `GetOrdersByUserQuery.cs` oluşturuldu
- `GetOrdersByUserHandler.cs` oluşturuldu

**Kontrol:**
```bash
dotnet build src/Services/Ordering/Ordering.API/Ordering.API.csproj
# Build başarılı olmalı
```

---

## Faz 6.3 Özet

### Tamamlanan Adımlar:
1. ✅ MediatR, FluentValidation, AutoMapper konfigürasyonu (Program.cs)
2. ✅ DTO'lar oluşturuldu (OrderDto, OrderItemDto)
3. ✅ AutoMapper MappingProfile oluşturuldu
4. ✅ CreateOrderCommand + Handler + Validator
5. ✅ UpdateOrderCommand + Handler
6. ✅ DeleteOrderCommand + Handler
7. ✅ GetOrdersQuery + Handler
8. ✅ GetOrderByIdQuery + Handler
9. ✅ GetOrdersByUserQuery + Handler

### Kontrol Sonuçları:
- ✅ Build başarılı (0 hata, 0 uyarı)
- ✅ Tüm handler'lar oluşturuldu
- ✅ Validation kuralları doğru
- ✅ Mapping doğru yapılandırıldı
- ✅ Navigation property ile Include() kullanılıyor

### Oluşturulan Dosyalar:

#### DTO'lar:
- `Dtos/OrderDto.cs` → Order DTO
- `Dtos/OrderItemDto.cs` → OrderItem DTO

#### Mapping:
- `Mapping/MappingProfile.cs` → AutoMapper profile

#### Commands:
- `Features/Orders/Commands/CreateOrder/CreateOrderCommand.cs`
- `Features/Orders/Commands/CreateOrder/CreateOrderHandler.cs`
- `Features/Orders/Commands/CreateOrder/CreateOrderValidator.cs`
- `Features/Orders/Commands/UpdateOrder/UpdateOrderCommand.cs`
- `Features/Orders/Commands/UpdateOrder/UpdateOrderHandler.cs`
- `Features/Orders/Commands/DeleteOrder/DeleteOrderCommand.cs`
- `Features/Orders/Commands/DeleteOrder/DeleteOrderHandler.cs`

#### Queries:
- `Features/Orders/Queries/GetOrders/GetOrdersQuery.cs`
- `Features/Orders/Queries/GetOrders/GetOrdersHandler.cs`
- `Features/Orders/Queries/GetOrderById/GetOrderByIdQuery.cs`
- `Features/Orders/Queries/GetOrderById/GetOrderByIdHandler.cs`
- `Features/Orders/Queries/GetOrdersByUser/GetOrdersByUserQuery.cs`
- `Features/Orders/Queries/GetOrdersByUser/GetOrdersByUserHandler.cs`

### Önemli Notlar:

#### CQRS Pattern:
- **Commands** → Yazma işlemleri (CreateOrder, UpdateOrder, DeleteOrder)
- **Queries** → Okuma işlemleri (GetOrders, GetOrderById, GetOrdersByUser)
- Her feature kendi klasöründe (Vertical Slice Architecture)

#### Validation:
- FluentValidation ile request doğrulama
- ValidationBehavior pipeline'da otomatik çalışır
- Geçersiz request'ler otomatik yakalanır

#### Mapping:
- AutoMapper ile Entity ↔ DTO mapping
- Enum → string conversion (OrderStatus)
- Command → Entity mapping

#### Navigation Property:
- `Include(o => o.Items)` ile OrderItems yüklenir
- DTO'lar kullanıldığı için döngüsel referans sorunu yok

#### Soft Delete:
- DeleteOrderHandler siparişi silmez, sadece `Cancelled` status'u verir
- Veri bütünlüğü için (raporlama, analiz)

#### Eksik (6.4'te eklenecek):
- `BasketCheckoutEvent → CreateOrderCommand` mapping'i (MappingProfile'da)
- RabbitMQ Consumer (BasketCheckoutConsumer)

---

**Tarih:** Aralık 2024  
**Faz:** Faz 6.2 - Ordering Database & Seed Data, Faz 6.3 - Ordering CQRS - Commands & Queries  
**Durum:** ✅ Tamamlandı

