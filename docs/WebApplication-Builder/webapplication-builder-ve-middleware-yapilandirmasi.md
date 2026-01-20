# WebApplication Builder ve Middleware Yapılandırması

## Genel Bakış

.NET 6+ minimal hosting model'inde, uygulama yapılandırması iki aşamada gerçekleşir:
1. **Builder Aşaması**: Servis kayıtları ve yapılandırma
2. **App Aşaması**: Middleware ve endpoint yapılandırması

## Temel Yapı

```csharp
// 1. Builder oluştur
var builder = WebApplication.CreateBuilder(args);

// 2. Servisleri kaydet
builder.Services.AddControllers();
builder.Services.AddDbContext<CatalogDbContext>(...);
// ... tüm servis kayıtları

// 3. Build et - WebApplication oluştur
var app = builder.Build();

// 4. Middleware ve endpoint'leri yapılandır
app.UseExceptionHandler();
app.MapControllers();
app.Run();
```

---

## 1. `WebApplication.CreateBuilder(args)`

### Ne Yapar?

`WebApplication.CreateBuilder(args)` metodu, web uygulaması için bir `WebApplicationBuilder` instance'ı oluşturur ve temel yapılandırmayı hazırlar.

### İçerik

`WebApplicationBuilder` şu özellikleri sağlar:

```csharp
builder.Configuration  // ← appsettings.json, environment variables vb. yapılandırma
builder.Services       // ← Dependency Injection container (IServiceCollection)
builder.Logging        // ← Logging yapılandırması
builder.Environment    // ← Development, Production, Staging ortam bilgisi
builder.WebHost        // ← Web host yapılandırması
```

### Otomatik Yüklenenler

1. **Configuration**: 
   - `appsettings.json`
   - `appsettings.{Environment}.json`
   - Environment variables
   - Command line arguments

2. **Logging**: 
   - Varsayılan logging provider'ları
   - Console, Debug, EventSource

3. **Services**: 
   - Temel ASP.NET Core servisleri
   - Routing, Authentication, Authorization vb.

### Kullanım Örneği

```csharp
var builder = WebApplication.CreateBuilder(args);

// builder.Services ile servisleri kaydet
builder.Services.AddControllers();
builder.Services.AddDbContext<CatalogDbContext>(...);
builder.Services.AddMediatR(...);
builder.Services.AddAutoMapper(...);
// ... tüm servis kayıtları burada
```

---

## 2. `builder.Build()`

### Ne Yapar?

`builder.Build()` metodu, `WebApplicationBuilder`'dan `WebApplication` instance'ı oluşturur. Bu aşamada:

1. Tüm servisler DI container'a eklenir
2. Service provider oluşturulur
3. Middleware pipeline hazırlanır
4. `WebApplication` instance döndürülür

### Öncesi vs Sonrası

#### Öncesi (Builder Aşaması)

```csharp
var builder = WebApplication.CreateBuilder(args);
// ✅ Servis kayıtları yapılır
builder.Services.AddControllers();
builder.Services.AddDbContext<CatalogDbContext>(...);
// ❌ Henüz servisler resolve edilemez
// ❌ Middleware pipeline yok
```

#### Sonrası (App Aşaması)

```csharp
var app = builder.Build();
// ✅ Servisler resolve edilebilir
// ✅ Middleware pipeline oluşturulur
app.UseExceptionHandler();
app.MapControllers();
app.Run();
```

### `app` Nesnesi ile Yapılabilecekler

```csharp
var app = builder.Build();

// Middleware ekleme
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();

// Endpoint mapping
app.MapControllers();
app.MapHealthChecks("/health");

// Servis resolve etme
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    // ...
}

// Uygulamayı çalıştır
app.Run();
```

---

## 3. Neden Middleware'ler Build Sonrasında Ekleniyor?

### ❓ Bu Bir Kural mı?

**Hayır, bu bir kural değil, mimari bir zorunluluktur!**

### 🔍 Nedenleri

#### 1. Service Provider Henüz Yok

**Builder Aşaması:**
```csharp
var builder = WebApplication.CreateBuilder(args);
// ❌ Henüz service provider yok
// ❌ Sadece servis kayıtları yapılıyor (IServiceCollection'a ekleniyor)
builder.Services.AddDbContext<CatalogDbContext>(...);
```

**Build Sonrası:**
```csharp
var app = builder.Build();
// ✅ Service provider oluşturuldu
// ✅ Artık servisler resolve edilebilir
```

**Neden Önemli?**
- Middleware'ler DI'dan servis alabilir
- Örnek: `UseExceptionHandler()` içinde `IExceptionHandler` resolve edilir
- Service provider olmadan bu çalışmaz

#### 2. Middleware Pipeline Henüz Oluşturulmamış

**Builder Aşaması:**
```csharp
var builder = WebApplication.CreateBuilder(args);
// ❌ HTTP pipeline henüz yok
// ❌ Request/Response context hazır değil
```

**Build Sonrası:**
```csharp
var app = builder.Build();
// ✅ Middleware pipeline oluşturuldu
// ✅ app.UseXxx() ile middleware eklenebilir
```

#### 3. Middleware'ler Çalışma Zamanında Çalışır

**Builder Aşaması (Compile/Startup):**
```csharp
var builder = WebApplication.CreateBuilder(args);
// Bu aşamada sadece YAPILANDIRMA yapılır
// Henüz hiçbir HTTP request gelmemiş
```

**Build Sonrası (Runtime):**
```csharp
var app = builder.Build();
app.UseExceptionHandler();  // ← Middleware pipeline'a eklenir
app.Run();  // ← Artık HTTP request'ler kabul edilir
```

---

## Görsel Karşılaştırma

### Builder Aşaması (Yapılandırma)

```
┌─────────────────────────────────────────┐
│ WebApplication.CreateBuilder(args)      │
│                                         │
│ • Configuration yüklenir                │
│ • IServiceCollection hazırlanır         │
│ • Servis kayıtları yapılır              │
│                                         │
│ ❌ Service Provider YOK                 │
│ ❌ Middleware Pipeline YOK              │
│ ❌ HTTP Context YOK                     │
└─────────────────────────────────────────┘
```

### Build Sonrası (Çalışma Zamanı)

```
┌─────────────────────────────────────────┐
│ builder.Build()                        │
│                                         │
│ • Service Provider oluşturulur         │
│ • Middleware Pipeline hazırlanır        │
│ • WebApplication instance döndürülür   │
│                                         │
│ ✅ Service Provider VAR                 │
│ ✅ Middleware Pipeline VAR              │
│ ✅ app.UseXxx() kullanılabilir         │
└─────────────────────────────────────────┘
```

---

## Teknik Detaylar

### Middleware'ler Servis Alır

```csharp
// ExceptionHandlerMiddleware içinde (framework kodu)
public class ExceptionHandlerMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // ❌ Builder aşamasında bu çalışmaz:
            // Service provider henüz yok!
            var handler = context.RequestServices  // ← Service Provider gerekli!
                .GetRequiredService<IExceptionHandler>();
            
            await handler.TryHandleAsync(context, ex);
        }
    }
}
```

### Pipeline Oluşturma

```csharp
// builder.Build() içinde (framework kodu)
public WebApplication Build()
{
    // 1. Service Provider oluştur
    var serviceProvider = _services.BuildServiceProvider();
    
    // 2. Middleware pipeline'ı hazırla
    var pipeline = new MiddlewarePipeline();
    
    // 3. WebApplication oluştur
    return new WebApplication
    {
        Services = serviceProvider,  // ← Service Provider hazır
        Pipeline = pipeline           // ← Pipeline hazır
    };
}
```

---

## Görsel Akış Diyagramı

```
┌─────────────────────────────────────────────────────────┐
│ 1. WebApplication.CreateBuilder(args)                   │
│    ↓                                                     │
│    • Configuration yüklenir (appsettings.json)          │
│    • Logging yapılandırılır                             │
│    • IServiceCollection hazırlanır                      │
│    • WebHost yapılandırılır                            │
└─────────────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ 2. builder.Services ile servis kayıtları                │
│    ↓                                                     │
│    builder.Services.AddControllers()                    │
│    builder.Services.AddDbContext(...)                  │
│    builder.Services.AddMediatR(...)                     │
│    builder.Services.AddAutoMapper(...)                  │
│    ... tüm servisler kaydedilir                         │
└─────────────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ 3. builder.Build()                                      │
│    ↓                                                     │
│    • Tüm servisler DI container'a eklenir                │
│    • Service provider oluşturulur                       │
│    • Middleware pipeline hazırlanır                    │
│    • WebApplication instance döndürülür                │
└─────────────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ 4. app ile middleware ve endpoint yapılandırması        │
│    ↓                                                     │
│    app.UseExceptionHandler()                            │
│    app.MapControllers()                                 │
│    app.MapHealthChecks("/health")                       │
│    ... middleware pipeline oluşturulur                 │
└─────────────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ 5. app.Run() - Uygulama çalıştırılır                   │
└─────────────────────────────────────────────────────────┘
```

---

## Önemli Farklar

| Özellik | `builder` (Build Öncesi) | `app` (Build Sonrası) |
|---------|-------------------------|----------------------|
| **Servis Kaydı** | ✅ `builder.Services.AddXxx()` | ❌ Servis kaydedilemez |
| **Servis Resolve** | ❌ Servis resolve edilemez | ✅ `app.Services.GetService<T>()` |
| **Middleware** | ❌ Middleware eklenemez | ✅ `app.UseXxx()` |
| **Endpoint Mapping** | ❌ Endpoint map edilemez | ✅ `app.MapXxx()` |
| **Configuration** | ✅ `builder.Configuration` | ✅ `app.Configuration` |

---

## Neden Bazı Şeyler `builder.Services` ile, Bazıları `app` ile Yapılıyor?

### Temel Fark

#### 1. `builder.Services.AddXxx()` → Servis Kaydı (DI Container)

Bu servisleri DI container'a kaydeder; henüz kullanılmaz.

```csharp
// Builder aşaması - Sadece kayıt
builder.Services.AddMediatR(...);      // MediatR servisini kaydet
builder.Services.AddAutoMapper(...);   // AutoMapper servisini kaydet
builder.Services.AddDbContext(...);    // DbContext servisini kaydet
builder.Services.AddControllers();     // Controller servislerini kaydet
```

**Ne yapar:**
- Servisleri DI container'a ekler
- "Bu servisler var" der
- Henüz kullanılmaz, sadece kaydedilir

#### 2. `app.MapXxx()` / `app.UseXxx()` → Pipeline Yapılandırması

Bunlar HTTP pipeline'a eklenir ve request/response akışında çalışır.

```csharp
// App aşaması - Pipeline'a ekleme
app.MapControllers();        // Controller'ları endpoint'lere map et
app.MapHealthChecks(...);    // Health check endpoint'ini oluştur
app.UseSwagger();           // Swagger middleware'ini ekle
```

**Ne yapar:**
- HTTP pipeline'a eklenir
- Request/Response akışında aktif olur
- Çalışma zamanında kullanılır

### Görsel Karşılaştırma

```
┌─────────────────────────────────────────────────────────┐
│ BUILDER AŞAMASI (Servis Kayıtları)                      │
│                                                          │
│ builder.Services.AddMediatR()                           │
│   ↓                                                      │
│   • MediatR servisini DI'a kaydet                       │
│   • Handler'ları bul ve kaydet                          │
│   ❌ Henüz kullanılmaz                                   │
│                                                          │
│ builder.Services.AddAutoMapper()                        │
│   ↓                                                      │
│   • AutoMapper servisini DI'a kaydet                    │
│   • Profile'ları bul ve kaydet                          │
│   ❌ Henüz kullanılmaz                                   │
│                                                          │
│ builder.Services.AddDbContext()                         │
│   ↓                                                      │
│   • DbContext servisini DI'a kaydet                     │
│   • Connection string'i yapılandır                      │
│   ❌ Henüz kullanılmaz                                   │
└─────────────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ APP AŞAMASI (Pipeline Yapılandırması)                  │
│                                                          │
│ app.MapControllers()                                    │
│   ↓                                                      │
│   • Controller'ları HTTP endpoint'lerine map et         │
│   • Routing tablosunu oluştur                           │
│   ✅ HTTP request'lerde kullanılır                       │
│                                                          │
│ app.MapHealthChecks()                                   │
│   ↓                                                      │
│   • Health check endpoint'ini oluştur                   │
│   ✅ HTTP request'lerde kullanılır                       │
└─────────────────────────────────────────────────────────┘
```

### Örnek: Controller'lar

#### Builder Aşaması
```csharp
builder.Services.AddControllers();
```
- Controller servislerini DI'a kaydeder
- Model binding, validation servislerini ekler
- Henüz endpoint yok

#### App Aşaması
```csharp
app.MapControllers();
```
- Controller'ları endpoint'lere map eder
- Routing tablosunu oluşturur
- HTTP request'lerde kullanılır

### Örnek: MediatR

#### Builder Aşaması
```csharp
builder.Services.AddMediatR(...);
```
- MediatR servisini DI'a kaydeder
- Handler'ları bulur ve kaydeder
- Henüz kullanılmaz

#### Kullanım (Handler'larda)
```csharp
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;  // ← DI'dan inject edilir
    
    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;  // ← builder.Services.AddMediatR() ile kaydedilmiş
    }
    
    public async Task<ActionResult> GetProducts()
    {
        var result = await _mediator.Send(new GetProductsQuery());  // ← Kullanım
        return Ok(result);
    }
}
```

### Özet Tablo

| İşlem | Aşama | Ne Yapar | Ne Zaman Kullanılır |
|-------|-------|----------|---------------------|
| `AddMediatR()` | Builder | MediatR servisini kaydet | Handler'larda inject edildiğinde |
| `AddAutoMapper()` | Builder | AutoMapper servisini kaydet | Handler'larda inject edildiğinde |
| `AddDbContext()` | Builder | DbContext servisini kaydet | Handler'larda inject edildiğinde |
| `AddControllers()` | Builder | Controller servislerini kaydet | Controller'lar oluşturulurken |
| `MapControllers()` | App | Controller'ları endpoint'lere map et | HTTP request geldiğinde |
| `MapHealthChecks()` | App | Health check endpoint'ini oluştur | HTTP request geldiğinde |
| `UseSwagger()` | App | Swagger middleware'ini ekle | HTTP request geldiğinde |

### Neden Bu Ayrım?

#### Servis Kayıtları (Builder)
- DI container'a eklenir
- Constructor injection ile kullanılır
- Çalışma zamanında resolve edilir

#### Pipeline Yapılandırması (App)
- HTTP pipeline'a eklenir
- Request/Response akışında çalışır
- Doğrudan HTTP request'lerde kullanılır

### Sonuç

- **`builder.Services.AddXxx()`** → Servis kaydı (DI container)
- **`app.MapXxx()` / `app.UseXxx()`** → Pipeline yapılandırması (HTTP pipeline)

MediatR, AutoMapper, DbContext gibi servisler DI'dan inject edilerek kullanılır; `MapControllers()` gibi yapılandırmalar ise HTTP pipeline'a eklenir ve request'lerde doğrudan çalışır.

Bu yüzden:
- **Servisler** → `builder.Services.AddXxx()` (kayıt)
- **Endpoint/Middleware** → `app.MapXxx()` / `app.UseXxx()` (pipeline)

---

## Örnek: Program.cs'deki Kullanım

```csharp
// 1. Builder oluştur
var builder = WebApplication.CreateBuilder(args);

// 2. Servisleri kaydet (builder aşaması)
builder.Services.AddControllers();
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));
builder.Services.AddMediatR(cfg => { ... });
builder.Services.AddAutoMapper(typeof(Program).Assembly);
// ... tüm servis kayıtları

// 3. Build et - WebApplication oluştur
var app = builder.Build();

// 4. Middleware ve endpoint'leri yapılandır (app aşaması)
app.UseExceptionHandler();
app.MapControllers();
app.MapHealthChecks("/health");

// 5. Servis resolve etme (app aşaması)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await context.Database.MigrateAsync();
    await SeedData.InitializeAsync(context);
}

// 6. Uygulamayı çalıştır
app.Run();
```

---

## Alternatif Yaklaşım (Mümkün ama Önerilmez)

Bazı middleware'ler builder aşamasında da eklenebilir, ancak genellikle önerilmez:

```csharp
// ❌ Mümkün ama önerilmez
builder.Services.AddSingleton<IMiddleware, MyMiddleware>();

// ✅ Önerilen yöntem
var app = builder.Build();
app.UseMiddleware<MyMiddleware>();
```

---

## Özet Tablo

| Konsept | Açıklama |
|---------|----------|
| **WebApplication.CreateBuilder** | Uygulama yapılandırmasını ve servis kayıtlarını hazırlar |
| **builder.Services** | Dependency Injection container (IServiceCollection) |
| **builder.Build()** | Servisleri DI container'a ekler, service provider oluşturur ve WebApplication döndürür |
| **app.UseXxx()** | Middleware pipeline'a middleware ekler |
| **app.MapXxx()** | Endpoint mapping yapar |
| **app.Services** | Service provider'a erişim sağlar |

---

## Sonuç

**Builder Aşaması** = Yapılandırma (servis kayıtları)
**Build** = Service provider ve pipeline oluşturma
**App Aşaması** = Middleware ve endpoint yapılandırması

Bu bir kural değil, **mimari bir zorunluluktur**. Middleware'ler çalışma zamanında çalışır ve service provider'a ihtiyaç duyar; bu yüzden `builder.Build()` sonrasında eklenir.

---

## Avantajlar

1. **Ayrım**: Yapılandırma ve çalışma zamanı mantığı ayrılır
2. **Test Edilebilirlik**: Builder aşaması test edilebilir
3. **Esneklik**: Middleware'ler runtime'da dinamik olarak eklenebilir
4. **Performans**: Service provider sadece gerektiğinde oluşturulur

## Dikkat Edilmesi Gerekenler

1. **Sıra Önemli**: Middleware'lerin eklenme sırası önemlidir
2. **Service Provider**: Build sonrasında servisler resolve edilebilir
3. **Pipeline**: Middleware pipeline sadece build sonrasında oluşturulur
4. **Lifetime**: Builder ve app farklı lifetime'lara sahiptir
