# Faz 4 - Discount Service (gRPC)

## Servis Hakkında

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
- ✅ Internal servis (dışarıya açık değil, sadece Basket Service kullanacak)

**Neden gRPC?**
- Basket Service sürekli indirim sorguluyor (her sepet işleminde)
- gRPC çok hızlı (binary format, HTTP/2)
- Internal servis (dışarıya açık değil)
- Yüksek performans gerekiyor

---

## 4.1 Discount.Grpc Projesi Oluştur

**Hedef:** gRPC servis projesi

### Görevler:

#### gRPC Server projesi oluştur
**Ne işe yarar:** Discount Service için gRPC servis projesi oluşturur (REST API değil, gRPC).

```bash
cd src/Services
mkdir Discount
cd Discount
dotnet new grpc -n Discount.Grpc
```

**Açıklama:**
- `grpc` template'i ile proje oluşturulur
- Otomatik olarak `Protos/` klasörü ve örnek proto dosyası oluşturulur
- `Program.cs` gRPC için konfigüre edilir

#### Projeyi solution'a ekle
**Ne işe yarar:** Projeyi solution'a ekler, böylece diğer projeler referans verebilir.

```bash
cd ../../..
dotnet sln add src/Services/Discount/Discount.Grpc/Discount.Grpc.csproj
```

#### NuGet paketlerini ekle
**Ne işe yarar:** EF Core, PostgreSQL ve Health Checks için gerekli paketleri ekler.

```bash
cd src/Services/Discount/Discount.Grpc
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package AspNetCore.HealthChecks.NpgSql
```

**Paketler:**
- `Grpc.AspNetCore` (latest) → gRPC servis desteği (template ile gelir)
- `Microsoft.EntityFrameworkCore` (9.0) → ORM için
- `Microsoft.EntityFrameworkCore.Design` (9.0) → Migration tooling için
- `Npgsql.EntityFrameworkCore.PostgreSQL` (latest) → PostgreSQL provider için
- `AspNetCore.HealthChecks.NpgSql` (latest) → PostgreSQL health check için

**Not:** `Grpc.AspNetCore` paketi gRPC template ile otomatik gelir, tekrar eklemeye gerek yok.

#### Klasör yapısını oluştur
**Ne işe yarar:** Entity, Data, Services ve Protos için klasör yapısını oluşturur.

```bash
mkdir -p Entities
mkdir -p Data
mkdir -p Services
mkdir -p Protos
```

**Klasör Açıklamaları:**

1. **`Entities/`** → Veritabanı tablolarını temsil eden class'lar
   - **Örnek**: `Coupon.cs`
   - Veritabanındaki Coupons tablosuna karşılık gelir
   - **Neden?**: EF Core ile veritabanı işlemleri için gerekli

2. **`Data/`** → Veritabanı bağlantı ve başlangıç verileri
   - **`DiscountDbContext.cs`**: EF Core DbContext (veritabanı bağlantısı)
   - **`SeedData.cs`**: Uygulama başlangıcında örnek veriler (kuponlar)
   - **Neden?**: Veritabanı konfigürasyonu ve örnek veriler için

3. **`Services/`** → gRPC servis implementasyonları
   - **`DiscountService.cs`**: gRPC servis implementasyonu
   - Proto dosyasında tanımlanan servis metodlarını implement eder
   - **Neden?**: gRPC servis metodlarının iş mantığı burada

4. **`Protos/`** → Proto dosyaları (gRPC contract'ları)
   - **`discount.proto`**: Discount servisinin gRPC contract'ı
   - Service, message ve RPC metodlarını tanımlar
   - **Neden?**: gRPC için service ve message tanımları burada

#### Proto dosyası oluştur
**Ne işe yarar:** gRPC servis contract'ını tanımlar (servis metodları ve message tipleri).

**Protos/discount.proto:**

```protobuf
syntax = "proto3";

option csharp_namespace = "Discount.Grpc.Protos";

package discount;

service DiscountProtoService {
  rpc GetDiscount (GetDiscountRequest) returns (CouponModel);
  rpc CreateDiscount (CreateDiscountRequest) returns (CouponModel);
  rpc UpdateDiscount (UpdateDiscountRequest) returns (CouponModel);
  rpc DeleteDiscount (DeleteDiscountRequest) returns (DeleteDiscountResponse);
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

message CreateDiscountRequest {
  CouponModel coupon = 1;
}

message UpdateDiscountRequest {
  CouponModel coupon = 1;
}

message DeleteDiscountRequest {
  string productName = 1;
}

message DeleteDiscountResponse {
  bool success = 1;
}
```

**Açıklama:**
- `syntax = "proto3"` → Proto3 syntax kullanılır
- `option csharp_namespace` → C# namespace'i belirlenir
- `service DiscountProtoService` → gRPC servis adı
- `rpc` metodları → Servis metodları (GetDiscount, CreateDiscount, vb.)
- `message` tipleri → Request/Response mesaj tipleri
- Field numaraları (1, 2, 3...) → Protocol Buffers için önemli (değiştirilmemeli)

**Önemli:** Proto dosyasını `.csproj` dosyasına eklemek gerekir (template ile otomatik eklenebilir, kontrol et):

```xml
<ItemGroup>
  <Protobuf Include="Protos\discount.proto" GrpcServices="Server" />
</ItemGroup>
```

#### Coupon Entity oluştur
**Ne işe yarar:** Veritabanı tablosunu temsil eden Coupon entity class'ını oluşturur.

**Entities/Coupon.cs:**

- `Id` (int, Identity) → Primary key, auto-increment
- `ProductName` (string, unique) → Ürün adı (unique constraint)
- `Description` (string, nullable) → İndirim açıklaması
- `Amount` (int) → İndirim miktarı (TL cinsinden)

**Entity Yapısı:**
```csharp
public class Coupon
{
    public int Id { get; set; }
    public string ProductName { get; set; } = default!;
    public string? Description { get; set; }
    public int Amount { get; set; }
}
```

**Neden int Identity?**
- Discount servisi için Identity (auto-increment) yeterli
- Guid kullanmak gerekmez (internal servis, küçük veri seti)

#### DiscountDbContext oluştur
**Ne işe yarar:** EF Core DbContext'i oluşturur (PostgreSQL bağlantısı, entity konfigürasyonları).

**Data/DiscountDbContext.cs:**

- `Coupons` (DbSet<Coupon>) → Coupons tablosu
- `OnModelCreating` → Entity konfigürasyonları:
  - Table name: `Coupons`
  - `ProductName` unique constraint
  - `Id` Identity (auto-increment)

**DbContext Örneği:**
```csharp
public class DiscountDbContext : DbContext
{
    public DiscountDbContext(DbContextOptions<DiscountDbContext> options) 
        : base(options) { }

    public DbSet<Coupon> Coupons { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Coupon>().HasKey(c => c.Id);
        modelBuilder.Entity<Coupon>().HasIndex(c => c.ProductName).IsUnique();
        // ... diğer konfigürasyonlar
    }
}
```

#### appsettings.json'a connection string ekle
**Ne işe yarar:** PostgreSQL veritabanı bağlantı string'ini ekler.

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5434;Database=DiscountDb;Username=postgres;Password=postgres"
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

**Not:** 
- Localhost'tan bağlanırken port: 5434 (host port)
- Container network içinde: `Host=discountdb;Port=5432;Database=DiscountDb;Username=postgres;Password=postgres`

### Kontrol:
- Proje build oluyor mu? (`dotnet build`)
- Solution'da görünüyor mu? (`dotnet sln list`)
- gRPC endpoint'ler generate edildi mi? (`DiscountProtoService.DiscountProtoServiceBase` class'ı oluştu mu?)

---

## 4.2 Discount Database & Seed Data

**Hedef:** Veritabanı ve başlangıç verileri

### Görevler:

#### EF Core Migration oluştur
**Ne işe yarar:** Veritabanı şemasını oluşturmak için migration dosyası oluşturur.

```bash
cd src/Services/Discount/Discount.Grpc
dotnet ef migrations add InitialCreate --startup-project . --context DiscountDbContext
```

**Açıklama:**
- `InitialCreate` → Migration adı
- `--startup-project .` → Startup proje (Discount.Grpc)
- `--context DiscountDbContext` → DbContext adı

#### Migration uygula
**Ne işe yarar:** Migration'ı veritabanına uygular (Coupons tablosu oluşturulur).

```bash
dotnet ef database update --startup-project . --context DiscountDbContext
```

**Not:** Docker container'da PostgreSQL çalışıyor olmalı (`docker-compose up -d`)

#### SeedData.cs oluştur
**Ne işe yarar:** Uygulama başlangıcında örnek indirim kuponları ekler.

**Data/SeedData.cs:**

- `InitializeAsync` static method → Seed data ekleme metodu
- Koşullu ekleme: Veri varsa tekrar eklemez
- Örnek kuponlar (2-3 adet)

**Seed İçeriği (Örnek):**
- **iPhone 15**: %10 indirim (5000 TL)
- **Samsung S24**: %15 indirim (6750 TL)
- **MacBook Pro**: %5 indirim (5000 TL)

**SeedData Örneği:**
```csharp
public static class SeedData
{
    public static async Task InitializeAsync(DiscountDbContext context)
    {
        if (await context.Coupons.AnyAsync())
            return;

        var coupons = new List<Coupon>
        {
            new() 
            { 
                ProductName = "iPhone 15", 
                Description = "Yılbaşı indirimi", 
                Amount = 5000 
            },
            new() 
            { 
                ProductName = "Samsung S24", 
                Description = "Kış kampanyası", 
                Amount = 6750 
            },
            new() 
            { 
                ProductName = "MacBook Pro", 
                Description = "Öğrenci indirimi", 
                Amount = 5000 
            }
        };

        await context.Coupons.AddRangeAsync(coupons);
        await context.SaveChangesAsync();
    }
}
```

#### Program.cs'de seed data çalıştır
**Ne işe yarar:** Uygulama başlangıcında migration ve seed data'yı otomatik çalıştırır.

**Program.cs güncellemesi:**

```csharp
var app = builder.Build();

// Migration ve Seed Data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DiscountDbContext>();
    
    // 1. Migration uygula
    await context.Database.MigrateAsync();
    
    // 2. Seed data ekle
    await SeedData.InitializeAsync(context);
}

app.Run();
```

**Önemli:** DbContext'i DI container'a eklemek gerekir:

```csharp
builder.Services.AddDbContext<DiscountDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));
```

### Test:
- Container'da DB oluştu mu? (`docker exec -it discountdb psql -U postgres -d DiscountDb`)
- Tablolar var mı? (`\dt` komutu ile tabloları listele)
- Coupons tablosu var mı? (`SELECT * FROM "Coupons"`)
- Seed data var mı? (`SELECT * FROM "Coupons"` → 3 kayıt görünmeli)

---

## 4.3 Discount gRPC Service Implementation

**Hedef:** gRPC metodlarını implement et (GetDiscount, CreateDiscount, UpdateDiscount, DeleteDiscount)

### Görevler:

#### DiscountService.cs oluştur
**Ne işe yarar:** gRPC servis implementasyonunu oluşturur (proto dosyasında tanımlanan metodları implement eder).

**Services/DiscountService.cs:**

- `DiscountProtoService.DiscountProtoServiceBase`'den inherit eder
- Proto dosyasında tanımlanan tüm RPC metodlarını override eder:
  - `GetDiscount` → ProductName'e göre kupon getirir
  - `CreateDiscount` → Yeni kupon oluşturur
  - `UpdateDiscount` → Mevcut kuponu günceller
  - `DeleteDiscount` → Kuponu siler

**DiscountService Yapısı:**
```csharp
public class DiscountService : DiscountProtoService.DiscountProtoServiceBase
{
    private readonly DiscountDbContext _context;
    private readonly ILogger<DiscountService> _logger;

    public DiscountService(
        DiscountDbContext context, 
        ILogger<DiscountService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GetDiscount, CreateDiscount, UpdateDiscount, DeleteDiscount metodları
}
```

#### GetDiscount implement et
**Ne işe yarar:** ProductName'e göre indirim kuponunu getirir (Basket Service tarafından en çok kullanılacak metod).

**Implementasyon:**
- `GetDiscountRequest` alır (ProductName içerir)
- DbContext'ten kuponu sorgular (ProductName'e göre)
- Kupon bulunamazsa `RpcException` fırlatır (StatusCode.NotFound)
- Kupon bulunursa `CouponModel` döner

**GetDiscount Örneği:**
```csharp
public override async Task<CouponModel> GetDiscount(
    GetDiscountRequest request, 
    ServerCallContext context)
{
    var coupon = await _context.Coupons
        .FirstOrDefaultAsync(c => c.ProductName == request.ProductName);
    
    if (coupon == null)
    {
        throw new RpcException(new Status(
            StatusCode.NotFound, 
            $"Discount for {request.ProductName} not found"));
    }

    _logger.LogInformation("Discount retrieved for {ProductName}: {Amount}", 
        coupon.ProductName, coupon.Amount);

    return new CouponModel
    {
        Id = coupon.Id,
        ProductName = coupon.ProductName,
        Description = coupon.Description ?? string.Empty,
        Amount = coupon.Amount
    };
}
```

#### CreateDiscount implement et
**Ne işe yarar:** Yeni indirim kuponu oluşturur (Admin işlemi).

**Implementasyon:**
- `CreateDiscountRequest` alır (CouponModel içerir)
- Entity'yi oluşturur
- DbContext'e ekler
- SaveChangesAsync ile kaydeder
- `CouponModel` döner

**CreateDiscount Örneği:**
```csharp
public override async Task<CouponModel> CreateDiscount(
    CreateDiscountRequest request, 
    ServerCallContext context)
{
    var coupon = new Coupon
    {
        ProductName = request.Coupon.ProductName,
        Description = request.Coupon.Description,
        Amount = request.Coupon.Amount
    };

    _context.Coupons.Add(coupon);
    await _context.SaveChangesAsync();

    _logger.LogInformation("Discount created for {ProductName}: {Amount}", 
        coupon.ProductName, coupon.Amount);

    return new CouponModel
    {
        Id = coupon.Id,
        ProductName = coupon.ProductName,
        Description = coupon.Description ?? string.Empty,
        Amount = coupon.Amount
    };
}
```

#### UpdateDiscount implement et
**Ne işe yarar:** Mevcut indirim kuponunu günceller (Admin işlemi).

**Implementasyon:**
- `UpdateDiscountRequest` alır (CouponModel içerir)
- DbContext'ten kuponu bulur (Id veya ProductName'e göre)
- Kupon bulunamazsa `RpcException` fırlatır
- Kuponu günceller
- SaveChangesAsync ile kaydeder
- `CouponModel` döner

#### DeleteDiscount implement et
**Ne işe yarar:** İndirim kuponunu siler (Admin işlemi).

**Implementasyon:**
- `DeleteDiscountRequest` alır (ProductName içerir)
- DbContext'ten kuponu bulur (ProductName'e göre)
- Kupon bulunamazsa `RpcException` fırlatır
- Remove ile siler
- SaveChangesAsync ile kaydeder
- `DeleteDiscountResponse` döner (success: true)

#### Error handling ekle
**Ne işe yarar:** Hata durumlarında standart gRPC hata response'ları döner.

**RpcException Kullanımı:**
- `StatusCode.NotFound` → Kupon bulunamadığında (404 benzeri)
- `StatusCode.InvalidArgument` → Geçersiz parametre (400 benzeri)
- `StatusCode.Internal` → Beklenmeyen hatalar (500 benzeri)

**Örnek:**
```csharp
if (coupon == null)
{
    throw new RpcException(new Status(
        StatusCode.NotFound, 
        $"Discount for {request.ProductName} not found"));
}
```

#### Program.cs'de gRPC servisi register et
**Ne işe yarar:** gRPC servisini middleware pipeline'a ekler.

**Program.cs güncellemesi:**

```csharp
// DbContext
builder.Services.AddDbContext<DiscountDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// gRPC servisi
builder.Services.AddGrpc();

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database"));

var app = builder.Build();

// gRPC endpoint mapping
app.MapGrpcService<DiscountService>();

// Health check endpoint
app.MapHealthChecks("/health");

app.Run();
```

**Önemli:** 
- `AddGrpc()` → gRPC servisleri için DI configuration
- `MapGrpcService<DiscountService>()` → DiscountService'i gRPC endpoint olarak map eder
- gRPC endpoint'i: `http://localhost:8080` (proto dosyasında tanımlı servis adı kullanılır)

### Test:
- gRPC servis çalışıyor mu? (`dotnet run` ile servisi başlat)
- gRPC endpoint'e erişilebiliyor mu?
- **Postman ile test:**
  - Postman'de gRPC request oluştur
  - Endpoint: `localhost:5004` (external port)
  - Service: `DiscountProtoService`
  - Method: `GetDiscount`
  - Request: `{ "productName": "iPhone 15" }`
  - Response: `{ "id": 1, "productName": "iPhone 15", "description": "Yılbaşı indirimi", "amount": 5000 }`

**Sonuç:** ✅ Discount Service çalışıyor (Port 5004)

---

## Özet: Faz 4 adımlar sırası

1. Discount klasörünü oluştur
2. Discount.Grpc gRPC projesi oluştur (`dotnet new grpc`)
3. Projeyi solution'a ekle
4. NuGet paketlerini ekle (EF Core, PostgreSQL, Health Checks)
5. Klasör yapısını oluştur (Entities, Data, Services, Protos)
6. Proto dosyası oluştur (`discount.proto`)
7. Proto dosyasını `.csproj`'a ekle (GrpcServices="Server")
8. Coupon Entity oluştur
9. DiscountDbContext oluştur
10. appsettings.json'a connection string ekle
11. EF Core Migration oluştur
12. Migration uygula
13. SeedData.cs oluştur (örnek kuponlar)
14. Program.cs'de seed data çalıştır
15. DiscountService.cs oluştur (DiscountProtoServiceBase'den inherit)
16. GetDiscount metodunu implement et
17. CreateDiscount metodunu implement et
18. UpdateDiscount metodunu implement et
19. DeleteDiscount metodunu implement et
20. Error handling ekle (RpcException)
21. Program.cs'de gRPC servisi register et
22. Health checks ekle
23. Tüm metodları test et (Postman veya gRPC client ile)

**Bu adımlar tamamlandıktan sonra Faz 5'e (Basket Service) geçilebilir.**

**Not:** Basket Service, Discount Service'i gRPC client olarak kullanacak, bu yüzden Discount Service'in çalışır durumda olması önemlidir.

