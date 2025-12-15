# Faz 3 - Catalog Service Notları

> Bu dosya, Faz 3 (Catalog Service) adım adım yaparken öğrendiklerimi not aldığım dosyadır.
> 
> **İçerik:**
> - Faz 3.1: Catalog.API Projesi Oluştur
> - Faz 3.2: Catalog Database & Seed Data

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

**Son Güncelleme:** Aralık 2024

