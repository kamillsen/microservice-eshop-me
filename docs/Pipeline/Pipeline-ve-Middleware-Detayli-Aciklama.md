# Pipeline ve Middleware - Detaylı Açıklama

## 📋 İçindekiler
1. [Pipeline Nedir?](#pipeline-nedir)
2. [İki Farklı Pipeline](#iki-farklı-pipeline)
3. [ASP.NET Core Middleware Pipeline](#aspnet-core-middleware-pipeline)
4. [MediatR Pipeline](#mediatr-pipeline)
5. [İki Pipeline'ın Farkı](#iki-pipelinin-farkı)
6. [Arka Planda Ne Oluyor?](#arka-planda-ne-oluyor)

---

## Pipeline Nedir?

**Pipeline = Bir zincir**

Tek bir pipeline var. Program.cs'de eklediğin middleware'ler bu zincire eklenir.

**Analoji:** Bir fabrika hattı - her istasyon (middleware) ürünü (request) işler ve bir sonrakine aktarır.

---

## İki Farklı Pipeline

Projende **2 farklı pipeline** var:

### A) ASP.NET Core Middleware Pipeline (HTTP Pipeline)
- HTTP request'lerin işlendiği zincir
- Program.cs'de `app.Use...()` ile eklenir

### B) MediatR Pipeline (Command/Query Pipeline)
- MediatR'ın kendi içindeki zincir
- Program.cs'de `cfg.AddBehavior()` ile eklenir

---

## ASP.NET Core Middleware Pipeline

### Tek Bir Pipeline Var!

Program.cs'de şunlar yapılır:
```csharp
var app = builder.Build();

app.UseExceptionHandler();  // ← Pipeline'a EKLENİYOR
app.UseSwagger();            // ← Pipeline'a EKLENİYOR
app.MapControllers();        // ← Pipeline'a EKLENİYOR
```

**Her `app.Use...()` veya `app.Map...()` çağrısı AYNI pipeline'a eklenir!**

### Arka Planda Ne Oluyor?

```csharp
// ASP.NET Core Framework içinde (sen görmüyorsun)
public class WebApplication
{
    private readonly List<Func<RequestDelegate, RequestDelegate>> _middlewares = new();
    
    // app.UseExceptionHandler() çağrıldığında:
    public IApplicationBuilder UseExceptionHandler()
    {
        // Middleware'i listeye ekle
        _middlewares.Add(next => 
        {
            return async (HttpContext context) =>
            {
                try
                {
                    await next(context);  // ← Bir sonraki middleware'e git
                }
                catch (Exception ex)
                {
                    // Exception işle
                }
            };
        });
        return this;
    }
    
    // app.MapControllers() çağrıldığında:
    public IApplicationBuilder MapControllers()
    {
        _middlewares.Add(next => 
        {
            return async (HttpContext context) =>
            {
                // Controller routing yap
                // Eğer route bulunursa Controller'ı çalıştır
                // Bulunamazsa next(context) çağır
            };
        });
        return this;
    }
    
    // app.Run() çağrıldığında:
    public void Run()
    {
        // Tüm middleware'leri birleştir (ters sırada!)
        RequestDelegate pipeline = async (context) => { };
        
        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            pipeline = _middlewares[i](pipeline);
        }
        
        // Pipeline'ı çalıştır
        await pipeline(httpContext);
    }
}
```

### Middleware Nedir?

**Middleware = Pipeline'daki bir halka**

Her middleware:
1. HttpContext'i alır
2. İşlem yapar (veya yapmaz)
3. Bir sonraki middleware'e aktarır (veya response döner)

### Basit Middleware Örneği

```csharp
// Custom middleware
public class MyMiddleware
{
    private readonly RequestDelegate _next;
    
    public MyMiddleware(RequestDelegate next)
    {
        _next = next;  // ← Bir sonraki middleware
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Request geldiğinde (önce)
        Console.WriteLine("Request geldi: " + context.Request.Path);
        
        // 2. Bir sonraki middleware'e git
        await _next(context);
        
        // 3. Response dönerken (sonra)
        Console.WriteLine("Response döndü: " + context.Response.StatusCode);
    }
}
```

### Pipeline Görseli (Gerçek Akış)

```
HTTP Request: GET /api/baskets
    ↓
┌─────────────────────────────────────────┐
│ ExceptionHandlerMiddleware               │
│   try {                                  │
│     ↓                                    │
│   ┌─────────────────────────────────┐   │
│   │ SwaggerMiddleware               │   │
│   │   if (path == "/swagger") {     │   │
│   │     return Swagger UI;           │   │
│   │   }                              │   │
│   │   ↓                              │   │
│   │ ┌───────────────────────────┐   │   │
│   │ │ MapControllers()           │   │   │
│   │ │   if (path == "/api/...") {│   │   │
│   │ │     Controller çalıştır;  │   │   │
│   │ │   }                        │   │   │
│   │ │   ↓                        │   │   │
│   │ │ Controller                 │   │   │
│   │ │   _mediator.Send(...)      │   │   │
│   │ │   ↓                        │   │   │
│   │ └───────────────────────────┘   │   │
│   └─────────────────────────────────┘   │
│   } catch (Exception ex) {                │
│     GlobalExceptionHandler işle;         │
│   }                                      │
└─────────────────────────────────────────┘
    ↓
HTTP Response
```

### Program.cs'deki Sıra ÖNEMLİ!

```csharp
app.UseExceptionHandler();  // 1. En üstte (tüm exception'ları yakalamalı)
app.UseSwagger();            // 2. Swagger
app.MapControllers();        // 3. Controller routing
```

**Sıra değişirse davranış değişir!**

Örnek: Eğer `UseExceptionHandler()` en altta olsaydı, Swagger ve Controller'daki exception'ları yakalayamazdı.

---

## MediatR Pipeline

### MediatR'ın Kendi Pipeline'ı

MediatR'ın kendi içinde bir pipeline var. Bu, HTTP Pipeline'dan **FARKLI**.

Program.cs'de:
```csharp
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));  // ← MediatR Pipeline'a ekleniyor
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));  // ← MediatR Pipeline'a ekleniyor
});
```

### MediatR Pipeline Akışı

```
Controller: _mediator.Send(command)
    ↓
MediatR Pipeline:
    1. LoggingBehavior
       - Request logu
       - ↓
    2. ValidationBehavior
       - FluentValidation çalıştır
       - ↓
    3. Handler
       - İş mantığı
       - ↓
    Response döner
```

### Behavior Nedir?

**Behavior = MediatR Pipeline'daki bir halka**

**Örnek: LoggingBehavior**
```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly RequestHandlerDelegate<TResponse> _next;  // ← Bir sonraki behavior/handler
    
    public async Task<TResponse> Handle(TRequest request, ...)
    {
        // 1. Önce log
        _logger.LogInformation("Request: {@Request}", request);
        
        // 2. Bir sonraki behavior'a git
        var response = await next();
        
        // 3. Sonra log
        _logger.LogInformation("Response: {@Response}", response);
        
        return response;
    }
}
```

**Örnek: ValidationBehavior**
```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly RequestHandlerDelegate<TResponse> _next;
    
    public async Task<TResponse> Handle(TRequest request, ...)
    {
        // 1. Validasyon yap
        var failures = await ValidateAsync(request);
        
        if (failures.Any())
        {
            throw new ValidationException(failures);
        }
        
        // 2. Bir sonraki behavior'a git (veya handler'a)
        return await next();
    }
}
```

### MediatR Pipeline'da Sıra ÖNEMLİ!

```csharp
cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));      // 1. Önce log
cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));   // 2. Sonra validasyon
```

**Sıra:**
1. LoggingBehavior çalışır
2. ValidationBehavior çalışır
3. Handler çalışır

Eğer sıra değişirse (örneğin Validation önce, Logging sonra), validasyon hatası olursa log yazılmaz.

---

## İki Pipeline'ın Farkı

| Özellik | ASP.NET Core Middleware Pipeline | MediatR Pipeline |
|---------|-----------------------------------|------------------|
| **Ne zaman çalışır?** | Her HTTP request | `_mediator.Send()` çağrıldığında |
| **Nerede tanımlanır?** | Program.cs'de `app.Use...()` | Program.cs'de `cfg.AddBehavior()` |
| **Ne işler?** | HttpContext | Command/Query |
| **Kaç tane var?** | Tek bir pipeline | Tek bir pipeline (MediatR için) |
| **Hangi katmanda?** | HTTP katmanı | İş mantığı katmanı |

### Görsel Karşılaştırma

```
HTTP Request
    ↓
┌─────────────────────────────────────┐
│ ASP.NET Core Middleware Pipeline    │ ← HTTP katmanı
│   UseExceptionHandler()             │
│   UseSwagger()                      │
│   MapControllers()                  │
└─────────────────────────────────────┘
    ↓
Controller: _mediator.Send(command)
    ↓
┌─────────────────────────────────────┐
│ MediatR Pipeline                    │ ← İş mantığı katmanı
│   LoggingBehavior                   │
│   ValidationBehavior                │
│   Handler                           │
└─────────────────────────────────────┘
    ↓
Response
```

---

## Arka Planda Ne Oluyor?

### 1. ASP.NET Core Pipeline Oluşturma

```csharp
// Framework içinde (sen görmüyorsun)
public class WebApplicationBuilder
{
    public WebApplication Build()
    {
        var app = new WebApplication();
        
        // Middleware listesi oluşturulur
        app._middlewares = new List<Func<RequestDelegate, RequestDelegate>>();
        
        return app;
    }
}

// app.UseExceptionHandler() çağrıldığında:
public IApplicationBuilder UseExceptionHandler()
{
    _middlewares.Add(next => 
    {
        return async (HttpContext context) =>
        {
            try
            {
                await next(context);  // ← Bir sonraki middleware
            }
            catch (Exception ex)
            {
                // Exception işle
                var handler = context.RequestServices
                    .GetRequiredService<IExceptionHandler>();
                await handler.TryHandleAsync(context, ex);
            }
        };
    });
    return this;
}
```

### 2. Pipeline Çalıştırma

```csharp
// app.Run() çağrıldığında:
public void Run()
{
    // Tüm middleware'leri birleştir (ters sırada!)
    RequestDelegate pipeline = async (context) => 
    {
        // Son middleware (hiçbir şey yapmaz, 404 döner)
    };
    
    // Ters sırada birleştir (en son eklenen en içte)
    for (int i = _middlewares.Count - 1; i >= 0; i--)
    {
        var middleware = _middlewares[i];
        var previous = pipeline;
        pipeline = middleware(previous);  // ← Zincir oluşturuluyor
    }
    
    // HTTP request geldiğinde:
    // await pipeline(httpContext);  ← Framework bunu çağırır
}
```

### 3. MediatR Pipeline Oluşturma

```csharp
// MediatR Framework içinde (sen görmüyorsun)
public class MediatRServiceConfiguration
{
    private readonly List<Type> _behaviors = new();
    
    // cfg.AddBehavior() çağrıldığında:
    public void AddBehavior(Type behaviorType)
    {
        _behaviors.Add(behaviorType);  // ← Listeye ekle
    }
}

// _mediator.Send() çağrıldığında:
public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
{
    // Behavior'ları sırayla çalıştır
    RequestHandlerDelegate<TResponse> pipeline = async () =>
    {
        // Son: Handler'ı çalıştır
        var handler = GetHandler(request);
        return await handler.Handle(request);
    };
    
    // Ters sırada birleştir
    for (int i = _behaviors.Count - 1; i >= 0; i--)
    {
        var behavior = CreateBehavior(_behaviors[i]);
        var previous = pipeline;
        pipeline = async () => await behavior.Handle(request, previous);
    }
    
    // Pipeline'ı çalıştır
    return await pipeline();
}
```

---

## Önemli Noktalar

### 1. Tek Bir Pipeline Var

- ASP.NET Core için: Tek bir HTTP Pipeline
- MediatR için: Tek bir MediatR Pipeline

**"Pipeline'a ekleniyor"** = Aynı zincire yeni halka ekleniyor

### 2. Sıra Önemli

```csharp
// ❌ YANLIŞ SIRA:
app.MapControllers();        // 1. Controller routing
app.UseExceptionHandler();   // 2. Exception handler (çok geç!)

// ✅ DOĞRU SIRA:
app.UseExceptionHandler();   // 1. Exception handler (en üstte)
app.MapControllers();        // 2. Controller routing
```

### 3. Middleware/Behavior Bir Sonrakine Aktarır

```csharp
// Her middleware/behavior:
await _next(context);  // ← Bir sonraki middleware'e git
// veya
await next();          // ← Bir sonraki behavior'a git
```

### 4. Pipeline Ters Sırada Birleştirilir

```csharp
// Eklenme sırası:
1. UseExceptionHandler()
2. UseSwagger()
3. MapControllers()

// Çalışma sırası (ters sırada birleştirilir):
1. UseExceptionHandler() (en dışta)
2. UseSwagger()
3. MapControllers() (en içte)
```

**Neden ters sırada?**
- En son eklenen en içte olur
- Request önce dıştaki middleware'den geçer
- Response dönerken içten dışa çıkar

---

## Özet

1. **Pipeline = Bir zincir** → Tek bir pipeline var
2. **Middleware = Pipeline'daki bir halka** → Her middleware bir sonrakine aktarır
3. **`app.Use...()`** → ASP.NET Core Pipeline'a ekler
4. **`cfg.AddBehavior()`** → MediatR Pipeline'a ekler
5. **Sıra önemli** → Önce eklenen önce çalışır
6. **Her middleware/behavior bir sonrakine `next()` ile aktarır**

**Bu yapı sayesinde:**
- Her request aynı pipeline'dan geçer
- Sırayla işlenir
- Merkezi yönetim sağlanır
- Kod tekrarı azalır

---

**Son Güncelleme:** Aralık 2024
