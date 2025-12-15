# Faz 3 - Catalog Service Notları

> Bu dosya, Faz 3 (Catalog Service) adım adım yaparken öğrendiklerimi not aldığım dosyadır.
> 
> **İçerik:**
> - Faz 3.1: Catalog.API Projesi Oluştur
> - Faz 3.2: Catalog Database & Seed Data
> - Faz 3.3: Catalog CQRS - Products Commands
> - Faz 3.4: Catalog CQRS - Products Queries

---

## Catalog Service Nedir?

**Catalog Service**, e-ticaret sitesindeki ürün ve kategori bilgilerini yönetir. Kullanıcılar ürünleri görüntüler, admin ürün ekler/düzenler.

### Temel İşlevler:
- Ürün listesini gösterme (sayfalama, filtreleme)
- Ürün detaylarını gösterme
- Kategori bazlı ürün arama
- Admin: Ürün ekleme, güncelleme, silme
- Admin: Kategori yönetimi

### Neden önce bu?
- ✅ Temel servis, bağımsız (diğer servislere bağımlı değil)
- ✅ Kullanıcı akışının başlangıcı (ürünleri görmek için)
- ✅ CQRS + MediatR öğrenmek için ideal
- ✅ Basket yaparken ürün ID'leri lazım olacak

---

## 3.1 Catalog.API Projesi Oluştur - Yapılanlar

### Adım 1: Web API Projesi Oluştur

**Komut:**
```bash
cd src/Services
mkdir Catalog
cd Catalog
dotnet new webapi -n Catalog.API
```

**Açıklamalar:**
- `cd src/Services` → Services klasörüne geç
- `mkdir Catalog` → Catalog klasörü oluştur
- `cd Catalog` → Catalog klasörüne geç
- `dotnet new webapi -n Catalog.API` → Catalog.API adında Web API projesi oluştur

**Ne işe yarar:**
- Catalog Service için REST API projesi oluşturur
- Web API projesi = REST endpoint'leri sağlar
- Controller-based API kullanıyoruz (Minimal API değil)

**Neden Web API?**
- REST endpoint'leri için standart ASP.NET Core projesi
- Controller pattern kullanıyoruz (daha organize)
- Swagger desteği otomatik gelir

**Sonuç:**
- `src/Services/Catalog/Catalog.API/` klasörü oluşturuldu
- `Catalog.API.csproj` dosyası oluşturuldu
- `Program.cs`, `appsettings.json` gibi temel dosyalar oluşturuldu

**Not:** Proje oluşturulurken `Microsoft.AspNetCore.OpenApi` paketi versiyonlu olarak eklendi. Central Package Management (CPM) kullanıldığı için versiyon `Directory.Packages.props` dosyasına taşındı.

---

### Adım 2: Projeyi Solution'a Ekle

**Komut:**
```bash
cd ../../..
dotnet sln add src/Services/Catalog/Catalog.API/Catalog.API.csproj
```

**Açıklamalar:**
- `cd ../../..` → Proje root dizinine dön (3 seviye yukarı: Catalog.API → Catalog → Services → src → root)
- `dotnet sln add` → Solution'a proje ekle
- `src/Services/Catalog/Catalog.API/Catalog.API.csproj` → Eklenecek proje dosyasının yolu

**Ne işe yarar:**
- Projeyi solution'a ekler
- `dotnet sln list` ile görülebilir
- Diğer projeler bu projeyi referans edebilir
- IDE'lerde (VS Code, Visual Studio) solution içinde görünür

**Neden solution'a ekleniyor?**
- **Solution** = Birden fazla projeyi bir arada yönetmek için kullanılan bir "proje grubu"
- Solution dosyası (`.sln`) içinde hangi projelerin olduğunu belirtir
- Proje solution'a eklenmezse:
  - ❌ IDE'de (VS Code, Visual Studio) görünmez
  - ❌ `dotnet build` solution root'tan çalıştırıldığında build edilmez
  - ❌ Diğer projeler bu projeyi kolay referans edemez
  - ❌ Solution'daki tüm projeleri bir arada yönetemezsin

**Solution'a eklenince ne oluyor?**
1. **Solution dosyası güncellenir** (`EShop.sln`)
   - Proje, solution dosyasına `<Project>` elementi olarak eklenir
   - Solution, projenin nerede olduğunu bilir
2. **IDE'de görünür**
   - VS Code, Visual Studio gibi IDE'ler solution'ı açtığında projeyi görür
   - Solution Explorer'da proje listelenir
3. **Build/Test işlemlerinde dahil edilir**
   - `dotnet build` solution root'tan çalıştırıldığında bu proje de build edilir
   - `dotnet test` tüm test projelerini çalıştırır
   - Tüm projeleri bir arada yönetebilirsin
4. **Project reference kolaylığı**
   - Diğer projeler bu projeyi referans ederken yol bilmek kolaylaşır
   - Solution içindeki projeler arası referanslar daha organize olur

**Örnek:**
Solution'a eklenmeden önce:
- `dotnet sln list` → Catalog.API görünmez
- VS Code Solution Explorer → Catalog.API görünmez

Solution'a eklendikten sonra:
- `dotnet sln list` → Catalog.API görünür
- VS Code Solution Explorer → Catalog.API görünür
- `dotnet build` → Catalog.API de build edilir

**Kontrol:**
```bash
dotnet sln list
```

**Beklenen çıktı:**
```
Project(s)
----------
src/BuildingBlocks/BuildingBlocks.Behaviors/BuildingBlocks.Behaviors.csproj
src/BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj
src/BuildingBlocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj
src/Services/Catalog/Catalog.API/Catalog.API.csproj
```

**Sonuç:** ✅ Proje solution'a eklendi

---

### Adım 3: NuGet Paketlerini Ekle

**Komutlar:**
```bash
cd src/Services/Catalog/Catalog.API
dotnet add package MediatR
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
dotnet add package AutoMapper
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package AspNetCore.HealthChecks.NpgSql
```

**Açıklamalar:**
- Her paket ayrı ayrı eklenir
- Central Package Management (CPM) kullanıldığı için versiyonlar `Directory.Packages.props` dosyasında tanımlı olmalı
- Paket versiyonları merkezi olarak yönetilir

**Ne işe yarar:**
- **MediatR**: CQRS pattern için (Command/Query ayrımı, Handler yönetimi)
- **FluentValidation**: Request validation için (Fluent API ile validation kuralları)
- **FluentValidation.DependencyInjectionExtensions**: FluentValidation'ı DI container'a entegre eder
- **AutoMapper**: Entity ↔ DTO mapping için (manuel dönüşüm kod yazmaya gerek yok)
- **AutoMapper.Extensions.Microsoft.DependencyInjection**: AutoMapper'ı DI container'a entegre eder
- **Microsoft.EntityFrameworkCore**: ORM (Object-Relational Mapping) - Veritabanı işlemleri için
- **Microsoft.EntityFrameworkCore.Design**: EF Core migration araçları için (Migration oluşturma, uygulama)
- **Npgsql.EntityFrameworkCore.PostgreSQL**: PostgreSQL provider (EF Core'un PostgreSQL ile konuşmasını sağlar)
- **AspNetCore.HealthChecks.NpgSql**: PostgreSQL health check için (veritabanı sağlık kontrolü)

**Central Package Management (CPM) Notu:**
- Proje oluşturulurken `Microsoft.AspNetCore.OpenApi` paketi versiyonlu olarak eklendi
- CPM aktif olduğu için versiyon `Directory.Packages.props` dosyasına taşındı
- Tüm paket versiyonları merkezi olarak yönetilir
- Yeni paket eklendiğinde versiyon `Directory.Packages.props`'a eklenmeli

**Eklenen Paket Versiyonları (Directory.Packages.props'a eklendi):**
- `Microsoft.AspNetCore.OpenApi` → 9.0.11
- `AutoMapper` → 12.0.1 (13.0.1'den düşürüldü, extension paketi ile uyumluluk için)
- `AutoMapper.Extensions.Microsoft.DependencyInjection` → 12.0.1
- `Microsoft.EntityFrameworkCore` → 9.0.0
- `Microsoft.EntityFrameworkCore.Design` → 9.0.0
- `Npgsql.EntityFrameworkCore.PostgreSQL` → 9.0.2
- `AspNetCore.HealthChecks.NpgSql` → 9.0.0

**Not:** İlk olarak `AutoMapper 13.0.1` kullanıldı ancak `AutoMapper.Extensions.Microsoft.DependencyInjection` paketinin en yüksek versiyonu 12.0.1 olduğu için versiyon uyumluluk uyarısı verdi. Uyumluluk için `AutoMapper` de 12.0.1'e düşürüldü.

**Sonuç:** ✅ Tüm paketler eklendi

---

### Adım 4: Project References Ekle

**Komutlar:**
```bash
cd src/Services/Catalog/Catalog.API
dotnet add reference ../../BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj
dotnet add reference ../../BuildingBlocks/BuildingBlocks.Behaviors/BuildingBlocks.Behaviors.csproj
```

**Açıklamalar:**
- `dotnet add reference` → Proje referansı ekle
- Göreceli yol kullanılabilir: `../../BuildingBlocks/...` (Catalog.API → Catalog → Services → src → BuildingBlocks)
- Mutlak yol da kullanılabilir: `/home/kSEN/Desktop/ Projects/microservice-practice-me/src/BuildingBlocks/...`

**Ne işe yarar:**
- **BuildingBlocks.Exceptions**: Global exception handler ve custom exception'ları kullanmak için
  - `GlobalExceptionHandler` class'ını kullanabiliriz
  - `NotFoundException`, `BadRequestException`, `InternalServerException` gibi custom exception'ları kullanabiliriz
- **BuildingBlocks.Behaviors**: MediatR pipeline behaviors'ları kullanmak için
  - `ValidationBehavior` → Request validation için
  - `LoggingBehavior` → Request/Response logging için

**Neden gerekli?**
- Catalog.API, BuildingBlocks'taki class'ları kullanacak
- Proje referansı olmadan bu class'lar kullanılamaz (derleme hatası)
- Örnek: `throw new NotFoundException("Product", id);` yazmak için `BuildingBlocks.Exceptions` referansı gerekli

**Sonuç:** ✅ Project references eklendi

---

### Adım 5: Klasör Yapısını Oluştur

**Komut:**
```bash
cd src/Services/Catalog/Catalog.API
mkdir -p Features/Products/Commands/CreateProduct
mkdir -p Features/Products/Commands/UpdateProduct
mkdir -p Features/Products/Commands/DeleteProduct
mkdir -p Features/Products/Queries/GetProducts
mkdir -p Features/Products/Queries/GetProductById
mkdir -p Features/Products/Queries/GetProductsByCategory
mkdir -p Features/Categories/Commands/CreateCategory
mkdir -p Features/Categories/Queries/GetCategories
mkdir -p Features/Categories/Queries/GetCategoryById
mkdir -p Entities
mkdir -p Data
mkdir -p Dtos
mkdir -p Mapping
mkdir -p Controllers
```

**Açıklamalar:**
- `mkdir -p` → Klasör oluştur, gerekli üst klasörleri de oluştur (parent directories)
- `-p` parametresi: Eğer klasör zaten varsa hata vermez
- Her klasör ayrı ayrı oluşturulabilir veya tek komutta birleştirilebilir

**Ne işe yarar:**
CQRS pattern için klasör yapısını oluşturur. Her klasörün amacı:

1. **`Features/`** → CQRS pattern için iş mantığı klasörü
   - **Commands/**: Veriyi değiştiren işlemler (Create, Update, Delete)
     - Her command için ayrı klasör: `CreateProduct/`, `UpdateProduct/`, `DeleteProduct/`
     - Her klasörde: `CreateProductCommand.cs`, `CreateProductHandler.cs`, `CreateProductValidator.cs` gibi dosyalar olacak
   - **Queries/**: Veriyi okuyan işlemler (Get, GetAll, GetByFilter)
     - Her query için ayrı klasör: `GetProducts/`, `GetProductById/`, `GetProductsByCategory/`
     - Her klasörde: `GetProductsQuery.cs`, `GetProductsHandler.cs` gibi dosyalar olacak
   - **Neden ayrı klasörler?**: Her feature kendi klasöründe, kod daha organize ve okunabilir

2. **`Entities/`** → Veritabanı tablolarını temsil eden class'lar
   - **Örnek**: `Product.cs`, `Category.cs`
   - Veritabanındaki tablolara karşılık gelir (örn: Products tablosu → Product entity)
   - EF Core ile veritabanı işlemleri için gerekli

3. **`Data/`** → Veritabanı bağlantı ve başlangıç verileri
   - **`CatalogDbContext.cs`**: EF Core DbContext (veritabanı bağlantısı, entity konfigürasyonları)
   - **`SeedData.cs`**: Uygulama başlangıcında örnek veriler (kategoriler, ürünler)

4. **`Dtos/`** → API'den dönen veri formatları (Data Transfer Objects)
   - **Örnek**: `ProductDto.cs`, `CreateProductDto.cs`
   - Entity'ler veritabanı formatı, DTO'lar API formatı (daha temiz, güvenli)
   - **Neden?**: Entity'lerin tüm property'lerini kullanıcıya göstermek istemeyiz
   - Örnek: Entity'de `Id`, `Name`, `Price`, `InternalNotes` varsa, DTO'da sadece `Id`, `Name`, `Price` gösteririz

5. **`Mapping/`** → Entity ↔ DTO dönüşümü için AutoMapper profilleri
   - **Örnek**: `MappingProfile.cs`
   - Entity'yi DTO'ya çevirme kurallarını tanımlar
   - **Neden?**: Manuel dönüşüm kod yazmak yerine AutoMapper otomatik yapar
   - Örnek: `Product` entity → `ProductDto` DTO'ya dönüşüm kuralları

6. **`Controllers/`** → REST API endpoint'leri
   - **Örnek**: `ProductsController.cs`, `CategoriesController.cs`
   - HTTP isteklerini karşılayan class'lar (GET /api/products, POST /api/products, vb.)
   - Kullanıcıların API'ye eriştiği nokta burası

**Oluşturulan Klasör Yapısı:**
```
Catalog.API/
├── Controllers/          ← REST API endpoint'leri
├── Data/                 ← DbContext ve SeedData
├── Dtos/                 ← Data Transfer Objects
├── Entities/             ← Veritabanı entity'leri
├── Features/             ← CQRS iş mantığı
│   ├── Categories/
│   │   ├── Commands/
│   │   │   └── CreateCategory/
│   │   └── Queries/
│   │       ├── GetCategories/
│   │       └── GetCategoryById/
│   └── Products/
│       ├── Commands/
│       │   ├── CreateProduct/
│       │   ├── UpdateProduct/
│       │   └── DeleteProduct/
│       └── Queries/
│           ├── GetProducts/
│           ├── GetProductById/
│           └── GetProductsByCategory/
└── Mapping/              ← AutoMapper profilleri
```

**Sonuç:** ✅ Klasör yapısı oluşturuldu

---

### Adım 6: Entity'leri Oluştur

**Dosyalar:**
- `Entities/Product.cs`
- `Entities/Category.cs`

**Ne işe yarar:**
- **Entity**: Veritabanı tablolarını temsil eden C# class'ları
- EF Core bu entity'leri kullanarak veritabanı şemasını oluşturur
- Entity'ler veritabanı tablolarına karşılık gelir

**Neden gerekli?**
- Veritabanında Products ve Categories tabloları oluşturmak için
- EF Core'un veritabanı işlemlerini yapabilmesi için entity'lere ihtiyaç var
- LINQ sorguları entity'ler üzerinden yapılır

**Product Entity Özellikleri:**
- `Id` (Guid): Benzersiz tanımlayıcı
- `Name` (string): Ürün adı
- `Description` (string?): Ürün açıklaması (nullable)
- `Price` (decimal): Ürün fiyatı
- `ImageUrl` (string?): Ürün resmi URL'i (nullable)
- `CategoryId` (Guid): Kategori foreign key
- `Category` (Category?): Navigation property (ilişkili kategori nesnesine erişim için)

**Category Entity Özellikleri:**
- `Id` (Guid): Benzersiz tanımlayıcı
- `Name` (string): Kategori adı
- `Products` (ICollection<Product>): Navigation property (bu kategoriye ait ürünler)

**Navigation Property Nedir?**
- İki entity arasındaki ilişkiyi temsil eder
- Örnek: `Product.Category` → Ürünün hangi kategoriye ait olduğunu gösterir
- Örnek: `Category.Products` → Kategorideki tüm ürünleri gösterir
- EF Core bu property'leri kullanarak JOIN sorguları oluşturur

**Sonuç:** ✅ Entity'ler oluşturuldu

---

### Adım 7: CatalogDbContext Oluştur

**Dosya:**
- `Data/CatalogDbContext.cs`

**Ne işe yarar:**
- **DbContext**: EF Core'un veritabanıyla konuşmasını sağlayan ana sınıf
- Veritabanı bağlantısını yönetir
- Entity konfigürasyonlarını tanımlar
- LINQ sorgularını SQL'e çevirir

**Neden gerekli?**
- EF Core'un çalışması için DbContext sınıfı zorunludur
- Entity'lerin veritabanı tablolarına nasıl dönüşeceğini tanımlar
- Veritabanı işlemlerini (CRUD) yapmak için gerekli

**CatalogDbContext İçeriği:**
1. **Constructor**: `DbContextOptions<CatalogDbContext>` alır (DI container'dan gelecek)
2. **DbSet'ler**: Her entity için bir DbSet tanımlanır
   - `DbSet<Product> Products` → Products tablosuna erişim
   - `DbSet<Category> Categories` → Categories tablosuna erişim
3. **OnModelCreating**: Entity konfigürasyonları
   - Tablo isimleri
   - Alan uzunlukları (MaxLength)
   - Primary key'ler
   - Foreign key ilişkileri
   - Veri tipleri (decimal için precision/scale)

**Entity Konfigürasyonları:**
- **Product**:
  - Tablo adı: `Products`
  - `Name`: Zorunlu, max 100 karakter
  - `Description`: Opsiyonel, max 500 karakter
  - `Price`: decimal(18,2) - 18 toplam, 2 ondalık basamak
  - `ImageUrl`: Opsiyonel, max 500 karakter
  - Foreign Key: `CategoryId` → `Categories.Id`
  - `OnDelete(DeleteBehavior.Restrict)`: Kategori silinirse, o kategoriye ait ürünler varsa silme işlemi engellenir

- **Category**:
  - Tablo adı: `Categories`
  - `Name`: Zorunlu, max 50 karakter

**DeleteBehavior.Restrict Nedir?**
- Kategori silinmeye çalışıldığında, o kategoriye ait ürünler varsa silme işlemi engellenir
- Veri bütünlüğünü korur (orphan records oluşmasını önler)
- Alternatif: `Cascade` (kategori silinirse ürünler de silinir) - bu durumda kullanılmadı

**Sonuç:** ✅ CatalogDbContext oluşturuldu

---

### Adım 8: Connection String Ekle

**Dosya:**
- `appsettings.json`

**Ne işe yarar:**
- Uygulamanın veritabanına bağlanması için gerekli bilgileri içerir
- Host, Port, Database, Username, Password bilgileri

**Neden gerekli?**
- DbContext'in veritabanına bağlanabilmesi için connection string'e ihtiyacı var
- `Program.cs`'de `AddDbContext` yaparken connection string kullanılır

**Connection String Formatı:**
```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5432;Database=CatalogDb;Username=postgres;Password=postgres"
  }
}
```

**Connection String Parçaları:**
- `Host=localhost`: PostgreSQL sunucusunun adresi (Docker container için `catalogdb`, local için `localhost`)
- `Port=5432`: PostgreSQL'in dinlediği port
- `Database=CatalogDb`: Veritabanı adı
- `Username=postgres`: Veritabanı kullanıcı adı
- `Password=postgres`: Veritabanı şifresi

**Not:** Şu anda `localhost` kullanıldı (development için). Production'da `catalogdb` (Docker container adı) kullanılacak.

**Sonuç:** ✅ Connection string eklendi

---

### Adım 9: AutoMapper Versiyon Düzeltmesi

**Dosya:**
- `Directory.Packages.props`

**Sorun:**
- İlk olarak `AutoMapper 13.0.1` kullanıldı
- Ancak `AutoMapper.Extensions.Microsoft.DependencyInjection` paketinin en yüksek versiyonu 12.0.1
- Versiyon uyumsuzluğu uyarısı verdi

**Çözüm:**
- `AutoMapper` versiyonu 12.0.1'e düşürüldü
- Her iki paket de 12.0.1 versiyonunda uyumlu

**Neden önemli?**
- Paket uyumluluğu için aynı majör versiyon kullanılmalı
- Uyarılar build'i engellemese de, çalışma zamanında sorun çıkarabilir
- Versiyon tutarlılığı best practice

**Sonuç:** ✅ AutoMapper versiyonları uyumlu hale getirildi

---

## 3.1 Bölümü - Tamamlanan Kontroller

✅ Catalog.API Web API projesi oluşturuldu
✅ Proje solution'a eklendi
✅ NuGet paketleri eklendi (MediatR, FluentValidation, AutoMapper, EF Core, PostgreSQL, HealthChecks)
✅ Project References eklendi (BuildingBlocks.Exceptions, BuildingBlocks.Behaviors)
✅ Klasör yapısı oluşturuldu (Features, Entities, Data, Dtos, Mapping, Controllers)
✅ Entity'ler oluşturuldu (Product.cs, Category.cs)
✅ CatalogDbContext oluşturuldu (entity konfigürasyonları ile)
✅ Connection string eklendi (appsettings.json)
✅ AutoMapper versiyon uyumluluğu düzeltildi
✅ Proje build oluyor mu? (`dotnet build`) → ✅ Başarıyla build oluyor (0 uyarı, 0 hata)

---

## Öğrenilenler (Faz 3.1)

### Central Package Management (CPM)

**CPM Nedir?**
- Tüm paket versiyonlarını merkezi bir dosyada (`Directory.Packages.props`) yönetme
- Her projede paket versiyonlarını tekrar yazmaya gerek yok
- Tüm projelerde aynı paket versiyonları kullanılır

**Nasıl Çalışır?**
1. `Directory.Packages.props` dosyasında `ManagePackageVersionsCentrally` = `true` olur
2. Paket versiyonları `Directory.Packages.props` dosyasına eklenir
3. `.csproj` dosyalarında sadece paket adı yazılır, versiyon yazılmaz
4. Build sırasında versiyon `Directory.Packages.props`'tan alınır

**Avantajları:**
- ✅ Versiyon tutarlılığı: Tüm projelerde aynı paket versiyonları
- ✅ Merkezi yönetim: Tek yerden versiyon güncellenir
- ✅ Kod tekrarı önlenir: Her `.csproj`'da versiyon yazmaya gerek yok

**Örnek:**
```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="MediatR" Version="14.0.0" />

<!-- Catalog.API.csproj -->
<PackageReference Include="MediatR" />
<!-- Versiyon yazılmaz, Directory.Packages.props'tan alınır -->
```

### Project Reference

**Project Reference Nedir?**
- Bir projenin başka bir projeyi (class library) kullanması
- `.csproj` dosyasında `ProjectReference` olarak tanımlanır

**Neden Gerekli?**
- BuildingBlocks projelerindeki class'ları kullanmak için
- Örnek: `BuildingBlocks.Exceptions` içindeki `NotFoundException` class'ını kullanmak için proje referansı gerekli

**Nasıl Eklenir?**
```bash
dotnet add reference <ProjeYolu>
```

**Örnek:**
```bash
dotnet add reference ../../BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj
```

**Sonuç:**
- Catalog.API, BuildingBlocks.Exceptions içindeki class'ları kullanabilir
- `using BuildingBlocks.Exceptions.Exceptions;` yazabilir
- `throw new NotFoundException(...);` kullanabilir

### CQRS Pattern Klasör Yapısı

**CQRS Pattern Nedir?**
- Command Query Responsibility Segregation
- Okuma (Query) ve yazma (Command) işlemlerini ayırma
- Her işlem için ayrı handler'lar

**Klasör Yapısı:**
- `Features/Products/Commands/CreateProduct/` → Yeni ürün oluşturma işlemi
- `Features/Products/Queries/GetProducts/` → Ürün listesi getirme işlemi
- Her feature kendi klasöründe, organize ve okunabilir

**Avantajları:**
- ✅ Kod organizasyonu: İlgili dosyalar bir arada
- ✅ Okunabilirlik: Her işlem için açık klasör yapısı
- ✅ Bakım kolaylığı: Değişiklik yaparken ilgili dosyaları kolay bulma

### Entity Framework Core ve DbContext

**Entity Framework Core Nedir?**
- Microsoft'un ORM (Object-Relational Mapping) framework'ü
- C# class'larını (Entity) veritabanı tablolarına dönüştürür
- LINQ sorgularını SQL'e çevirir
- CRUD işlemlerini kolaylaştırır

**Entity Nedir?**
- Veritabanı tablolarını temsil eden C# class'ları
- Her entity bir tabloya karşılık gelir
- Property'ler tablo kolonlarına karşılık gelir
- Navigation property'ler tablolar arası ilişkileri temsil eder

**DbContext Nedir?**
- EF Core'un veritabanıyla konuşmasını sağlayan ana sınıf
- `DbSet<T>` ile entity'lere erişim sağlar
- `OnModelCreating` ile entity konfigürasyonları yapılır
- Migration oluşturma ve uygulama için kullanılır

**Navigation Property Nedir?**
- İki entity arasındaki ilişkiyi temsil eden property
- Foreign key ile birlikte kullanılır
- LINQ sorgularında JOIN işlemlerini kolaylaştırır
- Örnek: `Product.Category` → Ürünün kategori nesnesine erişim

**DeleteBehavior Nedir?**
- Foreign key ilişkilerinde silme davranışını belirler
- `Restrict`: İlişkili kayıt varsa silme engellenir (veri bütünlüğü için)
- `Cascade`: Ana kayıt silinirse ilişkili kayıtlar da silinir
- `SetNull`: Ana kayıt silinirse foreign key NULL olur

### Connection String

**Connection String Nedir?**
- Veritabanına bağlanmak için gerekli bilgileri içeren string
- Host, Port, Database, Username, Password bilgilerini içerir
- `appsettings.json` veya `appsettings.Development.json` içinde saklanır

**Neden appsettings.json'da?**
- Güvenlik: Hassas bilgiler environment variable'larda tutulmalı (production)
- Kolaylık: Development için appsettings.json yeterli
- Yapılandırma: Farklı ortamlar için farklı dosyalar (Development, Production)

**PostgreSQL Connection String Formatı:**
```
Host={host};Port={port};Database={database};Username={user};Password={pass}
```

---

## 3.2 Catalog Database & Seed Data - Yapılanlar

**Hedef:** Veritabanı şemasını oluşturup örnek verilerle doldurmak

---

### Adım 1: Program.cs'de DbContext Registration

**Dosya:**
- `Program.cs`

**Ne yapıldı:**
- `AddDbContext<CatalogDbContext>` eklendi
- PostgreSQL connection string kullanılarak DbContext yapılandırıldı

**Kod:**
```csharp
using Catalog.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DbContext Registration
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));
```

**Ne işe yarar:**
- `AddDbContext`: DbContext'i Dependency Injection container'a kaydeder
- `UseNpgsql`: PostgreSQL provider'ı kullanılacağını belirtir
- `GetConnectionString("Database")`: `appsettings.json`'daki connection string'i okur

**Neden gerekli?**
- DbContext'in DI container'dan alınabilmesi için kayıt edilmesi gerekir
- Controller'larda veya handler'larda DbContext'i constructor'da inject edebilmek için

**Sonuç:** ✅ DbContext registration eklendi

---

### Adım 2: dotnet-ef Tool Kurulumu

**Komut:**
```bash
dotnet tool install --global dotnet-ef --version 9.0.0
```

**Açıklamalar:**
- `dotnet tool install --global` → Global tool olarak kur (tüm projelerde kullanılabilir)
- `dotnet-ef` → Entity Framework Core Command Line Tools
- `--version 9.0.0` → EF Core 9.0.0 versiyonuna uygun tool versiyonu

**Ne işe yarar:**
- Migration oluşturma (`dotnet ef migrations add`)
- Migration uygulama (`dotnet ef database update`)
- Migration silme (`dotnet ef migrations remove`)

**Neden gerekli?**
- EF Core migration işlemleri için bu tool gerekli
- `.NET SDK` ile birlikte gelmez, ayrı kurulması gerekir

**Not:** İlk kurulum denemesi başarısız oldu (version belirtmeden), versiyon belirtilince başarılı oldu.

**Sonuç:** ✅ dotnet-ef tool kuruldu

---

### Adım 3: EF Core Migration Oluştur

**Komut:**
```bash
cd src/Services/Catalog/Catalog.API
export DOTNET_ROOT=/usr/lib64/dotnet
dotnet ef migrations add InitialCreate --startup-project . --context CatalogDbContext
```

**Açıklamalar:**
- `dotnet ef migrations add` → Migration oluşturma komutu
- `InitialCreate` → Migration adı (istersen farklı isim verebilirsin)
- `--startup-project .` → Startup proje (Catalog.API, şu anki dizin)
- `--context CatalogDbContext` → Hangi DbContext kullanılacak
- `export DOTNET_ROOT=/usr/lib64/dotnet` → dotnet-ef tool'unun .NET runtime'ını bulması için (Fedora için gerekli)

**Ne işe yarar:**
- Entity'lerdeki değişiklikleri veritabanına uygulamak için migration dosyası oluşturur
- Migration dosyası, veritabanında hangi tabloların oluşturulacağını tanımlar

**Neden gerekli?**
- Veritabanı şemasını kod olarak yönetmek için
- Versiyon kontrol sisteminde (Git) migration dosyaları saklanır
- Farklı ortamlarda (dev, staging, prod) aynı şema oluşturulabilir

**Oluşturulan Dosyalar:**
- `Migrations/20251215174714_InitialCreate.cs` → Migration dosyası (Up ve Down metodları)
- `Migrations/20251215174714_InitialCreate.Designer.cs` → Migration metadata dosyası
- `Migrations/CatalogDbContextModelSnapshot.cs` → Veritabanı model snapshot (mevcut durum)

**Migration Dosyası İçeriği:**
- `Up()` metodu: Migration uygulandığında yapılacak işlemler (tablolar oluşturulur)
- `Down()` metodu: Migration geri alındığında yapılacak işlemler (tablolar silinir)

**Sonuç:** ✅ Migration dosyası oluşturuldu

---

### Adım 4: Migration'ı Veritabanına Uygula

**Önkoşul:**
- PostgreSQL container'ının çalışıyor olması gerekli

**Komutlar:**
```bash
# PostgreSQL container'ını başlat (eğer çalışmıyorsa)
cd /home/kSEN/Desktop/ Projects/microservice-practice-me
docker compose up -d catalogdb

# Migration'ı uygula
cd src/Services/Catalog/Catalog.API
export DOTNET_ROOT=/usr/lib64/dotnet
dotnet ef database update --startup-project . --context CatalogDbContext
```

**Açıklamalar:**
- `docker compose up -d catalogdb` → catalogdb container'ını arka planda başlat
- `dotnet ef database update` → Bekleyen migration'ları veritabanına uygular

**Ne işe yarar:**
- Migration dosyasındaki SQL komutlarını veritabanında çalıştırır
- Tablolar, index'ler, foreign key'ler oluşturulur
- `__EFMigrationsHistory` tablosu oluşturulur (hangi migration'ların uygulandığını tutar)

**Neden gerekli?**
- Entity'ler sadece C# class'larıdır, veritabanında tablo yoktur
- Migration uygulanınca tablolar oluşturulur

**Kontrol:**
```bash
docker exec catalogdb psql -U postgres -d CatalogDb -c "\dt"
```

**Beklenen çıktı:**
```
                 List of relations
 Schema |         Name          | Type  |  Owner   
--------+-----------------------+-------+----------
 public | Categories            | table | postgres
 public | Products              | table | postgres
 public | __EFMigrationsHistory | table | postgres
(3 rows)
```

**Oluşturulan Tablolar:**
- `Categories`: Id (uuid, PK), Name (varchar(50))
- `Products`: Id (uuid, PK), Name (varchar(100)), Description (varchar(500)), Price (numeric(18,2)), ImageUrl (varchar(500)), CategoryId (uuid, FK)
- `__EFMigrationsHistory`: Migration geçmişi tablosu

**Sonuç:** ✅ Migration veritabanına uygulandı

---

### Adım 5: SeedData.cs Oluştur

**Dosya:**
- `Data/SeedData.cs`

**Ne yapıldı:**
- Static class oluşturuldu
- `InitializeAsync` static method eklendi
- Örnek kategoriler ve ürünler tanımlandı

**Kod Yapısı:**
```csharp
public static class SeedData
{
    public static async Task InitializeAsync(CatalogDbContext context)
    {
        // 1. Kategorileri kontrol et ve ekle
        if (!await context.Categories.AnyAsync())
        {
            // Kategorileri ekle
        }

        // 2. Ürünleri kontrol et ve ekle
        if (!await context.Products.AnyAsync())
        {
            // Ürünleri ekle
        }
    }
}
```

**Ne işe yarar:**
- Uygulama başlangıcında örnek veriler ekler
- Veri varsa tekrar eklenmez (performans ve veri bütünlüğü için)

**Neden gerekli?**
- Geliştirme/test için örnek verilerle çalışmak
- Uygulamayı çalıştırınca hemen test edilebilir
- Boş veritabanıyla çalışma durumlarını test etmek için

**Seed İçeriği:**
- **3 Kategori:**
  - Elektronik
  - Giyim
  - Ev & Yaşam

- **9 Ürün (her kategoride 3'er adet):**
  - Elektronik: iPhone 15, Samsung Galaxy S24, MacBook Pro
  - Giyim: Beyaz T-Shirt, Siyah Pantolon, Spor Ayakkabı
  - Ev & Yaşam: Ofis Masası, Rahat Sandalye, LED Lamba

**Kontrol Mekanizması:**
- `AnyAsync()` → Veritabanında kayıt var mı kontrol eder
- `!` → Eğer YOKSA içeriye girer ve ekler
- İkinci çalıştırmada veriler tekrar eklenmez (performans)

**Sonuç:** ✅ SeedData.cs oluşturuldu

---

### Adım 6: Program.cs'de Migration ve Seed Data Çalıştırma

**Dosya:**
- `Program.cs`

**Ne yapıldı:**
- `app.Build()` sonrası scope oluşturuldu
- `MigrateAsync()` eklendi (migration'ları otomatik uygular)
- `SeedData.InitializeAsync()` eklendi (seed data'yı ekler)

**Kod:**
```csharp
var app = builder.Build();

// Migration ve Seed Data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    
    // 1. Migration uygula
    await context.Database.MigrateAsync();
    
    // 2. Seed data ekle
    await SeedData.InitializeAsync(context);
}

// Configure the HTTP request pipeline.
```

**Ne işe yarar:**
- `MigrateAsync()`: Bekleyen migration'ları otomatik uygular
- `SeedData.InitializeAsync()`: Veri yoksa örnek verileri ekler

**Neden gerekli?**
- Uygulama her başladığında migration'ları kontrol eder ve uygular (manuel `dotnet ef database update` gerekmez)
- Uygulama her başladığında seed data'yı kontrol eder ve yoksa ekler

**Scope Neden Kullanılıyor?**
- `CatalogDbContext` bir scoped service (her request'te yeni oluşur)
- Uygulama başlangıcında request olmadığı için manuel scope oluştururuz
- İş bitince scope dispose olur, kaynaklar temizlenir

**Akış:**
1. Uygulama başlar (`dotnet run`)
2. `Program.cs` çalışır
3. `app.Build()` çalışır (servisler hazırlanır)
4. Scope oluşturulur
5. `CatalogDbContext` alınır
6. `MigrateAsync()` çalışır → Bekleyen migration'lar uygulanır
7. `SeedData.InitializeAsync(context)` çağrılır → Veri yoksa eklenir
8. Scope dispose olur
9. `app.Run()` çalışır (API hazır)

**Sonuç:** ✅ Migration ve Seed Data otomatik çalıştırma eklendi

---

## 3.2 Bölümü - Tamamlanan Kontroller

✅ DbContext Program.cs'de kayıt edildi
✅ dotnet-ef tool kuruldu
✅ EF Core Migration oluşturuldu (InitialCreate)
✅ Migration veritabanına uygulandı (tablolar oluşturuldu)
✅ SeedData.cs oluşturuldu (örnek kategoriler ve ürünler)
✅ Program.cs'de MigrateAsync() ve SeedData.InitializeAsync() eklendi
✅ Proje build oluyor mu? (`dotnet build`) → ✅ Başarıyla build oluyor (0 uyarı, 0 hata)

---

## Öğrenilenler (Faz 3.2)

### Migration (EF Core)

**Migration Nedir?**
- Veritabanı şema değişikliklerini kod olarak yönetmek için kullanılan dosyalar
- Entity'lerdeki değişiklikleri veritabanına uygulamak için migration oluşturulur
- Migration dosyaları versiyon kontrol sisteminde (Git) saklanır

**Migration Nasıl Çalışır?**
1. Entity'lere değişiklik yapılır (yeni property, yeni entity, vb.)
2. Migration oluşturulur: `dotnet ef migrations add MigrationAdi`
3. Migration dosyası oluşturulur (Up ve Down metodları ile)
4. Migration uygulanır: `dotnet ef database update`
5. Veritabanı şeması güncellenir

**Migration Dosyası Yapısı:**
- `Up()` metodu: Migration uygulandığında yapılacak işlemler
- `Down()` metodu: Migration geri alındığında yapılacak işlemler
- `__EFMigrationsHistory` tablosu: Hangi migration'ların uygulandığını tutar

**MigrateAsync() Nedir?**
- `context.Database.MigrateAsync()` → Bekleyen migration'ları otomatik uygular
- Uygulama başlangıcında çalıştırılırsa, manuel `dotnet ef database update` yapmaya gerek yok
- Production'da da otomatik migration uygulanabilir (dikkatli kullanılmalı)

**Avantajları:**
- ✅ Veritabanı şeması kod olarak yönetilir
- ✅ Versiyon kontrol sisteminde saklanır
- ✅ Farklı ortamlarda aynı şema oluşturulabilir
- ✅ Geri alma (rollback) mümkündür

### Seed Data

**Seed Data Nedir?**
- Uygulama başlangıcında veritabanına otomatik eklenen örnek veriler
- Geliştirme/test için hazır verilerle çalışmayı sağlar

**Neden Kullanılır?**
- Geliştirme sırasında örnek verilerle çalışmak
- Test ortamında hazır veriler
- Demo/test için hazır veri

**Nasıl Çalışır?**
1. `InitializeAsync` metodu çağrılır
2. Veritabanında veri var mı kontrol edilir (`AnyAsync()`)
3. Veri yoksa örnek veriler eklenir
4. Veri varsa tekrar eklenmez (performans)

**Kontrol Mekanizması:**
- `if (!await context.Categories.AnyAsync())` → Kategori yoksa ekle
- İkinci çalıştırmada veriler tekrar eklenmez
- Performans ve veri bütünlüğü için önemli

**Best Practices:**
- ✅ Veri kontrolü yapılmalı (duplicate veri önlenir)
- ✅ Static method kullanılmalı (instance oluşturmaya gerek yok)
- ✅ Async/await kullanılmalı (performans)
- ✅ Development ortamında kullanılmalı (Production'da dikkatli)

### dotnet-ef Tool

**dotnet-ef Tool Nedir?**
- Entity Framework Core Command Line Tools
- Migration işlemleri için kullanılan bir tool

**Nasıl Kurulur?**
```bash
dotnet tool install --global dotnet-ef --version 9.0.0
```

**Komutlar:**
- `dotnet ef migrations add MigrationAdi` → Migration oluştur
- `dotnet ef database update` → Migration'ları uygula
- `dotnet ef migrations remove` → Son migration'ı sil
- `dotnet ef migrations list` → Migration listesini göster

**Neden Gerekli?**
- EF Core migration işlemleri için bu tool gerekli
- `.NET SDK` ile birlikte gelmez, ayrı kurulması gerekir

**Not:** Linux sistemlerde (Fedora) `DOTNET_ROOT` environment variable'ı gerekebilir.

### Dependency Injection ve Scope

**Scope Nedir?**
- Dependency Injection container'ında servislerin yaşam süresini yönetmek için kullanılan bir kavram
- `Scoped`: Her HTTP request'te yeni bir instance oluşturulur
- `DbContext` genellikle Scoped olarak kayıt edilir

**Neden Manuel Scope Oluşturuyoruz?**
- Uygulama başlangıcında HTTP request yoktur
- `DbContext` bir scoped service olduğu için, manuel scope oluşturmak gerekir
- `using (var scope = app.Services.CreateScope())` → Scope oluştur
- İş bitince scope dispose olur, kaynaklar temizlenir

**Örnek:**
```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    // DbContext kullan
} // Scope dispose olur, DbContext de dispose olur
```

---

## 3.3 Catalog CQRS - Products Commands - Yapılanlar

**Hedef:** Product yazma işlemleri (Create, Update, Delete) için CQRS pattern ile Command'lar oluşturmak

---

### Adım 1: CreateProductCommand Oluştur

**Dosya:**
- `Features/Products/Commands/CreateProduct/CreateProductCommand.cs`

**Ne yapıldı:**
- Yeni ürün oluşturma komutunu tanımlayan class oluşturuldu
- MediatR'ın `IRequest<Guid>` interface'ini implement etti

**Kod:**
```csharp
using MediatR;

namespace Catalog.API.Features.Products.Commands.CreateProduct;

public class CreateProductCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryId { get; set; }
}
```

**Ne işe yarar:**
- API'den gelen verileri alır (Name, Description, Price, vb.)
- MediatR'a "yeni ürün oluştur" komutunu iletir
- Handler'ın işleyeceği veri modelini tanımlar

**Neden gerekli?**
- CQRS pattern'inde Command, veriyi değiştiren işlemler için kullanılır
- MediatR pattern'i için gerekli: Command → Handler → İşlem

**Özellikler:**
- `IRequest<Guid>` → Handler Guid (Product ID) dönecek
- Property'ler Entity ile aynı (Id hariç, çünkü veritabanı oluşturacak)

**Sonuç:** ✅ CreateProductCommand oluşturuldu

---

### Adım 2: CreateProductHandler Oluştur

**Dosya:**
- `Features/Products/Commands/CreateProduct/CreateProductHandler.cs`

**Ne yapıldı:**
- `CreateProductCommand`'i işleyen Handler class'ı oluşturuldu
- MediatR'ın `IRequestHandler<CreateProductCommand, Guid>` interface'ini implement etti

**Kod:**
```csharp
using MediatR;
using AutoMapper;
using Catalog.API.Data;
using Catalog.API.Entities;

namespace Catalog.API.Features.Products.Commands.CreateProduct;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly CatalogDbContext _context;
    private readonly IMapper _mapper;

    public CreateProductHandler(CatalogDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Command'den Entity oluştur
        var product = _mapper.Map<Product>(request);
        product.Id = Guid.NewGuid();

        // 2. Veritabanına ekle
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        // 3. ID döndür
        return product.Id;
    }
}
```

**Ne işe yarar:**
- `CreateProductCommand`'i alır
- Command'deki verileri `Product` entity'sine dönüştürür (AutoMapper ile)
- Veritabanına kaydeder
- Product ID döner (Guid)

**Neden gerekli?**
- MediatR pattern'inde Command oluşturulur, Handler işler
- İş mantığı Handler'da toplanır (Controller'dan ayrılır)

**Dependency Injection:**
- `CatalogDbContext` → Veritabanı işlemleri için
- `IMapper` → AutoMapper, Command → Entity mapping için

**Akış:**
1. Controller command'i MediatR'a gönderir
2. MediatR `CreateProductHandler`'ı bulur
3. Handler'ın `Handle` metodu çağrılır
4. Command → Entity dönüşümü yapılır
5. Veritabanına kaydedilir
6. Product ID döner

**Sonuç:** ✅ CreateProductHandler oluşturuldu

---

### Adım 3: CreateProductValidator Oluştur

**Dosya:**
- `Features/Products/Commands/CreateProduct/CreateProductValidator.cs`

**Ne yapıldı:**
- `CreateProductCommand` için validation kurallarını tanımlayan class oluşturuldu
- FluentValidation'ın `AbstractValidator<CreateProductCommand>` class'ını inherit etti

**Kod:**
```csharp
using FluentValidation;

namespace Catalog.API.Features.Products.Commands.CreateProduct;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ürün adı boş olamaz")
            .MaximumLength(100).WithMessage("Ürün adı en fazla 100 karakter olabilir");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Fiyat 0'dan büyük olmalı");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Kategori seçilmeli");
    }
}
```

**Ne işe yarar:**
- `CreateProductCommand` için validation kuralları tanımlar
- FluentValidation ile veri doğrulama yapar
- Hatalı veri gelirse `ValidationException` fırlatır (ValidationBehavior sayesinde)

**Neden gerekli?**
- Gelen verinin doğruluğunu kontrol etmek için
- Geçersiz veriyi engellemek, hata mesajları göstermek için

**Validation Kuralları:**
- `Name`: Boş olamaz, max 100 karakter
- `Price`: 0'dan büyük olmalı
- `CategoryId`: Boş olamaz (Guid.Empty olamaz)

**Nasıl Çalışır?**
- MediatR pipeline'ında `ValidationBehavior` otomatik çalışır
- `ValidationBehavior`, `CreateProductValidator`'ı bulur ve çalıştırır
- Hata varsa `ValidationException` fırlatılır, Handler'a gitmez
- Hata yoksa Handler çalışır

**Sonuç:** ✅ CreateProductValidator oluşturuldu

---

### Adım 4: UpdateProductCommand Oluştur

**Dosya:**
- `Features/Products/Commands/UpdateProduct/UpdateProductCommand.cs`

**Ne yapıldı:**
- Mevcut ürünü güncelleme komutunu tanımlayan class oluşturuldu
- MediatR'ın `IRequest<Unit>` interface'ini implement etti

**Kod:**
```csharp
using MediatR;

namespace Catalog.API.Features.Products.Commands.UpdateProduct;

public class UpdateProductCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryId { get; set; }
}
```

**Ne işe yarar:**
- Mevcut ürünü güncelleme komutunu tanımlar
- CreateProductCommand'e benzer, ancak `Id` içerir (hangi ürün güncellenecek)

**CreateProductCommand ile farkları:**
- `IRequest<Unit>` → `IRequest<Guid>` yerine (hiçbir şey dönmez)
- `Id` property'si var → Hangi ürün güncellenecek

**Neden Unit?**
- Update işlemi başarılı olduğunda değer döndürmeye gerek yok
- `Unit` = "hiçbir şey" anlamına gelir (void gibi, ama generic constraint'ler için gerekli)

**Sonuç:** ✅ UpdateProductCommand oluşturuldu

---

### Adım 5: UpdateProductHandler Oluştur

**Dosya:**
- `Features/Products/Commands/UpdateProduct/UpdateProductHandler.cs`

**Ne yapıldı:**
- `UpdateProductCommand`'i işleyen Handler class'ı oluşturuldu
- MediatR'ın `IRequestHandler<UpdateProductCommand, Unit>` interface'ini implement etti

**Kod:**
```csharp
using MediatR;
using AutoMapper;
using Catalog.API.Data;
using Catalog.API.Entities;
using BuildingBlocks.Exceptions.Exceptions;

namespace Catalog.API.Features.Products.Commands.UpdateProduct;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, Unit>
{
    private readonly CatalogDbContext _context;
    private readonly IMapper _mapper;

    public UpdateProductHandler(CatalogDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Unit> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Ürünü bul
        var product = await _context.Products.FindAsync(request.Id, cancellationToken);
        
        if (product == null)
            throw new NotFoundException(nameof(Product), request.Id);

        // 2. Command'den Entity'yi güncelle (AutoMapper ile)
        _mapper.Map(request, product);

        // 3. Değişiklikleri kaydet
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Unit döndür (hiçbir şey dönmez)
        return Unit.Value;
    }
}
```

**Ne işe yarar:**
- `UpdateProductCommand`'i alır
- Ürünü veritabanından bulur (Id ile)
- Ürün yoksa `NotFoundException` fırlatır
- AutoMapper ile Command → Entity mapping yapar (mevcut entity'yi günceller)
- Veritabanına kaydeder
- Hiçbir şey dönmez (`Unit`)

**CreateProductHandler ile farkları:**
- `IRequestHandler<UpdateProductCommand, Unit>` → `Unit` döner
- `FindAsync(request.Id)` → Ürünü bulur
- `NotFoundException` → Ürün yoksa fırlatır
- `_mapper.Map(request, product)` → Mevcut entity'yi günceller (yeni oluşturmaz)
- `Unit.Value` → Hiçbir şey döner

**Önemli:**
- `_mapper.Map(request, product)` → Mevcut entity'ye property'leri kopyalar (Id hariç)
- `SaveChangesAsync()` → EF Core değişiklikleri algılar ve günceller

**Sonuç:** ✅ UpdateProductHandler oluşturuldu

---

### Adım 6: UpdateProductValidator Oluştur

**Dosya:**
- `Features/Products/Commands/UpdateProduct/UpdateProductValidator.cs`

**Ne yapıldı:**
- `UpdateProductCommand` için validation kurallarını tanımlayan class oluşturuldu
- FluentValidation'ın `AbstractValidator<UpdateProductCommand>` class'ını inherit etti

**Kod:**
```csharp
using FluentValidation;

namespace Catalog.API.Features.Products.Commands.UpdateProduct;

public class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Ürün ID'si boş olamaz");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ürün adı boş olamaz")
            .MaximumLength(100).WithMessage("Ürün adı en fazla 100 karakter olabilir");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Fiyat 0'dan büyük olmalı");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Kategori seçilmeli");
    }
}
```

**Ne işe yarar:**
- `UpdateProductCommand` için validation kuralları tanımlar
- CreateProductValidator ile benzer kurallar + `Id` kontrolü

**CreateProductValidator ile farkı:**
- `Id` kuralı eklendi (Update'te hangi ürün güncelleneceği için gerekli)
- Diğer kurallar aynı (Name, Price, CategoryId)

**Neden Id kontrolü gerekli?**
- Update işleminde hangi ürün güncelleneceğini bilmek için Id zorunlu
- Guid.Empty kontrolü yapılır

**Sonuç:** ✅ UpdateProductValidator oluşturuldu

---

### Adım 7: DeleteProductCommand Oluştur

**Dosya:**
- `Features/Products/Commands/DeleteProduct/DeleteProductCommand.cs`

**Ne yapıldı:**
- Ürün silme komutunu tanımlayan class oluşturuldu
- MediatR'ın `IRequest<Unit>` interface'ini implement etti

**Kod:**
```csharp
using MediatR;

namespace Catalog.API.Features.Products.Commands.DeleteProduct;

public class DeleteProductCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
}
```

**Ne işe yarar:**
- Ürün silme komutunu tanımlar
- Sadece `Id` property'si içerir (hangi ürün silinecek)

**Neden bu kadar basit?**
- Silme işlemi için sadece ID yeterli
- Diğer bilgilere gerek yok

**Özellikler:**
- `IRequest<Unit>` → Hiçbir şey dönmez (void gibi)
- Sadece `Id` property'si → Hangi ürün silinecek
- En basit command (sadece ID gerekiyor)

**Sonuç:** ✅ DeleteProductCommand oluşturuldu

---

### Adım 8: DeleteProductHandler Oluştur

**Dosya:**
- `Features/Products/Commands/DeleteProduct/DeleteProductHandler.cs`

**Ne yapıldı:**
- `DeleteProductCommand`'i işleyen Handler class'ı oluşturuldu
- MediatR'ın `IRequestHandler<DeleteProductCommand, Unit>` interface'ini implement etti

**Kod:**
```csharp
using MediatR;
using Catalog.API.Data;
using Catalog.API.Entities;
using BuildingBlocks.Exceptions.Exceptions;

namespace Catalog.API.Features.Products.Commands.DeleteProduct;

public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, Unit>
{
    private readonly CatalogDbContext _context;

    public DeleteProductHandler(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Ürünü bul
        var product = await _context.Products.FindAsync(request.Id, cancellationToken);
        
        if (product == null)
            throw new NotFoundException(nameof(Product), request.Id);

        // 2. Ürünü sil
        _context.Products.Remove(product);

        // 3. Değişiklikleri kaydet
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Unit döndür (hiçbir şey dönmez)
        return Unit.Value;
    }
}
```

**Ne işe yarar:**
- `DeleteProductCommand`'i alır
- Ürünü veritabanından bulur (Id ile)
- Ürün yoksa `NotFoundException` fırlatır
- `Remove` ile siler
- Veritabanına kaydeder
- Hiçbir şey dönmez (`Unit`)

**Neden IMapper yok?**
- Delete işleminde mapping'e gerek yok
- Sadece silme işlemi yapılır

**Remove vs Delete:**
- `_context.Products.Remove(product)` → Entity'yi context'ten kaldırır
- `SaveChangesAsync()` → Veritabanından siler
- EF Core'da `Remove` kullanılır (DELETE SQL'i oluşturur)

**Sonuç:** ✅ DeleteProductHandler oluşturuldu

---

### Adım 9: AutoMapper Profile Oluştur

**Dosya:**
- `Mapping/MappingProfile.cs`

**Ne yapıldı:**
- AutoMapper için Profile class'ı oluşturuldu
- Command → Entity mapping'leri tanımlandı

**Kod:**
```csharp
using AutoMapper;
using Catalog.API.Entities;
using Catalog.API.Features.Products.Commands.CreateProduct;
using Catalog.API.Features.Products.Commands.UpdateProduct;

namespace Catalog.API.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Command → Entity
        CreateMap<CreateProductCommand, Product>();
        CreateMap<UpdateProductCommand, Product>();
        
        // Entity → DTO (DTO'lar henüz oluşturulmadı, Faz 3.4'te eklenecek)
        // CreateMap<Product, ProductDto>();
    }
}
```

**Ne işe yarar:**
- Command ↔ Entity mapping'lerini tanımlar
- AutoMapper'ın nasıl dönüşüm yapacağını belirtir

**Neden gerekli?**
- Manuel dönüşüm kod yazmak yerine AutoMapper otomatik yapar
- Kod tekrarını önler
- Mapping kuralları tek yerde toplanır

**Mapping Kuralları:**
- `CreateProductCommand` → `Product`: Property adları aynı olduğu için otomatik mapping yapılır
- `UpdateProductCommand` → `Product`: Aynı şekilde otomatik mapping

**Önemli Not:**
- Entity → DTO mapping'leri henüz eklenmedi (DTO'lar Faz 3.4'te oluşturulacak)
- Faz 3.4'te ProductDto oluşturulunca mapping eklenecek

**Sonuç:** ✅ AutoMapper Profile oluşturuldu

---

## 3.3 Bölümü - Tamamlanan Kontroller

✅ CreateProductCommand oluşturuldu
✅ CreateProductHandler oluşturuldu
✅ CreateProductValidator oluşturuldu
✅ UpdateProductCommand oluşturuldu
✅ UpdateProductHandler oluşturuldu
✅ UpdateProductValidator oluşturuldu
✅ DeleteProductCommand oluşturuldu
✅ DeleteProductHandler oluşturuldu
✅ AutoMapper Profile oluşturuldu (Command → Entity mapping'leri)
✅ Proje build oluyor mu? (`dotnet build`) → ✅ Başarıyla build oluyor (0 uyarı, 0 hata)

---

## Öğrenilenler (Faz 3.3)

### MediatR ve IRequest<TResponse>

**MediatR Nedir?**
- Controller ile Handler arasında aracı görevi gören kütüphane
- Command/Query'leri Handler'lara yönlendirir
- Pipeline behavior'lar eklenebilir (Validation, Logging)
- In-process messaging pattern uygular (aynı process içinde mesajlaşma)

**IRequest<TResponse> Nedir ve Nasıl Çalışır?**

`IRequest<TResponse>`, MediatR'da Command/Query'leri temsil eden bir marker interface'dir (boş interface).

```csharp
// MediatR içindeki tanım (basitleştirilmiş)
public interface IRequest<out TResponse> : IBaseRequest
{
    // Boş! Sadece bir işaretçi
}
```

**Nasıl Kullanılır?**
```csharp
// Command tanımı
public class CreateProductCommand : IRequest<Guid>
{
    // IRequest<Guid> → Bu command'in handler'ı Guid dönecek
    // Guid = Product ID
}
```

**TResponse Parametresi:**
- `TResponse`: Handler'ın döneceği tipi belirtir
- `IRequest<Guid>` → Handler `Task<Guid>` döner
- `IRequest<Unit>` → Handler `Task<Unit>` döner (hiçbir şey dönmez)
- `IRequest<ProductDto>` → Handler `Task<ProductDto>` döner

**Neden TResponse Gerekli?**
- Generic constraint'ler için gerekli
- Handler'ın return type'ını belirlemek için
- Type safety sağlar (compile-time kontrol)

**IRequestHandler<TRequest, TResponse> Nedir ve Nasıl Çalışır?**

`IRequestHandler<TRequest, TResponse>`, MediatR'da Handler'ları temsil eden interface'dir.

```csharp
// MediatR içindeki tanım (basitleştirilmiş)
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
```

**Nasıl Kullanılır?**
```csharp
// Handler tanımı
public class CreateProductHandler : IRequestHandler<CreateProductCommand, Guid>
//                                                              ↑         ↑
//                                                         TRequest   TResponse
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    //                ↑
    //         Return type = Guid (TResponse'den geliyor)
    {
        // İş mantığı
        return productId;  // Guid döndürüyor
    }
}
```

**Generic Constraint:**
```csharp
where TRequest : IRequest<TResponse>
```
- `TRequest` mutlaka `IRequest<TResponse>` implement etmeli
- Bu sayede type safety sağlanır
- Örnek: `CreateProductCommand : IRequest<Guid>` → `TRequest` = `CreateProductCommand`, `TResponse` = `Guid`

**MediatR Nasıl Eşleştirme Yapar?**

1. **Command/Query gönderilir:**
```csharp
await _mediator.Send(new CreateProductCommand { ... });
```

2. **MediatR Handler'ı bulur:**
   - `CreateProductCommand` tipini alır
   - `IRequestHandler<CreateProductCommand, Guid>` tipindeki handler'ı arar
   - DI container'dan `CreateProductHandler` instance'ını alır

3. **Handler çalıştırılır:**
   - `CreateProductHandler.Handle()` metodu çağrılır
   - Command parametre olarak geçilir
   - Handler iş mantığını çalıştırır
   - `Task<Guid>` döner (TResponse tipinde)

**Akış Diyagramı (Detaylı):**

**1. Controller - Command Gönderimi**
- **Dosya:** `Controllers/ProductsController.cs` (örnek)
- **Kod:** `await _mediator.Send(new CreateProductCommand { ... })`
- **IMediator:** DI container'dan constructor injection ile alındı

**2. MediatR - Handler ve Behavior'ları Bulma**
- **Command tipi:** `CreateProductCommand : IRequest<Guid>`
- **Handler bulma:** DI Container'dan `IRequestHandler<CreateProductCommand, Guid>` → `CreateProductHandler` (Scoped)
- **Behavior'lar:** DI Container'dan `IPipelineBehavior<CreateProductCommand, Guid>[]`
  - `LoggingBehavior<CreateProductCommand, Guid>` (varsa)
  - `ValidationBehavior<CreateProductCommand, Guid>`
- **Sıra:** Program.cs'de eklendiği sıraya göre

**3. Pipeline Behaviors (Sırayla Çalışır)**

**3.1 LoggingBehavior** (varsa)
- **Dosya:** `BuildingBlocks.Behaviors/Behaviors/LoggingBehavior.cs`
- **İşlem:** Request loglanır → `next()` çağrılır (ValidationBehavior'a geçer)

**3.2 ValidationBehavior**
- **Dosya:** `BuildingBlocks.Behaviors/Behaviors/ValidationBehavior.cs`
- **Validator bulma:** DI Container'dan `IValidator<CreateProductCommand>[]` → `[CreateProductValidator]`
- **Validator dosyası:** `Features/.../CreateProduct/CreateProductValidator.cs`
- **İşlem:** 
  - `CreateProductValidator.ValidateAsync()` çağrılır
  - FluentValidation kuralları kontrol edilir
  - Hata varsa → `ValidationException` fırlatılır (Handler'a gitmez)
  - Hata yoksa → `next()` çağrılır (CreateProductHandler'a geçer)

**4. CreateProductHandler - İş Mantığı**
- **Dosya:** `Features/Products/Commands/CreateProduct/CreateProductHandler.cs`
- **DI Container'dan (constructor):**
  - `CatalogDbContext _context` (Scoped)
  - `IMapper _mapper` (Singleton)
- **İşlemler:**
  1. AutoMapper ile mapping: `_mapper.Map<Product>(request)`
     - MappingProfile: `Mapping/MappingProfile.cs` → `CreateMap<CreateProductCommand, Product>()`
  2. Entity oluşturulur: `product.Id = Guid.NewGuid()`
  3. Veritabanına eklenir: `_context.Products.Add(product)` (EF Core Added state)
  4. Kaydedilir: `await _context.SaveChangesAsync()` (PostgreSQL INSERT SQL)
  5. Product ID döndürülür: `return product.Id` (Guid)

**5. Response Geri Dönüş (Ters Sırada)**
```
Guid (Product ID)
    ↓
ValidationBehavior.next() → Guid (pipeline devam eder)
    ↓
LoggingBehavior.next() → Guid (response loglanır)
    ↓
MediatR.Send() → Guid
    ↓
ProductsController.CreateProduct() → Guid alır
    ↓
HTTP 201 Created Response (Location: /api/products/{id})
```

**DI Container Kayıtları (Program.cs - Faz 3.6'da eklenecek):**

**Handler'lar:**
```csharp
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});
// → CreateProductHandler : IRequestHandler<CreateProductCommand, Guid> (Scoped)
```

**Pipeline Behavior'lar:**
```csharp
builder.Services.AddMediatR(cfg => {
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});
// → Pipeline'a sırayla eklenir
```

**Validator'lar:**
```csharp
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
// → CreateProductValidator : AbstractValidator<CreateProductCommand>
// → IValidator<CreateProductCommand> olarak kaydedilir (Scoped)
```

**DbContext:**
```csharp
builder.Services.AddDbContext<CatalogDbContext>(...);
// → Scoped lifetime
```

**AutoMapper:**
```csharp
builder.Services.AddAutoMapper(typeof(Program).Assembly);
// → MappingProfile otomatik bulunur ve kaydedilir (Singleton)
```

**Örnek Senaryo:**

```csharp
// 1. Command tanımı
public class CreateProductCommand : IRequest<Guid>
{
    public string Name { get; set; }
    // IRequest<Guid> → Handler Guid dönecek
}

// 2. Handler tanımı
public class CreateProductHandler : IRequestHandler<CreateProductCommand, Guid>
{
    // IRequestHandler<CreateProductCommand, Guid>
    // → CreateProductCommand'i işler, Guid döner
    
    public async Task<Guid> Handle(CreateProductCommand request, ...)
    {
        // İş mantığı
        return productId;  // Guid döndürüyor
    }
}

// 3. Controller'dan kullanım
var productId = await _mediator.Send(new CreateProductCommand { Name = "iPhone" });
//                                                                              ↑
//                                                                        Guid döner
```

**IRequest vs IRequestHandler İlişkisi:**

| Bileşen | Rol | Generic Parametre |
|---------|-----|-------------------|
| `IRequest<TResponse>` | Command/Query'yi temsil eder (marker interface) | `TResponse`: Handler'ın döneceği tip |
| `IRequestHandler<TRequest, TResponse>` | Handler'ı temsil eder (iş mantığını içerir) | `TRequest`: İşlenecek Command/Query tipi<br>`TResponse`: Dönecek tip |
| **Constraint** | `where TRequest : IRequest<TResponse>` | `TRequest` ve `TResponse` eşleşmeli |

**Özet:**
- `IRequest<TResponse>`: Command/Query tipini tanımlar (sadece bir işaretçi)
- `IRequestHandler<TRequest, TResponse>`: İş mantığını içerir
- MediatR, `IRequest<TResponse>` tipine göre doğru `IRequestHandler<TRequest, TResponse>` handler'ını bulur
- Generic constraint sayesinde type safety sağlanır

### Unit (MediatR)

**Unit Nedir?**
- MediatR'da "hiçbir şey" anlamına gelir
- `void` yerine kullanılır (generic constraint'ler için)
- `IRequest<Unit>` → Handler hiçbir şey dönmez
- `Unit.Value` → Unit instance'ı döndürülür

**Neden Unit?**
- C# generic'lerde `void` kullanılamaz
- `Task<Unit>` → Async metodlar için gerekli
- Update/Delete gibi işlemlerde değer döndürmeye gerek yok

**Kullanım:**
```csharp
public class DeleteProductCommand : IRequest<Unit> { }
public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, Unit>
{
    public async Task<Unit> Handle(...)
    {
        // İşlem yap
        return Unit.Value;  // Hiçbir şey dönmez
    }
}
```

### FluentValidation ve ValidationBehavior İlişkisi

**FluentValidation Nedir?**
- Validation kurallarını tanımlamak için kullanılan kütüphane
- `AbstractValidator<T>` class'ını inherit eder
- `RuleFor` ile kurallar tanımlanır

**ValidationBehavior Nedir?**
- MediatR pipeline'ında çalışan bir behavior
- Tüm request'leri otomatik olarak FluentValidation ile doğrular
- Hata varsa `ValidationException` fırlatır

**Nasıl Çalışır?**
1. Controller command'i MediatR'a gönderir
2. ValidationBehavior pipeline'da çalışır
3. DI container'dan `IValidator<TRequest>` tipindeki validator'ları alır
4. Her validator'ı çalıştırır (`ValidateAsync()`)
5. Hata varsa `ValidationException` fırlatır, Handler'a gitmez
6. Hata yoksa Handler çalışır

**Örnek:**
```csharp
// Validator
public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    // Kurallar tanımlanır
}

// ValidationBehavior otomatik bulur ve çalıştırır
// CreateProductValidator → IValidator<CreateProductCommand> implement ediyor
// ValidationBehavior → DI container'dan IValidator<CreateProductCommand> alır
```

### AutoMapper ve Profile

**AutoMapper Nedir?**
- Object-to-object mapping kütüphanesi
- Entity ↔ DTO, Command → Entity dönüşümlerini kolaylaştırır
- Manuel mapping kod yazmaya gerek yok

**Profile Nedir?**
- AutoMapper mapping kurallarını tanımlayan class
- `Profile` class'ını inherit eder
- `CreateMap<TSource, TDestination>()` ile mapping tanımlanır

**Kullanım:**
```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CreateProductCommand, Product>();
        // Property adları aynıysa otomatik mapping yapılır
    }
}

// Kullanım
var product = _mapper.Map<Product>(command);
```

**Map Metotları:**
- `Map<TDestination>(source)` → Yeni instance oluşturur
- `Map(source, destination)` → Mevcut instance'ı günceller (Update için)

### NotFoundException

**NotFoundException Nedir?**
- BuildingBlocks.Exceptions içinde tanımlı custom exception
- Varlık bulunamadığında fırlatılır
- GlobalExceptionHandler yakalar ve 404 Not Found döner

**Kullanım:**
```csharp
var product = await _context.Products.FindAsync(id);
if (product == null)
    throw new NotFoundException(nameof(Product), id);
// GlobalExceptionHandler yakalar → 404 Not Found döner
```

### CQRS Pattern - Command Side

**CQRS Nedir?**
- Command Query Responsibility Segregation
- Okuma (Query) ve yazma (Command) işlemlerini ayırma
- Her işlem için ayrı Handler'lar

**Command Nedir?**
- Veriyi değiştiren işlemler (Create, Update, Delete)
- `IRequest<TResponse>` implement eder
- Handler ile işlenir

**Command Yapısı:**
```
CreateProduct/
  ├── CreateProductCommand.cs      (Command)
  ├── CreateProductHandler.cs      (Handler)
  └── CreateProductValidator.cs    (Validator)
```

**Avantajları:**
- ✅ Kod organizasyonu: İlgili dosyalar bir arada
- ✅ Okunabilirlik: Her işlem için açık klasör yapısı
- ✅ Bakım kolaylığı: Değişiklik yaparken ilgili dosyaları kolay bulma
- ✅ Test edilebilirlik: Her handler bağımsız test edilebilir

### EF Core - Add, Update, Remove

**Add:**
```csharp
var product = new Product { ... };
_context.Products.Add(product);
await _context.SaveChangesAsync();
// INSERT SQL'i çalıştırılır
```

**Update:**
```csharp
var product = await _context.Products.FindAsync(id);
_mapper.Map(command, product);  // Property'leri güncelle
await _context.SaveChangesAsync();
// UPDATE SQL'i çalıştırılır (EF Core değişiklikleri algılar)
```

**Remove:**
```csharp
var product = await _context.Products.FindAsync(id);
_context.Products.Remove(product);
await _context.SaveChangesAsync();
// DELETE SQL'i çalıştırılır
```

**Önemli:**
- `SaveChangesAsync()` çağrılmadan değişiklikler veritabanına yazılmaz
- EF Core Change Tracking sayesinde değişiklikleri algılar
- `FindAsync()` → Primary key ile arar, hızlıdır

---

## 3.4 Catalog CQRS - Products Queries - Yapılanlar

**Hedef:** Product okuma işlemleri (GET) için CQRS pattern ile Query'ler oluşturmak

---

### Adım 1: DTO'ları Oluştur

**Dosya Konumları:**
- `src/Services/Catalog/Catalog.API/Dtos/ProductDto.cs`
- `src/Services/Catalog/Catalog.API/Dtos/CreateProductDto.cs`
- `src/Services/Catalog/Catalog.API/Dtos/UpdateProductDto.cs`

**Klasör Yapısı:**
```
Catalog.API/
└── Dtos/                    ← API response/request için DTO'lar
    ├── ProductDto.cs        ← Entity → API response mapping
    ├── CreateProductDto.cs  ← Create request için (gelecekte kullanılabilir)
    └── UpdateProductDto.cs  ← Update request için (gelecekte kullanılabilir)
```

**Neden `Dtos/` klasöründe?**
- DTO'lar API katmanına ait olduğu için proje root'una yakın
- Tüm DTO'lar bir arada, kolay bulunur
- Entity'lerden (`Entities/`) ayrı, sorumluluklar net

**ProductDto.cs:**

**Namespace:** `Catalog.API.Dtos`

**Kullanılan Kütüphaneler:**
- ❌ **Harici kütüphane yok** (sadece .NET built-in tipler kullanılıyor)

**Built-in Tipler:**
- `Guid`: Entity ID'si için (benzersiz tanımlayıcı)
- `string`: Metin alanları (Name, Description, ImageUrl, CategoryName)
- `decimal`: Fiyat için (ondalıklı sayı, para birimi için uygun)
- `string.Empty`: Default değer (null yerine boş string)

**Property'ler:**
- `Id` (Guid): Ürün ID'si
- `Name` (string): Ürün adı
- `Description` (string?): Ürün açıklaması (nullable)
- `Price` (decimal): Ürün fiyatı
- `ImageUrl` (string?): Ürün resmi URL'i (nullable)
- `CategoryId` (Guid): Kategori ID'si (foreign key)
- `CategoryName` (string): **Ekstra alan** - Navigation property'den (`Product.Category.Name`) alınır

**Ne işe yarar:**
- **DTO (Data Transfer Object)**: API response'ları için veri transfer nesneleri
- Entity'lerin doğrudan döndürülmesini engeller
- API contract'ını kontrol eder (hangi alanlar dönecek)

**Neden gerekli?**
- **Güvenlik**: Entity'lerin tüm property'lerini kullanıcıya göstermeyiz
- **Esneklik**: Entity yapısı değişse bile API contract'ı sabit kalır
- **Ekstra alanlar**: CategoryName gibi navigation property'den gelen alanları ekleyebiliriz
- **Versioning**: API versiyonlama için farklı DTO'lar kullanılabilir

**CategoryName neden önemli?**
- Entity'de sadece `CategoryId` var, ama API'de kategori adını da göstermek istiyoruz
- Navigation property (`Product.Category.Name`) AutoMapper ile DTO'ya aktarılır
- Bu sayede client tek sorgu ile hem ID hem de ad bilgisini alır

**CreateProductDto.cs ve UpdateProductDto.cs:**

**Namespace:** `Catalog.API.Dtos`

**Kullanılan Kütüphaneler:**
- ❌ **Harici kütüphane yok** (sadece .NET built-in tipler)

**Ne zaman kullanılacak?**
- Şu an Command'lar (`CreateProductCommand`, `UpdateProductCommand`) kullanılıyor
- Gelecekte Controller'larda direkt DTO kullanılırsa bu DTO'lar kullanılabilir
- Alternatif yaklaşım: Command'lar DTO gibi kullanılıyor (aynı amaç)

**Sonuç:** ✅ DTO'lar oluşturuldu

---

### Adım 2: GetProductsQuery + Handler Oluştur

**Dosya Konumları:**
- `src/Services/Catalog/Catalog.API/Features/Products/Queries/GetProducts/GetProductsQuery.cs`
- `src/Services/Catalog/Catalog.API/Features/Products/Queries/GetProducts/GetProductsHandler.cs`

**Klasör Yapısı:**
```
Catalog.API/
└── Features/
    └── Products/
        └── Queries/                    ← CQRS Query işlemleri
            └── GetProducts/            ← "Tüm ürünleri getir" query'si
                ├── GetProductsQuery.cs      ← Query tanımı
                └── GetProductsHandler.cs    ← Query işleyici
```

**Neden `Features/Products/Queries/GetProducts/` klasöründe?**
- CQRS pattern: Her feature kendi klasöründe (Products, Categories, vb.)
- Query'ler ayrı klasörde (Commands'tan ayrı)
- Her query kendi klasöründe (GetProducts, GetProductById, vb.)
- İlgili dosyalar bir arada, kolay bulunur ve organize

---

**GetProductsQuery.cs:**

**Namespace:** `Catalog.API.Features.Products.Queries.GetProducts`

**Kullanılan Kütüphaneler:**
- `MediatR`: `IRequest<IEnumerable<ProductDto>>` için
- `Catalog.API.Dtos`: `ProductDto` tipi için

**MediatR - IRequest<TResponse>:**
- **Ne işe yarar**: MediatR pattern'i için Query tanımı
- **IRequest<IEnumerable<ProductDto>>**: Bu query `IEnumerable<ProductDto>` dönecek
- **Neden gerekli**: MediatR'a "bu query'yi işle" komutunu iletir

**Property'ler:**
- `PageNumber` (int, default: 1): Hangi sayfa (1'den başlar)
- `PageSize` (int, default: 10): Sayfa başına kaç kayıt
- `CategoryId` (Guid?, nullable): Opsiyonel kategori filtresi (null ise tüm ürünler)

**Ne işe yarar:**
- Tüm ürünleri getirme sorgusunu tanımlar
- Sayfalama parametreleri içerir
- Filtreleme parametresi içerir

---

**GetProductsHandler.cs:**

**Namespace:** `Catalog.API.Features.Products.Queries.GetProducts`

**Kullanılan Kütüphaneler:**
- `MediatR`: `IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>` için
- `AutoMapper`: `IMapper` - Entity → DTO mapping için
- `Microsoft.EntityFrameworkCore`: `Include`, `AsQueryable`, `ToListAsync` için
- `Catalog.API.Data`: `CatalogDbContext` için
- `Catalog.API.Dtos`: `ProductDto` tipi için

**Kütüphane Detayları:**

**1. MediatR - IRequestHandler<TRequest, TResponse>:**
- **Ne işe yarar**: Query'yi işleyen handler interface'i
- **IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>**: 
  - `GetProductsQuery` tipindeki query'yi işler
  - `IEnumerable<ProductDto>` döner
- **Neden gerekli**: MediatR pattern'i, iş mantığını handler'da toplar

**2. AutoMapper - IMapper:**
- **Ne işe yarar**: Entity → DTO dönüşümü
- **Kullanım**: `_mapper.Map<IEnumerable<ProductDto>>(products)`
- **Neden gerekli**: Manuel dönüşüm kod yazmaya gerek yok
- **Mapping Profile**: `Mapping/MappingProfile.cs` içinde tanımlı

**3. Entity Framework Core - Include:**
- **Ne işe yarar**: Navigation property'leri eager loading ile yükler
- **Kullanım**: `.Include(p => p.Category)`
- **Neden gerekli**: CategoryName için `Product.Category.Name`'e erişmek gerekir
- **SQL Karşılığı**: `LEFT JOIN Categories ON Products.CategoryId = Categories.Id`

**4. Entity Framework Core - AsQueryable:**
- **Ne işe yarar**: IQueryable döner, sorgu henüz çalıştırılmadı
- **Kullanım**: `.AsQueryable()` → Koşullu filtreleme yapılabilir
- **Neden gerekli**: Veritabanında filtreleme/sayfalama yapılır (performans)
- **Alternatif**: `ToList()` olsaydı, tüm veri memory'ye çekilir, sonra filtreleme yapılırdı (yavaş)

**5. Entity Framework Core - Skip ve Take:**
- **Skip**: Belirtilen sayıda kaydı atlar (sayfalama için)
- **Take**: Belirtilen sayıda kayıt alır (sayfa boyutu için)
- **SQL Karşılığı**: `OFFSET ... ROWS FETCH NEXT ... ROWS ONLY`

**6. Entity Framework Core - ToListAsync:**
- **Ne işe yarar**: SQL sorgusunu çalıştırır, liste olarak döner
- **Kullanım**: `.ToListAsync(cancellationToken)`
- **Neden Async**: Veritabanı I/O işlemi, async/await ile thread blocking önlenir

**7. CatalogDbContext:**
- **Ne işe yarar**: EF Core DbContext, veritabanı işlemleri için
- **Kullanım**: `_context.Products` → Products tablosuna erişim
- **Lifetime**: Scoped (her HTTP request'te yeni instance)

**Handler İşlemleri:**
1. **Query oluştur**: `_context.Products.Include(p => p.Category).AsQueryable()`
   - Include ile Category navigation property yüklenir (CategoryName için)
2. **Filtreleme**: `CategoryId` varsa `Where(p => p.CategoryId == request.CategoryId)`
3. **Sayfalama**: `Skip((PageNumber - 1) * PageSize).Take(PageSize)`
4. **SQL çalıştır**: `ToListAsync()` → Veritabanında sorgu çalışır
5. **Mapping**: AutoMapper ile `Product` → `ProductDto` dönüşümü
6. **Liste döndür**: `IEnumerable<ProductDto>`

**Önemli Notlar:**
- **Include**: Navigation property'yi eager loading ile yükler (CategoryName için gerekli)
- **AsQueryable()**: IQueryable döner, veritabanında filtreleme/sayfalama yapılır (performans)
- **ToListAsync()**: SQL sorgusu çalıştırılır, liste olarak döner
- **Sayfalama formülü**: `Skip((pageNumber - 1) * pageSize)` → İlk sayfa için 0, ikinci sayfa için 10, vb.

**Sonuç:** ✅ GetProductsQuery + Handler oluşturuldu

---

### Adım 3: GetProductByIdQuery + Handler Oluştur

**Dosya Konumları:**
- `src/Services/Catalog/Catalog.API/Features/Products/Queries/GetProductById/GetProductByIdQuery.cs`
- `src/Services/Catalog/Catalog.API/Features/Products/Queries/GetProductById/GetProductByIdHandler.cs`

**Klasör Yapısı:**
```
Catalog.API/
└── Features/
    └── Products/
        └── Queries/
            └── GetProductById/         ← "ID'ye göre ürün getir" query'si
                ├── GetProductByIdQuery.cs      ← Query tanımı
                └── GetProductByIdHandler.cs    ← Query işleyici
```

**Neden `Features/Products/Queries/GetProductById/` klasöründe?**
- Her query kendi klasöründe (GetProducts, GetProductById ayrı)
- İlgili dosyalar bir arada
- CQRS pattern organizasyonu

---

**GetProductByIdQuery.cs:**

**Namespace:** `Catalog.API.Features.Products.Queries.GetProductById`

**Kullanılan Kütüphaneler:**
- `MediatR`: `IRequest<ProductDto>` için
- `Catalog.API.Dtos`: `ProductDto` tipi için

**MediatR - IRequest<TResponse>:**
- **IRequest<ProductDto>**: Bu query `ProductDto` dönecek (tek nesne, liste değil)
- GetProductsQuery'den farkı: `IEnumerable<ProductDto>` yerine `ProductDto`

**Property:**
- `Id` (Guid): Hangi ürün getirilecek

**Ne işe yarar:**
- ID'ye göre tek ürün getirme sorgusunu tanımlar
- Ürün detay sayfası için kullanılır

---

**GetProductByIdHandler.cs:**

**Namespace:** `Catalog.API.Features.Products.Queries.GetProductById`

**Kullanılan Kütüphaneler:**
- `MediatR`: `IRequestHandler<GetProductByIdQuery, ProductDto>` için
- `AutoMapper`: `IMapper` - Entity → DTO mapping için
- `Microsoft.EntityFrameworkCore`: `Include`, `FirstOrDefaultAsync` için
- `Catalog.API.Data`: `CatalogDbContext` için
- `Catalog.API.Entities`: `Product` entity tipi için (`nameof(Product)` için)
- `Catalog.API.Dtos`: `ProductDto` tipi için
- `BuildingBlocks.Exceptions.Exceptions`: `NotFoundException` için

**Kütüphane Detayları:**

**1. Entity Framework Core - FirstOrDefaultAsync:**
- **Ne işe yarar**: Predicate ile entity arar, bulamazsa null döner
- **Kullanım**: `.FirstOrDefaultAsync(p => p.Id == request.Id)`
- **Neden FindAsync değil?**: 
  - `FindAsync` Include ile çalışmaz (navigation property yüklenmez)
  - `FirstOrDefaultAsync` Include ile çalışır (CategoryName için gerekli)
- **SQL**: `SELECT * FROM Products WHERE Id = @id`

**2. BuildingBlocks.Exceptions - NotFoundException:**
- **Ne işe yarar**: Varlık bulunamadığında fırlatılan custom exception
- **Kullanım**: `throw new NotFoundException(nameof(Product), request.Id)`
- **Neden gerekli**: Ürün yoksa 404 Not Found dönmek için
- **GlobalExceptionHandler**: Exception'ı yakalar, HTTP 404 response döner

**Handler İşlemleri:**
1. **Ürünü bul**: `FirstOrDefaultAsync(p => p.Id == request.Id)`
   - Include ile Category navigation property yüklenir
2. **NotFound kontrolü**: Ürün null ise `NotFoundException` fırlatılır
3. **Mapping**: AutoMapper ile `Product` → `ProductDto` dönüşümü
4. **Ürün döndür**: `ProductDto`

**Önemli Notlar:**
- **FindAsync vs FirstOrDefaultAsync**: 
  - `FindAsync` Include ile çalışmaz (navigation property yüklenmez)
  - `FirstOrDefaultAsync` Include ile çalışır (CategoryName için gerekli)
- **NotFoundException**: BuildingBlocks.Exceptions içinde, GlobalExceptionHandler yakalar → 404 döner
- **Include zorunlu**: CategoryName için `Product.Category.Name`'e erişmek gerekir

**Sonuç:** ✅ GetProductByIdQuery + Handler oluşturuldu

---

### Adım 4: GetProductsByCategoryQuery + Handler Oluştur

**Dosya Konumları:**
- `src/Services/Catalog/Catalog.API/Features/Products/Queries/GetProductsByCategory/GetProductsByCategoryQuery.cs`
- `src/Services/Catalog/Catalog.API/Features/Products/Queries/GetProductsByCategory/GetProductsByCategoryHandler.cs`

**Klasör Yapısı:**
```
Catalog.API/
└── Features/
    └── Products/
        └── Queries/
            └── GetProductsByCategory/  ← "Kategoriye göre ürünleri getir" query'si
                ├── GetProductsByCategoryQuery.cs      ← Query tanımı
                └── GetProductsByCategoryHandler.cs    ← Query işleyici
```

**Neden `Features/Products/Queries/GetProductsByCategory/` klasöründe?**
- Her query kendi klasöründe (semantik olarak farklı: kategoriye göre filtreleme)
- GetProductsQuery'den ayrı (CategoryId zorunlu vs opsiyonel)

---

**GetProductsByCategoryQuery.cs:**

**Namespace:** `Catalog.API.Features.Products.Queries.GetProductsByCategory`

**Kullanılan Kütüphaneler:**
- `MediatR`: `IRequest<IEnumerable<ProductDto>>` için
- `Catalog.API.Dtos`: `ProductDto` tipi için

**MediatR - IRequest<TResponse>:**
- **IRequest<IEnumerable<ProductDto>>**: Liste döner (GetProductsQuery ile aynı)

**Property:**
- `CategoryId` (Guid): **Zorunlu** kategori filtresi (nullable değil)

**Ne işe yarar:**
- Belirli bir kategoriye ait ürünleri getirme sorgusunu tanımlar
- Kategori sayfasında o kategorideki ürünleri göstermek için

---

**GetProductsByCategoryHandler.cs:**

**Namespace:** `Catalog.API.Features.Products.Queries.GetProductsByCategory`

**Kullanılan Kütüphaneler:**
- `MediatR`: `IRequestHandler<GetProductsByCategoryQuery, IEnumerable<ProductDto>>` için
- `AutoMapper`: `IMapper` - Entity → DTO mapping için
- `Microsoft.EntityFrameworkCore`: `Include`, `ToListAsync` için
- `Catalog.API.Data`: `CatalogDbContext` için
- `Catalog.API.Dtos`: `ProductDto` tipi için

**Handler İşlemleri:**
1. **Kategoriye ait ürünleri sorgula**: `Where(p => p.CategoryId == request.CategoryId)`
   - Include ile Category navigation property yüklenir
   - Sayfalama yok (tüm ürünler getirilir)
2. **SQL çalıştır**: `ToListAsync()` → Veritabanında sorgu çalışır
3. **Mapping**: AutoMapper ile `Product` → `ProductDto` dönüşümü
4. **Liste döndür**: `IEnumerable<ProductDto>`

**GetProductsQuery ile Farkı:**
- **GetProductsQuery**: 
  - CategoryId opsiyonel (null ise tüm ürünler)
  - Sayfalama var (PageNumber, PageSize)
  - Daha esnek (filtreleme opsiyonel)
- **GetProductsByCategoryQuery**: 
  - CategoryId zorunlu (sadece o kategoriye ait ürünler)
  - Sayfalama yok (tüm ürünler getirilir)
  - Daha spesifik (semantik olarak "kategoriye göre" anlamı taşır)

**Ne zaman hangisi kullanılır?**
- **GetProductsQuery**: Genel ürün listesi, sayfalama gerekiyorsa, filtreleme opsiyonelse
- **GetProductsByCategoryQuery**: Kategori sayfası, o kategorideki tüm ürünleri göstermek için

**Sonuç:** ✅ GetProductsByCategoryQuery + Handler oluşturuldu

---

### Adım 5: AutoMapper Profile Güncelleme

**Dosya Konumu:**
- `src/Services/Catalog/Catalog.API/Mapping/MappingProfile.cs`

**Klasör Yapısı:**
```
Catalog.API/
└── Mapping/                ← AutoMapper mapping profilleri
    └── MappingProfile.cs   ← Tüm mapping kuralları burada
```

**Neden `Mapping/` klasöründe?**
- Tüm mapping kuralları tek yerde (kolay bulunur ve yönetilir)
- AutoMapper için standart klasör yapısı

---

**MappingProfile.cs:**

**Namespace:** `Catalog.API.Mapping`

**Kullanılan Kütüphaneler:**
- `AutoMapper`: `Profile` base class için
- `Catalog.API.Entities`: `Product` entity tipi için
- `Catalog.API.Dtos`: `ProductDto` DTO tipi için
- `Catalog.API.Features.Products.Commands.CreateProduct`: `CreateProductCommand` için
- `Catalog.API.Features.Products.Commands.UpdateProduct`: `UpdateProductCommand` için

**AutoMapper - Profile:**
- **Ne işe yarar**: AutoMapper mapping kurallarını tanımlayan base class
- **Neden gerekli**: Entity ↔ DTO, Command → Entity mapping'leri için
- **Program.cs'de kayıt**: `AddAutoMapper(typeof(Program).Assembly)` ile otomatik bulunur

**Eklenen Mapping:**
```csharp
CreateMap<Product, ProductDto>()
    .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => 
        src.Category != null ? src.Category.Name : string.Empty));
```

**Mapping Kuralları:**

**1. Product → ProductDto (Otomatik Mapping):**
- Property adları aynıysa otomatik map edilir:
  - `Id` → `Id`
  - `Name` → `Name`
  - `Description` → `Description`
  - `Price` → `Price`
  - `ImageUrl` → `ImageUrl`
  - `CategoryId` → `CategoryId`

**2. CategoryName (ForMember ile Manuel Mapping):**
- **ForMember**: Özel mapping kuralı tanımlar
- **dest**: Hedef property (`ProductDto.CategoryName`)
- **opt.MapFrom**: Kaynak değer (`Product.Category.Name`)
- **Null kontrolü**: `src.Category != null ? src.Category.Name : string.Empty`
  - Category null ise boş string döner
  - Category yüklüyse Category.Name döner

**Ne işe yarar:**
- Entity → DTO dönüşümü için AutoMapper kuralları
- CategoryName: Navigation property'den (`Product.Category.Name`) alınır
- Null kontrolü: Category null ise boş string döner

**Neden ForMember?**
- Property adları farklı: `Product.Category.Name` (nested) → `ProductDto.CategoryName` (flat)
- Otomatik mapping çalışmaz, manuel mapping gerekli

**Önemli Notlar:**
- **Include zorunlu**: Handler'larda mutlaka `Include(p => p.Category)` kullanılmalı
- Include kullanılmazsa `Product.Category` null olur → CategoryName boş string döner
- Mapping sadece Include ile yüklenmiş navigation property'leri kullanabilir

**Mevcut Mapping'ler (Faz 3.3'ten):**
- `CreateProductCommand` → `Product` (Command → Entity)
- `UpdateProductCommand` → `Product` (Command → Entity)

**Sonuç:** ✅ AutoMapper Profile güncellendi

---

## 3.4 Bölümü - Tamamlanan Kontroller

✅ ProductDto oluşturuldu (CategoryName dahil)
✅ CreateProductDto oluşturuldu
✅ UpdateProductDto oluşturuldu
✅ GetProductsQuery + Handler oluşturuldu (sayfalama + filtreleme)
✅ GetProductByIdQuery + Handler oluşturuldu (NotFoundException ile)
✅ GetProductsByCategoryQuery + Handler oluşturuldu
✅ AutoMapper Profile güncellendi (Product → ProductDto, CategoryName mapping)
✅ Proje build oluyor mu? (`dotnet build`) → ✅ Başarıyla build oluyor (0 uyarı, 0 hata)

---

## Öğrenilenler (Faz 3.4)

### DTO (Data Transfer Object)

**DTO Nedir?**
- API response/request için kullanılan veri transfer nesneleri
- Entity'lerin doğrudan döndürülmesini engeller
- API contract'ını tanımlar

**Neden Kullanılır?**
- **Güvenlik**: Entity'nin tüm property'leri API'de görünmez
- **Esneklik**: Entity değişse bile API contract'ı sabit kalır
- **Ekstra alanlar**: Navigation property'den gelen alanlar eklenebilir (CategoryName)
- **Versioning**: Farklı API versiyonları için farklı DTO'lar kullanılabilir

**Entity vs DTO:**
- **Entity**: Veritabanı tablosunu temsil eder (iç yapı)
- **DTO**: API response/request formatını temsil eder (dış yapı)

**Örnek:**
```csharp
// Entity
public class Product 
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Category? Category { get; set; }  // Navigation property
}

// DTO
public class ProductDto 
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string CategoryName { get; set; }  // Navigation property'den alınır
}
```

### EF Core Include (Eager Loading)

**Include Nedir?**
- Navigation property'leri eager loading ile yükler
- İlişkili entity'leri tek sorguda getirir
- JOIN SQL'i oluşturur

**Neden Gerekli?**
- CategoryName için `Product.Category.Name`'e erişmek gerekir
- Include kullanılmazsa `Category` null olur (lazy loading aktif değilse)

**Kullanım:**
```csharp
var products = await _context.Products
    .Include(p => p.Category)  // Category navigation property yüklenir
    .ToListAsync();
```

**SQL Karşılığı:**
```sql
SELECT p.*, c.*
FROM Products p
LEFT JOIN Categories c ON p.CategoryId = c.Id
```

**Önemli:**
- Include sadece IQueryable üzerinde çalışır
- ToListAsync() çağrılmadan önce Include eklenmelidir
- FindAsync ile Include kullanılamaz (FirstOrDefaultAsync kullanılmalı)

### EF Core Query Methods

**AsQueryable() Nedir?**
- IQueryable döner (veritabanı sorgusu henüz çalıştırılmadı)
- Filtreleme/sayfalama veritabanında yapılır (performans)

**ToListAsync() Nedir?**
- SQL sorgusunu çalıştırır
- Sonuçları liste olarak döner

**Skip() ve Take() Nedir?**
- **Skip**: Belirtilen sayıda kaydı atlar (sayfalama için)
- **Take**: Belirtilen sayıda kayıt alır (sayfa boyutu için)
- SQL'de `OFFSET` ve `LIMIT` olarak çevrilir

**Sayfalama Formülü:**
```csharp
.Skip((pageNumber - 1) * pageSize)  // Önceki sayfadaki kayıtları atla
.Take(pageSize)                      // Sadece bu sayfadaki kayıtları al
```

**Örnek:**
- PageNumber = 2, PageSize = 10
- Skip(10) → İlk 10 kaydı atla
- Take(10) → Sonraki 10 kaydı al (11-20 arası)

### FindAsync vs FirstOrDefaultAsync

**FindAsync:**
- Primary key ile arar (hızlıdır)
- Include ile çalışmaz (navigation property yüklenmez)
- Tracking entity döner (Change Tracking aktif)

**FirstOrDefaultAsync:**
- LINQ predicate ile arar (herhangi bir alan)
- Include ile çalışır (navigation property yüklenebilir)
- Tracking entity döner

**Ne Zaman Hangisi Kullanılır?**
- **FindAsync**: Sadece ID ile arama, Include gerekmez
- **FirstOrDefaultAsync**: ID ile arama + Include gerekli (CategoryName için)

**Örnek:**
```csharp
// Include gerekiyorsa
var product = await _context.Products
    .Include(p => p.Category)
    .FirstOrDefaultAsync(p => p.Id == id);

// Include gerekmiyorsa
var product = await _context.Products.FindAsync(id);
```

### AutoMapper ForMember

**ForMember Nedir?**
- Özel mapping kuralları tanımlamak için kullanılır
- Property adları farklıysa veya özel dönüşüm gerekiyorsa kullanılır

**Kullanım:**
```csharp
CreateMap<Product, ProductDto>()
    .ForMember(
        dest => dest.CategoryName,           // Hedef property
        opt => opt.MapFrom(src =>            // Kaynak değer
            src.Category != null ? src.Category.Name : string.Empty));
```

**Ne Zaman Kullanılır?**
- Property adları farklıysa (CategoryName vs Category.Name)
- Özel dönüşüm gerekiyorsa (null kontrolü, format, vb.)

**Otomatik Mapping:**
- Property adları aynıysa otomatik mapping yapılır
- `CreateMap<Product, ProductDto>()` → Name, Price, vb. otomatik map edilir

### CQRS Pattern - Query Side

**Query Nedir?**
- Veriyi okuma işlemleri (GET)
- `IRequest<TResponse>` implement eder
- Handler ile işlenir

**Query Yapısı:**
```
GetProducts/
  ├── GetProductsQuery.cs      (Query)
  └── GetProductsHandler.cs     (Handler)
```

**Command vs Query:**
- **Command**: Veriyi değiştirir (Create, Update, Delete)
- **Query**: Veriyi okur (Get, GetAll, GetByFilter)

**Query Avantajları:**
- ✅ İş mantığı Controller'dan ayrılır
- ✅ Test edilebilirlik
- ✅ Kod organizasyonu
- ✅ MediatR pipeline'ından yararlanır (Validation, Logging)

**Query Handler İşlemleri:**
1. DbContext'ten veri çekilir
2. Gerekirse filtreleme/sayfalama uygulanır
3. Entity → DTO mapping yapılır
4. DTO döndürülür

### Sayfalama (Pagination)

**Sayfalama Nedir?**
- Büyük veri setlerini sayfalara bölme
- Performans için önemli (tüm veriyi çekmek yerine sadece gerekli sayfa)

**Sayfalama Parametreleri:**
- **PageNumber**: Hangi sayfa (1'den başlar)
- **PageSize**: Sayfa başına kaç kayıt

**EF Core Sayfalama:**
```csharp
.Skip((pageNumber - 1) * pageSize)
.Take(pageSize)
```

**SQL Karşılığı:**
```sql
SELECT * FROM Products
OFFSET 10 ROWS
FETCH NEXT 10 ROWS ONLY
```

**Avantajları:**
- ✅ Performans: Sadece gerekli kayıtlar çekilir
- ✅ Memory: Daha az bellek kullanımı
- ✅ Network: Daha az veri transferi

### Filtreleme (Filtering)

**Filtreleme Nedir?**
- Belirli kriterlere göre veri çekme
- CategoryId, DateRange, vb. ile filtreleme

**EF Core Filtreleme:**
```csharp
var query = _context.Products.AsQueryable();

if (categoryId.HasValue)
{
    query = query.Where(p => p.CategoryId == categoryId.Value);
}
```

**Neden AsQueryable()?**
- IQueryable döner, sorgu henüz çalıştırılmadı
- Koşullu filtreleme yapılabilir
- ToListAsync() çağrılınca SQL sorgusu çalıştırılır

**Avantajları:**
- ✅ Koşullu filtreleme yapılabilir
- ✅ Dinamik sorgu oluşturulabilir
- ✅ Performans: Filtreleme veritabanında yapılır

---

**Son Güncelleme:** Aralık 2024

