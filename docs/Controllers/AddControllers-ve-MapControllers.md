# AddControllers() ve MapControllers() - Detaylı Açıklama

## Genel Bakış

ASP.NET Core'da controller'ların çalışması için iki aşama gereklidir:
1. **`AddControllers()`**: Controller servislerini DI container'a kaydeder (Builder aşaması)
2. **`MapControllers()`**: Controller'ları endpoint'lere map eder (App aşaması)

---

## 1. `builder.Services.AddControllers();`

### Ne Yapar?

Controller'ların çalışması için gerekli servisleri Dependency Injection container'a kaydeder.

### Builder Aşamasında (Yapılandırma)

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();  // ← Servisleri kaydet
```

### Eklenen Servisler

`AddControllers()` şu servisleri ekler:

```csharp
// AddControllers() şunları ekler:
- IControllerFactory          // Controller instance'ları oluşturur
- IControllerActivator        // Controller'ları aktifleştirir
- Model binding servisleri    // HTTP request'ten veri almak için
- Action result formatters    // JSON, XML vb. formatlar
- Validation servisleri       // Model validation için
- API behavior'ları           // API controller özellikleri
- Routing servisleri          // Route attribute'larını tanımak için
- Action method discovery     // Action method'ları bulmak için
```

### Ne Zaman Çalışır?

- **Uygulama başlangıcında** (startup)
- Controller'lar henüz kullanılmıyor, sadece servisler kaydediliyor
- Henüz endpoint'ler oluşturulmadı

### Örnek: ProductsController

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)  // ← DI'dan inject edilecek
    {
        _mediator = mediator;
    }
}
```

`AddControllers()` çağrıldığında:
- `ProductsController` servis olarak kaydedilir (Scoped lifetime)
- Constructor parametreleri (`IMediator`) DI container'dan resolve edilebilir hale gelir
- Model binding, validation vb. servisler hazırlanır

---

## 2. `app.MapControllers();`

### Ne Yapar?

Controller'ları endpoint'lere map eder ve routing yapılandırmasını yapar. HTTP request'lerin controller'lara yönlendirilmesini sağlar.

### App Aşamasında (Runtime)

```csharp
var app = builder.Build();
app.MapControllers();  // ← Controller'ları endpoint'lere map et
```

### Nasıl Çalışır?

`MapControllers()` şu adımları izler:

#### 1. Controller'ları Bulur (Reflection ile)

```csharp
// MapControllers() arka planda şunu yapar (basitleştirilmiş):
var assembly = typeof(Program).Assembly;  // Catalog.API assembly'si

var controllerTypes = assembly.GetTypes()  // Tüm tipleri al
    .Where(t => 
        t.IsClass &&                                    // Class olmalı
        !t.IsAbstract &&                                // Abstract olmamalı
        t.IsSubclassOf(typeof(ControllerBase))          // ControllerBase'den türemeli
        // VEYA
        t.Name.EndsWith("Controller")                   // "Controller" ile bitmeli
    );
```

**Reflection Kontrolü:**
```csharp
// ProductsController için:
// 1. IsClass? → ✅ Evet
// 2. IsAbstract? → ❌ Hayır
// 3. IsSubclassOf(ControllerBase)? → ✅ Evet
// 4. Name.EndsWith("Controller")? → ✅ Evet
// SONUÇ: ✅ Controller olarak bulunur!
```

#### 2. Route Attribute'larını Tarar

Her Controller için route attribute'larını okur:

```csharp
// ProductsController.cs
[ApiController]
[Route("api/[controller]")]  // ← "api/products" olur
public class ProductsController : ControllerBase
{
    [HttpGet]  // ← GET /api/products
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(...)
    
    [HttpGet("{id}")]  // ← GET /api/products/{id}
    public async Task<ActionResult<ProductDto>> GetProductById(Guid id)
    
    [HttpPost]  // ← POST /api/products
    public async Task<ActionResult<Guid>> CreateProduct(...)
}
```

#### 3. Endpoint'leri Oluşturur

`MapControllers()` bu endpoint'leri oluşturur:

- `GET /api/products` → `GetProducts()`
- `GET /api/products/{id}` → `GetProductById(Guid id)`
- `GET /api/products/category/{categoryId}` → `GetProductsByCategory(Guid categoryId)`
- `POST /api/products` → `CreateProduct(CreateProductCommand)`
- `PUT /api/products/{id}` → `UpdateProduct(Guid id, UpdateProductCommand)`
- `DELETE /api/products/{id}` → `DeleteProduct(Guid id)`

---

## Farklar

| Özellik | `AddControllers()` | `MapControllers()` |
|---------|-------------------|-------------------|
| **Aşama** | Builder (yapılandırma) | App (runtime) |
| **Ne zaman** | Uygulama başlangıcında | `builder.Build()` sonrasında |
| **Ne yapar** | Servisleri kaydeder | Endpoint'leri oluşturur |
| **Çalışma zamanı** | Startup | Her HTTP request |
| **Gereksinim** | Controller'ların çalışması için servisler | Controller'ların endpoint'lere map edilmesi |
| **Reflection kullanır mı?** | Evet (Controller'ları bulmak için) | Evet (Route attribute'larını bulmak için) |
| **DI Container kullanır mı?** | Evet (Controller'ları kaydetmek için) | Evet (Controller instance'ları almak için) |
| **Route oluşturur mu?** | Hayır | Evet |
| **HTTP pipeline'a ekler mi?** | Hayır | Evet |

---

## Görsel Akış

```
┌─────────────────────────────────────────────────────────┐
│ 1. builder.Services.AddControllers()                   │
│    ↓                                                     │
│    • Controller servislerini DI'a kaydet                │
│    • Model binding servislerini ekle                    │
│    • JSON formatter'ları ekle                          │
│    • Validation servislerini ekle                       │
│                                                         │
│    ❌ Henüz endpoint'ler oluşturulmadı                  │
│    ❌ Controller'lar kullanılamaz                      │
└─────────────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ 2. builder.Build()                                      │
│    ↓                                                     │
│    • Service provider oluşturulur                      │
│    • Controller servisleri hazır                        │
└─────────────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ 3. app.MapControllers()                                 │
│    ↓                                                     │
│    • Reflection ile Controller'ları bulur                │
│      - assembly.GetTypes()                              │
│      - .Where(t => t.IsSubclassOf(typeof(ControllerBase))) │
│    • ProductsController bulunur                         │
│    • CategoriesController bulunur                        │
│    • [Route] attribute'larını okur                      │
│    • [HttpGet], [HttpPost] vb. okunur                   │
│    • Endpoint'ler oluşturulur:                          │
│      - GET /api/products                                │
│      - GET /api/products/{id}                           │
│      - POST /api/products                               │
│      - PUT /api/products/{id}                           │
│      - DELETE /api/products/{id}                        │
│                                                         │
│    ✅ Artık HTTP request'ler controller'lara yönlenir  │
└─────────────────────────────────────────────────────────┘
```

---

## Reflection Mekanizması Detayı

### MapControllers() Nasıl Controller'ları Bulur?

#### 1. Assembly Taraması

```csharp
// Framework içinde (basitleştirilmiş)
var assembly = typeof(Program).Assembly;  // Catalog.API assembly'si
var types = assembly.GetTypes();          // Tüm tipleri al
```

#### 2. Controller Filtreleme

```csharp
var controllerTypes = types
    .Where(t => 
        t.IsClass &&                                    // Class olmalı
        !t.IsAbstract &&                                // Abstract olmamalı
        t.IsSubclassOf(typeof(ControllerBase))          // ControllerBase'den türemeli
    );
```

#### 3. Route Attribute Okuma

```csharp
foreach (var controllerType in controllerTypes)
{
    // Controller seviyesinde route
    var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();
    var baseRoute = routeAttribute?.Template ?? "";  // "api/[controller]"
    
    // Action method'ları bul
    var methods = controllerType.GetMethods()
        .Where(m => m.IsPublic && !m.IsSpecialName);
    
    foreach (var method in methods)
    {
        // HTTP verb attribute'larını bul
        var httpGet = method.GetCustomAttribute<HttpGetAttribute>();
        var httpPost = method.GetCustomAttribute<HttpPostAttribute>();
        // ...
        
        // Endpoint oluştur
        var endpoint = $"{baseRoute}/{methodRoute}";
    }
}
```

### Örnek: ProductsController Bulunması

```csharp
// ProductsController.cs
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(...)
}
```

**Reflection Kontrolü:**
```csharp
// 1. IsClass? → ✅ Evet
// 2. IsAbstract? → ❌ Hayır
// 3. IsSubclassOf(ControllerBase)? → ✅ Evet
// 4. Name.EndsWith("Controller")? → ✅ Evet
// SONUÇ: ✅ Controller olarak bulunur!

// Route okuma:
// [Route("api/[controller]")] → "api/products"
// [HttpGet] → GET verb
// Final endpoint: GET /api/products
```

---

## Örnek: HTTP Request Akışı

```
1. HTTP Request: GET /api/products
   ↓
2. Routing middleware endpoint'i bulur
   ↓
3. ProductsController.GetProducts() çağrılır
   ↓
4. Controller servisleri (model binding, validation vb.) kullanılır
   ↓
5. Response döndürülür
```

---

## Önemli Notlar

### 1. AddControllers() Olmadan MapControllers() Çalışmaz

**Neden?**
- `MapControllers()` DI container'dan Controller'ları alır
- `AddControllers()` olmadan Controller'lar kayıtlı değildir
- Sonuç: `MapControllers()` hiçbir Controller bulamaz, route oluşturulamaz

**Örnek:**
```csharp
// ❌ YANLIŞ - AddControllers() yok
builder.Services.AddOpenApi();
// AddControllers() yok!
app.MapControllers();  // Controller bulunamaz, route oluşturulamaz

// ✅ DOĞRU - İkisi de var
builder.Services.AddControllers();  // Controller'lar kaydedilir
app.MapControllers();  // Route'lar oluşturulur
```

### 2. MapControllers() Olmadan Endpoint'ler Çalışmaz

**Neden?**
- `AddControllers()` Controller'ları kaydeder ama route oluşturmaz
- `MapControllers()` olmadan endpoint'ler oluşturulmaz
- Sonuç: HTTP request'ler controller'lara yönlenemez

**Örnek:**
```csharp
// ❌ YANLIŞ - MapControllers() yok
builder.Services.AddControllers();
// MapControllers() yok!
// GET /api/products → 404 Not Found

// ✅ DOĞRU - İkisi de var
builder.Services.AddControllers();
app.MapControllers();  // Endpoint'ler oluşturulur
// GET /api/products → 200 OK
```

### 3. Sıra Önemli

```csharp
// ✅ DOĞRU SIRA:
app.UseExceptionHandler();   // 1. Exception handler (en üstte)
app.MapControllers();         // 2. Controller routing

// ❌ YANLIŞ SIRA:
app.MapControllers();         // 1. Controller routing
app.UseExceptionHandler();    // 2. Exception handler (çok geç!)
```

---

## Özet

### AddControllers()
- **Ne zaman**: Builder aşamasında
- **Ne yapar**: Controller servislerini DI container'a kaydeder
- **Sonuç**: Controller'lar çalışmaya hazır hale gelir

### MapControllers()
- **Ne zaman**: App aşamasında (`builder.Build()` sonrasında)
- **Ne yapar**: Controller'ları endpoint'lere map eder (Reflection ile)
- **Sonuç**: HTTP request'ler controller'lara yönlenir

### İkisi Birlikte
1. `AddControllers()` → Servisleri hazırlar
2. `MapControllers()` → Endpoint'leri oluşturur

**İkisi de gerekli!** Biri olmadan controller'lar çalışmaz.

---

## Reflection Özeti

**MapControllers() Reflection ile:**
1. `typeof(Program).Assembly` → Assembly'yi alır
2. `assembly.GetTypes()` → Tüm tipleri alır
3. `.Where(t => t.IsSubclassOf(typeof(ControllerBase)))` → Controller'ları filtreler
4. Route attribute'larını okur
5. Endpoint'leri oluşturur

**Bu yüzden controller'ları manuel kaydetmeye gerek yok!** Reflection otomatik bulur.

---

## Avantajlar

1. **Otomatik Keşif**: Controller'lar otomatik bulunur
2. **Manuel Kayıt Gereksiz**: Reflection sayesinde manuel kayıt yapmaya gerek yok
3. **Convention-Based**: Naming convention'a göre çalışır
4. **Attribute-Based Routing**: Route attribute'ları ile esnek routing

## Dikkat Edilmesi Gerekenler

1. **Sıra Önemli**: Middleware sırası önemlidir
2. **İkisi de Gerekli**: AddControllers() ve MapControllers() birlikte kullanılmalı
3. **Reflection Overhead**: İlk başlangıçta reflection maliyeti olabilir (genellikle önemsiz)
