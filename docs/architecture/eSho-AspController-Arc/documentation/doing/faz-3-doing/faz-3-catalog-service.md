# Faz 3 - Catalog Service (REST API)

## Servis Hakkında

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

---

## 3.1 Catalog.API Projesi Oluştur

**Hedef:** REST API projesi

### Görevler:

#### Web API projesi oluştur
**Ne işe yarar:** Catalog Service için REST API projesi oluşturur.

```bash
cd src/Services
mkdir Catalog
cd Catalog
dotnet new webapi -n Catalog.API
```

#### Projeyi solution'a ekle
**Ne işe yarar:** Projeyi solution'a ekler, böylece diğer projeler referans verebilir.

```bash
cd ../../..
dotnet sln add src/Services/Catalog/Catalog.API/Catalog.API.csproj
```

#### NuGet paketlerini ekle
**Ne işe yarar:** MediatR, FluentValidation, AutoMapper, EF Core ve PostgreSQL için gerekli paketleri ekler.

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

**Paketler:**
- `MediatR` (latest) → CQRS pattern için
- `FluentValidation` (latest) → Request validation için
- `FluentValidation.DependencyInjectionExtensions` (latest) → DI entegrasyonu için
- `AutoMapper` (latest) → Entity ↔ DTO mapping için
- `AutoMapper.Extensions.Microsoft.DependencyInjection` (latest) → AutoMapper DI entegrasyonu için
- `Microsoft.EntityFrameworkCore` (9.0) → ORM için
- `Microsoft.EntityFrameworkCore.Design` (9.0) → Migration tooling için
- `Npgsql.EntityFrameworkCore.PostgreSQL` (latest) → PostgreSQL provider için
- `AspNetCore.HealthChecks.NpgSql` (latest) → PostgreSQL health check için

#### Project References ekle
**Ne işe yarar:** BuildingBlocks projelerini referans eder (exception handling, validation, logging).

```bash
dotnet add reference ../../BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj
dotnet add reference ../../BuildingBlocks/BuildingBlocks.Behaviors/BuildingBlocks.Behaviors.csproj
```

**Project References:**
- `BuildingBlocks.Exceptions` → Global exception handler için
- `BuildingBlocks.Behaviors` → Validation ve Logging behaviors için

#### Klasör yapısını oluştur
**Ne işe yarar:** CQRS pattern için klasör yapısını oluşturur (Features, Entities, Data, Dtos, Mapping).

```bash
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

**Açıklama:**
- `Features/` → CQRS features (Commands ve Queries)
- `Entities/` → Domain entity'ler (Product, Category)
- `Data/` → DbContext ve SeedData
- `Dtos/` → Data Transfer Objects
- `Mapping/` → AutoMapper profiles
- `Controllers/` → REST API controllers

#### Entity'leri oluştur
**Ne işe yarar:** Veritabanı tablolarını temsil eden entity class'larını oluşturur.

**Entities/Product.cs:**
- `Id` (Guid) → Primary key
- `Name` (string, max 100) → Ürün adı
- `Description` (string, nullable) → Ürün açıklaması
- `Price` (decimal) → Ürün fiyatı
- `ImageUrl` (string, nullable) → Ürün resim URL'i
- `CategoryId` (Guid, FK) → Kategori ID (foreign key)
- `Category` (Navigation Property) → Kategori referansı

**Entities/Category.cs:**
- `Id` (Guid) → Primary key
- `Name` (string, max 50) → Kategori adı
- `Products` (Collection Navigation Property) → Ürünler koleksiyonu

#### CatalogDbContext oluştur
**Ne işe yarar:** EF Core DbContext'i oluşturur (PostgreSQL bağlantısı, entity konfigürasyonları).

**Data/CatalogDbContext.cs:**
- `Products` (DbSet<Product>) → Products tablosu
- `Categories` (DbSet<Category>) → Categories tablosu
- `OnModelCreating` → Entity konfigürasyonları (table names, constraints)

#### appsettings.json'a connection string ekle
**Ne işe yarar:** PostgreSQL veritabanı bağlantı string'ini ekler.

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5432;Database=CatalogDb;Username=postgres;Password=postgres"
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

### Kontrol:
- Proje build oluyor mu? (`dotnet build`)
- Solution'da görünüyor mu? (`dotnet sln list`)

---

## 3.2 Catalog Database & Seed Data

**Hedef:** Veritabanı ve başlangıç verileri

### Görevler:

#### EF Core Migration oluştur
**Ne işe yarar:** Veritabanı şemasını oluşturmak için migration dosyası oluşturur.

```bash
cd src/Services/Catalog/Catalog.API
dotnet ef migrations add InitialCreate --startup-project . --context CatalogDbContext
```

**Açıklama:**
- `InitialCreate` → Migration adı
- `--startup-project .` → Startup proje (Catalog.API)
- `--context CatalogDbContext` → DbContext adı

#### Migration uygula
**Ne işe yarar:** Migration'ı veritabanına uygular (tablolar oluşturulur).

```bash
dotnet ef database update --startup-project . --context CatalogDbContext
```

**Not:** Docker container'da PostgreSQL çalışıyor olmalı (`docker-compose up -d`)

#### SeedData.cs oluştur
**Ne işe yarar:** Uygulama başlangıcında örnek veri ekler (kategoriler ve ürünler).

**Data/SeedData.cs:**
- `InitializeAsync` static method → Seed data ekleme metodu
- Koşullu ekleme: Veri varsa tekrar eklemez
- 3 kategori: Elektronik, Giyim, Ev & Yaşam
- Her kategoride 2-3 ürün örneği

**Seed İçeriği:**
- **Kategoriler:**
  - Elektronik
  - Giyim
  - Ev & Yaşam
- **Ürünler (örnek):**
  - Elektronik: iPhone 15, Samsung S24, MacBook Pro
  - Giyim: T-Shirt, Pantolon, Ayakkabı
  - Ev & Yaşam: Masa, Sandalye, Lamba

#### Program.cs'de seed data çalıştır
**Ne işe yarar:** Uygulama başlangıcında migration ve seed data'yı otomatik çalıştırır.

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

app.Run();
```

### Test:
- Container'da DB oluştu mu? (`docker exec -it catalogdb psql -U postgres -d CatalogDb`)
- Tablolar var mı? (`\dt` komutu ile tabloları listele)
- Seed data var mı? (`SELECT * FROM "Categories"`, `SELECT * FROM "Products"`)

---

## 3.3 Catalog CQRS - Products Commands

**Hedef:** Product yazma işlemleri (Create, Update, Delete)

### Görevler:

#### CreateProductCommand oluştur
**Ne işe yarar:** Yeni ürün oluşturma komutunu tanımlar.

**Features/Products/Commands/CreateProduct/CreateProductCommand.cs:**
- `IRequest<Guid>` implement eder (MediatR)
- Property'ler: Name, Description, Price, ImageUrl, CategoryId
- Return: Product ID (Guid)

#### CreateProductHandler oluştur
**Ne işe yarar:** CreateProductCommand'i işler, ürünü veritabanına kaydeder.

**Features/Products/Commands/CreateProduct/CreateProductHandler.cs:**
- `IRequestHandler<CreateProductCommand, Guid>` implement eder
- AutoMapper ile Command → Entity mapping
- DbContext'e ürün ekler
- SaveChangesAsync ile kaydeder
- Product ID döner

#### CreateProductValidator oluştur
**Ne işe yarar:** CreateProductCommand için validation kurallarını tanımlar.

**Features/Products/Commands/CreateProduct/CreateProductValidator.cs:**
- `AbstractValidator<CreateProductCommand>` inherit eder
- FluentValidation kuralları:
  - Name: NotEmpty, MaximumLength(100)
  - Price: GreaterThan(0)
  - CategoryId: NotEmpty

#### UpdateProductCommand oluştur
**Ne işe yarar:** Mevcut ürünü güncelleme komutunu tanımlar.

**Features/Products/Commands/UpdateProduct/CreateProductCommand.cs:**
- `IRequest<Unit>` implement eder (void return)
- Property'ler: Id, Name, Description, Price, ImageUrl, CategoryId

#### UpdateProductHandler oluştur
**Ne işe yarar:** UpdateProductCommand'i işler, ürünü günceller.

**Features/Products/Commands/UpdateProduct/UpdateProductHandler.cs:**
- `IRequestHandler<UpdateProductCommand, Unit>` implement eder
- Ürünü veritabanından bulur (Id ile)
- NotFoundException fırlatır (ürün yoksa)
- AutoMapper ile günceller
- SaveChangesAsync ile kaydeder

#### UpdateProductValidator oluştur
**Ne işe yarar:** UpdateProductCommand için validation kurallarını tanımlar.

**Features/Products/Commands/UpdateProduct/UpdateProductValidator.cs:**
- CreateProductValidator ile aynı kurallar
- Id: NotEmpty (ekstra kural)

#### DeleteProductCommand oluştur
**Ne işe yarar:** Ürün silme komutunu tanımlar.

**Features/Products/Commands/DeleteProduct/DeleteProductCommand.cs:**
- `IRequest<Unit>` implement eder
- Property: Id (Guid)

#### DeleteProductHandler oluştur
**Ne işe yarar:** DeleteProductCommand'i işler, ürünü siler.

**Features/Products/Commands/DeleteProduct/DeleteProductHandler.cs:**
- `IRequestHandler<DeleteProductCommand, Unit>` implement eder
- Ürünü veritabanından bulur
- NotFoundException fırlatır (ürün yoksa)
- Remove ile siler
- SaveChangesAsync ile kaydeder

#### AutoMapper Profile oluştur
**Ne işe yarar:** Command ↔ Entity ve Entity ↔ DTO mapping'lerini tanımlar.

**Mapping/MappingProfile.cs:**
- `Profile` inherit eder
- CreateMaps:
  - `CreateProductCommand` → `Product`
  - `UpdateProductCommand` → `Product`
  - `Product` → `ProductDto`
  - `Product` → `CreateProductDto`
  - `Product` → `UpdateProductDto`

### Kontrol:
- Handler'lar çalışıyor mu? (Unit test veya manuel test)
- Validation çalışıyor mu? (geçersiz veri ile test)

---

## 3.4 Catalog CQRS - Products Queries

**Hedef:** Product okuma işlemleri (GetAll, GetById, GetByCategory)

### Görevler:

#### DTO'ları oluştur
**Ne işe yarar:** API response'ları için Data Transfer Objects oluşturur.

**Dtos/ProductDto.cs:**
- Property'ler: Id, Name, Description, Price, ImageUrl, CategoryId, CategoryName

**Dtos/CreateProductDto.cs:**
- Property'ler: Name, Description, Price, ImageUrl, CategoryId

**Dtos/UpdateProductDto.cs:**
- Property'ler: Id, Name, Description, Price, ImageUrl, CategoryId

#### GetProductsQuery oluştur
**Ne işe yarar:** Tüm ürünleri getirme sorgusunu tanımlar (sayfalama, filtreleme).

**Features/Products/Queries/GetProducts/GetProductsQuery.cs:**
- `IRequest<IEnumerable<ProductDto>>` implement eder
- Property'ler:
  - `PageNumber` (int, default: 1) → Sayfa numarası
  - `PageSize` (int, default: 10) → Sayfa boyutu
  - `CategoryId` (Guid?, nullable) → Kategori filtresi

#### GetProductsHandler oluştur
**Ne işe yarar:** GetProductsQuery'i işler, ürünleri getirir.

**Features/Products/Queries/GetProducts/GetProductsHandler.cs:**
- `IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>` implement eder
- DbContext'ten ürünleri sorgular
- CategoryId filtresi uygular (varsa)
- Sayfalama uygular (Skip, Take)
- AutoMapper ile Entity → DTO mapping
- ProductDto listesi döner

#### GetProductByIdQuery oluştur
**Ne işe yarar:** ID'ye göre ürün getirme sorgusunu tanımlar.

**Features/Products/Queries/GetProductById/GetProductByIdQuery.cs:**
- `IRequest<ProductDto>` implement eder
- Property: Id (Guid)

#### GetProductByIdHandler oluştur
**Ne işe yarar:** GetProductByIdQuery'i işler, ürünü getirir.

**Features/Products/Queries/GetProductById/GetProductByIdHandler.cs:**
- `IRequestHandler<GetProductByIdQuery, ProductDto>` implement eder
- DbContext'ten ürünü bulur (Id ile)
- NotFoundException fırlatır (ürün yoksa)
- AutoMapper ile Entity → DTO mapping
- ProductDto döner

#### GetProductsByCategoryQuery oluştur
**Ne işe yarar:** Kategoriye göre ürün getirme sorgusunu tanımlar.

**Features/Products/Queries/GetProductsByCategory/GetProductsByCategoryQuery.cs:**
- `IRequest<IEnumerable<ProductDto>>` implement eder
- Property: CategoryId (Guid)

#### GetProductsByCategoryHandler oluştur
**Ne işe yarar:** GetProductsByCategoryQuery'i işler, kategoriye ait ürünleri getirir.

**Features/Products/Queries/GetProductsByCategory/GetProductsByCategoryHandler.cs:**
- `IRequestHandler<GetProductsByCategoryQuery, IEnumerable<ProductDto>>` implement eder
- DbContext'ten kategoriye ait ürünleri sorgular
- AutoMapper ile Entity → DTO mapping
- ProductDto listesi döner

### Kontrol:
- Query'ler çalışıyor mu? (manuel test)
- Sayfalama çalışıyor mu?
- Filtreleme çalışıyor mu?

---

## 3.5 Catalog CQRS - Categories

**Hedef:** Category işlemleri (GetAll, GetById, Create)

### Görevler:

#### Category DTO'ları oluştur
**Ne işe yarar:** Category API response'ları için DTO'lar oluşturur.

**Dtos/CategoryDto.cs:**
- Property'ler: Id, Name

**Dtos/CreateCategoryDto.cs:**
- Property: Name

#### GetCategoriesQuery oluştur
**Ne işe yarar:** Tüm kategorileri getirme sorgusunu tanımlar.

**Features/Categories/Queries/GetCategories/GetCategoriesQuery.cs:**
- `IRequest<IEnumerable<CategoryDto>>` implement eder

#### GetCategoriesHandler oluştur
**Ne işe yarar:** GetCategoriesQuery'i işler, kategorileri getirir.

**Features/Categories/Queries/GetCategories/GetCategoriesHandler.cs:**
- `IRequestHandler<GetCategoriesQuery, IEnumerable<CategoryDto>>` implement eder
- DbContext'ten tüm kategorileri getirir
- AutoMapper ile Entity → DTO mapping
- CategoryDto listesi döner

#### GetCategoryByIdQuery oluştur
**Ne işe yarar:** ID'ye göre kategori getirme sorgusunu tanımlar.

**Features/Categories/Queries/GetCategoryById/GetCategoryByIdQuery.cs:**
- `IRequest<CategoryDto>` implement eder
- Property: Id (Guid)

#### GetCategoryByIdHandler oluştur
**Ne işe yarar:** GetCategoryByIdQuery'i işler, kategoriyi getirir.

**Features/Categories/Queries/GetCategoryById/GetCategoryByIdHandler.cs:**
- `IRequestHandler<GetCategoryByIdQuery, CategoryDto>` implement eder
- DbContext'ten kategoriyi bulur (Id ile)
- NotFoundException fırlatır (kategori yoksa)
- AutoMapper ile Entity → DTO mapping
- CategoryDto döner

#### CreateCategoryCommand oluştur
**Ne işe yarar:** Yeni kategori oluşturma komutunu tanımlar.

**Features/Categories/Commands/CreateCategory/CreateCategoryCommand.cs:**
- `IRequest<Guid>` implement eder
- Property: Name (string)

#### CreateCategoryHandler oluştur
**Ne işe yarar:** CreateCategoryCommand'i işler, kategoriyi veritabanına kaydeder.

**Features/Categories/Commands/CreateCategory/CreateCategoryHandler.cs:**
- `IRequestHandler<CreateCategoryCommand, Guid>` implement eder
- AutoMapper ile Command → Entity mapping
- DbContext'e kategori ekler
- SaveChangesAsync ile kaydeder
- Category ID döner

#### CreateCategoryValidator oluştur
**Ne işe yarar:** CreateCategoryCommand için validation kurallarını tanımlar.

**Features/Categories/Commands/CreateCategory/CreateCategoryValidator.cs:**
- `AbstractValidator<CreateCategoryCommand>` inherit eder
- FluentValidation kuralları:
  - Name: NotEmpty, MaximumLength(50)

#### AutoMapper Profile'a Category mapping ekle
**Ne işe yarar:** Category için mapping'leri ekler.

**Mapping/MappingProfile.cs:**
- CreateMaps:
  - `CreateCategoryCommand` → `Category`
  - `Category` → `CategoryDto`

### Kontrol:
- Category işlemleri çalışıyor mu? (manuel test)
- Validation çalışıyor mu?

---

## 3.6 Catalog Controllers & MediatR Entegrasyonu

**Hedef:** REST API endpoint'leri

### Görevler:

#### Program.cs'de servisleri register et
**Ne işe yarar:** MediatR, FluentValidation, AutoMapper, EF Core ve BuildingBlocks'ları DI container'a ekler.

```csharp
// MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// DbContext
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
```

#### ProductsController oluştur
**Ne işe yarar:** Product işlemleri için REST API endpoint'leri oluşturur.

**Controllers/ProductsController.cs:**
- `[ApiController]` attribute
- `[Route("api/[controller]")]` attribute
- `IMediator` dependency injection
- Endpoint'ler:
  - `GET /api/products` → GetProductsQuery
  - `GET /api/products/{id}` → GetProductByIdQuery
  - `GET /api/products/category/{categoryId}` → GetProductsByCategoryQuery
  - `POST /api/products` → CreateProductCommand
  - `PUT /api/products/{id}` → UpdateProductCommand
  - `DELETE /api/products/{id}` → DeleteProductCommand

#### CategoriesController oluştur
**Ne işe yarar:** Category işlemleri için REST API endpoint'leri oluşturur.

**Controllers/CategoriesController.cs:**
- `[ApiController]` attribute
- `[Route("api/[controller]")]` attribute
- `IMediator` dependency injection
- Endpoint'ler:
  - `GET /api/categories` → GetCategoriesQuery
  - `GET /api/categories/{id}` → GetCategoryByIdQuery
  - `POST /api/categories` → CreateCategoryCommand

#### Swagger konfigürasyonu
**Ne işe yarar:** API dokümantasyonu için Swagger'ı yapılandırır.

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Catalog API", 
        Version = "v1" 
    });
});

// app.UseSwagger();
// app.UseSwaggerUI();
```

#### Exception middleware ekle
**Ne işe yarar:** Global exception handler'ı middleware pipeline'a ekler.

```csharp
app.UseExceptionHandler();
```

#### Health checks ekle
**Ne işe yarar:** PostgreSQL veritabanı sağlık kontrolü ekler.

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database"));

app.MapHealthChecks("/health");
```

### Test:
- Swagger açılıyor mu? (http://localhost:5001/swagger)
- Endpoint'ler çalışıyor mu?
  - GET /api/products
  - GET /api/products/{id}
  - POST /api/products
  - PUT /api/products/{id}
  - DELETE /api/products/{id}
  - GET /api/categories
  - GET /api/categories/{id}
  - POST /api/categories
- Health check çalışıyor mu? (http://localhost:5001/health)
- Exception handling çalışıyor mu? (olmayan ID ile test)

**Sonuç:** ✅ Catalog Service çalışıyor (Port 5001)

---

## Özet: Faz 3 adımlar sırası

1. Catalog.API Web API projesi oluştur
2. Projeyi solution'a ekle
3. NuGet paketlerini ekle (MediatR, FluentValidation, AutoMapper, EF Core, PostgreSQL)
4. BuildingBlocks project references ekle
5. Klasör yapısını oluştur (Features, Entities, Data, Dtos, Mapping, Controllers)
6. Entity'leri oluştur (Product, Category)
7. CatalogDbContext oluştur
8. appsettings.json'a connection string ekle
9. EF Core Migration oluştur
10. Migration uygula
11. SeedData.cs oluştur
12. Program.cs'de seed data çalıştır
13. Products Commands oluştur (Create, Update, Delete)
14. Products Queries oluştur (GetAll, GetById, GetByCategory)
15. Products DTO'ları oluştur
16. Categories Commands ve Queries oluştur
17. Categories DTO'ları oluştur
18. AutoMapper Profile oluştur
19. Program.cs'de servisleri register et
20. ProductsController oluştur
21. CategoriesController oluştur
22. Swagger konfigürasyonu
23. Exception middleware ekle
24. Health checks ekle
25. Tüm endpoint'leri test et

**Bu adımlar tamamlandıktan sonra Faz 4'e (Discount Service) geçilebilir.**

