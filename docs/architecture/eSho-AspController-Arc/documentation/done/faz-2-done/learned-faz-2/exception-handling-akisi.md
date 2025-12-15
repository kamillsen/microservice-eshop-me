# Exception Handling Akışı - Öğrenme Notları

> Exception fırlatıldığında GlobalExceptionHandler'a nasıl ulaşıyor? Detaylı akış açıklaması

---

## Soru: Exception fırlatıldığında direkt GlobalExceptionHandler'a mı gidiyor?

**Cevap:** Hayır, direkt GlobalExceptionHandler'a gitmiyor. Önce middleware pipeline'dan geçiyor.

---

## Detaylı Akış - Adım Adım

### Senaryo: Product bulunamadı

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetProduct(int id)
{
    var product = await _productService.GetByIdAsync(id);
    if (product == null)
    {
        throw new NotFoundException("Product", id);  // ← Exception fırlatıldı
    }
    return Ok(product);
}
```

---

## Akış Diyagramı

```
┌─────────────────────────────────────────────────────────────┐
│ ADIM 1: Exception fırlatıldı                                │
│                                                              │
│ throw new NotFoundException("Product", id);                  │
│                                                              │
│ .NET Runtime exception'ı yakalar                             │
│ → Exception objesi oluşturulur                             │
│ → Stack trace oluşturulur                                   │
│ → Exception yukarı doğru fırlatılır (unwind)               │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│ ADIM 2: Controller metodundan çıkılıyor                     │
│                                                              │
│ GetProduct() metodu exception fırlattı                      │
│ → Metod sonlanır (return Ok(product) çalışmaz)             │
│ → Exception yukarı doğru fırlatılır                        │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│ ADIM 3: Middleware Pipeline'a gidiyor                       │
│                                                              │
│ Exception, middleware pipeline'dan geçer                    │
│ → Her middleware'in catch bloğu yoksa exception yukarı    │
│   doğru fırlatılır                                          │
│ → UseHttpsRedirection() → Exception geçer                   │
│ → UseAuthorization() → Exception geçer                    │
│ → MapControllers() → Exception geçer                      │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│ ADIM 4: UseExceptionHandler() Middleware'i yakalıyor       │
│                                                              │
│ UseExceptionHandler() middleware'i exception'ı yakalar       │
│ → try-catch bloğu var                                       │
│ → catch (Exception ex) { ... }                             │
│                                                              │
│ ASP.NET Core şunu yapar:                                    │
│ 1. DI container'dan IExceptionHandler implementasyonlarını │
│    bulur (GlobalExceptionHandler)                           │
│ 2. TryHandleAsync() metodunu çağırır                        │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│ ADIM 5: GlobalExceptionHandler çalışıyor                     │
│                                                              │
│ GlobalExceptionHandler.TryHandleAsync() çağrılır            │
│ → Exception loglanır                                        │
│ → ProblemDetails oluşturulur                                │
│ → Response döndürülür                                       │
│ → return true;  ← Exception handle edildi                  │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│ ADIM 6: Response döndürülüyor                               │
│                                                              │
│ HTTP 404 Not Found                                           │
│ Content-Type: application/problem+json                       │
│ {                                                             │
│   "type": "https://tools.ietf.org/html/rfc7807",            │
│   "title": "Not Found",                                      │
│   "status": 404,                                             │
│   "detail": "Entity \"Product\" (999) was not found."       │
│ }                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Yazılımsal Bağlantı - Nasıl Çalışıyor?

### 1. Dependency Injection (DI) Container

```csharp
// Program.cs
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
```

**Ne yapıyor?**
- `GlobalExceptionHandler`'ı DI container'a kaydeder
- `IExceptionHandler` interface'ini implement ettiği için otomatik keşfedilir

**Kod içinde (ASP.NET Core framework - basitleştirilmiş):**
```csharp
// ASP.NET Core framework içinde
public class ExceptionHandlerMiddleware
{
    private readonly IEnumerable<IExceptionHandler> _handlers;
    
    public ExceptionHandlerMiddleware(
        IEnumerable<IExceptionHandler> handlers)  // ← DI container'dan gelir
    {
        _handlers = handlers;  // ← GlobalExceptionHandler burada
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);  // ← Controller'a gider
        }
        catch (Exception ex)  // ← Exception yakalandı!
        {
            // DI container'dan gelen handler'ları kullan
            foreach (var handler in _handlers)
            {
                if (await handler.TryHandleAsync(context, ex, cancellationToken))
                {
                    return;  // ← Exception handle edildi, çık
                }
            }
        }
    }
}
```

---

### 2. Middleware Pipeline

```csharp
// Program.cs
app.UseExceptionHandler();  // ← Bu middleware exception'ları yakalar
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
```

**Ne yapıyor?**
- `UseExceptionHandler()` → Exception handling middleware'ini pipeline'a ekler
- Bu middleware, pipeline'daki tüm exception'ları yakalar

**Pipeline görseli:**
```
Request
  ↓
┌─────────────────────────────────────┐
│ UseExceptionHandler()               │ ← Exception yakalama noktası
│   try {                             │
│     ↓                               │
│   ┌─────────────────────────────┐ │
│   │ UseHttpsRedirection()       │ │
│   │   ↓                         │ │
│   │ ┌─────────────────────────┐ │ │
│   │ │ UseAuthorization()      │ │ │
│   │ │   ↓                     │ │ │
│   │ │ ┌─────────────────────┐ │ │ │
│   │ │ │ MapControllers()    │ │ │ │
│   │ │ │   ↓                 │ │ │ │
│   │ │ │ Controller çalışıyor│ │ │ │
│   │ │ │   throw Exception   │ ← Exception fırlatıldı!
│   │ │ └─────────────────────┘ │ │ │
│   │ └─────────────────────────┘ │ │
│   └─────────────────────────────┘ │
│   } catch (Exception ex) {        │ ← Exception yakalandı!
│     // GlobalExceptionHandler      │
│     // TryHandleAsync() çağrılır   │
│   }                                │
└─────────────────────────────────────┘
  ↓
Response
```

---

### 3. IExceptionHandler Interface'i

```csharp
// GlobalExceptionHandler.cs
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(...)
    {
        // Exception'ı handle et
        return true;  // ← Exception handle edildi
    }
}
```

**Ne yapıyor?**
- `IExceptionHandler` interface'ini implement eder
- ASP.NET Core, DI container'dan bu interface'i implement eden tüm sınıfları bulur
- Exception fırlatıldığında `TryHandleAsync()` metodunu çağırır

**ASP.NET Core framework içinde (basitleştirilmiş):**
```csharp
// ASP.NET Core framework içinde
public class ExceptionHandlerMiddleware
{
    private readonly IServiceProvider _serviceProvider;
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // DI container'dan IExceptionHandler implementasyonlarını al
            var handlers = _serviceProvider.GetServices<IExceptionHandler>();
            
            // Her handler'ı dene
            foreach (var handler in handlers)
            {
                // TryHandleAsync() çağrılır
                if (await handler.TryHandleAsync(context, ex, cancellationToken))
                {
                    return;  // ← Exception handle edildi, çık
                }
            }
        }
    }
}
```

---

## Neden Try-Catch Gerekmez?

### Senaryo 1: Try-Catch ile (Gereksiz)

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetProduct(int id)
{
    try
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
        {
            throw new NotFoundException("Product", id);
        }
        return Ok(product);
    }
    catch (NotFoundException ex)  // ← Gereksiz!
    {
        // GlobalExceptionHandler zaten yakalayacak
        throw;  // ← Tekrar fırlatıyor, gereksiz
    }
}
```

**Sorun:**
- Try-catch gereksiz
- Exception'ı tekrar fırlatmak gereksiz
- GlobalExceptionHandler zaten yakalayacak

---

### Senaryo 2: Try-Catch Olmadan (Doğru)

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetProduct(int id)
{
    var product = await _productService.GetByIdAsync(id);
    if (product == null)
    {
        throw new NotFoundException("Product", id);  // ← Direkt fırlat
    }
    return Ok(product);
}
```

**Neden çalışıyor?**
- Exception fırlatıldığında middleware pipeline'ına gider
- `UseExceptionHandler()` middleware'i yakalar
- GlobalExceptionHandler'ın `TryHandleAsync()` metodu çağrılır
- Exception handle edilir ve response döndürülür

---

## Yazılımsal Bağlantı Özeti

### Bağlantı Zinciri:

```
1. Program.cs
   ↓
   builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
   → DI container'a kaydedilir
   ↓
2. Program.cs
   ↓
   app.UseExceptionHandler();
   → Exception handling middleware pipeline'a eklenir
   ↓
3. Controller
   ↓
   throw new NotFoundException(...);
   → Exception fırlatıldı
   ↓
4. Middleware Pipeline
   ↓
   UseExceptionHandler() middleware exception'ı yakalar
   → DI container'dan IExceptionHandler implementasyonlarını bulur
   → GlobalExceptionHandler.TryHandleAsync() çağrılır
   ↓
5. GlobalExceptionHandler
   ↓
   TryHandleAsync() çalışır
   → Exception loglanır
   → ProblemDetails oluşturulur
   → Response döndürülür
```

---

## Önemli Noktalar

### 1. DI Container Bağlantısı
- `AddExceptionHandler<GlobalExceptionHandler>()` → Handler'ı DI container'a kaydeder
- `UseExceptionHandler()` → Middleware, DI container'dan handler'ları alır

### 2. Middleware Pipeline
- `UseExceptionHandler()` → Pipeline'daki tüm exception'ları yakalar
- Try-catch'e gerek yok, middleware otomatik yakalar

### 3. Interface Pattern
- `IExceptionHandler` interface'i → ASP.NET Core'un standart pattern'i
- Framework, interface'i implement eden sınıfları otomatik bulur

### 4. Middleware Sırası
```csharp
// ✅ Doğru sıralama
app.UseExceptionHandler();  // ← En üstte (diğer middleware'lerden önce)
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
```

**Neden?**
- Exception handler, tüm middleware'lerde oluşan exception'ları yakalamalı
- En üstte olmalı ki diğer middleware'lerdeki hataları da yakalasın

---

## Özet Tablo

| Adım | Ne Oluyor? | Nerede? |
|------|------------|---------|
| 1 | Exception fırlatıldı | Controller |
| 2 | .NET Runtime exception'ı yakalar | Runtime |
| 3 | Exception yukarı doğru fırlatılır | Stack unwind |
| 4 | Middleware pipeline'dan geçer | Pipeline |
| 5 | UseExceptionHandler() yakalar | Middleware |
| 6 | GlobalExceptionHandler bulunur | DI Container |
| 7 | TryHandleAsync() çağrılır | GlobalExceptionHandler |
| 8 | Response döndürülür | GlobalExceptionHandler |

---

## Sonuç

**Exception fırlatıldığında:**
1. ✅ .NET Runtime exception'ı yakalar
2. ✅ Exception yukarı doğru fırlatılır (stack unwind)
3. ✅ Middleware pipeline'dan geçer
4. ✅ `UseExceptionHandler()` middleware'i yakalar
5. ✅ DI container'dan `GlobalExceptionHandler` bulunur
6. ✅ `TryHandleAsync()` çağrılır
7. ✅ Exception handle edilir ve response döndürülür

**Önemli:** Exception direkt GlobalExceptionHandler'a gitmiyor, önce middleware pipeline'dan geçiyor ve `UseExceptionHandler()` middleware'i yakalıyor, sonra GlobalExceptionHandler çağrılıyor.

---

**Son Güncelleme:** Aralık 2024

