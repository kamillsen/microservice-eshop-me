# 🔄 HttpContext ve Middleware Pipeline - Detaylı Açıklama

## 📋 İçindekiler
1. [HttpContext Nedir ve Nasıl Oluşturulur?](#httpcontext-nedir-ve-nasıl-oluşturulur)
2. [Middleware Pipeline Nedir?](#middleware-pipeline-nedir)
3. [HttpContext'i Kim Yakalıyor?](#httpcontexti-kim-yakalıyor)
4. [Interface Pattern ve Dependency Inversion Principle](#interface-pattern-ve-dependency-inversion-principle)
5. [Tam Akış Diyagramı](#tam-akış-diyagramı)
6. [Kod Seviyesinde Detaylar](#kod-seviyesinde-detaylar)

---

## 🎯 HttpContext Nedir ve Nasıl Oluşturulur?

### **HttpContext Nedir?**

`HttpContext`, ASP.NET Core'da **her HTTP isteği için oluşturulan bir context nesnesidir**. Bu nesne, o isteğe ait tüm bilgileri ve durumu taşır.

**Analoji:** Bir müşteri talebi geldiğinde, o talebe ait tüm bilgilerin (kimlik, istek detayları, cevap hazırlığı) tek bir dosyada toplanması gibi.

### **HttpContext İçeriği:**

```csharp
public class HttpContext
{
    // Gelen istek bilgileri
    public HttpRequest Request { get; }
    
    // Gönderilecek cevap bilgileri
    public HttpResponse Response { get; }
    
    // Kullanıcı bilgileri (authentication/authorization)
    public ClaimsPrincipal User { get; }
    
    // Request boyunca kullanılabilecek key-value çiftleri
    public IDictionary<object, object> Items { get; }
    
    // Dependency Injection container'a erişim
    public IServiceProvider RequestServices { get; }
}
```

### **HttpContext Nasıl Oluşturulur?**

HttpContext, **ASP.NET Core'un web server'ı (Kestrel/HttpListener) tarafından** her HTTP request geldiğinde otomatik olarak oluşturulur.

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
   ↓
4. HttpContext middleware pipeline'a aktarılır
```

---

## 🔄 Middleware Pipeline Nedir?

### **Middleware Nedir?**

Middleware, HTTP request ve response pipeline'ında çalışan **küçük bileşenlerdir**. Her middleware, request'i işleyebilir, değiştirebilir veya bir sonraki middleware'e aktarabilir.

**Analoji:** Bir fabrika hattı gibi - her istasyon (middleware) ürünü (request) işler ve bir sonrakine aktarır.

### **Middleware Pipeline Yapısı:**

```csharp
// Program.cs'de middleware'ler sırayla eklenir:
app.UseExceptionHandler();      // 1. Exception handling
app.UseHttpsRedirection();      // 2. HTTPS yönlendirme
app.UseAuthentication();         // 3. Kimlik doğrulama
app.UseAuthorization();         // 4. Yetkilendirme
app.MapControllers();           // 5. Controller routing
```

### **Pipeline Görseli:**

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

---

## 🎯 HttpContext'i Kim Yakalıyor?

### **Kısa Cevap:**

1. **HttpContext'i oluşturan:** ASP.NET Core'un web server'ı (Kestrel)
2. **HttpContext'i yakalayan:** Middleware Pipeline
3. **HttpContext'i kullanan:** Her middleware ve son olarak Controller'lar

### **Detaylı Akış:**

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

### **Kod Seviyesinde:**

```csharp
// 1. ASP.NET Core Framework (Kestrel) - HttpContext oluşturur
// Bu kod framework içinde, sen görmüyorsun ama oluyor:
public class KestrelServer
{
    public async Task ProcessRequestAsync(HttpContext context)
    {
        // HttpContext burada oluşturulur
        // Request ve Response nesneleri hazırlanır
        // context.Request.Path = "/api/products"
        // context.Response.StatusCode = 200
    }
}

// 2. Middleware Pipeline - HttpContext'i aktarır
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

// 3. ExceptionHandlerMiddleware - Exception yakalar ve HttpContext'i kullanır
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    
    public async Task InvokeAsync(HttpContext context) // ← HttpContext burada!
    {
        try
        {
            await _next(context); // ← HttpContext bir sonraki middleware'e aktarılır
        }
        catch (Exception ex)
        {
            // HttpContext'i handler'a ver
            var handler = context.RequestServices
                .GetRequiredService<IExceptionHandler>();
            
            await handler.TryHandleAsync(context, ex, cancellationToken);
            //                      ↑ HttpContext burada handler'a aktarılır
        }
    }
}

// 4. GlobalExceptionHandler - HttpContext'i alır ve kullanır
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, // ← HttpContext burada!
        Exception exception,
        CancellationToken cancellationToken)
    {
        // HttpContext'i kullan
        httpContext.Response.StatusCode = 500;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsync(json);
    }
}
```

---

## 🏗️ Interface Pattern ve Dependency Inversion Principle

### **Neden Interface Kullanılıyor?**

.NET'te sürekli interface'lerle karşılaşmanın nedeni, **Dependency Inversion Principle (DIP)** prensibine uyulmasıdır. Bu, SOLID prensiplerinden biridir.

### **Sorun: Interface Olmadan Ne Olurdu?**

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

### **Çözüm: Interface ile**

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

### **Dependency Inversion Principle (DIP) Nedir?**

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

### **Gerçek Hayat Benzetmesi:**

**Interface Olmadan:**
- Priz sadece belirli bir marka cihazı kabul eder
- Farklı bir cihaz kullanmak istersen prizi değiştirmen gerekir

**Interface ile:**
- Priz standart bir şekil (interface) kabul eder
- Bu standarda uyan her cihaz çalışır
- Priz değişmeden farklı cihazlar kullanabilirsin

---

## 📊 Tam Akış Diyagramı

### **HttpContext Oluşturma ve Pipeline Akışı:**

```
┌─────────────────────────────────────────────────────────┐
│ 1. HTTP Request Gelir                                    │
│    GET /api/products                                     │
└─────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────┐
│ 2. Kestrel Server                                       │
│    HttpContext context = new HttpContext();             │
│    context.Request.Path = "/api/products"              │
│    context.Response = new HttpResponse();              │
│    context.RequestServices = ServiceProvider;           │
└─────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────┐
│ 3. Middleware Pipeline                                  │
│    app.UseExceptionHandler()                            │
│    app.UseHttpsRedirection()                            │
│    app.UseAuthorization()                               │
│    app.MapControllers()                                  │
└─────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────┐
│ 4. ExceptionHandlerMiddleware.InvokeAsync(context)      │
│    try {                                                │
│      await _next(context);  ← HttpContext aktarılır    │
│    }                                                    │
│    catch (Exception ex) {                               │
│      var handler = context.RequestServices             │
│          .GetRequiredService<IExceptionHandler>();      │
│      handler.TryHandleAsync(context, ex);               │
│    }                                                    │
└─────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────┐
│ 5. GlobalExceptionHandler.TryHandleAsync(                │
│      httpContext,  ← HttpContext burada!                │
│      exception)                                         │
│    httpContext.Response.StatusCode = 500;              │
│    httpContext.Response.ContentType =                   │
│        "application/problem+json";                     │
│    await httpContext.Response.WriteAsync(json);        │
└─────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────┐
│ 6. HTTP Response                                        │
│    Status: 500 Internal Server Error                    │
│    Content-Type: application/problem+json               │
│    Body: { "type": "...", "title": "...", ... }        │
└─────────────────────────────────────────────────────────┘
```

---

## 💻 Kod Seviyesinde Detaylar

### **1. HttpContext Oluşturma (Framework İçi)**

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

### **2. Middleware Pipeline (Basitleştirilmiş)**

```csharp
// ASP.NET Core Framework içinde
public class MiddlewarePipeline
{
    private readonly List<Func<RequestDelegate, RequestDelegate>> _middlewares;
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Her middleware'e context aktarılır
        RequestDelegate pipeline = async (ctx) => { };
        
        // Middleware'ler ters sırada birleştirilir
        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            pipeline = _middlewares[i](pipeline);
        }
        
        // Pipeline'ı çalıştır
        await pipeline(context);
    }
}
```

### **3. ExceptionHandlerMiddleware (Gerçek Implementasyon)**

```csharp
// ASP.NET Core Framework içinde (Microsoft.AspNetCore.Diagnostics)
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;
    
    public ExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Bir sonraki middleware'e HttpContext'i aktar
            await _next(context);
        }
        catch (Exception exception)
        {
            // Exception yakalandı!
            _logger.LogError(exception, "Unhandled exception occurred");
            
            // DI container'dan IExceptionHandler al
            var exceptionHandler = context.RequestServices
                .GetRequiredService<IExceptionHandler>();
            
            // HttpContext'i handler'a ver
            var handled = await exceptionHandler.TryHandleAsync(
                context,           // ← HttpContext burada!
                exception,
                CancellationToken.None);
            
            if (!handled)
            {
                // Handler exception'ı handle edemedi, yeniden fırlat
                throw;
            }
        }
    }
}
```

### **4. GlobalExceptionHandler (Senin Kodun)**

```csharp
// Senin yazdığın kod
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }
    
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,  // ← HttpContext burada!
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. Exception'ı logla
        _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);
        
        // 2. ProblemDetails oluştur
        var problemDetails = CreateProblemDetails(exception, httpContext);
        
        // 3. HttpContext.Response'i kullan
        httpContext.Response.StatusCode = problemDetails.Status ?? 500;
        httpContext.Response.ContentType = "application/problem+json";
        
        // 4. Response body'ye yaz
        var json = JsonSerializer.Serialize(problemDetails);
        await httpContext.Response.WriteAsync(json, cancellationToken);
        
        // 5. Exception handle edildi
        return true;
    }
    
    private static ProblemDetails CreateProblemDetails(
        Exception exception, 
        HttpContext httpContext)  // ← HttpContext burada da kullanılıyor
    {
        return exception switch
        {
            NotFoundException notFound => new ProblemDetails
            {
                Status = 404,
                Title = "Not Found",
                Detail = notFound.Message,
                Instance = httpContext.Request.Path  // ← HttpContext kullanımı
            },
            // ... diğer exception'lar
        };
    }
}
```

---

## 🎯 Önemli Noktalar

### **1. HttpContext'i Kim Oluşturur?**
- ✅ **ASP.NET Core'un web server'ı (Kestrel/HttpListener)** her HTTP request için bir HttpContext oluşturur.

### **2. HttpContext'i Kim Yakalıyor?**
- ✅ **Middleware Pipeline** yakalar ve her middleware'e aktarır.
- ✅ **ExceptionHandlerMiddleware** exception yakaladığında HttpContext'i handler'a verir.

### **3. HttpContext Nereden Geliyor?**
- ✅ Framework tarafından oluşturulur ve pipeline'a enjekte edilir.
- ✅ Her middleware `InvokeAsync(HttpContext context)` metodunu alır.

### **4. Neden HttpContext Parametre Olarak Geliyor?**
- ✅ Dependency Injection ile değil, **pipeline mekanizması** ile aktarılır.
- ✅ Her request için **yeni bir HttpContext** oluşturulur (scoped).

### **5. Interface Pattern Neden Kullanılıyor?**
- ✅ **Dependency Inversion Principle (DIP)** prensibine uyum
- ✅ Esneklik, test edilebilirlik, genişletilebilirlik sağlar
- ✅ Somut sınıflara değil, abstraction'lara (interface) bağımlılık

---

## 📝 Özet

### **HttpContext:**
- Her HTTP request için oluşturulan context nesnesi
- Request, Response, User, Items, RequestServices içerir
- Kestrel Server tarafından oluşturulur
- Middleware Pipeline tarafından yakalanır ve aktarılır

### **Middleware Pipeline:**
- HTTP request'leri işleyen bileşenler zinciri
- Her middleware HttpContext'i alır ve bir sonrakine aktarır
- ExceptionHandlerMiddleware exception'ları yakalar

### **Interface Pattern:**
- Dependency Inversion Principle (DIP) prensibine uyum
- Esneklik, test edilebilirlik, genişletilebilirlik sağlar
- Somut sınıflara değil, abstraction'lara bağımlılık

### **Akış:**
```
HTTP Request 
  → Kestrel (HttpContext oluşturur)
  → Middleware Pipeline (HttpContext aktarır)
  → ExceptionHandlerMiddleware (exception yakalar)
  → GlobalExceptionHandler (HttpContext kullanır)
  → HTTP Response
```

---

**Son Güncelleme:** Aralık 2024
