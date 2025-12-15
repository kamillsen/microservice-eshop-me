# Faz 2 - BuildingBlocks Notları

> Bu dosya, Faz 2 (BuildingBlocks) adım adım yaparken öğrendiklerimi not aldığım dosyadır.
> 
> **İçerik:**
> - Faz 2.1: BuildingBlocks.Exceptions (Exception handling)
> - Faz 2.2: BuildingBlocks.Behaviors (MediatR Pipeline Behaviors)

---

## BuildingBlocks Nedir?

**BuildingBlocks**, tüm microservice'lerde ortak kullanılacak kod parçalarını içeren class library projeleridir. Bu sayede:

- ✅ Kod tekrarı önlenir
- ✅ Tutarlılık sağlanır
- ✅ Merkezi yönetim yapılır
- ✅ Bakım kolaylaşır

### Bu Bölümde:

**BuildingBlocks.Exceptions** → Exception handling (hata yönetimi)
**BuildingBlocks.Behaviors** → MediatR Pipeline Behaviors (Validation, Logging)

> **Not:** 2.3 (BuildingBlocks.Messaging) henüz yapılmadı. Yapıldığında ayrı dokümantasyon eklenecek.

---

## 2.1 BuildingBlocks.Exceptions - Yapılanlar

### Adım 1: Class Library Projesi Oluştur

**Komut:**
```bash
cd src/BuildingBlocks
dotnet new classlib -n BuildingBlocks.Exceptions
```

**Açıklamalar:**
- `cd src/BuildingBlocks` → BuildingBlocks klasörüne geç
- `dotnet new classlib` → Yeni class library projesi oluştur
- `-n BuildingBlocks.Exceptions` → Proje adı

**Ne işe yarar:**
- Paylaşılan exception yapısı için class library projesi oluşturur
- Bu proje, diğer servisler tarafından referans edilecek
- Class library = çalıştırılabilir değil, sadece kod içerir (kütüphane)

**Sonuç:** 
- `src/BuildingBlocks/BuildingBlocks.Exceptions/` klasörü oluşturuldu
- `BuildingBlocks.Exceptions.csproj` dosyası oluşturuldu
- Varsayılan `Class1.cs` dosyası oluşturuldu (sonra silinecek)

---

### Adım 2: Projeyi Solution'a Ekle

**Komut:**
```bash
cd ../..
dotnet sln add src/BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj
```

**Açıklamalar:**
- `cd ../..` → Proje root dizinine dön (2 seviye yukarı)
- `dotnet sln add` → Solution'a proje ekle
- `src/BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj` → Eklenecek proje dosyasının yolu

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
src/BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj
```

**Sonuç:** ✅ Proje solution'a eklendi

---

### Adım 3: Framework Reference Ekle

**Komut:**
```bash
cd src/BuildingBlocks/BuildingBlocks.Exceptions
```

**Açıklamalar:**
- `cd src/BuildingBlocks/BuildingBlocks.Exceptions` → Proje klasörüne geç
- Framework reference, `.csproj` dosyasına manuel olarak eklenir

**Neden Framework Reference?**
- Paket yerine framework reference kullanılır çünkü:
  1. **Daha hafif:** Ayrı paket indirmeye gerek yok, .NET 9 ile birlikte gelir
  2. **Versiyon uyumluluğu:** .NET 9 ile birlikte gelen versiyon kullanılır, uyumsuzluk riski yok
  3. **Best Practice:** .NET 8+ için önerilen yaklaşım
  4. **Performans:** Paket restore işlemi gerekmez, daha hızlı

**Ne işe yarar:**
- `Microsoft.AspNetCore.App` framework reference, ASP.NET Core'un temel bileşenlerini sağlar
- Bu framework reference sayesinde şu sınıflar kullanılabilir:
  - `IExceptionHandler` → Exception handling interface'i
  - `ProblemDetails` → RFC 7807 standart hata formatı
  - `HttpContext` → Request/Response'a erişim
  - `ILogger<T>` → Logging için
  - `StatusCodes` → HTTP status code sabitleri

**Framework Reference vs Paket:**
- **Framework Reference:** .NET SDK ile birlikte gelir, ayrı indirme gerekmez
- **Paket:** NuGet'ten indirilir, versiyon yönetimi gerekir
- Bu projede framework reference tercih edildi (daha modern, daha hafif)

**BuildingBlocks.Exceptions.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
</Project>
```

**Sonuç:** ✅ Framework reference eklendi, `BuildingBlocks.Exceptions.csproj` dosyasına eklendi

---

### Adım 4: Klasör Yapısını Oluştur

**Komut:**
```bash
mkdir Exceptions
mkdir Handler
```

**Açıklamalar:**
- `mkdir Exceptions` → Exception class'ları için klasör
- `mkdir Handler` → Global exception handler için klasör

**Neden bu klasörler?**
1. **Kod organizasyonu:** İlgili dosyalar bir arada tutulur
2. **Okunabilirlik:** Proje yapısı anlaşılır olur
3. **Bakım kolaylığı:** İlgili dosyaları bulmak kolaylaşır
4. **Namespace yapısı:** Klasör yapısı namespace yapısını yansıtır

**Ne işe yarar:**
- `Exceptions/` → Custom exception class'ları için
  - `NotFoundException.cs` → 404 Not Found hatası
  - `BadRequestException.cs` → 400 Bad Request hatası
  - `InternalServerException.cs` → 500 Internal Server hatası
- `Handler/` → Global exception handler için
  - `GlobalExceptionHandler.cs` → Tüm exception'ları yakalayan handler

**Klasör Yapısı:**
```
BuildingBlocks.Exceptions/
├── Exceptions/
│   ├── NotFoundException.cs
│   ├── BadRequestException.cs
│   └── InternalServerException.cs
├── Handler/
│   └── GlobalExceptionHandler.cs
└── BuildingBlocks.Exceptions.csproj
```

**Namespace Yapısı:**
- `BuildingBlocks.Exceptions.Exceptions` → Exception class'ları
- `BuildingBlocks.Exceptions.Handler` → Handler class'ları

**Sonuç:** ✅ Klasör yapısı oluşturuldu

---

### Adım 5: Exception Class'larını Oluştur

**Ne yapacağız:** Tüm servislerde kullanılacak özel exception tiplerini oluşturacağız.

**Neden özel exception class'ları?**
1. **Tutarlılık:** Tüm servislerde aynı exception tipleri kullanılır
2. **HTTP status code mapping:** Her exception tipi belirli bir HTTP status code'u temsil eder
3. **Okunabilirlik:** Kod daha anlaşılır olur (`throw new NotFoundException(...)` vs `throw new Exception(...)`)
4. **Global handler:** GlobalExceptionHandler bu exception'ları tanır ve doğru HTTP status code döner

**Oluşturulacak Dosyalar:**

#### 1. `Exceptions/NotFoundException.cs` → 404 Not Found hatası

**Ne işe yarar:**
- Kayıt bulunamadığında fırlatılır
- HTTP 404 Not Found status code'u ile eşleşir
- GlobalExceptionHandler bu exception'ı yakalayıp 404 ProblemDetails döner

**Kod:**
```csharp
namespace BuildingBlocks.Exceptions.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.")
    {
    }
}
```

**Constructor'lar:**
- `NotFoundException(string message)` → Özel mesaj ile
- `NotFoundException(string name, object key)` → Entity adı + key ile standart format

**Kullanım örneği:**
```csharp
if (product == null)
{
    throw new NotFoundException("Product", id);
    // Sonuç: "Entity "Product" (123) was not found."
}
```

---

#### 2. `Exceptions/BadRequestException.cs` → 400 Bad Request hatası

**Ne işe yarar:**
- Geçersiz istek durumunda fırlatılır (validasyon hatası, eksik parametre, vb.)
- HTTP 400 Bad Request status code'u ile eşleşir
- GlobalExceptionHandler bu exception'ı yakalayıp 400 ProblemDetails döner

**Kod:**
```csharp
namespace BuildingBlocks.Exceptions.Exceptions;

public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message)
    {
    }
}
```

**Constructor:**
- `BadRequestException(string message)` → Hata mesajı ile

**Kullanım örneği:**
```csharp
if (id <= 0)
{
    throw new BadRequestException("Product ID must be greater than 0");
}
```

---

#### 3. `Exceptions/InternalServerException.cs` → 500 Internal Server hatası

**Ne işe yarar:**
- Beklenmeyen sunucu hatası durumunda fırlatılır (veritabanı hatası, vb.)
- HTTP 500 Internal Server Error status code'u ile eşleşir
- GlobalExceptionHandler bu exception'ı yakalayıp 500 ProblemDetails döner

**Kod:**
```csharp
namespace BuildingBlocks.Exceptions.Exceptions;

public class InternalServerException : Exception
{
    public InternalServerException(string message) : base(message)
    {
    }

    public InternalServerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
```

**Constructor'lar:**
- `InternalServerException(string message)` → Sadece mesaj ile
- `InternalServerException(string message, Exception innerException)` → Mesaj + orijinal hata ile

**InnerException nedir?**
- Orijinal hatayı saklar (debug için)
- Kullanıcıya genel mesaj, log'a detaylı hata
- Hata zincirini korur

**Kullanım örneği:**
```csharp
try
{
    await _repository.CreateAsync(entity);
}
catch (SqlException ex)
{
    throw new InternalServerException("Database error occurred", ex);
}
```

---

**Özet:**
- **NotFoundException** → 404 Not Found (kayıt bulunamadı)
- **BadRequestException** → 400 Bad Request (geçersiz istek)
- **InternalServerException** → 500 Internal Server Error (sunucu hatası)

**Sonuç:** ✅ Exception class'ları oluşturuldu

---

### Adım 6: Global Exception Handler Oluştur

**Ne yapacağız:** Tüm exception'ları yakalayıp standart ProblemDetails formatında response dönen handler oluşturacağız.

**Neden GlobalExceptionHandler?**
1. **Merkezi exception handling:** Tüm exception'lar tek bir yerde handle edilir
2. **Standart format:** Tüm servislerde aynı hata formatı (ProblemDetails - RFC 7807)
3. **HTTP status code mapping:** Exception tipine göre otomatik HTTP status code belirlenir
4. **Logging:** Tüm exception'lar otomatik loglanır
5. **Güvenlik:** Stack trace gösterilmez, kullanıcı dostu mesajlar

**Oluşturulacak Dosya:**
- `Handler/GlobalExceptionHandler.cs`

**Ne işe yarar:**
- `IExceptionHandler` interface'ini implement eder
- Tüm exception'ları yakalar (middleware pipeline üzerinden)
- Exception tipine göre HTTP status code belirler
- ProblemDetails formatında response döner (RFC 7807 standardı)
- Tüm servislerde aynı hata formatı kullanılır
- Exception'ları loglar (ILogger ile)

**Kod Yapısı:**
```csharp
// GlobalExceptionHandler.cs
using BuildingBlocks.Exceptions.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BuildingBlocks.Exceptions.Handler;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        var problemDetails = CreateProblemDetails(exception, httpContext);
        
        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        var json = JsonSerializer.Serialize(problemDetails);
        await httpContext.Response.WriteAsync(json, cancellationToken);

        return true;
    }

    private static ProblemDetails CreateProblemDetails(Exception exception, HttpContext httpContext)
    {
        return exception switch
        {
            NotFoundException notFound => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = notFound.Message,
                Instance = httpContext.Request.Path
            },
            BadRequestException badRequest => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = badRequest.Message,
                Instance = httpContext.Request.Path
            },
            InternalServerException internalServer => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = internalServer.Message,
                Instance = httpContext.Request.Path
            },
            _ => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred",
                Instance = httpContext.Request.Path
            }
        };
    }
}
```

**Özellikler:**
- `IExceptionHandler` interface'ini implement eder
- Dependency Injection ile `ILogger<GlobalExceptionHandler>` alır
- Exception tipine göre ProblemDetails oluşturur (switch expression)
- NotFoundException → 404, BadRequestException → 400, InternalServerException → 500
- Diğer exception'lar → 500 (genel hata)

**Kullanılan Sınıflar ve Görevleri:**
- `IExceptionHandler` → ASP.NET Core'un exception handling interface'i (.NET 8+)
- `ILogger<T>` → Exception'ları loglamak için
- `ProblemDetails` → RFC 7807 standart hata formatı
- `HttpContext` → Request/Response'a erişim
- `StatusCodes` → HTTP status code sabitleri (404, 400, 500)
- `JsonSerializer` → ProblemDetails'i JSON'a çevirmek için

**Akış:**
1. Exception fırlatıldı (Controller'da)
2. Middleware pipeline'dan geçer
3. `UseExceptionHandler()` middleware'i yakalar
4. DI container'dan `GlobalExceptionHandler` bulunur
5. `TryHandleAsync()` çağrılır
6. Exception loglanır (`_logger.LogError()`)
7. ProblemDetails oluşturulur (`CreateProblemDetails()`)
8. HTTP status code ve content type ayarlanır
9. JSON response döndürülür

**Sonuç:** ✅ Global exception handler oluşturuldu

---

### Adım 7: GlobalExceptionHandler'ı Kullanma (Web API'de)

**Ne yapacağız:** Web API projelerinde (Catalog.API, Basket.API, vb.) GlobalExceptionHandler'ı register edip kullanacağız.

**Neden bu adım gerekli?**
- GlobalExceptionHandler oluşturuldu ama henüz kullanılmıyor
- Web API projelerinde register edilmesi gerekir
- Her serviste aynı exception handling mekanizması kullanılır

**Program.cs'de Register Etme:**

**Neden `AddExceptionHandler<GlobalExceptionHandler>()`?**
- Handler'ı DI container'a kaydeder
- ASP.NET Core, `IExceptionHandler` implementasyonlarını otomatik bulur
- Exception fırlatıldığında handler çağrılır

**Neden `AddProblemDetails()`?**
- ProblemDetails desteğini aktif eder
- RFC 7807 standart formatını kullanır
- Tüm servislerde aynı hata formatı sağlar

**Neden `UseExceptionHandler()` en üstte?**
- Tüm middleware'lerde oluşan exception'ları yakalamalı
- En üstte olmalı ki diğer middleware'lerdeki hataları da yakalasın
- Sıralama önemli: `UseExceptionHandler()` → diğer middleware'ler

**Program.cs'de:**
```csharp
using BuildingBlocks.Exceptions.Handler;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ GlobalExceptionHandler'ı register et
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(); // ProblemDetails desteğini ekle

var app = builder.Build();

// ✅ Exception handler middleware'ini ekle (en üstte olmalı)
app.UseExceptionHandler();

// Middleware pipeline
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**Controller'da Kullanım:**

**Neden try-catch gerekmez?**
- GlobalExceptionHandler tüm exception'ları otomatik yakalar
- Try-catch gereksiz, exception'ı direkt fırlatmak yeterli
- Kod daha temiz ve okunabilir olur

**Controller'da:**
```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetProduct(int id)
{
    var product = await _productService.GetByIdAsync(id);
    if (product == null)
    {
        throw new NotFoundException("Product", id);  // ← Direkt fırlat, try-catch gerekmez
    }
    return Ok(product);
}
```

**Akış:**
1. Exception fırlatıldı (Controller'da) → `.NET Runtime` exception'ı yakalar
2. Exception yukarı doğru fırlatılır (stack unwind) → Middleware pipeline'a gider
3. `UseExceptionHandler()` middleware'i yakalar → DI container'dan handler'ları bulur
4. `GlobalExceptionHandler.TryHandleAsync()` çağrılır → Exception loglanır, ProblemDetails oluşturulur
5. ProblemDetails formatında response döndürülür → HTTP status code + JSON response

**Sonuç:** ✅ GlobalExceptionHandler kullanıma hazır

---

### 2.1 Bölümü - Tamamlanan Kontroller

✅ BuildingBlocks.Exceptions projesi oluşturuldu
✅ Proje solution'a eklendi
✅ Microsoft.AspNetCore.App framework reference eklendi
✅ Klasör yapısı oluşturuldu (Exceptions/, Handler/)
✅ Exception class'ları oluşturuldu (NotFoundException, BadRequestException, InternalServerException)
✅ GlobalExceptionHandler oluşturuldu
✅ Proje build oluyor mu? (`dotnet build`) → ✅ Başarılı
✅ Solution'da görünüyor mu? (`dotnet sln list`) → ✅ Görünüyor

---

## Öğrenilenler

### Class Library Projesi Nedir?

**Class Library** = Paylaşılan kod kütüphanesi

- Çalıştırılabilir bir uygulama değildir (exe/dll olarak çalışmaz)
- Diğer projeler tarafından kullanılmak üzere kod içeren bir kütüphanedir
- Ortak kodları tek bir yerde toplar, diğer projeler bunu referans eder

**Fark:**

| Proje Tipi | Ne İşe Yarar | Çalıştırılabilir mi? |
|------------|--------------|----------------------|
| **Web API** | REST API servisi | ✅ Evet (çalıştırılır) |
| **Class Library** | Paylaşılan kod | ❌ Hayır (sadece referans edilir) |

### Solution'a Proje Ekleme

**Neden gerekli?**
- Projeyi solution'a eklemek, projenin solution'ın bir parçası olduğunu belirtir
- IDE'lerde (VS Code, Visual Studio) görünür
- `dotnet sln list` ile kontrol edilebilir
- Diğer projeler bu projeyi referans edebilir

**Komut:**
```bash
dotnet sln add <proje-yolu>
```

### ProblemDetails Nedir?

**ProblemDetails** = RFC 7807 standardı (API hata response formatı)

**Neden ProblemDetails?**
1. **Standart format:** Tüm API'lerde aynı hata formatı
2. **Kullanıcı dostu:** Anlaşılır hata mesajları
3. **Güvenlik:** Stack trace gösterilmez, güvenlik riski azalır
4. **Frontend uyumluluğu:** Frontend'de kolay işlenir
5. **RFC 7807 standardı:** Endüstri standardı, tüm API'lerde kullanılır

**Örnek Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Not Found",
  "status": 404,
  "detail": "Product with id 999 not found",
  "instance": "/api/products/999"
}
```

**Property'ler:**
- `type` → Hata tipi (RFC 7807 standardı)
- `title` → Hata başlığı (kısa açıklama)
- `status` → HTTP status code (404, 400, 500)
- `detail` → Detaylı hata mesajı
- `instance` → Hatanın oluştuğu request path

**Avantajları:**
- Standart format (tüm API'lerde aynı)
- Kullanıcı dostu hata mesajları
- Güvenlik (stack trace göstermez)
- Frontend'de kolay işlenir

### IExceptionHandler Interface'i

**IExceptionHandler** = ASP.NET Core'un exception handling interface'i (.NET 8+)

**Neden bu interface?**
- .NET 8+ ile gelen standart pattern
- Önceden custom middleware yazılıyordu, şimdi interface yeterli
- Framework otomatik bulur ve kullanır
- Best practice yaklaşımı

**Ne işe yarar:**
- Global exception handling için standart pattern
- `TryHandleAsync()` metodu ile exception'ları handle eder
- Middleware pipeline üzerinden çalışır
- DI container üzerinden otomatik keşfedilir

**Interface Signature:**
```csharp
public interface IExceptionHandler
{
    ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken);
}
```

**Parametreler:**
- `HttpContext` → Request/Response'a erişim
- `Exception` → Fırlatılan exception
- `CancellationToken` → İptal desteği

**Return:**
- `ValueTask<bool>` → Async işlem
- `true` → Exception handle edildi (başka handler'a gitmez)
- `false` → Bir sonraki handler'a geç

**Kullanım:**
```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Exception'ı handle et
        return true;  // ← Exception handle edildi
    }
}
```

**Program.cs'de Register:**
```csharp
// Handler'ı DI container'a kaydet
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Exception handling middleware'ini aktif et
app.UseExceptionHandler();
```

### Exception Handling Akışı

**Akış:**
1. Exception fırlatıldı (Controller'da)
2. .NET Runtime exception'ı yakalar
3. Exception yukarı doğru fırlatılır (stack unwind)
4. Middleware pipeline'dan geçer
5. `UseExceptionHandler()` middleware'i yakalar
6. DI container'dan `IExceptionHandler` implementasyonları bulunur
7. `GlobalExceptionHandler.TryHandleAsync()` çağrılır
8. Exception loglanır, ProblemDetails oluşturulur, response döndürülür

**Detaylı açıklama için:** `learned-faz-2/exception-handling-akisi.md` dosyasına bak.

---

## 2.2 BuildingBlocks.Behaviors - Yapılanlar

**Hedef:** MediatR Pipeline Behaviors (Validation, Logging)

### Adım 1: Class Library Projesi Oluştur

**Komut:**
```bash
cd src/BuildingBlocks
dotnet new classlib -n BuildingBlocks.Behaviors
```

**Açıklamalar:**
- `cd src/BuildingBlocks` → BuildingBlocks klasörüne geç
- `dotnet new classlib` → Yeni class library projesi oluştur
- `-n BuildingBlocks.Behaviors` → Proje adı

**Ne işe yarar:**
- MediatR pipeline behaviors için class library projesi oluşturur
- Validation ve Logging behavior'ları bu projede olacak
- Diğer servisler bu projeyi referans edecek
- Class library = çalıştırılabilir değil, sadece kod içerir (kütüphane)

**Sonuç:** 
- `src/BuildingBlocks/BuildingBlocks.Behaviors/` klasörü oluşturuldu
- `BuildingBlocks.Behaviors.csproj` dosyası oluşturuldu
- Varsayılan `Class1.cs` dosyası oluşturuldu (sonra silinecek)

---

### Adım 2: Projeyi Solution'a Ekle

**Komut:**
```bash
cd ../..
dotnet sln add src/BuildingBlocks/BuildingBlocks.Behaviors/BuildingBlocks.Behaviors.csproj
```

**Açıklamalar:**
- `cd ../..` → Proje root dizinine dön (2 seviye yukarı)
- `dotnet sln add` → Solution'a proje ekle
- `src/BuildingBlocks/BuildingBlocks.Behaviors/BuildingBlocks.Behaviors.csproj` → Eklenecek proje dosyasının yolu

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
src/BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj
src/BuildingBlocks/BuildingBlocks.Behaviors/BuildingBlocks.Behaviors.csproj
```

**Sonuç:** ✅ Proje solution'a eklendi

---

### Adım 3: NuGet Paketlerini Ekle

**Komutlar:**
```bash
cd src/BuildingBlocks/BuildingBlocks.Behaviors
dotnet add package MediatR
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
dotnet add package Serilog.AspNetCore
```

**Açıklamalar:**
- `cd src/BuildingBlocks/BuildingBlocks.Behaviors` → Proje klasörüne geç
- `dotnet add package` → NuGet paketi ekle
- Her paket ayrı ayrı eklenir
- Paketler `Directory.Packages.props` dosyasına merkezi paket yönetimi ile eklenir

**Neden bu paketler?**
1. **MediatR** → Pipeline behavior pattern'i için gerekli
2. **FluentValidation** → Request validation için
3. **FluentValidation.DependencyInjectionExtensions** → FluentValidation'ı DI container'a entegre etmek için
4. **Serilog.AspNetCore** → Structured logging için

**Paketler ve Görevleri:**

#### 1. `MediatR` (14.0.0)
**Ne işe yarar:**
- MediatR pipeline mekanizmasını sağlar
- `IPipelineBehavior<TRequest, TResponse>` interface'ini içerir
- Request/Response pipeline'ını yönetir
- Handler pattern'i için aracı (mediator) görevi görür

**Neden gerekli?**
- ValidationBehavior ve LoggingBehavior, MediatR pipeline'ında çalışır
- Tüm request'ler pipeline'dan geçer, behavior'lar bu sırada çalışır

#### 2. `FluentValidation` (12.1.1)
**Ne işe yarar:**
- Fluent API ile validation kuralları yazmayı sağlar
- Validation logic'i merkezi hale getirir
- ValidationBehavior'da kullanılacak

**Neden gerekli?**
- ValidationBehavior, FluentValidation kullanarak request'leri doğrular
- Her request için validator yazılır, ValidationBehavior otomatik çalıştırır

#### 3. `FluentValidation.DependencyInjectionExtensions` (12.1.1)
**Ne işe yarar:**
- FluentValidation'ı DI container'a entegre eder
- `AddValidatorsFromAssembly()` gibi extension metodlarını sağlar
- ValidationBehavior'da validator'ları bulmak için gerekli

**Neden gerekli?**
- ValidationBehavior, DI container'dan validator'ları alır
- Bu paket olmadan validator'lar DI container'a kaydedilemez

#### 4. `Serilog.AspNetCore` (10.0.0)
**Ne işe yarar:**
- Structured logging (yapılandırılmış loglama) sağlar
- ASP.NET Core entegrasyonu içerir
- LoggingBehavior'da kullanılacak

**Neden gerekli?**
- LoggingBehavior, Serilog kullanarak request/response'ları loglar
- Structured logging sayesinde loglar kolay analiz edilir

**Sonuç:** ✅ Tüm paketler eklendi, `Directory.Packages.props` dosyasına merkezi paket yönetimi ile eklendi

---

### Adım 4: Klasör Yapısını Oluştur

**Komut:**
```bash
mkdir Behaviors
```

**Açıklamalar:**
- `mkdir Behaviors` → Behavior class'ları için klasör

**Neden bu klasör?**
1. **Kod organizasyonu:** İlgili dosyalar bir arada tutulur
2. **Okunabilirlik:** Proje yapısı anlaşılır olur
3. **Bakım kolaylığı:** İlgili dosyaları bulmak kolaylaşır
4. **Namespace yapısı:** Klasör yapısı namespace yapısını yansıtır

**Ne işe yarar:**
- `Behaviors/` → MediatR pipeline behavior'ları için
  - `ValidationBehavior.cs` → Request validation behavior'ı
  - `LoggingBehavior.cs` → Request/Response logging behavior'ı

**Klasör Yapısı:**
```
BuildingBlocks.Behaviors/
├── Behaviors/
│   ├── ValidationBehavior.cs
│   └── LoggingBehavior.cs
└── BuildingBlocks.Behaviors.csproj
```

**Namespace Yapısı:**
- `BuildingBlocks.Behaviors.Behaviors` → Behavior class'ları

**Sonuç:** ✅ Klasör yapısı oluşturuldu

---

### Adım 5: ValidationBehavior Oluştur

**Ne yapacağız:** MediatR pipeline'ında tüm request'leri otomatik olarak FluentValidation ile doğrulayan bir behavior oluşturacağız.

**Neden ValidationBehavior?**
1. **Merkezi validation:** Tüm request'ler otomatik doğrulanır
2. **Kod tekrarı önlenir:** Her handler'da validation yazmaya gerek yok
3. **Tutarlılık:** Tüm servislerde aynı validation mekanizması
4. **Hata yönetimi:** Validation hataları standart format ile döner

**Oluşturulacak Dosya:**
- `Behaviors/ValidationBehavior.cs`

**Ne işe yarar:**
- `IPipelineBehavior<TRequest, TResponse>` interface'ini implement eder
- MediatR pipeline'ında çalışır
- Her request handler'dan önce çalışır
- Request'i FluentValidation ile doğrular
- Validation hatası varsa `ValidationException` fırlatır
- Validation başarılıysa handler'a devam eder (`await next()`)

**Kod Yapısı:**
- Generic class: `ValidationBehavior<TRequest, TResponse>`
- `IEnumerable<IValidator<TRequest>>` → DI container'dan validator'ları alır
- `Handle()` metodu → Pipeline'da çalışır
- FluentValidation kullanarak request'i doğrular
- Hata varsa `ValidationException` fırlatır

**Akış:**
```
Request geldi
  ↓
ValidationBehavior çalışır
  ↓
FluentValidation ile doğrula
  ↓
Hata var mı?
  ├─ Evet → ValidationException fırlat (handler'a gitmez)
  └─ Hayır → Handler'a devam et (await next())
```

**Kullanılan Sınıflar:**
- `IPipelineBehavior<TRequest, TResponse>` → MediatR pipeline interface'i
- `IValidator<TRequest>` → FluentValidation validator interface'i
- `ValidationContext<TRequest>` → FluentValidation validation context
- `ValidationException` → FluentValidation'ın exception'ı

**Neden ValidationBehavior var?**
- Tüm request'lerin otomatik doğrulanması için
- Handler'larda validation kodu yazmaya gerek kalmaz
- Merkezi validation yönetimi sağlar
- Validation hataları standart format ile döner

**Sonuç:** ✅ ValidationBehavior oluşturuldu

---

### Adım 6: LoggingBehavior Oluştur

**Ne yapacağız:** MediatR pipeline'ında tüm request/response'ları otomatik loglayan bir behavior oluşturacağız.

**Neden LoggingBehavior?**
1. **Merkezi logging:** Tüm request/response'lar otomatik loglanır
2. **Kod tekrarı önlenir:** Her handler'da logging yazmaya gerek yok
3. **Tutarlılık:** Tüm servislerde aynı logging formatı
4. **Debug kolaylığı:** Tüm request'lerin logları tek yerde
5. **Monitoring:** Production'da request/response takibi için

**Oluşturulacak Dosya:**
- `Behaviors/LoggingBehavior.cs`

**Ne işe yarar:**
- `IPipelineBehavior<TRequest, TResponse>` interface'ini implement eder
- MediatR pipeline'ında çalışır
- Handler'dan önce request'i loglar
- Handler'dan sonra response'u loglar
- Serilog kullanarak structured logging yapar
- Tüm MediatR request'lerini otomatik loglar

**Kod Yapısı:**
- Generic class: `LoggingBehavior<TRequest, TResponse>`
- `ILogger<LoggingBehavior<TRequest, TResponse>>` → DI container'dan logger alır
- `Handle()` metodu → Pipeline'da çalışır
- Request'i handler'dan önce loglar
- Response'u handler'dan sonra loglar

**Akış:**
```
Request geldi
  ↓
LoggingBehavior çalışır
  ↓
"Handling {RequestName}: {@Request}" loglanır
  ↓
Handler çalışır (await next())
  ↓
"Handled {RequestName}: {@Response}" loglanır
  ↓
Response döner
```

**Kullanılan Sınıflar:**
- `IPipelineBehavior<TRequest, TResponse>` → MediatR pipeline interface'i
- `ILogger<T>` → Microsoft.Extensions.Logging logger interface'i
- `typeof(TRequest).Name` → Request tipinin adını alır
- `{@Request}` ve `{@Response}` → Structured logging için object serialization

**Log Örneği:**
```
[INFO] Handling CreateProductCommand: {"Name": "iPhone 15", "Price": 35000, ...}
[INFO] Handled CreateProductCommand: {"Id": "guid", "Name": "iPhone 15", ...}
```

**Neden LoggingBehavior var?**
- Tüm request/response'ların otomatik loglanması için
- Handler'larda logging kodu yazmaya gerek kalmaz
- Merkezi logging yönetimi sağlar
- Production'da request takibi için
- Debug ve monitoring için faydalıdır

**Sonuç:** ✅ LoggingBehavior oluşturuldu

---

### 2.2 Bölümü - Tamamlanan Kontroller

✅ BuildingBlocks.Behaviors projesi oluşturuldu
✅ Proje solution'a eklendi
✅ NuGet paketleri eklendi (MediatR, FluentValidation, FluentValidation.DependencyInjectionExtensions, Serilog.AspNetCore)
✅ Klasör yapısı oluşturuldu (Behaviors/)
✅ ValidationBehavior oluşturuldu
✅ LoggingBehavior oluşturuldu
✅ Proje build oluyor mu? (`dotnet build`) → ✅ Başarılı
✅ Solution'da görünüyor mu? (`dotnet sln list`) → ✅ Görünüyor

---

## Öğrenilenler (Faz 2.2)

### MediatR Pipeline Behavior Nedir?

**Pipeline Behavior** = MediatR request/response pipeline'ında çalışan ara katmanlar

**Neden gerekli?**
- Cross-cutting concerns (validation, logging, caching, vb.) için
- Handler'larda bu kodları yazmaya gerek kalmaz
- Merkezi yönetim sağlar
- Kod tekrarı önlenir

**Nasıl çalışır?**
```
Request → Behavior 1 → Behavior 2 → Handler → Response
         (Logging)   (Validation)   (İş Mantığı)
```

**Pipeline Sırası:**
1. LoggingBehavior (request'i logla)
2. ValidationBehavior (request'i doğrula)
3. Handler (iş mantığını çalıştır)
4. LoggingBehavior (response'u logla)

### IPipelineBehavior Interface'i

**IPipelineBehavior** = MediatR pipeline behavior interface'i

**Ne işe yarar:**
- Pipeline'da çalışan behavior'lar için standart interface
- `Handle()` metodu ile request/response'u işler
- `next()` delegate'i ile bir sonraki adıma geçer

**Interface Signature:**
```csharp
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
```

**Parametreler:**
- `TRequest` → Request tipi
- `TResponse` → Response tipi
- `request` → Gelen request
- `next` → Bir sonraki adıma geçmek için delegate
- `cancellationToken` → İptal desteği

**Return:**
- `Task<TResponse>` → Response döner

**Kullanım:**
```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Validation yap
        // Hata varsa exception fırlat
        // Hata yoksa handler'a devam et
        return await next();
    }
}
```

### FluentValidation Nedir?

**FluentValidation** = Fluent API ile validation kuralları yazmayı sağlayan kütüphane

**Neden FluentValidation?**
1. **Okunabilirlik:** Validation kuralları açık ve anlaşılır
2. **Test edilebilirlik:** Validation logic'i kolay test edilir
3. **Merkezi yönetim:** Validation kuralları tek yerde
4. **Hata mesajları:** Detaylı ve özelleştirilebilir hata mesajları

**Örnek Validator:**
```csharp
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(100).WithMessage("Product name must not exceed 100 characters");
        
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0");
    }
}
```

**ValidationBehavior ile Kullanım:**
- ValidationBehavior, DI container'dan validator'ları alır
- Request'i validator'larla doğrular
- Hata varsa `ValidationException` fırlatır
- Handler'a gitmez, kullanıcıya hata döner

### Structured Logging Nedir?

**Structured Logging** = Logları yapılandırılmış formatta (JSON) kaydetme

**Neden Structured Logging?**
1. **Analiz kolaylığı:** Loglar kolay analiz edilir
2. **Arama:** Belirli alanlara göre arama yapılabilir
3. **Monitoring:** Log aggregation tool'ları ile entegre edilir
4. **Debug:** Production'da sorun tespiti kolaylaşır

**Serilog ile Örnek:**
```csharp
_logger.LogInformation("Handling {RequestName}: {@Request}", requestName, request);
```

**Çıktı:**
```json
{
  "Timestamp": "2024-12-15T10:30:00Z",
  "Level": "Information",
  "Message": "Handling CreateProductCommand",
  "RequestName": "CreateProductCommand",
  "Request": {
    "Name": "iPhone 15",
    "Price": 35000
  }
}
```

**LoggingBehavior ile Kullanım:**
- LoggingBehavior, tüm request/response'ları structured format ile loglar
- `{@Request}` ve `{@Response}` → Object serialization için
- Handler'larda logging kodu yazmaya gerek kalmaz

---

## Diğer Notlar

### [Tarih: ...]
- ...

---

**Son Güncelleme:** Aralık 2024

