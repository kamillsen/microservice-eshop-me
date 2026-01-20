# Class Library (Sınıf Kütüphanesi) — Notlar

Bu not, **Class Library** kavramını, ne olduğunu, nasıl çalıştığını ve bu projedeki kullanımını açıklamak için yazıldı.

---

## Class Library Nedir?

**Class Library (Sınıf Kütüphanesi)**, başka projeler tarafından referans edilip kullanılan, kendi başına çalışmayan bir kütüphane projesidir.

### Temel Özellikler

- ✅ **Yeniden kullanılabilir kod** içerir
- ✅ **Birden fazla projede** kullanılabilir
- ✅ **Doğrudan çalıştırılamaz** (executable değildir)
- ✅ **`.dll` (Dynamic Link Library)** olarak derlenir
- ✅ **Modüler yapı** sağlar

---

## DLL Nedir?

**DLL (Dynamic Link Library)** = **Dinamik Bağlantı Kütüphanesi**

### Ne İşe Yarar?

- Çalışma zamanında (runtime) yüklenen kütüphane dosyasıdır
- Birden fazla uygulama tarafından paylaşılabilir
- Bellekte tek bir kopya tutulur (bellek tasarrufu)
- Modüler yapı sağlar

### .NET'te DLL

.NET'te class library'ler derlendiğinde `.dll` dosyası oluşur:

```
BuildingBlocks.Behaviors.csproj → BuildingBlocks.Behaviors.dll
BuildingBlocks.Exceptions.csproj → BuildingBlocks.Exceptions.dll
BuildingBlocks.Messaging.csproj → BuildingBlocks.Messaging.dll
```

### Nasıl Çalışır?

1. **Derleme**: Class library projesi derlenir → `.dll` oluşur
2. **Referans**: API projesi bu DLL'i referans eder
3. **Çalışma**: Uygulama çalıştığında DLL yüklenir ve kullanılır

---

## Class Library vs Web API Farkı

| Özellik | Class Library | Web API |
|---------|---------------|---------|
| **SDK** | `Microsoft.NET.Sdk` | `Microsoft.NET.Sdk.Web` |
| **Çalıştırılabilir mi?** | ❌ Hayır (sadece referans) | ✅ Evet (kendi başına çalışır) |
| **Kullanım** | Diğer projelere eklenir | Bağımsız servis olarak çalışır |
| **Çıktı** | `.dll` dosyası | Çalışan uygulama |
| **Program.cs** | ❌ Yok | ✅ Var |
| **Main metodu** | ❌ Yok | ✅ Var |

### Örnek: .csproj Dosyaları

**Class Library (.csproj):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>
```

**Web API (.csproj):**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>
```

---

## Bu Projedeki Class Library'ler

Projede **3 adet class library** bulunmaktadır. Hepsi `src/BuildingBlocks/` klasörü altındadır:

### 1. BuildingBlocks.Behaviors

**Konum:** `src/BuildingBlocks/BuildingBlocks.Behaviors/`

**Amaç:** MediatR pipeline behavior'ları için ortak kod

**İçerik:**
- `LoggingBehavior.cs` - Request/Response loglama
- `ValidationBehavior.cs` - FluentValidation ile otomatik validasyon

**Kullanılan Paketler:**
- `MediatR` - Pipeline behavior'ları için
- `FluentValidation` - Validation için
- `Serilog.AspNetCore` - Logging için

**Kullanım:**
```csharp
// Catalog.API/Program.cs
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateCategoryHandler).Assembly);
    cfg.AddBehavior<IPipelineBehavior<CreateCategoryCommand, CreateCategoryCommandResult>, ValidationBehavior<CreateCategoryCommand, CreateCategoryCommandResult>>();
    cfg.AddBehavior<IPipelineBehavior<CreateCategoryCommand, CreateCategoryCommandResult>, LoggingBehavior<CreateCategoryCommand, CreateCategoryCommandResult>>();
});
```

---

### 2. BuildingBlocks.Exceptions

**Konum:** `src/BuildingBlocks/BuildingBlocks.Exceptions/`

**Amaç:** Ortak exception yönetimi ve global exception handler

**İçerik:**
- `Exceptions/` klasörü:
  - `BadRequestException.cs` - 400 Bad Request hatası
  - `NotFoundException.cs` - 404 Not Found hatası
  - `InternalServerException.cs` - 500 Internal Server Error hatası
- `Handler/GlobalExceptionHandler.cs` - Tüm exception'ları yakalayan global handler

**Kullanılan Paketler:**
- `Microsoft.AspNetCore.App` (FrameworkReference) - ASP.NET Core için

**Kullanım:**
```csharp
// Catalog.API/Program.cs
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ...

app.UseExceptionHandler();
```

**Örnek Exception Kullanımı:**
```csharp
// Handler içinde
throw new NotFoundException($"Category with id {request.Id} not found");
```

---

### 3. BuildingBlocks.Messaging

**Konum:** `src/BuildingBlocks/BuildingBlocks.Messaging/`

**Amaç:** Servisler arası iletişim için integration event'ler

**İçerik:**
- `Events/IntegrationEvent.cs` - Base integration event sınıfı
- `Events/BasketCheckoutEvent.cs` - Sepet checkout event'i

**Kullanılan Paketler:**
- `MassTransit` - Message bus için
- `MassTransit.RabbitMQ` - RabbitMQ entegrasyonu için

**Kullanım:**
```csharp
// Integration event tanımlama
public record BasketCheckoutEvent : IntegrationEvent
{
    public string UserId { get; init; }
    public List<BasketItem> Items { get; init; }
    // ...
}
```

---

## Class Library Oluşturma

### 1. Yeni Class Library Projesi Oluşturma

**Visual Studio:**
```
File → New → Project → Class Library (.NET)
```

**dotnet CLI:**
```bash
dotnet new classlib -n MyClassLibrary
```

### 2. .csproj Yapılandırması

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Paket referansları -->
    <PackageReference Include="SomePackage" />
  </ItemGroup>
</Project>
```

### 3. Diğer Projeye Referans Ekleme

**Visual Studio:**
```
Solution Explorer → Proje → Add → Project Reference → Class Library seç
```

**dotnet CLI:**
```bash
dotnet add reference ../BuildingBlocks/MyClassLibrary/MyClassLibrary.csproj
```

**Manuel (.csproj):**
```xml
<ItemGroup>
  <ProjectReference Include="..\..\BuildingBlocks\MyClassLibrary\MyClassLibrary.csproj" />
</ItemGroup>
```

---

## Neden Class Library Kullanılır?

### 1. **Kod Tekrarını Önler (DRY Principle)**
- Aynı kodu birden fazla projede yazmak yerine, bir kez yazıp her yerde kullanılır

### 2. **Ortak İşlevsellik Sağlar**
- Tüm projelerde kullanılan ortak kodlar merkezi bir yerde toplanır
- Örnek: Exception handling, logging, validation

### 3. **Bakımı Kolaylaştırır**
- Bir değişiklik yapıldığında, tüm projeler otomatik olarak güncellenir
- Tek bir yerden yönetilir

### 4. **Test Edilebilirliği Artırır**
- Class library'ler bağımsız olarak test edilebilir
- Unit test'ler daha kolay yazılır

### 5. **Modüler Yapı Sağlar**
- Projeler daha küçük ve yönetilebilir hale gelir
- Her class library belirli bir sorumluluğa sahiptir

### 6. **Bellek Optimizasyonu**
- Aynı DLL birden fazla uygulama tarafından paylaşılabilir
- Bellekte tek bir kopya tutulur

---

## Class Library Kullanım Senaryoları

### Senaryo 1: Ortak Exception Handling

```csharp
// BuildingBlocks.Exceptions/Exceptions/NotFoundException.cs
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

// Catalog.API/Features/Categories/Commands/DeleteCategory/DeleteCategoryHandler.cs
public async Task<DeleteCategoryCommandResult> Handle(...)
{
    var category = await _context.Categories.FindAsync(request.Id);
    if (category == null)
        throw new NotFoundException($"Category with id {request.Id} not found");
    
    // ...
}
```

### Senaryo 2: Pipeline Behavior'ları

```csharp
// BuildingBlocks.Behaviors/Behaviors/LoggingBehavior.cs
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, ...)
    {
        _logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {RequestName}", typeof(TRequest).Name);
        return response;
    }
}
```

### Senaryo 3: Integration Events

```csharp
// BuildingBlocks.Messaging/Events/IntegrationEvent.cs
public record IntegrationEvent
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; }
}

// Ordering.API/Events/OrderCreatedEvent.cs
public record OrderCreatedEvent : IntegrationEvent
{
    public int OrderId { get; init; }
    public string UserId { get; init; }
    // ...
}
```

---

## Class Library vs NuGet Package

| Özellik | Class Library | NuGet Package |
|---------|---------------|---------------|
| **Dağıtım** | Proje referansı | NuGet repository |
| **Versiyonlama** | Proje bazlı | Semantic versioning |
| **Kullanım** | Aynı solution içinde | Herhangi bir projede |
| **Güncelleme** | Proje rebuild | NuGet update |
| **Paylaşım** | Lokal | Global (NuGet.org) |

### Ne Zaman Hangisi?

- **Class Library:** Aynı solution içindeki projeler arasında paylaşım için
- **NuGet Package:** Farklı projeler/solution'lar arasında paylaşım için

---

## Best Practices

### 1. **Tek Sorumluluk Prensibi (SRP)**
Her class library belirli bir amaca hizmet etmeli:
- ✅ `BuildingBlocks.Exceptions` → Sadece exception handling
- ✅ `BuildingBlocks.Behaviors` → Sadece pipeline behavior'ları
- ❌ `BuildingBlocks.Common` → Her şey (çok geniş)

### 2. **Namespace Organizasyonu**
```csharp
// ✅ İyi
namespace BuildingBlocks.Exceptions.Exceptions;
namespace BuildingBlocks.Exceptions.Handler;

// ❌ Kötü
namespace BuildingBlocks;
namespace Exceptions;
```

### 3. **Bağımlılık Yönetimi**
- Mümkün olduğunca az bağımlılık kullan
- Gereksiz paket ekleme
- Versiyon uyumluluğuna dikkat et

### 4. **Dokümantasyon**
- Public API'leri dokümante et
- Kullanım örnekleri ekle
- README dosyası ekle

### 5. **Test Edilebilirlik**
- Class library'ler bağımsız test edilebilir olmalı
- Mock/fake kullanımına uygun tasarla

---

## Derleme ve Çıktı

### Derleme Süreci

```bash
# Class library'yi derle
dotnet build src/BuildingBlocks/BuildingBlocks.Behaviors/BuildingBlocks.Behaviors.csproj

# Çıktı: bin/Debug/net9.0/BuildingBlocks.Behaviors.dll
```

### Çıktı Dosyaları

```
bin/Debug/net9.0/
├── BuildingBlocks.Behaviors.dll          # Ana kütüphane
├── BuildingBlocks.Behaviors.pdb          # Debug sembolleri
└── BuildingBlocks.Behaviors.deps.json    # Bağımlılık bilgileri
```

### Kullanım

API projesi derlendiğinde, referans edilen class library'ler otomatik olarak:
1. Derlenir (eğer değiştiyse)
2. Çıktı klasörüne kopyalanır
3. Uygulama ile birlikte çalışır

---

## Sorun Giderme

### Problem 1: "Type not found" Hatası

**Sebep:** Class library referans edilmemiş

**Çözüm:**
```bash
dotnet add reference ../BuildingBlocks/MyClassLibrary/MyClassLibrary.csproj
```

### Problem 2: "Package not found" Hatası

**Sebep:** Class library'deki paket, API projesinde yok

**Çözüm:** Paketi API projesine de ekle veya class library'yi kullan

### Problem 3: Circular Dependency (Döngüsel Bağımlılık)

**Sebep:** A class library'si B'yi, B de A'yı referans ediyor

**Çözüm:** Ortak kodu üçüncü bir class library'ye taşı

---

## Özet

- **Class Library:** Yeniden kullanılabilir kod içeren, `.dll` olarak derlenen proje türü
- **DLL:** Dinamik bağlantı kütüphanesi, çalışma zamanında yüklenen dosya
- **Kullanım:** Kod tekrarını önler, modüler yapı sağlar, bakımı kolaylaştırır
- **Bu Projede:** 3 class library var (Behaviors, Exceptions, Messaging)
- **SDK:** `Microsoft.NET.Sdk` (Web API'den farklı)

---

## Kaynaklar

- [Microsoft Docs: Class Library](https://learn.microsoft.com/en-us/dotnet/core/tutorials/library-with-visual-studio)
- [Microsoft Docs: DLL](https://learn.microsoft.com/en-us/dotnet/standard/assembly/)
- [.NET Assembly Overview](https://learn.microsoft.com/en-us/dotnet/standard/assembly/)
