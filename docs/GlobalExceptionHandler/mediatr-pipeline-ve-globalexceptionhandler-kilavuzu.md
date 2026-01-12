# 🎯 MediatR Pipeline ve GlobalExceptionHandler - Kapsamlı Kılavuz

## 📋 İçindekiler
1. [Temel Kavramlar](#1-temel-kavramlar)
2. [Sistem Nasıl Çalışır? - Genel Akış](#2-sistem-nasıl-çalışır---genel-akış)
3. [HttpContext ve Middleware Pipeline](#3-httpcontext-ve-middleware-pipeline)
4. [Interface Pattern ve Dependency Inversion Principle](#4-interface-pattern-ve-dependency-inversion-principle)
5. [GlobalExceptionHandler Kurulumu ve Çalışma Mantığı](#5-globalexceptionhandler-kurulumu-ve-çalışma-mantığı)
6. [MediatR Pipeline ve Exception Akışı](#6-mediatr-pipeline-ve-exception-akışı)
7. [Exception Nasıl Fırlatılır ve Yakalanır?](#7-exception-nasıl-fırlatılır-ve-yakalanır)
8. [Pratik Senaryolar ve Örnekler](#8-pratik-senaryolar-ve-örnekler)
9. [En İyi Pratikler ve Öneriler](#9-en-iyi-pratikler-ve-öneriler)

---

## 1. Temel Kavramlar

### 1.1 MediatR Nedir?

**Analoji:** Santral Operatörü - Gelen çağrıları doğru departmana bağlar

**Teknik Açıklama:**
- Controller'lar ile handler'lar arasında aracılık yapan mediator pattern implementasyonu
- Controller sadece `_mediator.Send(command)` çağırır
- MediatR, command tipine bakarak doğru handler'ı bulur
- Handler iş mantığını çalıştırır ve sonucu döner
- Bu sayede Controller ve Handler arasında gevşek bağlantı (loose coupling) sağlanır

**Faydaları:**
- ✅ Controller'lar handler'ları doğrudan bilmek zorunda kalmaz
- ✅ İş mantığı handler'larda toplanır
- ✅ Pipeline behavior'ları (logging, validation) merkezi yönetilir

### 1.2 GlobalExceptionHandler Nedir?

**Analoji:** Hastane Acil Servisi - Tüm acil vakaları tek merkezde yönetir

**Teknik Açıklama:**
- Yakalanmamış exception'ları yakalayıp HTTP response'una çeviren ASP.NET Core middleware'i
- `IExceptionHandler` interface'ini implement eder
- Exception tipine göre uygun HTTP status code ve ProblemDetails oluşturur
- Tüm servislerde standart hata formatı sağlar

**Faydaları:**
- ✅ Merkezi exception handling
- ✅ Standart hata formatı (RFC 7807 - ProblemDetails)
- ✅ HTTP status code mapping
- ✅ Güvenlik (stack trace göstermez)

### 1.3 HttpContext Nedir?

**Analoji:** Bir müşteri talebi geldiğinde, o talebe ait tüm bilgilerin tek bir dosyada toplanması

**Teknik Açıklama:**
- Her HTTP request için ASP.NET Core tarafından oluşturulan context nesnesi
- Request, Response, User, Items, RequestServices içerir
- Kestrel Server tarafından oluşturulur
- Middleware Pipeline tarafından yakalanır ve aktarılır

**İçeriği:**
```csharp
public class HttpContext
{
    public HttpRequest Request { get; }           // Gelen istek bilgileri
    public HttpResponse Response { get; }        // Gönderilecek cevap bilgileri
    public ClaimsPrincipal User { get; }          // Kullanıcı bilgileri
    public IDictionary<object, object> Items { get; }  // Request boyunca kullanılabilecek key-value çiftleri
    public IServiceProvider RequestServices { get; }   // DI container'a erişim
}
```

### 1.4 Middleware Pipeline Nedir?

**Analoji:** Bir fabrika hattı - her istasyon (middleware) ürünü (request) işler ve bir sonrakine aktarır

**Teknik Açıklama:**
- HTTP request ve response pipeline'ında çalışan küçük bileşenler zinciri
- Her middleware, request'i işleyebilir, değiştirebilir veya bir sonraki middleware'e aktarabilir
- HttpContext her middleware'e parametre olarak aktarılır
- ExceptionHandlerMiddleware exception'ları yakalar

### 1.5 IExceptionHandler Interface Nedir?

**Analoji:** Standart Priz Sistemi - Herhangi bir cihaz (handler) standart prize (interface) takılabilir

**Teknik Açıklama:**
- ASP.NET Core'un exception handling için sağladığı interface (.NET 8+)
- `IExceptionHandler` implement eden sınıflar, yakalanmamış exception'ları işler
- Dependency Inversion Principle (DIP) prensibine uyum sağlar

**Neden Interface?**
- ✅ Esneklik: Farklı handler'lar kullanılabilir
- ✅ Test edilebilirlik: Mock interface ile test edilebilir
- ✅ Genişletilebilirlik: Yeni handler'lar eklenebilir

---

## 2. Sistem Nasıl Çalışır? - Genel Akış

### 2.1 Tam Akış Diyagramı

```
┌─────────────────────────────────────────────────────────┐
│ 1. HTTP Request Gelir                                    │
│    GET /api/products                                     │
└─────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────┐
│ 2. Kestrel Server                                       │
│    HttpContext oluşturulur                              │
│    - Request.Path = "/api/products"                    │
│    - Response.StatusCode = 200 (başlangıç)              │
│    - RequestServices = ServiceProvider (DI container)   │
└─────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────┐
│ 3. Middleware Pipeline                                   │
│    app.UseExceptionHandler()  ← Exception yakalama      │
│    app.UseHttpsRedirection()                            │
│    app.UseAuthentication()                              │
│    app.UseAuthorization()                               │
│    app.MapControllers()                                  │
└─────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────┐
│ 4. Controller                                           │
│    [HttpGet]                                            │
│    public async Task<IActionResult> GetProducts()        │
│    {                                                     │
│        var result = await _mediator.Send(query);        │
│        return Ok(result);                                │
│    }                                                     │
└─────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────┐
│ 5. MediatR Pipeline                                     │
│    LoggingBehavior → ValidationBehavior → Handler        │
│    (Exception fırlatılabilir burada)                    │
└─────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────┐
│ 6. Exception Fırlatıldı (Örnek: NotFoundException)      │
│    throw new NotFoundException("Product not found");     │
└─────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────┐
│ 7. ExceptionHandlerMiddleware                           │
│    catch (Exception ex) {                               │
│        var handler = context.RequestServices           │
│            .GetRequiredService<IExceptionHandler>();    │
│        await handler.TryHandleAsync(context, ex);       │
│    }                                                     │
└─────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────┐
│ 8. GlobalExceptionHandler                               │
│    TryHandleAsync(HttpContext, Exception)                │
│    - Exception tipine göre ProblemDetails oluştur      │
│    - httpContext.Response.StatusCode = 404              │
│    - httpContext.Response.WriteAsJsonAsync(...)         │
└─────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────┐
│ 9. HTTP Response                                        │
│    Status: 404 Not Found                                 │
│    Content-Type: application/problem+json                │
│    Body: {                                              │
│      "type": "...",                                      │
│      "title": "Not Found",                               │
│      "status": 404,                                      │
│      "detail": "Product not found"                       │
│    }                                                     │
└─────────────────────────────────────────────────────────┘
```

### 2.2 Özet Akış

1. **HTTP Request** → Kestrel Server tarafından alınır
2. **HttpContext Oluşturma** → Her request için yeni HttpContext oluşturulur
3. **Middleware Pipeline** → HttpContext pipeline'dan geçer
4. **Controller** → Request'i alır ve MediatR'a yönlendirir
5. **MediatR Pipeline** → Logging → Validation → Handler
6. **Exception Fırlatma** → Handler veya Repository'de exception fırlatılabilir
7. **Exception Yakalama** → ExceptionHandlerMiddleware yakalar
8. **Exception İşleme** → GlobalExceptionHandler exception'ı işler
9. **HTTP Response** → ProblemDetails formatında döner

---

## 3. HttpContext ve Middleware Pipeline

### 3.1 HttpContext Nasıl Oluşturulur?

**Oluşturan:** ASP.NET Core'un web server'ı (Kestrel/HttpListener)

**Akış:**
```
1. HTTP Request gelir (örn: GET /api/products)
   ↓
2. Kestrel Server request'i alır
   ↓
3. HttpContext nesnesi oluşturulur
   - Request.Path = "/api/products"
   - Request.Method = "GET"
   - Response.StatusCode = 200 (başlangıç)
   - RequestServices = ServiceProvider (DI container)
   ↓
4. HttpContext middleware pipeline'a aktarılır
```

**Kod Seviyesinde (Framework İçi):**
```csharp
// ASP.NET Core Framework içinde (sen görmüyorsun)
public class KestrelServer
{
    public async Task ProcessRequestAsync(HttpContext context)
    {
        // Her HTTP request için yeni HttpContext oluşturulur
        context.Request.Path = "/api/products";
        context.Request.Method = "GET";
        context.Response.StatusCode = 200; // Başlangıç değeri
        context.RequestServices = _serviceProvider; // DI container
        
        // Middleware pipeline'a aktar
        await _middlewarePipeline.InvokeAsync(context);
    }
}
```

### 3.2 Middleware Pipeline Nasıl Çalışır?

**Yapı:**
```csharp
// Program.cs'de middleware'ler sırayla eklenir:
app.UseExceptionHandler();      // 1. Exception handling
app.UseHttpsRedirection();      // 2. HTTPS yönlendirme
app.UseAuthentication();         // 3. Kimlik doğrulama
app.UseAuthorization();         // 4. Yetkilendirme
app.MapControllers();           // 5. Controller routing
```

**Pipeline Görseli:**
```
HTTP Request
    ↓
┌─────────────────────────────────────┐
│ UseExceptionHandler()               │ ← 1. Exception yakalama
│   try {                             │
│     ↓                               │
│   ┌─────────────────────────────┐ │
│   │ UseHttpsRedirection()       │ │ ← 2. HTTPS yönlendirme
│   │   ↓                         │ │
│   │ ┌─────────────────────────┐ │ │
│   │ │ UseAuthentication()      │ │ │ ← 3. Kimlik doğrulama
│   │ │   ↓                     │ │ │
│   │ │ ┌─────────────────────┐ │ │ │
│   │ │ │ UseAuthorization()  │ │ │ │ ← 4. Yetkilendirme
│   │ │ │   ↓                 │ │ │ │
│   │ │ │ ┌─────────────────┐ │ │ │ │
│   │ │ │ │ MapControllers() │ │ │ │ │ ← 5. Controller
│   │ │ │ │   ↓              │ │ │ │ │
│   │ │ │ │ Controller       │ │ │ │ │
│   │ │ │ │   ↓              │ │ │ │ │
│   │ │ │ └─────────────────┘ │ │ │ │
│   │ │ └─────────────────────┘ │ │ │
│   │ └─────────────────────────┘ │ │
│   └─────────────────────────────┘ │
└─────────────────────────────────────┘
    ↓
HTTP Response
```

**HttpContext Aktarımı:**
```csharp
// Her middleware HttpContext'i alır ve bir sonrakine aktarır
public class MiddlewarePipeline
{
    public async Task InvokeAsync(HttpContext context) // ← HttpContext burada!
    {
        // Her middleware'e context aktarılır
        await middleware1.InvokeAsync(context);
        await middleware2.InvokeAsync(context);
        // ...
    }
}
```

### 3.3 HttpContext'i Kim Yakalıyor?

**Kısa Cevap:**
1. **Oluşturan:** Kestrel Server
2. **Yakalayan:** Middleware Pipeline
3. **Kullanan:** Her middleware ve Controller'lar

**Detaylı Akış:**
```
1. HTTP Request gelir
   ↓
2. Kestrel/HttpListener → HttpContext oluşturur
   ↓
3. Middleware Pipeline başlar
   ↓
4. Her middleware'e HttpContext aktarılır
   ↓
5. ExceptionHandlerMiddleware exception yakalar
   ↓
6. HttpContext'i IExceptionHandler'a verir
```

**Önemli Noktalar:**
- ✅ HttpContext her request için yeni oluşturulur (scoped)
- ✅ Dependency Injection ile değil, pipeline mekanizması ile aktarılır
- ✅ Her middleware `InvokeAsync(HttpContext context)` metodunu alır

---

## 4. Interface Pattern ve Dependency Inversion Principle

### 4.1 Neden Interface Kullanılıyor?

.NET'te sürekli interface'lerle karşılaşmanın nedeni, **Dependency Inversion Principle (DIP)** prensibine uyulmasıdır. Bu, SOLID prensiplerinden biridir.

### 4.2 Sorun: Interface Olmadan Ne Olurdu?

```csharp
// ❌ KÖTÜ YAKLAŞIM: Interface olmadan
public class ExceptionHandlerMiddleware
{
    // GlobalExceptionHandler'a direkt bağımlı!
    private readonly GlobalExceptionHandler _handler;
    
    public ExceptionHandlerMiddleware(GlobalExceptionHandler handler)
    {
        _handler = handler; // ← Sıkı bağlantı (tight coupling)
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Sadece GlobalExceptionHandler kullanabilirsin!
            await _handler.TryHandleAsync(context, ex);
        }
    }
}
```

**Sorunlar:**
- ❌ Farklı bir handler kullanmak istersen middleware'i değiştirmen gerekir
- ❌ Test etmek zor (mock yapamazsın)
- ❌ Esnek değil

### 4.3 Çözüm: Interface ile

```csharp
// ✅ İYİ YAKLAŞIM: Interface ile
public class ExceptionHandlerMiddleware
{
    // Interface'e bağımlı, somut sınıfa değil!
    private readonly IExceptionHandler _handler;
    
    public ExceptionHandlerMiddleware(IExceptionHandler handler)
    {
        _handler = handler; // ← Gevşek bağlantı (loose coupling)
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Herhangi bir IExceptionHandler implementasyonu kullanabilir!
            await _handler.TryHandleAsync(context, ex);
        }
    }
}
```

**Faydalar:**
- ✅ **Esneklik:** Farklı handler'lar kullanabilirsin
- ✅ **Test edilebilirlik:** Mock interface ile test edebilirsin
- ✅ **Genişletilebilirlik:** Yeni handler'lar ekleyebilirsin
- ✅ **Bağımlılık tersine çevrilir:** Middleware, somut sınıfa değil interface'e bağımlı

### 4.4 Dependency Inversion Principle (DIP) Nedir?

**Tanım:** 
- Üst seviye modüller, alt seviye modüllere bağımlı olmamalı
- İkisi de **abstraction'lara (interface)** bağımlı olmalı

**Bu örnekte:**
- **Üst seviye:** `ExceptionHandlerMiddleware`
- **Alt seviye:** `GlobalExceptionHandler`
- **Abstraction:** `IExceptionHandler`

**Mantık:**
```
ExceptionHandlerMiddleware (üst seviye)
    ↓ bağımlı
IExceptionHandler (abstraction/interface)
    ↑ implement eder
GlobalExceptionHandler (alt seviye)
```

### 4.5 Gerçek Hayat Benzetmesi

**Interface Olmadan:**
- Priz sadece belirli bir marka cihazı kabul eder
- Farklı bir cihaz kullanmak istersen prizi değiştirmen gerekir

**Interface ile:**
- Priz standart bir şekil (interface) kabul eder
- Bu standarda uyan her cihaz çalışır
- Priz değişmeden farklı cihazlar kullanabilirsin

---

## 5. GlobalExceptionHandler Kurulumu ve Çalışma Mantığı

### 5.1 ⚠️ KRİTİK: Program.cs'de 3 Satır ZORUNLU!

Exception'ın yakalanabilmesi için Program.cs'de **3 satırın** mutlaka olması gerekir:

```csharp
// Program.cs'de:

// 1. Handler'ı DI container'a kaydet:
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
// ↑ Bu satır şunu yapar:
// builder.Services.AddSingleton<IExceptionHandler, GlobalExceptionHandler>();

// 2. ProblemDetails desteğini ekle (opsiyonel ama önerilir):
builder.Services.AddProblemDetails();
// ↑ ProblemDetails formatını destekler (RFC 7807)

// 3. Middleware'ı aktif et:
app.UseExceptionHandler();
// ↑ ASP.NET Core'un exception yakalama sistemini açar
```

**Bu 3 satırdan biri eksikse, GlobalExceptionHandler ÇALIŞMAZ!**

### 5.2 3 Aşamalı Sistem

#### Aşama 1: `app.UseExceptionHandler()` - Exception Yakalama Middleware'i

**Ne Yapar?**
- ASP.NET Core pipeline'ına exception yakalama middleware'i ekler
- Pipeline'daki herhangi bir yerde fırlatılan ve yakalanmamış exception'ları yakalar

**Arka Planda Ne Oluyor?**
```csharp
// ASP.NET Core'un içindeki ExceptionHandlerMiddleware:
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context) // ← HttpContext burada!
    {
        try
        {
            await _next(context); // ← HttpContext bir sonraki middleware'e aktarılır
        }
        catch (Exception exception) // ← BURADA YAKALANIR!
        {
            // DI'dan IExceptionHandler al (HttpContext.RequestServices kullanarak)
            var handler = context.RequestServices
                .GetRequiredService<IExceptionHandler>();
            
            // HttpContext'i handler'a ver ve TryHandleAsync'ı çağır
            await handler.TryHandleAsync(context, exception, cancellationToken);
            //                      ↑ HttpContext burada handler'a aktarılır
        }
    }
}
```

**Akış:**
1. `UseExceptionHandler()` → `ExceptionHandlerMiddleware` pipeline'a eklenir
2. Her HTTP request için Kestrel Server bir `HttpContext` oluşturur
3. `HttpContext` middleware pipeline'dan geçer
4. Controller çalışırken exception fırlatılırsa
5. `ExceptionHandlerMiddleware`'in `catch` bloğu yakalar
6. `HttpContext.RequestServices` üzerinden DI'dan `IExceptionHandler` alınır (senin `GlobalExceptionHandler`'ın)
7. `HttpContext` ile birlikte `TryHandleAsync()` çağrılır
8. `GlobalExceptionHandler` `HttpContext.Response`'i kullanarak HTTP response oluşturur

#### Aşama 2: `builder.Services.AddExceptionHandler<GlobalExceptionHandler>()` - DI Kaydı

**Ne Yapar?**
- GlobalExceptionHandler'ı `IExceptionHandler` olarak DI container'a kaydeder
- Böylece yakalanan exception'lar bu handler'a yönlendirilir

**Kod Seviyesinde:**
```csharp
// AddExceptionHandler() metodunun yaptığı:
builder.Services.AddSingleton<IExceptionHandler, GlobalExceptionHandler>();
// ↑ GlobalExceptionHandler'ı IExceptionHandler olarak kaydeder
// ExceptionHandlerMiddleware, DI'dan IExceptionHandler'ı alırken
// senin GlobalExceptionHandler'ını bulur
```

#### Aşama 3: `GlobalExceptionHandler` Sınıfı - Exception İşleyici

**Ne Yapar?**
- Yakalanan exception'ı analiz eder
- Tipine göre uygun HTTP status code ve ProblemDetails oluşturur
- Kullanıcıya JSON formatında döner

**Kod:**
```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }
    
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, // ← HttpContext burada! ExceptionHandlerMiddleware'den gelir
        Exception exception,     // ← GELEN EXCEPTION
        CancellationToken cancellationToken)
    {
        // 1. Exception'ı logla
        _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);
        
        // 2. Exception tipine bak (HttpContext'i de kullanarak)
        var problemDetails = CreateProblemDetails(exception, httpContext);
        
        // 3. HttpContext.Response kullanarak HTTP Response oluştur
        httpContext.Response.StatusCode = problemDetails.Status ?? 500;
        httpContext.Response.ContentType = "application/problem+json";
        
        // 4. JSON response yaz
        var json = JsonSerializer.Serialize(problemDetails);
        await httpContext.Response.WriteAsync(json, cancellationToken);
        
        return true; // "Exception'ı ben handle ettim"
    }
    
    private static ProblemDetails CreateProblemDetails(
        Exception exception, 
        HttpContext httpContext) // ← HttpContext burada da kullanılıyor
    {
        return exception switch
        {
            NotFoundException notFound => new ProblemDetails
            {
                Status = 404,
                Title = "Not Found",
                Detail = notFound.Message,
                Instance = httpContext.Request.Path // ← HttpContext.Request kullanımı
            },
            BadRequestException badRequest => new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = badRequest.Message,
                Instance = httpContext.Request.Path
            },
            InternalServerException internalServer => new ProblemDetails
            {
                Status = 500,
                Title = "Internal Server Error",
                Detail = internalServer.Message,
                Instance = httpContext.Request.Path
            },
            _ => new ProblemDetails
            {
                Status = 500,
                Title = "An error occurred while processing your request",
                Detail = "An unexpected error occurred",
                Instance = httpContext.Request.Path
            }
        };
    }
}
```

**HttpContext Kullanımı:**
- ✅ `httpContext.Request.Path` → Request path bilgisi (ProblemDetails.Instance için)
- ✅ `httpContext.Response.StatusCode` → HTTP status code ayarlama
- ✅ `httpContext.Response.ContentType` → Response content type ayarlama
- ✅ `httpContext.Response.WriteAsync()` → JSON response yazma
- ✅ `httpContext.RequestServices` → DI container'a erişim (gerekirse)

### 5.3 `builder.Services.AddProblemDetails()` - ProblemDetails Desteği (Opsiyonel)

**Ne İşe Yarar?**
- ProblemDetails formatını destekler (RFC 7807 standardı)
- Standart hata formatı sağlar
- Swagger/OpenAPI entegrasyonu için faydalıdır
- Opsiyoneldir ama önerilir

---

## 6. MediatR Pipeline ve Exception Akışı

### 6.1 MediatR Pipeline Akışı

```
HTTP Request → Controller → _mediator.Send(command)
    ↓
MediatR Pipeline:
    1. LoggingBehavior (Request logu)
    2. ValidationBehavior (FluentValidation)
    3. Handler (İş mantığı)
    ↓
Response → Controller → HTTP Response
```

### 6.2 ValidationBehavior ve Exception Fırlatma

```csharp
public async Task<TResponse> Handle(TRequest request, ...)
{
    // Validator'ları çalıştır
    var validationResults = await Task.WhenAll(
        _validators.Select(v => v.ValidateAsync(context)));
    
    if (failures.Any())
    {
        // ⚡ Exception fırlat
        throw new ValidationException(failures);
        // Bu exception GlobalExceptionHandler'a gidecek
    }
    
    return await next();
}
```

### 6.3 Exception Akışı (MediatR'dan GlobalExceptionHandler'a)

```
1. Handler'da exception fırlatılır
   ↓
2. MediatR pipeline'dan çıkar
   ↓
3. Controller'a gelir
   ↓
4. Controller'da try-catch YOK (en iyi pratik)
   ↓
5. Exception Controller'dan çıkar
   ↓
6. Middleware Pipeline'a gelir
   ↓
7. ExceptionHandlerMiddleware yakalar
   ↓
8. DI'dan IExceptionHandler alınır (GlobalExceptionHandler)
   ↓
9. GlobalExceptionHandler.TryHandleAsync() çağrılır
   ↓
10. HTTP Response olarak döner
```

---

## 7. Exception Nasıl Fırlatılır ve Yakalanır?

### 7.1 Exception Nasıl Fırlatılır?

```csharp
// 1. Basit exception fırlatma:
throw new Exception("Hata mesajı");

// 2. Custom exception fırlatma:
throw new NotFoundException("Ürün bulunamadı");

// 3. İç exception ile fırlatma:
throw new InvalidOperationException("İşlem geçersiz", innerException);
```

### 7.2 GlobalExceptionHandler'ın Yakalaması İçin Gerekenler

#### KRİTİK KURAL: Exception **YAKALANMAMALI** veya **YENİDEN FIRLATILMALI**

```csharp
// ✅ Senaryo 1: Hiç yakalanmaz (GlobalExceptionHandler çalışır)
throw new Exception();

// ✅ Senaryo 2: Yakalanır ve yeniden fırlatılır (GlobalExceptionHandler çalışır)
try 
{ 
    throw new Exception(); 
} 
catch 
{ 
    throw; // ⭐ Yeniden fırlat!
}

// ❌ Senaryo 3: Yakalanır ve fırlatılmaz (GlobalExceptionHandler ÇALIŞMAZ)
try 
{ 
    throw new Exception(); 
} 
catch 
{ 
    _logger.LogError("Hata"); 
    // throw YOK!
}
```

### 7.3 Async Metotlar Otomatik Exception Fırlatabilir!

**Önemli:** Async metotlar (özellikle EF Core metotları), senin kodunda `throw` keyword'ü görünmese bile, arka planda exception fırlatabilir.

**Örnek:**
```csharp
// Bu satırda "throw" gözükmüyor ama...
var basket = await _context.ShoppingCarts
    .FirstOrDefaultAsync(x => x.UserName == userName);
// ⚡ ARKA PLANDA EXCEPTION FIRLATABİLİR!
```

**Neden Exception Fırlatıyor?**

1. **Database Bağlantı Hatası:**
   - Database down
   - Connection string yanlış
   - Network problemi
   - → `DbUpdateException` fırlatır

2. **SQL Sorgu Hatası:**
   - Tablo yok
   - Kolon yok
   - Yanlış SQL syntax
   - → `SqlException` fırlatır

3. **Timeout:**
   - Query çok uzun sürüyor
   - Database yavaş
   - → `TimeoutException` fırlatır

**İşin Arkasındaki Teknik Detay:**
```csharp
// FirstOrDefaultAsync() metodunun basitleştirilmiş implementasyonu:
public async Task<TEntity> FirstOrDefaultAsync(...)
{
    try
    {
        // Database'e bağlan
        // SQL çalıştır
        // Sonucu dön
    }
    catch (Exception ex)
    {
        // ⭐ İŞTE BURASI KRİTİK!
        // Exception yakalanır VE YENİDEN FIRLATILIR!
        throw new DbUpdateException("Database error", ex);
    }
}
```

**Önemli Nokta:** EF Core, kendi içinde exception'ları yakalayıp, daha anlamlı exception tiplerine çevirerek yeniden fırlatır. Bu yüzden senin kodunda `throw` keyword'ü görünmese bile, async metot çağrısı exception fırlatabilir.

### 7.4 GlobalExceptionHandler'a Nasıl Ulaşır?

```
1. FirstOrDefaultAsync() INTERNALLY exception fırlatır
2. Exception senin DeleteBasket() metoduna gelir
3. DeleteBasket() method'unda try-catch YOK
4. Exception DeleteBasket()'ten çıkar
5. MediatR Handler'a gelir
6. Handler'da try-catch YOK
7. Controller'a gelir
8. Controller'da try-catch YOK
9. app.UseExceptionHandler() yakalar
10. GlobalExceptionHandler işler
```

---

## 8. Pratik Senaryolar ve Örnekler

### 8.1 Controller'da Try-Catch Durumu

#### Senaryo 1: Controller Exception'ı Yakalar ve Yeniden FIRLATMAZSA ❌

```csharp
[HttpPost]
public async Task<IActionResult> StoreBasket([FromBody] ShoppingCartDto basket)
{
    try
    {
        var result = await _mediator.Send(new StoreBasketCommand(basket));
        return Ok(result);
    }
    catch (Exception ex)
    {
        // ❌ Sadece logluyor, yeniden fırlatmıyor!
        _logger.LogError(ex, "Hata oluştu");
        return StatusCode(500, "Internal Server Error");
    }
}
```

**SONUÇ:** GlobalExceptionHandler ÇALIŞMAZ! Exception Controller'da yakalanıp bitirildi.

#### Senaryo 2: Controller Exception'ı Yakalar ve Yeniden FIRLATIRSA ✅

```csharp
[HttpPost]
public async Task<IActionResult> StoreBasket([FromBody] ShoppingCartDto basket)
{
    try
    {
        var result = await _mediator.Send(new StoreBasketCommand(basket));
        return Ok(result);
    }
    catch (Exception ex)
    {
        // ✅ Logladı ve YENİDEN FIRLATTI
        _logger.LogError(ex, "Hata oluştu");
        throw; // ⭐ KRİTİK: GlobalExceptionHandler çalışır
    }
}
```

**SONUÇ:** GlobalExceptionHandler ÇALIŞIR! Exception yeniden fırlatıldı.

#### Senaryo 3: Controller Exception'ı Yakalamazsa (EN İYİ PRATİK) ✅

```csharp
[HttpPost]
public async Task<ActionResult<ShoppingCartDto>> StoreBasket([FromBody] ShoppingCartDto basket)
{
    // ✅ Exception yakalamıyor, direkt dışarı fırlatıyor
    var result = await _mediator.Send(new StoreBasketCommand(basket));
    return Ok(result);
}
```

**SONUÇ:** GlobalExceptionHandler ÇALIŞIR! Exception hiç yakalanmadı.

**ÖZET:**
- ❌ **Controller yakalayıp fırlatmazsa** → GlobalExceptionHandler çalışmaz
- ✅ **Controller yakalayıp yeniden fırlatırsa** → GlobalExceptionHandler çalışır
- ✅ **Controller hiç yakalamazsa** → GlobalExceptionHandler çalışır (EN İYİ PRATİK)

### 8.2 Repository'de Exception Fırlatma Analizi

**Örnek Kod:**
```csharp
public async Task<bool> DeleteBasket(string userName)
{
    // 1. PostgreSQL'den sil
    var basket = await _context.ShoppingCarts
        .FirstOrDefaultAsync(x => x.UserName == userName);
    // ⚠️ Bu satır exception fırlatabilir: DbUpdateException, SqlException, vb.
    
    if (basket != null)
    {
        _context.ShoppingCarts.Remove(basket);
        await _context.SaveChangesAsync(); 
        // ⚠️ Bu satır exception fırlatabilir: DbUpdateException
    }
    
    // 2. Redis'ten sil
    try
    {
        var deleted = await _redis.KeyDeleteAsync(GetRedisKey(userName));
        if (deleted || basket != null)
        {
            _logger.LogInformation("Basket deleted for {UserName}", userName);
            return true;
        }
    }
    catch (RedisConnectionException ex)
    {
        _logger.LogWarning(ex, "Redis unavailable...");
        // ❌ throw YOK! GlobalExceptionHandler çalışmaz
    }
    
    return basket != null;
}
```

**Exception Fırlatan Yerler:**

1. **`FirstOrDefaultAsync()` - EXCEPTION FIRLATABİLİR ✅**
   - `DbUpdateException` - Database connection hatası
   - `SqlException` - SQL sorgu hatası
   - `TimeoutException` - Timeout hatası
   - **GlobalExceptionHandler çalışır mı?** → **EVET** (Çünkü yakalanmıyor)

2. **`SaveChangesAsync()` - EXCEPTION FIRLATABİLİR ✅**
   - `DbUpdateException` - Constraint violation, foreign key hatası
   - `DbUpdateConcurrencyException` - Concurrency conflict
   - **GlobalExceptionHandler çalışır mı?** → **EVET** (Çünkü yakalanmıyor)

3. **`KeyDeleteAsync()` - EXCEPTION FIRLATABİLİR ama YAKALANIYOR ❌**
   - `RedisConnectionException` fırlatabilir
   - Ama catch bloğunda yakalanıyor ve throw yok
   - **GlobalExceptionHandler çalışır mı?** → **HAYIR** (Çünkü catch var ve throw yok)

**Eksik Olan: NotFoundException Fırlatma**

Şu anki kod:
```csharp
if (basket != null)
{
    // Silme işlemi
}
return basket != null; // false döner
```

**GlobalExceptionHandler'ın çalışması için:**
```csharp
public async Task DeleteBasket(string userName)
{
    var basket = await _context.ShoppingCarts
        .FirstOrDefaultAsync(x => x.UserName == userName);
    
    // ⭐ EXCEPTION FIRLAT!
    if (basket == null)
        throw new NotFoundException($"Basket for {userName} not found");
    
    _context.ShoppingCarts.Remove(basket);
    await _context.SaveChangesAsync();
    
    // Redis silme...
}
```

### 8.3 Exception Yakalama Şartları

**GlobalExceptionHandler'ın Çalışması İçin 3 Şart:**

#### 1. Program.cs'de 3 Satır ZORUNLU OLMALI ⭐⭐⭐

```csharp
// Program.cs'de MUTLAKA olmalı:

// 1. Handler'ı DI'ya kaydet:
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// 2. ProblemDetails desteği (opsiyonel ama önerilir):
builder.Services.AddProblemDetails();

// 3. Middleware'ı aktif et:
app.UseExceptionHandler();
```

**Bu 3 satırdan biri eksikse, GlobalExceptionHandler ÇALIŞMAZ!**

#### 2. THROW OLMALI ⭐

```csharp
// ✅ Çalışır: throw var
throw new NotFoundException("Ürün yok");

// ✅ Çalışır: Async metot exception fırlatır (throw görünmez ama olur)
await _context.ShoppingCarts.FirstOrDefaultAsync(...);

// ❌ Çalışmaz: throw yok
_logger.LogError("Hata");
return false;
```

#### 3. YAKALANMAMALI veya YENİDEN FIRLATILMALI

```csharp
// ✅ Senaryo 1: Hiç yakalanmaz
throw new Exception(); // GlobalExceptionHandler ÇALIŞIR

// ✅ Senaryo 2: Yakalanır ve yeniden fırlatılır
try { throw; } catch { throw; } // GlobalExceptionHandler ÇALIŞIR

// ❌ Senaryo 3: Yakalanır ve fırlatılmaz
try { throw; } catch { log; } // GlobalExceptionHandler ÇALIŞMAZ
```

---

## 9. En İyi Pratikler ve Öneriler

### 9.1 GlobalExceptionHandler Güncellemesi

```csharp
// GlobalExceptionHandler.cs'de:
using FluentValidation;

return exception switch
{
    // Mevcutlar:
    NotFoundException => 404,
    BadRequestException => 400,
    InternalServerException => 500,
    
    // EKLENMESİ GEREKEN:
    ValidationException validationException => new ProblemDetails
    {
        Status = 400,
        Title = "Validation Error",
        Extensions = { ["errors"] = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()) }
    },
    
    // Diğer exception'lar:
    DbUpdateException => 500,
    RedisConnectionException => 500,
    
    // Default (exception mesajını göster):
    _ => new ProblemDetails
    {
        Status = 500,
        Title = "Internal Server Error",
        Detail = exception.Message // ⭐ Mesajı göster!
    }
};
```

### 9.2 Repository Pattern Düzeltmeleri

```csharp
// Redis işlemlerinde:
catch (RedisConnectionException ex)
{
    _logger.LogWarning(ex, "Redis hatası");
    throw; // ⭐ BU SATIRI EKLE!
}

// DeleteBasket'te:
public async Task DeleteBasket(string userName)
{
    var basket = await _context.ShoppingCarts
        .FirstOrDefaultAsync(x => x.UserName == userName);
    
    // ⭐ Exception fırlat:
    if (basket == null)
        throw new NotFoundException($"Basket for {userName} not found");
    
    _context.ShoppingCarts.Remove(basket);
    await _context.SaveChangesAsync();
    
    // Redis silme...
}
```

### 9.3 Handler'larda Exception Standardizasyonu

```csharp
public async Task<Response> Handle()
{
    try
    {
        // İş mantığı
        if (product == null)
            throw new NotFoundException("Ürün bulunamadı"); // 404
        
        if (user.Balance < product.Price)
            throw new BadRequestException("Yetersiz bakiye"); // 400
    }
    catch (Exception ex) when (ex is not NotFoundException and not BadRequestException)
    {
        throw new InternalServerException("Sistem hatası", ex); // 500
    }
}
```

### 9.4 Özet Tablolar

**GlobalExceptionHandler Çalışma Matrisi:**

| Durum | throw | catch | GlobalExceptionHandler |
|-------|-------|-------|----------------------|
| Direkt throw | ✅ VAR | ❌ YOK | ✅ ÇALIŞIR |
| Controller'da try-catch → throw; | ✅ VAR | ✅ VAR | ✅ ÇALIŞIR |
| Controller'da try-catch → return | ❌ YOK | ✅ VAR | ❌ ÇALIŞMAZ |
| Repository'de catch → throw; | ✅ VAR | ✅ VAR | ✅ ÇALIŞIR |
| Repository'de catch → log | ❌ YOK | ✅ VAR | ❌ ÇALIŞMAZ |
| Async metot exception (EF Core) | ✅ VAR (görünmez) | ❌ YOK | ✅ ÇALIŞIR |

---

## 🏆 Sonuç

### Özet:

- ✅ **Program.cs'de 3 satır ZORUNLU:**
  - `builder.Services.AddExceptionHandler<GlobalExceptionHandler>();` → Handler'ı DI'ya kaydet
  - `builder.Services.AddProblemDetails();` → ProblemDetails desteği (opsiyonel ama önerilir)
  - `app.UseExceptionHandler();` → Exception YAKALAMA motoru (ExceptionHandlerMiddleware)

- ✅ **`ExceptionHandlerMiddleware`** → Arka planda exception'ları yakalar, DI'dan `IExceptionHandler` alır, `TryHandleAsync()` çağırır

- ✅ **`GlobalExceptionHandler`** → Exception AYIRT ETME ve response oluşturma

- ✅ **Controller exception yakalamaz** → En iyi pratik

- ✅ **Async metotlar (EF Core) otomatik exception fırlatabilir** → `throw` görünmese bile olur

- ✅ **HttpContext** → Her request için oluşturulur, middleware pipeline'dan geçer, handler'a aktarılır

- ✅ **Interface Pattern** → Dependency Inversion Principle (DIP) prensibine uyum

**Bu düzenlemeleri yaparsan, exception handling sistemin TAMAMEN SAĞLAM olacak!** 🚀
