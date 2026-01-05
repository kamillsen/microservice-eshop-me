# ASP.NET Core — HttpContext & ILogger<T> Çalışma Mantığı (Örnekli & Kod Referanslı)

## ✅ 1. HttpContext – Temel Mantık

Her gelen HTTP isteği için ASP.NET Core bir **HttpContext nesnesi oluşturur**.  
Bu nesne, o isteğe ait tüm bilgileri ve durumu taşır.

> **HttpContext = Bu HTTP isteğine ait ortam (context).**

---

## 🔹 HttpContext İçeriği — Örnek + Kod Karşılığı

### 🟡 `httpContext.Request` → Gelen istek (Client → Server)

İstemciden gelen HTTP verilerini içerir.

Örnek istek:
```
GET /api/products?id=5 HTTP/1.1
Host: example.com
Authorization: Bearer xyz123
```

| Alan | Açıklama | Kodda Karşılığı | Örnek |
|------|--------|----------------|------|
| URL (Path) | Endpoint yolu | `httpContext.Request.Path` | `/api/products` |
| Query string | Sorgu parametreleri | `httpContext.Request.QueryString` | `?id=5` |
| Headers | HTTP başlıkları | `httpContext.Request.Headers["Authorization"]` | `Bearer xyz123` |
| Body (gövde) | POST/PUT içeriği | `httpContext.Request.Body` | `{ "name": "Laptop" }` |

Body okuma örneği:
```csharp
using var reader = new StreamReader(httpContext.Request.Body);
var body = await reader.ReadToEndAsync();
```

---

### 🟢 `httpContext.Response` → Gönderilecek cevap (Server → Client)

Sunucunun client’a döneceği cevabı temsil eder.

Örnek:
```csharp
httpContext.Response.StatusCode = 404;
httpContext.Response.ContentType = "application/json";

await httpContext.Response.WriteAsync(
    "{\"message\": \"Product not found\"}"
);
```

| Alan | Açıklama | Kodda Karşılığı | Örnek Değer |
|------|--------|----------------|-----------|
| StatusCode | HTTP durum kodu | `httpContext.Response.StatusCode` | `404` |
| ContentType | İçerik formatı | `httpContext.Response.ContentType` | `application/json` |
| Body | Cevap içeriği | `httpContext.Response.WriteAsync("...")` | `{ "message": "Product not found" }` |

---

## 🔹 HttpContext Pipeline Akışı

1. İstek gelir  
2. Middleware’ler çalışır  
3. Endpoint / Controller çalışır  
4. Hata olursa exception fırlar  
5. **GlobalExceptionHandler devreye girer**  
6. `httpContext.Response` doldurulur  
7. Cevap client’a gönderilir  

---

## ⚙️ GlobalExceptionHandler Bağlantısı

```csharp
public async ValueTask<bool> TryHandleAsync(
    HttpContext httpContext,
    Exception exception,
    CancellationToken cancellationToken)
{
    await httpContext.Response.WriteAsync(json, cancellationToken);
}
```

- `httpContext` → isteğin context’i  
- `exception` → yakalanan hata  
- `cancellationToken` → bağlantı iptali  

> Yeni istek oluşturmaz — aynı isteğin **cevap gövdesine yazar**.

---

## ✅ 2. ILogger<T> – Çalışma Mantığı

### 🔹 ILogger<T> nedir?

```csharp
private readonly ILogger<MyService> _logger;
```

- `<MyService>` = **log kategorisi**
- Hangi sınıftan log geldiği belirlenir

---

### 🔹 Log Yazma + Çıktı Örneği

```csharp
_logger.LogInformation("İşlem başladı");
_logger.LogError(ex, "Hata oluştu");
```

Olası konsol çıktısı:
```
info: MyApp.Services.MyService[0]
      İşlem başladı

fail: MyApp.Services.MyService[0]
      Hata oluştu
      System.Exception: Bir şey patladı!
```

---

## 🔹 Logger Çalışma Akışı (Basitleştirilmiş)

1. `_logger.LogInformation(...)` çağrılır  
2. Log mesajı + seviye yakalanır  
3. Logger → provider’lara iletir  
4. Provider’lar logu şu hedeflere yazabilir:
   - Console  
   - Debug  
   - Dosya (Serilog / NLog)  
   - Cloud (Seq, AppInsights, Elastic)

> **Nereye yazılacağını provider belirler.**

---

## 🔹 Log Seviyesi Konfigürasyonu

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft": "Warning"
  }
}
```

---

## 🧩 HttpContext vs ILogger — Kavramsal Karşılaştırma

| Alan | Ne ifade eder | Kodda Karşılığı |
|------|--------------|----------------|
| Request Path | İstek yolu | `httpContext.Request.Path` |
| QueryString | Sorgu parametreleri | `httpContext.Request.QueryString` |
| Headers | HTTP başlıkları | `httpContext.Request.Headers[...]` |
| Request Body | Gelen gövde | `httpContext.Request.Body` |
| StatusCode | Dönen durum kodu | `httpContext.Response.StatusCode` |
| ContentType | Dönen içerik tipi | `httpContext.Response.ContentType` |
| Response Body | Dönen veri | `httpContext.Response.WriteAsync()` |
| Log yazma | Uygulama günlüğü | `_logger.LogInformation()` |
| Log hedefi | Log’un yazıldığı yer | Logger Provider |

---

## 🎯 Kısa Özet

- **HttpContext**
  - Request + Response + User + Items içerir  
  - Her istek için oluşturulur

- **ILogger<T>**
  - Log yazma abstraction’ıdır  
  - Gerçek yazma işini provider’lar yapar
