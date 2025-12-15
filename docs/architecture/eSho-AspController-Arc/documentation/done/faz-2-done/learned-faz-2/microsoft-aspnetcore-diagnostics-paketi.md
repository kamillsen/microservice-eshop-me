# Microsoft.AspNetCore.Diagnostics Paketi - Öğrenme Notları

> Bu paket, ProblemDetails formatı ve global exception handling için kullanılır.

---

## Paket Ne İşe Yarar?

**Microsoft.AspNetCore.Diagnostics** paketi, standart API hata formatı (ProblemDetails) ve global exception handling sağlar.

---

## ProblemDetails Nedir?

**ProblemDetails** = RFC 7807 standardı (API hata response formatı)

### Örnek Response:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Not Found",
  "status": 404,
  "detail": "Product with id 999 not found",
  "instance": "/api/products/999"
}
```

**Avantajları:**
- ✅ Standart format (tüm API'lerde aynı)
- ✅ Kullanıcı dostu hata mesajları
- ✅ Güvenlik (stack trace göstermez)
- ✅ Frontend'de kolay işlenir

---

## Senaryo: Ürün Bulunamadı

### Paket OLMADAN (❌ Kötü):

```
Kullanıcı: GET /api/products/999
↓
Hata: NullReferenceException
↓
Response: 500 Internal Server Error
{
  "error": "Object reference not set...",
  "stackTrace": "at ProductController..."
}
```

**Sorunlar:**
- ❌ Kullanıcı ne olduğunu anlamaz
- ❌ Stack trace gösterilir (güvenlik riski)
- ❌ Frontend işleyemez

---

### Paket İLE (✅ İyi):

```
Kullanıcı: GET /api/products/999
↓
Handler: throw new NotFoundException("Product not found")
↓
GlobalExceptionHandler: Yakalar
↓
ProblemDetails: { status: 404, title: "Not Found", detail: "Product not found" }
↓
Response: 404 Not Found
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Not Found",
  "status": 404,
  "detail": "Product with id 999 not found"
}
```

**Avantajlar:**
- ✅ Kullanıcı anlaşılır mesaj görür
- ✅ Stack trace gösterilmez (güvenli)
- ✅ Frontend kolay işler
- ✅ Standart format

---

## Basit Kod Örneği

### 1. ProblemDetails Kullanımı

```csharp
// GlobalExceptionHandler.cs
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // ProblemDetails oluştur (Microsoft.AspNetCore.Diagnostics sayesinde)
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "Not Found",
            Status = 404,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        // Response döndür
        httpContext.Response.StatusCode = 404;
        await httpContext.Response.WriteAsJsonAsync(problemDetails);
        return true;
    }
}
```

### 2. Exception Tipine Göre ProblemDetails

```csharp
ProblemDetails problemDetails = exception switch
{
    NotFoundException notFound => new ProblemDetails
    {
        Status = 404,
        Title = "Not Found",
        Detail = notFound.Message
    },
    BadRequestException badRequest => new ProblemDetails
    {
        Status = 400,
        Title = "Bad Request",
        Detail = badRequest.Message
    },
    _ => new ProblemDetails
    {
        Status = 500,
        Title = "Internal Server Error",
        Detail = "An error occurred"
    }
};
```

---

## Paket Ne Sağlıyor?

### 1. ProblemDetails Class

```csharp
// Bu class Microsoft.AspNetCore.Diagnostics paketinden gelir
public class ProblemDetails
{
    public string? Type { get; set; }      // Hata tipi
    public string? Title { get; set; }    // Başlık
    public int? Status { get; set; }       // HTTP status code
    public string? Detail { get; set; }    // Detaylı mesaj
    public string? Instance { get; set; }  // Request path
}
```

### 2. IExceptionHandler Interface

```csharp
// Bu interface Microsoft.AspNetCore.Diagnostics paketinden gelir
public interface IExceptionHandler
{
    ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken);
}
```

---

## Özet

**Microsoft.AspNetCore.Diagnostics paketi:**
- ✅ ProblemDetails class'ını sağlar (standart hata formatı)
- ✅ IExceptionHandler interface'ini sağlar (global exception handling)
- ✅ Tüm servislerde aynı hata formatını kullanmamızı sağlar

**Basitçe:**
- Paket yok → Karmaşık, güvensiz hata mesajları
- Paket var → Standart, güvenli, kullanıcı dostu hata mesajları

---

**Son Güncelleme:** Aralık 2024

