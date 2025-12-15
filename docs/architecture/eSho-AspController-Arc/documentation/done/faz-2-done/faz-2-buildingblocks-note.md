# Faz 2.1 - BuildingBlocks.Exceptions Notları

> Bu dosya, Faz 2.1 (BuildingBlocks.Exceptions) adım adım yaparken öğrendiklerimi not aldığım dosyadır.

---

## BuildingBlocks Nedir?

**BuildingBlocks**, tüm microservice'lerde ortak kullanılacak kod parçalarını içeren class library projeleridir. Bu sayede:

- ✅ Kod tekrarı önlenir
- ✅ Tutarlılık sağlanır
- ✅ Merkezi yönetim yapılır
- ✅ Bakım kolaylaşır

### Bu Bölümde:

**BuildingBlocks.Exceptions** → Exception handling (hata yönetimi)

> **Not:** 2.2 (BuildingBlocks.Behaviors) ve 2.3 (BuildingBlocks.Messaging) henüz yapılmadı. Yapıldığında ayrı dokümantasyon eklenecek.

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

## Diğer Notlar

### [Tarih: ...]
- ...

---

**Son Güncelleme:** Aralık 2024

