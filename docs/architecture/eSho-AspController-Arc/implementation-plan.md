# 📋 E-Shop Microservice Implementation Plan

> **Amaç:** Bu plan, projeye ara verip döndüğünde "şu adımda kaldım" dediğinde, o adımdan devam edebilmen için hazırlanmıştır.
> 
> **Yaklaşım:** Her adım kendi içinde tamamlanabilir ve test edilebilir. Her servisten sonra çalışan bir servis olacak.

---

## 🎯 Genel Strateji

### Yaklaşım Karşılaştırması

| Yaklaşım | Sıralama | Avantaj | Dezavantaj |
|----------|----------|---------|------------|
| **Basitten Karmaşığa** (Öğrenme Odaklı) | Discount → Catalog → Basket → Ordering | ✅ Her adımda yeni teknoloji öğrenilir<br>✅ Hata yapma riski düşük<br>✅ Adım adım ilerleme | ❌ Gerçek kullanım akışına uymaz<br>❌ End-to-end test zor |
| **İş Akışına Göre** (Profesyonel) | Catalog → Discount → Basket → Ordering | ✅ Gerçek kullanım sırası<br>✅ Her adımda end-to-end test<br>✅ Daha gerçekçi | ❌ Basket yaparken Discount hazır olmalı |

### Seçilen Yaklaşım: **İş Akışına Göre** ✅

**Neden?**
- Profesyonel projelerde **gerçek kullanım sırası** önemlidir
- Her servisten sonra **end-to-end test** yapılabilir
- Kullanıcı akışını takip eder: Ürün gör → Sepete ekle → Ödeme yap

**Sıralama:**
1. **Önce altyapı** → Solution, BuildingBlocks, Docker
2. **Catalog Service** → Ürünleri görüntüleme (temel, bağımsız)
3. **Discount Service** → Basket yapmadan önce hazır olmalı
4. **Basket Service** → Catalog'dan ürün alır, Discount'tan indirim sorgular
5. **Ordering Service** → Basket'ten event alır
6. **Son olarak Gateway** → Tüm servisleri birleştir
7. **Her adımda test** → Servis çalışıyor mu + End-to-end test

---

## 📚 Referans Bilgileri

> **Not:** Bu bölüm, her görevde hangi teknolojilerin, paketlerin ve yapıların kullanılacağını belirtir. Kod örnekleri değil, referans bilgileridir.

### Entity Modelleri

#### Product (Catalog Service)
- `Id` (Guid)
- `Name` (string, max 100)
- `Description` (string, nullable)
- `Price` (decimal)
- `ImageUrl` (string, nullable)
- `CategoryId` (Guid, FK)
- `Category` (Navigation Property)

#### Category (Catalog Service)
- `Id` (Guid)
- `Name` (string, max 50)
- `Products` (Collection Navigation Property)

#### Coupon (Discount Service)
- `Id` (int, Identity)
- `ProductName` (string, unique)
- `Description` (string, nullable)
- `Amount` (int, discount amount)

#### ShoppingCart (Basket Service)
- `UserName` (string, key)
- `Items` (List<ShoppingCartItem>)
- `TotalPrice` (decimal, calculated)

#### ShoppingCartItem (Basket Service)
- `ProductId` (Guid)
- `ProductName` (string)
- `Quantity` (int)
- `Price` (decimal)

#### Order (Ordering Service)
- `Id` (Guid)
- `UserName` (string)
- `TotalPrice` (decimal)
- `OrderDate` (DateTime)
- `Status` (enum: Pending, Shipped, Delivered, Cancelled)
- `Items` (Collection Navigation Property)

#### OrderItem (Ordering Service)
- `Id` (Guid)
- `OrderId` (Guid, FK)
- `ProductId` (Guid)
- `ProductName` (string)
- `Quantity` (int)
- `Price` (decimal)

### NuGet Paket Listesi

#### BuildingBlocks.Exceptions
- `Microsoft.AspNetCore.Diagnostics` (ProblemDetails için)

#### BuildingBlocks.Behaviors
- `MediatR` (latest)
- `FluentValidation` (latest)
- `FluentValidation.DependencyInjectionExtensions` (latest)
- `Serilog.AspNetCore` (latest)

#### BuildingBlocks.Messaging
- `MassTransit` (latest)
- `MassTransit.RabbitMQ` (latest)

#### Catalog.API
- `MediatR` (latest)
- `FluentValidation` (latest)
- `FluentValidation.DependencyInjectionExtensions` (latest)
- `AutoMapper` (latest)
- `AutoMapper.Extensions.Microsoft.DependencyInjection` (latest)
- `Microsoft.EntityFrameworkCore` (9.0)
- `Microsoft.EntityFrameworkCore.Design` (9.0)
- `Npgsql.EntityFrameworkCore.PostgreSQL` (latest)
- `AspNetCore.HealthChecks.NpgSql` (latest)
- `BuildingBlocks.Exceptions` (project reference)
- `BuildingBlocks.Behaviors` (project reference)

#### Discount.Grpc
- `Grpc.AspNetCore` (latest)
- `Microsoft.EntityFrameworkCore` (9.0)
- `Microsoft.EntityFrameworkCore.Design` (9.0)
- `Npgsql.EntityFrameworkCore.PostgreSQL` (latest)
- `AspNetCore.HealthChecks.NpgSql` (latest)

#### Basket.API
- `MediatR` (latest)
- `FluentValidation` (latest)
- `FluentValidation.DependencyInjectionExtensions` (latest)
- `AutoMapper` (latest)
- `AutoMapper.Extensions.Microsoft.DependencyInjection` (latest)
- `StackExchange.Redis` (latest)
- `Grpc.Net.Client` (latest)
- `Google.Protobuf` (latest)
- `Grpc.Tools` (latest, PrivateAssets="All")
- `MassTransit` (latest)
- `MassTransit.RabbitMQ` (latest)
- `AspNetCore.HealthChecks.Redis` (latest)
- `BuildingBlocks.Exceptions` (project reference)
- `BuildingBlocks.Behaviors` (project reference)
- `BuildingBlocks.Messaging` (project reference)

#### Ordering.API
- `MediatR` (latest)
- `FluentValidation` (latest)
- `FluentValidation.DependencyInjectionExtensions` (latest)
- `AutoMapper` (latest)
- `AutoMapper.Extensions.Microsoft.DependencyInjection` (latest)
- `Microsoft.EntityFrameworkCore` (9.0)
- `Microsoft.EntityFrameworkCore.Design` (9.0)
- `Npgsql.EntityFrameworkCore.PostgreSQL` (latest)
- `MassTransit` (latest)
- `MassTransit.RabbitMQ` (latest)
- `AspNetCore.HealthChecks.NpgSql` (latest)
- `AspNetCore.HealthChecks.RabbitMQ` (latest)
- `BuildingBlocks.Exceptions` (project reference)
- `BuildingBlocks.Behaviors` (project reference)
- `BuildingBlocks.Messaging` (project reference)

#### Gateway.API
- `Yarp.ReverseProxy` (latest)
- `AspNetCore.HealthChecks.Uris` (latest)

### Connection String Formatları

#### PostgreSQL
```
Host={hostname};Port={port};Database={database};Username={username};Password={password}
```
**Örnek:** `Host=catalogdb;Port=5432;Database=CatalogDb;Username=postgres;Password=postgres`

#### Redis
```
{hostname}:{port}
```
**Örnek:** `basketdb:6379`

#### RabbitMQ (MassTransit)
```
amqp://{username}:{password}@{hostname}:{port}
```
**Örnek:** `amqp://guest:guest@messagebroker:5672`

### appsettings.json Yapısı

#### Catalog.API / Ordering.API
```json
{
  "ConnectionStrings": {
    "Database": "..."
  },
  "Logging": { ... },
  "AllowedHosts": "*"
}
```

#### Basket.API
```json
{
  "ConnectionStrings": {
    "Redis": "..."
  },
  "GrpcSettings": {
    "DiscountUrl": "http://discount.grpc:8080"
  },
  "MessageBroker": {
    "Host": "..."
  },
  "Logging": { ... }
}
```

#### Discount.Grpc
```json
{
  "ConnectionStrings": {
    "Database": "..."
  },
  "Logging": { ... }
}
```

#### Gateway.API
```json
{
  "ReverseProxy": {
    "Routes": { ... },
    "Clusters": { ... }
  },
  "Logging": { ... }
}
```

### Proto Dosyası Yapısı (discount.proto)

**Service:**
- `DiscountProtoService` (service name)
- Methods: `GetDiscount`, `CreateDiscount`, `UpdateDiscount`, `DeleteDiscount`

**Messages:**
- `GetDiscountRequest` → `productName` (string)
- `CouponModel` → `id`, `productName`, `description`, `amount`
- `CreateDiscountRequest` → `coupon` (CouponModel)
- `UpdateDiscountRequest` → `coupon` (CouponModel)
- `DeleteDiscountRequest` → `productName` (string)
- `DeleteDiscountResponse` → `success` (bool)

**Namespace:** `Discount.Grpc.Protos`

### Dockerfile Stratejisi

**Multi-stage build:**
1. **build** stage → `mcr.microsoft.com/dotnet/sdk:9.0`
   - Restore packages
   - Build solution
   - Publish

2. **final** stage → `mcr.microsoft.com/dotnet/aspnet:9.0`
   - Copy published files
   - Expose port 8080
   - Entry point: `dotnet {ProjectName}.dll`

**Build context:** Solution root (shared projelere erişim için)

### dotnet CLI Komutları

#### Proje Oluşturma
- Web API: `dotnet new webapi -n {ProjectName}`
- Class Library: `dotnet new classlib -n {ProjectName}`
- gRPC Service: `dotnet new grpc -n {ProjectName}`

#### Paket Ekleme
- `dotnet add package {PackageName}`
- Project reference: `dotnet add reference {ProjectPath}`

#### EF Core Migration
- `dotnet ef migrations add {MigrationName} --project {ProjectPath} --startup-project {StartupProjectPath}`
- `dotnet ef database update --project {ProjectPath} --startup-project {StartupProjectPath}`

#### Build & Run
- `dotnet build`
- `dotnet run --project {ProjectPath}`

---

## 📦 Faz 1: Solution & Altyapı

### 1.1 Solution ve Temel Proje Yapısı
**Hedef:** Solution oluştur, klasör yapısını kur

**Görevler:**
- Solution dosyası oluştur (`dotnet new sln -n EShop`)
- Klasör yapısını oluştur:
  - `src/Services/` (Catalog, Basket, Ordering, Discount için)
  - `src/ApiGateway/` (Gateway için)
  - `src/BuildingBlocks/` (Paylaşılan kod için)
  - `tests/` (Test projeleri için)
- `global.json` kontrol et/oluştur (.NET 9 SDK: `"version": "9.0.112"`)
- `Directory.Build.props` oluştur (ortak MSBuild ayarları: TargetFramework, Nullable, vb.)
- `Directory.Packages.props` oluştur (Central Package Management: tüm paket versiyonları burada)

**Kontrol:** Solution açılıyor mu? (`dotnet sln list`) Klasörler doğru mu?

---

### 1.2 Docker Compose (Sadece Infrastructure)
**Hedef:** Veritabanları, Redis, RabbitMQ container'larını ayağa kaldır

**Görevler:**
- `docker-compose.yml` oluştur
- PostgreSQL container'ları ekle (CatalogDb, OrderingDb, DiscountDb)
- Redis container ekle (Redis Stack + RedisInsight UI)
- RabbitMQ container ekle (Management UI ile)
- Volume'ları tanımla (veri kalıcılığı için)
- `.env` dosyası oluştur (opsiyonel)

**Test:** `docker-compose up -d` → Tüm container'lar çalışıyor mu?
- PostgreSQL: `docker exec -it catalogdb psql -U postgres -d CatalogDb`
- Redis: `docker exec -it basketdb redis-cli ping`
- RabbitMQ: http://localhost:15672 (guest/guest)

**Sonuç:** ✅ Altyapı hazır, servisler için hazırız

---

## 🧱 Faz 2: BuildingBlocks (Paylaşılan Kod)

### 2.1 BuildingBlocks.Exceptions
**Hedef:** Tüm servislerde kullanılacak exception yapısı

**Görevler:**
- Class library projesi oluştur (`dotnet new classlib -n BuildingBlocks.Exceptions`)
- Projeyi solution'a ekle
- **Paketler:** [Referans Bilgileri - BuildingBlocks.Exceptions](#nuget-paket-listesi) bölümündeki tüm paketleri ekle
- `Exceptions/` klasörü oluştur
- `NotFoundException`, `BadRequestException`, `InternalServerException` class'ları (Exception'dan inherit)
- `Handler/GlobalExceptionHandler.cs` middleware oluştur (IExceptionHandler implement et)
- `ProblemDetails` response formatı (RFC 7807 standardı)

**Test:** Proje build oluyor mu? (`dotnet build`)

**Sonuç:** ✅ Exception handling hazır

---

### 2.2 BuildingBlocks.Behaviors
**Hedef:** MediatR Pipeline Behaviors (Validation, Logging)

**Görevler:**
- Class library projesi oluştur (`dotnet new classlib -n BuildingBlocks.Behaviors`)
- Projeyi solution'a ekle
- **Paketler:** [Referans Bilgileri - BuildingBlocks.Behaviors](#nuget-paket-listesi) bölümündeki tüm paketleri ekle
- `ValidationBehavior.cs` oluştur (`IPipelineBehavior<TRequest, TResponse>` implement et, FluentValidation kullan)
- `LoggingBehavior.cs` oluştur (`IPipelineBehavior<TRequest, TResponse>` implement et, Serilog kullan)

**Test:** Proje build oluyor mu? (`dotnet build`)

**Sonuç:** ✅ Pipeline behaviors hazır

---

### 2.3 BuildingBlocks.Messaging
**Hedef:** RabbitMQ + MassTransit için event'ler

**Görevler:**
- Class library projesi oluştur (`dotnet new classlib -n BuildingBlocks.Messaging`)
- Projeyi solution'a ekle
- **Paketler:** [Referans Bilgileri - BuildingBlocks.Messaging](#nuget-paket-listesi) bölümündeki tüm paketleri ekle
- `Events/IntegrationEvent.cs` base class oluştur (record, `Id`, `CreatedAt` property'leri)
- `Events/BasketCheckoutEvent.cs` oluştur (IntegrationEvent'ten inherit, tüm checkout bilgileri)

**Test:** Proje build oluyor mu? (`dotnet build`)

**Sonuç:** ✅ Messaging yapısı hazır

---

## 📦 Faz 3: Catalog Service (REST API)

### Servis Hakkında

**Ne İşe Yarar?**
- E-ticaret sitesindeki ürün ve kategori bilgilerini yönetir
- Kullanıcılar ürünleri görüntüler, arama yapar
- Admin ürün ekler, günceller, siler

**Örnek Kullanım:**
- Kullanıcı: "Elektronik kategorisindeki ürünleri göster"
- Catalog Service: Kategorideki tüm ürünleri döner (iPhone, Samsung, MacBook...)
- Kullanıcı: "iPhone 15'in detaylarını göster"
- Catalog Service: Ürün bilgilerini döner (fiyat, açıklama, resim...)

**Neden önce bu?** 
- ✅ Temel servis, bağımsız (diğer servislere bağımlı değil)
- ✅ Kullanıcı akışının başlangıcı (ürünleri görmek için)
- ✅ CQRS + MediatR öğrenmek için ideal
- ✅ Basket yaparken ürün ID'leri lazım olacak

### 3.1 Catalog.API Projesi Oluştur
**Hedef:** REST API projesi

**Görevler:**
- Web API projesi oluştur (`dotnet new webapi -n Catalog.API`)
- Projeyi solution'a ekle
- **Paketler:** [Referans Bilgileri - Catalog.API](#nuget-paket-listesi) bölümündeki tüm paketleri ekle
- **Project References:** BuildingBlocks.Exceptions, BuildingBlocks.Behaviors
- `CatalogDbContext` oluştur (EF Core, PostgreSQL)
- `Product`, `Category` entity'lerini oluştur ([Entity Modelleri](#entity-modelleri) bölümüne bak)
- `appsettings.json`'a PostgreSQL connection string ekle (format: [Connection String Formatları](#connection-string-formatları))

**Test:** Proje build oluyor mu? (`dotnet build`)

---

### 3.2 Catalog Database & Seed Data
**Hedef:** Veritabanı ve başlangıç verileri

**Görevler:**
- EF Core Migration oluştur (`dotnet ef migrations add InitialCreate`)
- Migration uygula (`dotnet ef database update`)
- `SeedData.cs` oluştur (static class, `InitializeAsync` method)
- Seed içeriği: 3 kategori (Elektronik, Giyim, Ev & Yaşam), her kategoride 2-3 ürün
- `Program.cs`'de seed data çalıştır (app build edildikten sonra, scope oluştur)

**Test:**
- Container'da DB oluştu mu? (`docker exec -it catalogdb psql -U postgres -d CatalogDb`)
- Seed data var mı? (`SELECT * FROM "Categories"`, `SELECT * FROM "Products"`)

---

### 3.3 Catalog CQRS - Products Commands
**Hedef:** Product yazma işlemleri

**Görevler:**
- `CreateProductCommand` + `CreateProductHandler` + `CreateProductValidator`
- `UpdateProductCommand` + `UpdateProductHandler` + `UpdateProductValidator`
- `DeleteProductCommand` + `DeleteProductHandler`
- AutoMapper profile oluştur (Command → Entity, Entity → DTO)

**Test:** Handler'lar çalışıyor mu? (Unit test veya manuel test)

---

### 3.4 Catalog CQRS - Products Queries
**Hedef:** Product okuma işlemleri

**Görevler:**
- `GetProductsQuery` + `GetProductsHandler` (sayfalama, filtreleme)
- `GetProductByIdQuery` + `GetProductByIdHandler`
- `GetProductsByCategoryQuery` + `GetProductsByCategoryHandler`
- DTO'ları oluştur (`ProductDto`, `CreateProductDto`, `UpdateProductDto`)

**Test:** Query'ler çalışıyor mu?

---

### 3.5 Catalog CQRS - Categories
**Hedef:** Category işlemleri

**Görevler:**
- `GetCategoriesQuery` + `GetCategoriesHandler`
- `GetCategoryByIdQuery` + `GetCategoryByIdHandler`
- `CreateCategoryCommand` + `CreateCategoryHandler` + `CreateCategoryValidator`
- Category DTO'ları

**Test:** Category işlemleri çalışıyor mu?

---

### 3.6 Catalog Controllers & MediatR Entegrasyonu
**Hedef:** REST API endpoint'leri

**Görevler:**
- `ProductsController` oluştur (MediatR ile)
- `CategoriesController` oluştur
- Swagger konfigürasyonu
- Exception middleware ekle
- Health checks ekle (PostgreSQL)

**Test:**
- Swagger açılıyor mu? (http://localhost:5001/swagger)
- Endpoint'ler çalışıyor mu?
- Health check çalışıyor mu? (http://localhost:5001/health)

**Sonuç:** ✅ Catalog Service çalışıyor (Port 5001)

---

## 🏷️ Faz 4: Discount Service (gRPC)

### Servis Hakkında

**Ne İşe Yarar?**
- Ürünlere özel indirim kuponlarını yönetir
- Basket Service, sepetteki ürünler için indirim sorgular (gRPC ile)
- Admin yeni indirim kuponu oluşturabilir

**Örnek Kullanım:**
- Kullanıcı sepete "iPhone 15" ekledi
- Basket Service → Discount Service'e gRPC ile sorar: "iPhone 15 için indirim var mı?"
- Discount Service: "Evet, %10 indirim var (5000 TL)"
- Basket Service: Sepet toplamına indirimi uygular

**Neden şimdi?** 
- ✅ Basket yapmadan önce hazır olmalı
- ✅ gRPC öğrenmek için iyi
- ✅ Basit servis, bağımlılığı yok

### 4.1 Discount.Grpc Projesi Oluştur
**Hedef:** gRPC servis projesi

**Görevler:**
- gRPC Server projesi oluştur (`dotnet new grpc -n Discount.Grpc`)
- Projeyi solution'a ekle
- **Paketler:** [Referans Bilgileri - Discount.Grpc](#nuget-paket-listesi) bölümündeki tüm paketleri ekle
- `Protos/discount.proto` dosyası oluştur ([Proto Dosyası Yapısı](#proto-dosyası-yapısı-discountproto) bölümüne bak)
- `DiscountService.cs` oluştur (`DiscountProtoService.DiscountProtoServiceBase`'den inherit)
- `DiscountDbContext` oluştur (EF Core, PostgreSQL)
- `Coupon` entity oluştur ([Entity Modelleri](#entity-modelleri) bölümüne bak)
- `appsettings.json`'a PostgreSQL connection string ekle

**Test:** Proje build oluyor mu? gRPC endpoint'ler generate edildi mi? (`dotnet build`)

---

### 4.2 Discount Database & Seed Data
**Hedef:** Veritabanı ve başlangıç verileri

**Görevler:**
- EF Core Migration oluştur
- Migration uygula
- `SeedData.cs` oluştur (örnek kuponlar)
- `Program.cs`'de seed data çalıştır

**Test:** 
- Container'da DB oluştu mu? (`docker exec -it discountdb psql -U postgres -d DiscountDb`)
- Seed data var mı?

---

### 4.3 Discount gRPC Service Implementation
**Hedef:** gRPC metodlarını implement et

**Görevler:**
- `GetDiscount` implement et
- `CreateDiscount` implement et
- `UpdateDiscount` implement et
- `DeleteDiscount` implement et
- Error handling ekle (RpcException)

**Test:** gRPC servis çalışıyor mu? (Postman veya gRPC client ile test et)

**Sonuç:** ✅ Discount Service çalışıyor (Port 5004)

---

## 🛒 Faz 5: Basket Service (Redis + gRPC Client)

### Servis Hakkında

**Ne İşe Yarar?**
- Kullanıcıların alışveriş sepetini yönetir
- Sepete ürün ekleme, çıkarma, güncelleme
- Sepeti görüntüleme (toplam fiyat, indirimler dahil)
- **Checkout (Ödeme):** Sepeti siparişe dönüştürme

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

### 5.1 Basket.API Projesi Oluştur
**Hedef:** Redis kullanan REST API

**Görevler:**
- Web API projesi oluştur (`dotnet new webapi -n Basket.API`)
- Projeyi solution'a ekle
- **Paketler:** [Referans Bilgileri - Basket.API](#nuget-paket-listesi) bölümündeki tüm paketleri ekle
- **Project References:** BuildingBlocks.Exceptions, BuildingBlocks.Behaviors, BuildingBlocks.Messaging
- `appsettings.json`'a Redis connection string ekle (format: [Connection String Formatları](#connection-string-formatları))
- `appsettings.json`'a `GrpcSettings.DiscountUrl` ekle
- `appsettings.json`'a `MessageBroker.Host` ekle (RabbitMQ)
- `ShoppingCart`, `ShoppingCartItem` entity'lerini oluştur ([Entity Modelleri](#entity-modelleri) bölümüne bak)
- `BasketRepository` interface ve implementation (Redis, JSON serialize/deserialize)

**Test:** Proje build oluyor mu? (`dotnet build`)

---

### 5.2 Basket Redis Repository
**Hedef:** Redis ile sepet işlemleri

**Görevler:**
- `GetBasket` implement et (Redis'ten JSON deserialize)
- `SaveBasket` implement et (Redis'e JSON serialize)
- `DeleteBasket` implement et
- Redis connection test et

**Test:**
- Redis'e bağlanıyor mu?
- Sepet kaydediliyor mu? (`docker exec -it basketdb redis-cli GET "basket:user1"`)

---

### 5.3 Basket gRPC Client (Discount)
**Hedef:** Discount servisine gRPC ile bağlan

**Görevler:**
- `discount.proto` dosyasını kopyala (`Protos/` klasörüne, Discount.Grpc'tan)
- `.csproj` dosyasına proto reference ekle (`<Protobuf Include="Protos/discount.proto" GrpcServices="Client" />`)
- `DiscountGrpcService` oluştur (wrapper class, `DiscountProtoService.DiscountProtoServiceClient` kullan)
- `Program.cs`'de gRPC client konfigürasyonu (`AddGrpcClient<DiscountProtoService.DiscountProtoServiceClient>`)

**Test:**
- gRPC client Discount'a bağlanıyor mu? (servis çalışırken test et)
- İndirim bilgisi alınabiliyor mu? (manuel test veya handler içinde test)

---

### 5.4 Basket CQRS - Commands & Queries
**Hedef:** Sepet işlemleri

**Görevler:**
- `GetBasketQuery` + `GetBasketHandler`
- `StoreBasketCommand` + `StoreBasketHandler` + `StoreBasketValidator`
- `DeleteBasketCommand` + `DeleteBasketHandler`
- `CheckoutBasketCommand` + `CheckoutBasketHandler` (RabbitMQ event publish)
- MassTransit konfigürasyonu (`IPublishEndpoint`)

**Test:**
- Sepet CRUD çalışıyor mu?
- Checkout event RabbitMQ'ya gidiyor mu? (RabbitMQ Management UI'da kontrol et)

---

### 5.5 Basket Controller & Entegrasyon
**Hedef:** REST API endpoint'leri

**Görevler:**
- `BasketsController` oluştur
- Swagger konfigürasyonu
- Exception middleware ekle
- Health checks ekle (Redis)

**Test:**
- Swagger açılıyor mu? (http://localhost:5002/swagger)
- Endpoint'ler çalışıyor mu?
- Health check çalışıyor mu?

**Sonuç:** ✅ Basket Service çalışıyor (Port 5002)

---

## 📋 Faz 6: Ordering Service (RabbitMQ Consumer)

### Servis Hakkında

**Ne İşe Yarar?**
- Kullanıcıların siparişlerini yönetir
- **Otomatik sipariş oluşturma:** Basket checkout event'ini dinler
- Sipariş listesini gösterme
- Sipariş durumunu güncelleme (Admin)

**Örnek Kullanım Senaryosu:**
```
1. Basket Service: Checkout yapıldı → BasketCheckoutEvent RabbitMQ'ya gönderildi

2. Ordering Service: Event'i dinledi (BasketCheckoutConsumer)
   → Event'ten CreateOrderCommand oluştur
   → MediatR ile CreateOrderHandler'ı çağır
   → Siparişi veritabanına kaydet
   → Sipariş numarası oluştur

3. Kullanıcı: "Siparişlerimi göster"
   → Ordering Service: Kullanıcıya ait siparişleri döner
   → Response: [{ orderId, orderDate, totalPrice, status: "Pending" }, ...]
```

**Neden şimdi?** Basket'ten event alacak, en karmaşık servis.

### 6.1 Ordering.API Projesi Oluştur
**Hedef:** RabbitMQ consumer REST API

**Görevler:**
- Web API projesi oluştur (`dotnet new webapi -n Ordering.API`)
- Projeyi solution'a ekle
- **Paketler:** [Referans Bilgileri - Ordering.API](#nuget-paket-listesi) bölümündeki tüm paketleri ekle
- **Project References:** BuildingBlocks.Exceptions, BuildingBlocks.Behaviors, BuildingBlocks.Messaging
- `OrderingDbContext` oluştur (EF Core, PostgreSQL)
- `Order`, `OrderItem` entity'lerini oluştur ([Entity Modelleri](#entity-modelleri) bölümüne bak)
- `appsettings.json`'a PostgreSQL connection string ekle
- `appsettings.json`'a `MessageBroker.Host` ekle (RabbitMQ)

**Test:** Proje build oluyor mu? (`dotnet build`)

---

### 6.2 Ordering Database & Seed Data
**Hedef:** Veritabanı ve başlangıç verileri

**Görevler:**
- EF Core Migration oluştur
- Migration uygula
- `SeedData.cs` oluştur (opsiyonel - sipariş seed data gerekmez)

**Test:** Container'da DB oluştu mu?

---

### 6.3 Ordering CQRS - Commands & Queries
**Hedef:** Sipariş işlemleri

**Görevler:**
- `CreateOrderCommand` + `CreateOrderHandler` + `CreateOrderValidator`
- `UpdateOrderCommand` + `UpdateOrderHandler`
- `GetOrdersQuery` + `GetOrdersHandler`
- `GetOrderByIdQuery` + `GetOrderByIdHandler`
- `GetOrdersByUserQuery` + `GetOrdersByUserHandler`
- DTO'ları oluştur

**Test:** Handler'lar çalışıyor mu?

---

### 6.4 Ordering RabbitMQ Consumer
**Hedef:** BasketCheckoutEvent'i dinle ve sipariş oluştur

**Görevler:**
- `BasketCheckoutConsumer` oluştur (`IConsumer<BasketCheckoutEvent>`)
- Consumer'da `IMediator` kullanarak `CreateOrderCommand` gönder
- MassTransit konfigürasyonu (RabbitMQ consumer)
- AutoMapper profile (Event → Command)

**Test:**
- Consumer RabbitMQ'dan event alıyor mu?
- Event geldiğinde sipariş oluşuyor mu?
- RabbitMQ Management UI'da queue görünüyor mu?

---

### 6.5 Ordering Controller & Entegrasyon
**Hedef:** REST API endpoint'leri

**Görevler:**
- `OrdersController` oluştur
- Swagger konfigürasyonu
- Exception middleware ekle
- Health checks ekle (PostgreSQL + RabbitMQ)

**Test:**
- Swagger açılıyor mu? (http://localhost:5003/swagger)
- Endpoint'ler çalışıyor mu?
- Health check çalışıyor mu?

**Sonuç:** ✅ Ordering Service çalışıyor (Port 5003)

---

## 🚪 Faz 7: API Gateway (YARP)

### Servis Hakkında

**Ne İşe Yarar?**
- Tüm servislere **tek giriş noktası** sağlar
- Kullanıcılar farklı servislerin portlarını bilmek zorunda kalmaz
- Request routing (hangi istek hangi servise gidecek)

**Örnek Kullanım:**
```
Kullanıcı: GET http://localhost:5000/catalog-service/api/products
  ↓
Gateway (YARP): Route'u kontrol et → Catalog.API'ye yönlendir
  ↓
Catalog.API: Response döner
  ↓
Gateway: Response'u kullanıcıya iletir
```

**Neden son?** Tüm servisler hazır olmalı.

### 7.1 Gateway.API Projesi Oluştur
**Hedef:** YARP reverse proxy

**Görevler:**
- Web API projesi oluştur (`dotnet new webapi -n Gateway.API`)
- Projeyi solution'a ekle
- **Paketler:** [Referans Bilgileri - Gateway.API](#nuget-paket-listesi) bölümündeki tüm paketleri ekle
- `appsettings.json`'da route ve cluster konfigürasyonu ([appsettings.json Yapısı](#appsettingsjson-yapısı) bölümüne bak)
- `Program.cs`'de YARP middleware ekle (`AddReverseProxy`, `MapReverseProxy`)

**Test:** Proje build oluyor mu? (`dotnet build`)

---

### 7.2 YARP Routing Konfigürasyonu
**Hedef:** Servisleri route'la

**Görevler:**
- Catalog route: `/catalog-service/**` → `http://catalog.api:8080`
- Basket route: `/basket-service/**` → `http://basket.api:8080`
- Ordering route: `/ordering-service/**` → `http://ordering.api:8080`
- Path transform (prefix kaldırma)

**Test:**
- Gateway çalışıyor mu? (http://localhost:5000)
- Route'lar çalışıyor mu?
  - `GET http://localhost:5000/catalog-service/api/products`
  - `GET http://localhost:5000/basket-service/api/baskets/user1`
  - `GET http://localhost:5000/ordering-service/api/orders`

---

### 7.3 Gateway Health Checks
**Hedef:** Downstream servislerin sağlığını kontrol et

**Görevler:**
- Health checks ekle (Catalog, Basket, Ordering)
- Health check endpoint'leri

**Test:** Health check çalışıyor mu?

**Sonuç:** ✅ API Gateway çalışıyor (Port 5000)

---

## 🐳 Faz 8: Docker Entegrasyonu

### 8.1 Dockerfile'lar Oluştur
**Hedef:** Her servis için Dockerfile

**Görevler:**
- Her servis için `Dockerfile` oluştur (proje klasöründe)
- Multi-stage build kullan ([Dockerfile Stratejisi](#dockerfile-stratejisi) bölümüne bak)
- Build context: Solution root (shared projelere erişim için)
- Port: 8080 (internal)
- Her Dockerfile için: `docker build -f {DockerfilePath} -t {ImageName} .`

**Test:** Her Dockerfile build oluyor mu? (`docker build` komutu çalışıyor mu?)

---

### 8.2 Docker Compose - Servisler
**Hedef:** Tüm servisleri Docker Compose'a ekle

**Görevler:**
- Catalog.API service ekle
- Basket.API service ekle
- Ordering.API service ekle
- Discount.Grpc service ekle
- Gateway.API service ekle
- `depends_on` ve `healthcheck` ekle

**Test:**
- `docker-compose up -d` → Tüm servisler çalışıyor mu?
- `docker-compose ps` → Tüm container'lar healthy mi?

---

### 8.3 End-to-End Test
**Hedef:** Tüm sistem çalışıyor mu?

**Test Senaryoları:**
1. Gateway üzerinden Catalog'a erişim
2. Gateway üzerinden Basket'a erişim
3. Sepet oluştur → Checkout → Sipariş oluştu mu?
4. gRPC çalışıyor mu? (Basket → Discount)
5. RabbitMQ event akışı çalışıyor mu?

**Sonuç:** ✅ Tüm sistem çalışıyor!

---

## 📊 İlerleme Takibi

### Tamamlanan Fazlar
- [ ] Faz 1: Solution & Altyapı
- [ ] Faz 2: BuildingBlocks
- [ ] Faz 3: Catalog Service (İlk servis - ürünleri görüntüleme)
- [ ] Faz 4: Discount Service (Basket için hazır olmalı)
- [ ] Faz 5: Basket Service (Catalog + Discount kullanır)
- [ ] Faz 6: Ordering Service (Basket'ten event alır)
- [ ] Faz 7: API Gateway
- [ ] Faz 8: Docker Entegrasyonu

### Şu Anda Neredeyim?
**Faz:** _______________  
**Görev:** _______________  
**Notlar:** _______________

---

## 💡 Notlar

- Her fazdan sonra **commit** yap (Git)
- Her servisten sonra **test et** (Swagger, Health Check)
- Sorun olursa **önceki fazlara dön**, temel yapıyı kontrol et
- Docker container'ları **volume** ile kalıcı (veri kaybolmasın)

---

## 🔄 Ara Verme & Devam Etme

**Ara vermeden önce:**
1. Hangi fazda olduğunu not al
2. Hangi görevde olduğunu not al
3. Son commit'i yap

**Döndüğünde:**
1. Bu planı aç
2. "Şu anda Faz X, Görev Y'deyim" de
3. O görevden devam et

---

**Son Güncelleme:** Aralık 2024

