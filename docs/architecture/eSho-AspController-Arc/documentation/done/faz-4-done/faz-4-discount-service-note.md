# Faz 4 - Discount Service Notları

> Bu dosya, Faz 4 (Discount Service) adım adım yaparken öğrendiklerimi not aldığım dosyadır.
> 
> **İçerik:**
> - Faz 4.1: Discount.Grpc Projesi Oluştur
> - Faz 4.2: Discount Database & Seed Data
> - Faz 4.3: Discount gRPC Service Implementation (Henüz yapılmadı)

---

## Discount Service Nedir?

**Discount Service**, ürünlere özel indirim kuponlarını yönetir. Basket Service, sepetteki ürünler için indirim sorgular (gRPC ile).

### Temel İşlevler:
- Ürün için indirim sorgulama (gRPC ile hızlı)
- Yeni indirim kuponu oluşturma
- İndirim kuponu güncelleme
- İndirim kuponu silme

### Neden şimdi?
- ✅ Basket yapmadan önce hazır olmalı
- ✅ gRPC öğrenmek için iyi
- ✅ Basit servis, bağımlılığı yok
- ✅ Internal servis (dışarıya açık değil, sadece Basket Service kullanacak)

### Neden gRPC?
- Basket Service sürekli indirim sorguluyor (her sepet işleminde)
- gRPC çok hızlı (binary format, HTTP/2)
- Internal servis (dışarıya açık değil)
- Yüksek performans gerekiyor

---

## 4.1 Discount.Grpc Projesi Oluştur - Yapılanlar

### Adım 1: Discount Klasör Yapısını Oluştur

**Komut:**
```bash
cd src/Services
mkdir Discount
cd Discount
```

**Açıklamalar:**
- `cd src/Services` → Services klasörüne geç
- `mkdir Discount` → Discount klasörü oluştur
- `cd Discount` → Discount klasörüne geç

**Ne işe yarar:**
- Discount servisi için klasör oluşturur
- Catalog gibi `src/Services/Discount/` altında olacak
- Sonra bu klasöre gRPC projesi ekleyeceğiz

**Sonuç:**
- `src/Services/Discount/` klasörü oluşturuldu

**Kontrol:**
```bash
ls -la src/Services/
# Discount klasörünü görmeli
```

---

### Adım 2: gRPC Server Projesi Oluştur

**Komut:**
```bash
cd src/Services/Discount
dotnet new grpc -n Discount.Grpc
```

**Açıklamalar:**
- `dotnet new grpc` → gRPC template'i ile proje oluştur
- `-n Discount.Grpc` → Proje adı
- Template otomatik olarak:
  - `Protos/` klasörü oluşturur
  - Örnek proto dosyası ekler
  - `Program.cs`'i gRPC için konfigüre eder
  - gRPC servis yapısını hazırlar

**Ne işe yarar:**
- gRPC servis projesi oluşturur (REST API değil, gRPC)
- gRPC template'i ile otomatik yapılandırma

**Neden gRPC Template?**
- REST API template'i yerine gRPC template'i kullanıyoruz
- gRPC için özel konfigürasyonlar otomatik gelir
- Proto dosyası yapısı hazır gelir

**Sorun:**
- `Grpc.AspNetCore` paketi versiyonlu olarak eklendi (Central Package Management ile uyumsuz)
- Hata: `error NU1008: Projects that use central package version management should not define the version on the PackageReference items`

**Çözüm:**
1. `Discount.Grpc.csproj`'dan versiyon kaldırıldı
2. `Directory.Packages.props`'a `Grpc.AspNetCore` eklendi

**Sonuç:**
- `src/Services/Discount/Discount.Grpc/` klasörü oluşturuldu
- `Discount.Grpc.csproj` dosyası oluşturuldu
- `Protos/greet.proto` örnek dosyası oluşturuldu
- `Program.cs` gRPC için konfigüre edildi

---

### Adım 3: Projeyi Solution'a Ekle

**Komut:**
```bash
cd ../../..
dotnet sln add src/Services/Discount/Discount.Grpc/Discount.Grpc.csproj
```

**Açıklamalar:**
- `cd ../../..` → Proje root dizinine dön
- `dotnet sln add` → Solution'a proje ekle
- `src/Services/Discount/Discount.Grpc/Discount.Grpc.csproj` → Eklenecek proje dosyasının yolu

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
src/Services/Discount/Discount.Grpc/Discount.Grpc.csproj
```

**Sonuç:** ✅ Proje solution'a eklendi

---

### Adım 4: NuGet Paketlerini Ekle

**Komut:**
```bash
cd src/Services/Discount/Discount.Grpc
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package AspNetCore.HealthChecks.NpgSql
```

**Açıklamalar:**
- `Microsoft.EntityFrameworkCore` → ORM (veritabanı işlemleri)
- `Microsoft.EntityFrameworkCore.Design` → Migration tooling (migration oluşturma)
- `Npgsql.EntityFrameworkCore.PostgreSQL` → PostgreSQL provider
- `AspNetCore.HealthChecks.NpgSql` → PostgreSQL health check

**Not:** `Grpc.AspNetCore` paketi zaten template ile gelmişti, onu eklemeye gerek yok.

**Ne işe yarar:**
- EF Core ile veritabanı işlemleri için gerekli paketler
- Migration oluşturmak için gerekli
- PostgreSQL bağlantısı için gerekli
- Health check için gerekli

**Central Package Management:**
- Paketler `Directory.Packages.props`'tan versiyon alır
- `.csproj` dosyasında versiyon belirtilmez
- Tüm projeler aynı versiyonu kullanır

**Sonuç:**
- Tüm paketler eklendi
- `Discount.Grpc.csproj` dosyası güncellendi
- `Directory.Packages.props` dosyası güncellendi (yeni paketler eklendi)

**Kontrol:**
```bash
cat Discount.Grpc.csproj
# PackageReference'ları görmeli (versiyonlar olmadan)
```

---

### Adım 5: Klasör Yapısını Oluştur

**Komut:**
```bash
cd src/Services/Discount/Discount.Grpc
mkdir -p Entities
mkdir -p Data
mkdir -p Services
```

**Açıklamalar:**
- `mkdir -p Entities` → Entities klasörü oluştur
- `mkdir -p Data` → Data klasörü oluştur
- `mkdir -p Services` → Services klasörü oluştur
- `Protos/` → Zaten var (template ile geldi)

**Ne işe yarar:**
- `Entities/` → Veritabanı tablolarını temsil eden class'lar (Coupon.cs)
- `Data/` → DbContext ve SeedData (DiscountDbContext.cs, SeedData.cs)
- `Services/` → gRPC servis implementasyonları (DiscountService.cs)
- `Protos/` → Proto dosyaları (gRPC contract'ları)

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

**Sonuç:**
- `Entities/` klasörü oluşturuldu
- `Data/` klasörü oluşturuldu
- `Services/` klasörü oluşturuldu (zaten vardı, GreeterService.cs içinde)
- `Protos/` klasörü zaten vardı

---

### Adım 6: Proto Dosyası Oluştur (discount.proto)

**Komut:**
- `Protos/greet.proto` dosyası silindi
- `Protos/discount.proto` dosyası oluşturuldu
- `.csproj` dosyası güncellendi

**Açıklamalar:**
- `greet.proto` → Template'den gelen örnek dosya (ihtiyacımız yok)
- `discount.proto` → Discount servisimizin gRPC contract'ı (service, message tanımları)

**Proto Dosyası İçeriği:**
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

**Açıklamalar:**
- `syntax = "proto3"` → Proto3 syntax kullanılır
- `option csharp_namespace` → C# namespace'i belirlenir
- `service DiscountProtoService` → gRPC servis adı
- `rpc` metodları → Servis metodları (GetDiscount, CreateDiscount, vb.)
- `message` tipleri → Request/Response mesaj tipleri
- Field numaraları (1, 2, 3...) → Protocol Buffers için önemli (değiştirilmemeli)

**Ne işe yarar:**
- gRPC servis contract'ını tanımlar
- Service, message ve RPC metodlarını tanımlar
- C# kodları otomatik generate edilir

**Yapılanlar:**
1. `discount.proto` dosyası oluşturuldu
2. `greet.proto` dosyası silindi
3. `GreeterService.cs` dosyası silindi (artık gerekli değil)
4. `.csproj` dosyası güncellendi (`discount.proto` kullanıyor)
5. `Program.cs` güncellendi (GreeterService referansı kaldırıldı)

**Sonuç:**
- `Protos/discount.proto` dosyası oluşturuldu
- Build başarılı

---

## 4.2 Discount Database & Seed Data - Yapılanlar

### Adım 1: Coupon Entity Oluştur

**Dosya:** `Entities/Coupon.cs`

**Entity Yapısı:**
```csharp
namespace Discount.Grpc.Entities;

public class Coupon
{
    public int Id { get; set; }
    public string ProductName { get; set; } = default!;
    public string? Description { get; set; }
    public int Amount { get; set; }
}
```

**Açıklamalar:**
- `Id` (int) → Primary key, Identity (auto-increment)
- `ProductName` (string) → Ürün adı (unique constraint, DbContext'te tanımlanacak)
- `Description` (string?) → İndirim açıklaması (nullable)
- `Amount` (int) → İndirim miktarı (TL cinsinden)

**Ne işe yarar:**
- Veritabanı tablosunu temsil eden entity class'ı
- EF Core ile veritabanı işlemleri için gerekli

**Neden int Identity?**
- Discount servisi için Identity (auto-increment) yeterli
- Guid kullanmak gerekmez (internal servis, küçük veri seti)

**Sonuç:**
- `Entities/Coupon.cs` dosyası oluşturuldu
- Build başarılı

---

### Adım 2: DiscountDbContext Oluştur

**Dosya:** `Data/DiscountDbContext.cs`

**DbContext Yapısı:**
```csharp
using Microsoft.EntityFrameworkCore;
using Discount.Grpc.Entities;

namespace Discount.Grpc.Data;

public class DiscountDbContext : DbContext
{
    public DiscountDbContext(DbContextOptions<DiscountDbContext> options) 
        : base(options) { }

    public DbSet<Coupon> Coupons { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id)
                .ValueGeneratedOnAdd();
            
            entity.HasIndex(c => c.ProductName)
                .IsUnique();
            
            entity.Property(c => c.ProductName)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(c => c.Description)
                .HasMaxLength(500);
            
            entity.Property(c => c.Amount)
                .IsRequired();
            
            entity.ToTable("Coupons");
        });
    }
}
```

**Açıklamalar:**
- `Coupons` (DbSet<Coupon>) → Coupons tablosu
- `OnModelCreating` → Entity konfigürasyonları:
  - `Id` → Primary key, Identity (auto-increment)
  - `ProductName` → Unique constraint, Required, MaxLength(100)
  - `Description` → MaxLength(500), nullable
  - `Amount` → Required
  - Table name: `Coupons`

**Ne işe yarar:**
- EF Core DbContext → Veritabanı bağlantısı ve entity konfigürasyonları
- PostgreSQL bağlantısı için gerekli

**Sonuç:**
- `Data/DiscountDbContext.cs` dosyası oluşturuldu
- Build başarılı

---

### Adım 3: appsettings.json'a Connection String Ekle

**Dosya:** `appsettings.json`

**Connection String:**
```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5434;Database=DiscountDb;Username=postgres;Password=postgres"
  },
  ...
}
```

**Açıklamalar:**
- `Host=localhost` → Localhost'tan bağlanırken
- `Port=5434` → Host port (docker-compose.yml'de `5434:5432` mapping)
- `Database=DiscountDb` → Veritabanı adı
- `Username=postgres` → PostgreSQL kullanıcı adı
- `Password=postgres` → PostgreSQL şifresi

**Not:** 
- Localhost için port: **5434** (host port)
- Container network içinde: `Host=discountdb;Port=5432;...` (Docker Compose environment variable ile override edilecek)

**Ne işe yarar:**
- EF Core veritabanına bağlanmak için connection string gerekiyor
- Localhost'tan bağlanırken: `Host=localhost;Port=5434;...` (host port)
- Container network içinde: `Host=discountdb;Port=5432;...` (container port)

**Sonuç:**
- Connection string eklendi
- Build başarılı

---

### Adım 4: Program.cs'de DbContext'i Kaydet

**Dosya:** `Program.cs`

**Eklenen Kod:**
```csharp
using Discount.Grpc.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<DiscountDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));
```

**Açıklamalar:**
- `AddDbContext<DiscountDbContext>` → DbContext'i DI container'a kaydet
- `UseNpgsql` → PostgreSQL provider kullan
- `GetConnectionString("Database")` → appsettings.json'dan connection string al

**Ne işe yarar:**
- EF Core DbContext'i DI container'a kaydetmek gerekiyor
- PostgreSQL connection string'i kullanılacak
- Migration ve veritabanı işlemleri için gerekli

**Sonuç:**
- DbContext kaydı eklendi
- Build başarılı

---

### Adım 5: Health Checks Ekle

**Dosya:** `Program.cs`

**Eklenen Kod:**
```csharp
// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!);

// ...

app.MapHealthChecks("/health");
```

**Açıklamalar:**
- `AddHealthChecks()` → Health check servislerini ekle
- `.AddNpgSql(...)` → PostgreSQL health check ekle
- `app.MapHealthChecks("/health")` → Health check endpoint'i map et

**Ne işe yarar:**
- Container sağlık durumunu izlemek için
- PostgreSQL bağlantısının çalışıp çalışmadığını kontrol etmek için
- Docker Compose health check için gerekli

**Health Check Endpoint:** `/health` → PostgreSQL bağlantısını kontrol eder

**Sonuç:**
- Health Checks eklendi
- Build başarılı

---

### Adım 6: EF Core Migration Oluştur

**Komut:**
```bash
export DOTNET_ROOT=/usr/lib64/dotnet
cd src/Services/Discount/Discount.Grpc
dotnet ef migrations add InitialCreate --startup-project . --context DiscountDbContext
```

**Açıklamalar:**
- `export DOTNET_ROOT=/usr/lib64/dotnet` → DOTNET_ROOT environment variable'ını ayarla (dotnet-ef tool sorunu için)
- `dotnet ef migrations add InitialCreate` → Migration oluştur
- `--startup-project .` → Startup proje (Discount.Grpc)
- `--context DiscountDbContext` → DbContext adı

**Sorun:**
- dotnet-ef tool çalışmıyordu: "You must install .NET to run this application"
- **Sebep:** DOTNET_ROOT environment variable'ı yanlış ayarlanmış (`/home/kSEN/.dotnet` yerine `/usr/lib64/dotnet` olmalı)
- **Çözüm:** `export DOTNET_ROOT=/usr/lib64/dotnet` ile geçici olarak düzeltildi

**Detaylı sorun ve çözüm:** `docs/architecture/eSho-AspController-Arc/documentation/troubleshooting/dotnet-ef-tool-dotnet-runtime-not-found.md`

**Ne işe yarar:**
- Veritabanı şemasını oluşturmak için migration dosyası oluşturur
- Migration dosyası, veritabanı tablosunu oluşturacak SQL komutlarını içerir

**Oluşturulan Migration Dosyaları:**
- `20251219041813_InitialCreate.cs` → Migration dosyası
- `20251219041813_InitialCreate.Designer.cs` → Migration metadata
- `DiscountDbContextModelSnapshot.cs` → Model snapshot

**Not:** Container'lar şu an çalışmıyor; bu normal. Migration dosyası oluşturuldu, uygulama sonra yapılacak.

**Sonuç:**
- Migration dosyaları oluşturuldu
- Build başarılı

---

### Adım 7: SeedData.cs Oluştur

**Dosya:** `Data/SeedData.cs`

**SeedData Yapısı:**
```csharp
using Microsoft.EntityFrameworkCore;
using Discount.Grpc.Entities;

namespace Discount.Grpc.Data;

public static class SeedData
{
    public static async Task InitializeAsync(DiscountDbContext context)
    {
        // Zaten veri varsa ekleme
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

**Açıklamalar:**
- `InitializeAsync` static method → Seed data ekleme metodu
- Koşullu ekleme: Veri varsa tekrar eklemez (`if (await context.Coupons.AnyAsync()) return;`)
- Örnek kuponlar (3 adet):
  - **iPhone 15**: 5000 TL indirim (Yılbaşı indirimi)
  - **Samsung S24**: 6750 TL indirim (Kış kampanyası)
  - **MacBook Pro**: 5000 TL indirim (Öğrenci indirimi)

**Ne işe yarar:**
- Uygulama başlangıcında örnek veriler eklemek için
- Test için hazır veri olması için
- Development ortamında hızlı başlangıç için

**Neden Koşullu Ekleme?**
- Veri varsa tekrar eklemez (idempotent)
- Container yeniden başlatıldığında duplicate data oluşmaz
- Production'da mevcut veriler korunur

**Sonuç:**
- `Data/SeedData.cs` dosyası oluşturuldu
- Build başarılı

---

### Adım 8: Program.cs'de Migration ve Seed Data Çalıştır

**Dosya:** `Program.cs`

**Eklenen Kod:**
```csharp
// Migration ve Seed Data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DiscountDbContext>();
    
    // 1. Migration uygula (tabloları oluştur)
    await context.Database.MigrateAsync();
    
    // 2. Seed data ekle (eğer boşsa)
    await SeedData.InitializeAsync(context);
}
```

**Açıklamalar:**
- `using (var scope = ...)` → Service scope oluştur (DbContext için)
- `GetRequiredService<DiscountDbContext>()` → DbContext'i DI'dan al
- `MigrateAsync()` → Migration'ı uygula (tabloları oluştur)
- `SeedData.InitializeAsync(context)` → Seed data ekle (eğer boşsa)

**Ne işe yarar:**
- Uygulama başlangıcında migration'ı otomatik uygular
- Seed data'yı otomatik ekler
- Container başlatıldığında veritabanı hazır olur

**Güvenlik (Idempotent - Tekrar Çalıştırılabilir):**

Kod güvenli bir şekilde tekrar çalıştırılabilir:

| Durum | Ne Olur? | Sonuç |
|-------|----------|-------|
| **Tablo yok** | `MigrateAsync()` tabloyu oluşturur | ✅ Tablo oluşturulur |
| **Tablo var** | `MigrateAsync()` hiçbir şey yapmaz (sadece yeni migration'ları uygular) | ✅ Hata vermez |
| **Data yok** | `SeedData.InitializeAsync()` seed data ekler | ✅ 3 kupon eklenir |
| **Data var** | `SeedData.InitializeAsync()` direkt return eder (tekrar eklemez) | ✅ Hata vermez |
| **Her ikisi de var** | Hiçbir şey yapmaz | ✅ Hata vermez |

**Örnek Senaryolar:**

**Senaryo 1: İlk Çalıştırma (Tablo ve Data Yok)**
```
1. MigrateAsync() → Tabloyu oluşturur ✅
2. SeedData.InitializeAsync() → 3 kupon ekler ✅
Sonuç: Tablo ve data hazır
```

**Senaryo 2: İkinci Çalıştırma (Tablo ve Data Var)**
```
1. MigrateAsync() → Hiçbir şey yapmaz (tablo zaten var) ✅
2. SeedData.InitializeAsync() → Hiçbir şey yapmaz (data zaten var) ✅
Sonuç: Hiçbir şey değişmez, hata vermez
```

**Senaryo 3: Container Yeniden Başlatıldı (Tablo ve Data Var)**
```
1. MigrateAsync() → Hiçbir şey yapmaz ✅
2. SeedData.InitializeAsync() → Hiçbir şey yapmaz ✅
Sonuç: Mevcut veriler korunur, duplicate data oluşmaz
```

**Not:** Container'lar şu an çalışmıyor; bu normal. Kod hazır, container'lar çalıştığında otomatik çalışacak.

**Sonuç:**
- Migration ve seed data çalıştırma kodu eklendi
- Build başarılı

---

## Özet: Şu Ana Kadar Yapılanlar

### ✅ Faz 4.1 Tamamlandı:

1. ✅ Discount klasör yapısı oluşturuldu
2. ✅ gRPC Server projesi oluşturuldu
3. ✅ Proje solution'a eklendi
4. ✅ NuGet paketleri eklendi
5. ✅ Klasör yapısı oluşturuldu (Entities, Data, Services)
6. ✅ Proto dosyası oluşturuldu (discount.proto)

### ✅ Faz 4.2 Tamamlandı:

1. ✅ Coupon Entity oluşturuldu
2. ✅ DiscountDbContext oluşturuldu
3. ✅ Connection string eklendi
4. ✅ DbContext kaydı eklendi
5. ✅ Health Checks eklendi
6. ✅ EF Core Migration oluşturuldu
7. ✅ SeedData.cs oluşturuldu
8. ✅ Program.cs'de migration ve seed data çalıştırma eklendi

---

## 4.3 Discount gRPC Service Implementation - Yapılanlar

### Adım 1: DiscountService.cs Oluştur

**Dosya:** `Services/DiscountService.cs`

**Ne Yaptık:**
- gRPC servis implementasyonu için `DiscountService` class'ı oluşturuldu
- `DiscountProtoService.DiscountProtoServiceBase` base class'ından inherit edildi
- Dependency Injection ile `DiscountDbContext` ve `ILogger<DiscountService>` inject edildi

**Neden Yaptık:**
- Proto dosyasında tanımlanan `DiscountProtoService` servisinin implementasyonu gerekiyor
- Base class'tan inherit ederek RPC metodlarını override edeceğiz
- DbContext ile veritabanı işlemleri yapacağız
- Logger ile işlemleri loglayacağız

**Ne İşe Yarar:**
- gRPC servis metodlarının iş mantığını içerir
- Veritabanı işlemlerini yönetir
- RPC request'leri işler ve response döner

**Class Yapısı:**
```csharp
public class DiscountService : DiscountProtoService.DiscountProtoServiceBase
{
    private readonly DiscountDbContext _context;
    private readonly ILogger<DiscountService> _logger;
    
    // Constructor ile DI
    public DiscountService(DiscountDbContext context, ILogger<DiscountService> logger)
}
```

**Önemli Noktalar:**
- `DiscountProtoService.DiscountProtoServiceBase` → Proto dosyasından generate edilen base class
- `override` keyword → Base class'taki virtual metodları override eder
- `ServerCallContext` → gRPC context (metadata, cancellation token, vb.)

---

### Adım 2: GetDiscount Metodu Implement Et

**Metod:** `GetDiscount(GetDiscountRequest request, ServerCallContext context)`

**Ne Yaptık:**
- ProductName'e göre kuponu veritabanından sorgulayan metod implement edildi
- Kupon bulunamazsa `RpcException` fırlatılıyor (StatusCode.NotFound)
- Başarılı sorgu loglanıyor
- Entity'den CouponModel'e mapping yapılıyor

**Neden Yaptık:**
- Basket Service'in en çok kullanacağı metod (her sepet işleminde indirim sorgular)
- Hızlı olmalı (gRPC ile binary format)
- Hata durumlarında standart gRPC hata response'u dönmeli

**Ne İşe Yarar:**
- Ürün adına göre indirim kuponunu getirir
- Basket Service tarafından kullanılır (sepet işlemlerinde indirim hesaplama)

**Metod Akışı:**
1. `_context.Coupons.FirstOrDefaultAsync()` → ProductName'e göre kupon sorgula
2. `if (coupon == null)` → Kupon bulunamazsa hata fırlat
3. `RpcException` → StatusCode.NotFound ile hata döndür
4. `_logger.LogInformation()` → Başarılı sorguyu logla
5. `new CouponModel { ... }` → Entity'den CouponModel'e mapping yap ve döndür

**Önemli Noktalar:**
- `RpcException` → gRPC'de standart hata fırlatma yöntemi
- `StatusCode.NotFound` → HTTP 404 benzeri (kupon bulunamadı)
- `Description ?? string.Empty` → Null ise boş string döndür (proto'da string nullable değil)
- `FirstOrDefaultAsync()` → Async veritabanı sorgusu

---

### Adım 3: CreateDiscount Metodu Implement Et

**Metod:** `CreateDiscount(CreateDiscountRequest request, ServerCallContext context)`

**Ne Yaptık:**
- Yeni indirim kuponu oluşturan metod implement edildi
- ProductName unique kontrolü yapılıyor (çakışma kontrolü)
- Kupon zaten varsa `RpcException` fırlatılıyor (StatusCode.AlreadyExists)
- Yeni kupon veritabanına ekleniyor
- Başarılı oluşturma loglanıyor

**Neden Yaptık:**
- Admin işlemi (yeni kupon oluşturma)
- ProductName unique constraint'i var, çakışma kontrolü gerekli
- Hata durumlarında standart gRPC hata response'u dönmeli

**Ne İşe Yarar:**
- Yeni indirim kuponu oluşturur
- Admin panelinden veya başka servislerden kullanılabilir

**Metod Akışı:**
1. `_context.Coupons.FirstOrDefaultAsync()` → Aynı ProductName'e sahip kupon var mı kontrol et
2. `if (existingCoupon != null)` → Kupon varsa hata fırlat
3. `RpcException` → StatusCode.AlreadyExists ile hata döndür
4. `new Coupon { ... }` → Yeni Coupon entity oluştur
5. `_context.Coupons.AddAsync()` → Veritabanına ekle
6. `_context.SaveChangesAsync()` → Değişiklikleri kaydet (ID otomatik oluşur)
7. `_logger.LogInformation()` → Başarılı oluşturmayı logla
8. `new CouponModel { ... }` → CouponModel oluştur ve döndür

**Önemli Noktalar:**
- `StatusCode.AlreadyExists` → HTTP 409 Conflict benzeri (kupon zaten var)
- Unique constraint kontrolü → ProductName unique index olduğu için çakışma olabilir
- `SaveChangesAsync()` sonrası ID → Entity Framework otomatik ID'yi doldurur (auto-increment)
- `AddAsync()` → Async ekleme işlemi

---

### Adım 4: UpdateDiscount Metodu Implement Et

**Metod:** `UpdateDiscount(UpdateDiscountRequest request, ServerCallContext context)`

**Ne Yaptık:**
- Mevcut indirim kuponunu güncelleyen metod implement edildi
- ID'ye göre kuponu veritabanından buluyor
- Kupon bulunamazsa `RpcException` fırlatılıyor (StatusCode.NotFound)
- Kupon bilgileri güncelleniyor (ProductName, Description, Amount)
- Başarılı güncelleme loglanıyor

**Neden Yaptık:**
- Admin işlemi (kupon bilgilerini güncelleme)
- Kupon bulunamazsa hata dönmeli
- Hata durumlarında standart gRPC hata response'u dönmeli

**Ne İşe Yarar:**
- Mevcut indirim kuponunu günceller
- Admin panelinden veya başka servislerden kullanılabilir

**Metod Akışı:**
1. `_context.Coupons.FirstOrDefaultAsync()` → ID'ye göre kuponu bul
2. `if (coupon == null)` → Kupon bulunamazsa hata fırlat
3. `RpcException` → StatusCode.NotFound ile hata döndür
4. `coupon.ProductName = ...` → Kupon bilgilerini güncelle
5. `_context.SaveChangesAsync()` → Değişiklikleri kaydet
6. `_logger.LogInformation()` → Başarılı güncellemeyi logla
7. `new CouponModel { ... }` → CouponModel oluştur ve döndür

**Önemli Noktalar:**
- ID'ye göre arama → Update işleminde ID kullanılır (proto'daki CouponModel.Id)
- Entity tracking → EF Core entity'yi track eder, değişiklikleri algılar
- `SaveChangesAsync()` → Track edilen değişiklikleri veritabanına kaydeder

---

### Adım 5: DeleteDiscount Metodu Implement Et

**Metod:** `DeleteDiscount(DeleteDiscountRequest request, ServerCallContext context)`

**Ne Yaptık:**
- İndirim kuponunu silen metod implement edildi
- ProductName'e göre kuponu veritabanından buluyor
- Kupon bulunamazsa `RpcException` fırlatılıyor (StatusCode.NotFound)
- Kupon siliniyor
- Başarılı silme loglanıyor
- `DeleteDiscountResponse` döndürülüyor (success: true)

**Neden Yaptık:**
- Admin işlemi (kupon silme)
- Kupon bulunamazsa hata dönmeli
- Hata durumlarında standart gRPC hata response'u dönmeli
- Silme işleminin başarılı olup olmadığını döndürmeli

**Ne İşe Yarar:**
- İndirim kuponunu siler
- Admin panelinden veya başka servislerden kullanılabilir

**Metod Akışı:**
1. `_context.Coupons.FirstOrDefaultAsync()` → ProductName'e göre kuponu bul
2. `if (coupon == null)` → Kupon bulunamazsa hata fırlat
3. `RpcException` → StatusCode.NotFound ile hata döndür
4. `_context.Coupons.Remove()` → Kuponu sil
5. `_context.SaveChangesAsync()` → Değişiklikleri kaydet
6. `_logger.LogInformation()` → Başarılı silmeyi logla
7. `new DeleteDiscountResponse { Success = true }` → Response döndür

**Önemli Noktalar:**
- `Remove()` → Entity'yi silme listesine ekler
- `SaveChangesAsync()` → Silme işlemini veritabanına uygular
- `DeleteDiscountResponse` → Proto dosyasında tanımlı response tipi (success: bool)

---

### Adım 6: Error Handling (RpcException Kullanımı)

**Ne Yaptık:**
- Tüm metodlarda hata durumlarında `RpcException` kullanıldı
- Standart gRPC status code'ları kullanıldı:
  - `StatusCode.NotFound` → Kupon bulunamadığında
  - `StatusCode.AlreadyExists` → Kupon zaten varsa

**Neden Yaptık:**
- gRPC'de standart hata yönetimi için `RpcException` kullanılmalı
- HTTP status code'larına benzer şekilde gRPC status code'ları var
- Client tarafında hata durumlarını anlamak için standart kodlar gerekli

**Ne İşe Yarar:**
- Hata durumlarında standart gRPC hata response'u döner
- Client tarafında hata tipini anlamak kolaylaşır
- Debugging ve monitoring için loglama yapılabilir

**Kullanılan Status Code'lar:**
- `StatusCode.NotFound` → HTTP 404 benzeri (kupon bulunamadı)
- `StatusCode.AlreadyExists` → HTTP 409 Conflict benzeri (kupon zaten var)
- `StatusCode.InvalidArgument` → HTTP 400 Bad Request benzeri (geçersiz parametre)
- `StatusCode.Internal` → HTTP 500 Internal Server Error benzeri (beklenmeyen hatalar)

**RpcException Kullanımı:**
```csharp
throw new RpcException(new Status(
    StatusCode.NotFound, 
    $"Discount for {request.ProductName} not found"));
```

**Önemli Noktalar:**
- `RpcException` → gRPC'de standart hata fırlatma yöntemi
- `Status` → Status code ve mesaj içerir
- Client tarafında `catch (RpcException ex)` ile yakalanabilir

---

### Adım 7: Program.cs'de gRPC Servisi Register Et

**Dosya:** `Program.cs`

**Ne Yaptık:**
- `using Discount.Grpc.Services;` eklendi
- `app.MapGrpcService<DiscountService>();` eklendi
- gRPC servisi middleware pipeline'a eklendi

**Neden Yaptık:**
- gRPC servisini kullanılabilir hale getirmek için register etmek gerekiyor
- `MapGrpcService<T>()` → gRPC servisini endpoint olarak map eder
- Middleware pipeline'a eklenmezse servis çalışmaz

**Ne İşe Yarar:**
- gRPC servisini kullanılabilir hale getirir
- Client'lar servise bağlanabilir
- gRPC endpoint'i aktif olur

**Eklenen Kod:**
```csharp
using Discount.Grpc.Services; // Added for DiscountService

// ...

app.MapGrpcService<DiscountService>();
```

**Önemli Noktalar:**
- `AddGrpc()` → gRPC servisleri için DI configuration (zaten vardı)
- `MapGrpcService<T>()` → gRPC servisini endpoint olarak map eder
- gRPC endpoint'i: `http://localhost:8080` (proto dosyasında tanımlı servis adı kullanılır)
- `MapGrpcService` → Middleware pipeline'a ekler, HTTP/2 üzerinden gRPC isteklerini işler

---

## Özet: Faz 4.3 Tamamlandı

### ✅ Yapılanlar:

1. ✅ **DiscountService.cs oluşturuldu**
   - Base class'tan inherit edildi
   - DI ile DbContext ve Logger inject edildi

2. ✅ **GetDiscount metodu implement edildi**
   - ProductName'e göre kupon sorgular
   - NotFound hatası fırlatır (kupon bulunamazsa)
   - Loglama yapılır

3. ✅ **CreateDiscount metodu implement edildi**
   - Yeni kupon oluşturur
   - AlreadyExists hatası fırlatır (kupon zaten varsa)
   - Loglama yapılır

4. ✅ **UpdateDiscount metodu implement edildi**
   - Mevcut kuponu günceller
   - NotFound hatası fırlatır (kupon bulunamazsa)
   - Loglama yapılır

5. ✅ **DeleteDiscount metodu implement edildi**
   - Kuponu siler
   - NotFound hatası fırlatır (kupon bulunamazsa)
   - Loglama yapılır

6. ✅ **Error handling eklendi**
   - RpcException kullanıldı
   - Standart gRPC status code'ları kullanıldı

7. ✅ **Program.cs'de gRPC servisi register edildi**
   - MapGrpcService<DiscountService>() eklendi
   - gRPC servisi aktif hale geldi

### 📋 DiscountService Metodları:

| Metod | Request | Response | Ne İşe Yarar |
|-------|---------|----------|---------------|
| **GetDiscount** | GetDiscountRequest (ProductName) | CouponModel | ProductName'e göre kupon getirir (Basket Service kullanır) |
| **CreateDiscount** | CreateDiscountRequest (CouponModel) | CouponModel | Yeni kupon oluşturur (Admin işlemi) |
| **UpdateDiscount** | UpdateDiscountRequest (CouponModel) | CouponModel | Mevcut kuponu günceller (Admin işlemi) |
| **DeleteDiscount** | DeleteDiscountRequest (ProductName) | DeleteDiscountResponse | Kuponu siler (Admin işlemi) |

### 🔑 Önemli Class'lar ve Kullanımları:

1. **DiscountProtoService.DiscountProtoServiceBase**
   - Proto dosyasından generate edilen base class
   - Tüm RPC metodları için virtual metodlar içerir
   - `override` keyword ile metodları implement ederiz

2. **RpcException**
   - gRPC'de standart hata fırlatma yöntemi
   - `Status` ile status code ve mesaj içerir
   - Client tarafında yakalanabilir

3. **ServerCallContext**
   - gRPC context (metadata, cancellation token, vb.)
   - Her RPC metodunda parametre olarak gelir
   - Request metadata'sına erişim sağlar

4. **CouponModel, GetDiscountRequest, vb.**
   - Proto dosyasından generate edilen message tipleri
   - Request/Response için kullanılır
   - `Discount.Grpc.Protos` namespace'inde

---

## 4.4 Test ve Sorun Giderme - Yapılanlar

### Adım 1: HTTP/2 Sorunu ve Çözümü

**Sorun:**
- `appsettings.json`'da `Protocols: "Http2"` ayarı vardı
- Bu, tüm endpoint'lerin sadece HTTP/2 kabul etmesine neden oluyordu
- Health check endpoint'i HTTP/1.1 ile çalıştığı için hata veriyordu: `An HTTP/1.x request was sent to an HTTP/2 only endpoint.`

**Ne Yaptık:**
- `appsettings.json`'da `Protocols: "Http1AndHttp2"` olarak değiştirdik
- Artık hem HTTP/1.1 hem HTTP/2 destekleniyor
- Health check endpoint'i çalışıyor

**Neden Yaptık:**
- Health check endpoint'i HTTP/1.1 ile çalışır (curl, browser, vb.)
- gRPC endpoint'leri HTTP/2 kullanır
- Her iki protokolü desteklemek gerekiyor

**Ne İşe Yarar:**
- Health check endpoint'i çalışır (`/health`)
- gRPC endpoint'leri çalışır (HTTP/2)
- Her iki protokol desteklenir

**Değişiklik:**
```json
// Önce (Hatalı)
"Kestrel": {
  "EndpointDefaults": {
    "Protocols": "Http2"  // ❌ Sadece HTTP/2
  }
}

// Sonra (Doğru)
"Kestrel": {
  "EndpointDefaults": {
    "Protocols": "Http1AndHttp2"  // ✅ Hem HTTP/1.1 hem HTTP/2
  }
}
```

**Önemli Noktalar:**
- `Http1AndHttp2` → Her iki protokolü destekler
- Health check HTTP/1.1 ile çalışır
- gRPC endpoint'leri HTTP/2 ile çalışır
- Her iki protokol aynı port'ta çalışabilir

---

### Adım 2: Health Check Testi

**Ne Yaptık:**
- Container'ları başlattık (`docker compose up -d discountdb`)
- Servisi başlattık (`dotnet run`)
- Health check endpoint'ini test ettik

**Test Komutu:**
```bash
curl http://localhost:5152/health
```

**Beklenen Sonuç:**
```
Healthy
```

**Ne İşe Yarar:**
- Servisin çalışıp çalışmadığını kontrol eder
- PostgreSQL bağlantısını kontrol eder
- Container health check için kullanılır

**Önemli Noktalar:**
- Health check endpoint'i HTTP/1.1 ile çalışır
- PostgreSQL bağlantısı kontrol edilir
- `Healthy` veya `Unhealthy` döner

---

### Adım 3: gRPC Servis Testi (Postman)

**Ne Yaptık:**
- Postman ile gRPC servisini test ettik
- Proto dosyasını import ettik
- Tüm RPC metodlarını test ettik

**Test Adımları:**

1. **Postman'i Aç**
   - New Request → gRPC seç

2. **Endpoint Ayarla**
   - Endpoint: `http://localhost:5152`

3. **Proto Dosyasını Import Et**
   - `src/Services/Discount/Discount.Grpc/Protos/discount.proto`

4. **Service ve Method Seç**
   - Service: `DiscountProtoService`
   - Method: `GetDiscount`, `CreateDiscount`, `UpdateDiscount`, `DeleteDiscount`

5. **Request Gönder**

**Test Senaryoları:**

**GetDiscount - Başarılı:**
```json
Request:
{
  "productName": "iPhone 15"
}

Response:
{
  "id": 1,
  "productName": "iPhone 15",
  "description": "Yılbaşı indirimi",
  "amount": 5000
}
```

**GetDiscount - Hata (Kupon Bulunamadı):**
```json
Request:
{
  "productName": "Olmayan Ürün"
}

Response:
StatusCode: NotFound
Message: "Discount for Olmayan Ürün not found"
```

**CreateDiscount - Başarılı:**
```json
Request:
{
  "coupon": {
    "productName": "iPad Pro",
    "description": "Yeni ürün indirimi",
    "amount": 3000
  }
}

Response:
{
  "id": 4,
  "productName": "iPad Pro",
  "description": "Yeni ürün indirimi",
  "amount": 3000
}
```

**CreateDiscount - Hata (Kupon Zaten Var):**
```json
Request:
{
  "coupon": {
    "productName": "iPhone 15",  // Zaten var
    "description": "Test",
    "amount": 1000
  }
}

Response:
StatusCode: AlreadyExists
Message: "Discount for iPhone 15 already exists"
```

**UpdateDiscount - Başarılı:**
```json
Request:
{
  "coupon": {
    "id": 1,
    "productName": "iPhone 15",
    "description": "Güncellenmiş indirim",
    "amount": 6000
  }
}

Response:
{
  "id": 1,
  "productName": "iPhone 15",
  "description": "Güncellenmiş indirim",
  "amount": 6000
}
```

**DeleteDiscount - Başarılı:**
```json
Request:
{
  "productName": "iPhone 15"
}

Response:
{
  "success": true
}
```

**Ne İşe Yarar:**
- Tüm gRPC metodlarının çalışıp çalışmadığını test eder
- Hata durumlarını test eder
- Response formatını kontrol eder

**Önemli Noktalar:**
- Postman gRPC desteği gerekiyor
- Proto dosyası import edilmeli
- HTTP/2 protokolü kullanılır
- Binary format (Protocol Buffers)

---

## Özet: Faz 4 Tamamlandı

### ✅ Faz 4.1: Discount.Grpc Projesi Oluştur
- ✅ Discount klasör yapısı oluşturuldu
- ✅ gRPC Server projesi oluşturuldu
- ✅ Proje solution'a eklendi
- ✅ NuGet paketleri eklendi
- ✅ Klasör yapısı oluşturuldu
- ✅ Proto dosyası oluşturuldu

### ✅ Faz 4.2: Discount Database & Seed Data
- ✅ Coupon Entity oluşturuldu
- ✅ DiscountDbContext oluşturuldu
- ✅ Connection string eklendi
- ✅ DbContext kaydı eklendi
- ✅ Health Checks eklendi
- ✅ EF Core Migration oluşturuldu
- ✅ SeedData.cs oluşturuldu
- ✅ Program.cs'de migration ve seed data çalıştırma eklendi

### ✅ Faz 4.3: Discount gRPC Service Implementation
- ✅ DiscountService.cs oluşturuldu
- ✅ GetDiscount metodu implement edildi
- ✅ CreateDiscount metodu implement edildi
- ✅ UpdateDiscount metodu implement edildi
- ✅ DeleteDiscount metodu implement edildi
- ✅ Error handling eklendi
- ✅ Program.cs'de gRPC servisi register edildi

### ✅ Faz 4.4: Test ve Sorun Giderme
- ✅ HTTP/2 sorunu çözüldü (Http1AndHttp2)
- ✅ Health check test edildi
- ✅ gRPC servis test edildi (Postman)

---

### 🎯 Sonraki Adımlar:

- ⏳ Basket Service'ten Discount Service'e gRPC çağrısı yapmak (Faz 5'te)
- ⏳ Production deployment hazırlıkları

---

**Son Güncelleme:** Aralık 2024

