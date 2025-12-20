# AddControllers() ve MapControllers() - Detaylı Mekanizma

> Bu dokümanda, `AddControllers()` ve `MapControllers()` metodlarının nasıl çalıştığı, arka planda ne yaptığı ve kendi kodlarımız üzerinden detaylı akış açıklanmaktadır.

---

## Genel Bakış

**AddControllers() ve MapControllers() Nedir?**
- `AddControllers()`: Controller'ları DI container'a kaydeder (servis kaydı)
- `MapControllers()`: Controller route'larını HTTP pipeline'a ekler (routing)

**Neden İkisi de Gerekli?**
- `AddControllers()` olmadan → Controller'lar kayıtlı değil, kullanılamaz
- `MapControllers()` olmadan → Route'lar oluşturulmaz, endpoint'ler çalışmaz
- İkisi birlikte → Controller'lar çalışır, endpoint'ler erişilebilir olur

---

## AddControllers() - Detaylı Mekanizma

### Program.cs'deki Kullanım

```csharp
// Program.cs - Satır 16
builder.Services.AddControllers();
```

### Ne Yapar?

**1. Controller'ları Bulur (Reflection ile Tarama)**

ASP.NET Core, `AddControllers()` çağrıldığında şunları yapar:

```csharp
// Arka planda yapılan (basitleştirilmiş)
var assembly = typeof(Program).Assembly;  // Catalog.API assembly'si
var controllerTypes = assembly.GetTypes()
    .Where(t => 
        t.IsClass &&                                    // Class olmalı
        !t.IsAbstract &&                                // Abstract olmamalı
        t.IsSubclassOf(typeof(ControllerBase)) &&       // ControllerBase'den türemeli
        t.GetCustomAttribute<ApiControllerAttribute>() != null  // [ApiController] attribute'u olmalı
    );
```

**Bizim Kodumuzda Bulunan Controller'lar:**
- ✅ `ProductsController` → `[ApiController]` var, `ControllerBase`'den türüyor
- ✅ `CategoriesController` → `[ApiController]` var, `ControllerBase`'den türüyor

**2. Controller'ları DI Container'a Kaydeder**

```csharp
// Arka planda yapılan (basitleştirilmiş)
foreach (var controllerType in controllerTypes)
{
    // Her Controller'ı Scoped lifetime ile kaydeder
    services.AddScoped(controllerType);
    
    // Controller'ın constructor parametrelerini analiz eder
    // Dependency injection için hazırlar
}
```

**Bizim Kodumuzda:**
```csharp
// ProductsController.cs - Satır 19-22
public ProductsController(IMediator mediator)
{
    _mediator = mediator;  // DI container'dan IMediator inject edilecek
}

// CategoriesController.cs - Satır 16-19
public CategoriesController(IMediator mediator)
{
    _mediator = mediator;  // DI container'dan IMediator inject edilecek
}
```

**DI Container Kayıtları:**
- `ProductsController` → Scoped lifetime ile kaydedilir
- `CategoriesController` → Scoped lifetime ile kaydedilir
- Her HTTP request'te yeni instance oluşturulur

**3. Controller Özelliklerini Etkinleştirir**

`AddControllers()` ayrıca şunları da yapar:
- **Model Binding**: HTTP request'ten veri almak için
- **Validation**: Model validation için
- **Routing**: Route attribute'larını tanımak için
- **Action Method Discovery**: Action method'ları bulmak için

---

## MapControllers() - Detaylı Mekanizma

### Program.cs'deki Kullanım

```csharp
// Program.cs - Satır 89
app.MapControllers();
```

### Ne Yapar?

**1. Controller'ları Bulur (Zaten Kayıtlı)**

`MapControllers()` çağrıldığında, DI container'dan kayıtlı Controller'ları alır:

```csharp
// Arka planda yapılan (basitleştirilmiş)
var controllerTypes = serviceProvider.GetServices<ControllerBase>()
    .Select(c => c.GetType())
    .Distinct();
```

**Bizim Kodumuzda:**
- `ProductsController` → DI container'dan alınır
- `CategoriesController` → DI container'dan alınır

**2. Route Attribute'larını Tarar**

Her Controller için route attribute'larını tarar:

```csharp
// Arka planda yapılan (basitleştirilmiş)
foreach (var controllerType in controllerTypes)
{
    // Controller seviyesinde route
    var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();
    var baseRoute = routeAttribute?.Template;  // "api/[controller]"
    
    // Action method'ları bulur
    var actionMethods = controllerType.GetMethods()
        .Where(m => 
            m.IsPublic && 
            !m.IsSpecialName &&
            m.GetCustomAttribute<HttpMethodAttribute>() != null  // [HttpGet], [HttpPost], vb.
        );
    
    foreach (var actionMethod in actionMethods)
    {
        // Action seviyesinde route
        var httpMethodAttr = actionMethod.GetCustomAttribute<HttpMethodAttribute>();
        var actionRoute = httpMethodAttr?.Template;  // "{id}", "category/{categoryId}", vb.
        
        // Final route oluşturulur
        var finalRoute = CombineRoutes(baseRoute, actionRoute);
    }
}
```

**Bizim Kodumuzda - ProductsController:**

**Controller Route:**
```csharp
// ProductsController.cs - Satır 14
[Route("api/[controller]")]
// → "api/[controller]" → "api/products" (controller adından "Controller" çıkarılır)
```

**Action Method Route'ları:**
```csharp
// Satır 25: [HttpGet]
// → Route: "api/products"
// → HTTP Method: GET
// → Action: GetProducts()

// Satır 33: [HttpGet("{id}")]
// → Route: "api/products/{id}"
// → HTTP Method: GET
// → Action: GetProductById(Guid id)

// Satır 41: [HttpGet("category/{categoryId}")]
// → Route: "api/products/category/{categoryId}"
// → HTTP Method: GET
// → Action: GetProductsByCategory(Guid categoryId)

// Satır 49: [HttpPost]
// → Route: "api/products"
// → HTTP Method: POST
// → Action: CreateProduct([FromBody] CreateProductCommand command)

// Satır 57: [HttpPut("{id}")]
// → Route: "api/products/{id}"
// → HTTP Method: PUT
// → Action: UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)

// Satır 66: [HttpDelete("{id}")]
// → Route: "api/products/{id}"
// → HTTP Method: DELETE
// → Action: DeleteProduct(Guid id)
```

**Bizim Kodumuzda - CategoriesController:**

**Controller Route:**
```csharp
// CategoriesController.cs - Satır 11
[Route("api/[controller]")]
// → "api/[controller]" → "api/categories"
```

**Action Method Route'ları:**
```csharp
// Satır 22: [HttpGet]
// → Route: "api/categories"
// → HTTP Method: GET
// → Action: GetCategories()

// Satır 30: [HttpGet("{id}")]
// → Route: "api/categories/{id}"
// → HTTP Method: GET
// → Action: GetCategoryById(Guid id)

// Satır 38: [HttpPost]
// → Route: "api/categories"
// → HTTP Method: POST
// → Action: CreateCategory([FromBody] CreateCategoryCommand command)
```

**3. Route'ları HTTP Pipeline'a Ekler**

```csharp
// Arka planda yapılan (basitleştirilmiş)
foreach (var route in routes)
{
    app.Map(route.Path, route.HttpMethod, async (HttpContext context) =>
    {
        // Controller instance oluştur (DI container'dan)
        var controller = serviceProvider.GetRequiredService(route.ControllerType);
        
        // Action method'u çağır
        var result = await route.ActionMethod.Invoke(controller, parameters);
        
        // Response döndür
        return result;
    });
}
```

**Oluşturulan Route'lar:**

**ProductsController Route'ları:**
- `GET /api/products` → `ProductsController.GetProducts()`
- `GET /api/products/{id}` → `ProductsController.GetProductById(Guid id)`
- `GET /api/products/category/{categoryId}` → `ProductsController.GetProductsByCategory(Guid categoryId)`
- `POST /api/products` → `ProductsController.CreateProduct([FromBody] CreateProductCommand)`
- `PUT /api/products/{id}` → `ProductsController.UpdateProduct(Guid id, [FromBody] UpdateProductCommand)`
- `DELETE /api/products/{id}` → `ProductsController.DeleteProduct(Guid id)`

**CategoriesController Route'ları:**
- `GET /api/categories` → `CategoriesController.GetCategories()`
- `GET /api/categories/{id}` → `CategoriesController.GetCategoryById(Guid id)`
- `POST /api/categories` → `CategoriesController.CreateCategory([FromBody] CreateCategoryCommand)`

---

## Tam Akış - Adım Adım

### 1. Uygulama Başlangıcı (Program.cs Çalışır)

```
┌─────────────────────────────────────────────────────────────┐
│ Program.cs Çalışır                                          │
│                                                              │
│ var builder = WebApplication.CreateBuilder(args);           │
└─────────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────────┐
│ builder.Services.AddControllers() Çağrılır (Satır 16)       │
│                                                              │
│ 1. Assembly taranır: Catalog.API                            │
│ 2. Controller'lar bulunur:                                  │
│    - ProductsController                                     │
│    - CategoriesController                                   │
│ 3. DI Container'a kaydedilir (Scoped):                     │
│    - ProductsController → Scoped                            │
│    - CategoriesController → Scoped                          │
│ 4. Controller özellikleri etkinleştirilir:                 │
│    - Model Binding                                          │
│    - Validation                                             │
│    - Routing                                                │
└─────────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────────┐
│ var app = builder.Build(); (Satır 53)                       │
│                                                              │
│ DI Container hazır, Controller'lar kayıtlı                 │
└─────────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────────┐
│ app.MapControllers() Çağrılır (Satır 89)                   │
│                                                              │
│ 1. DI Container'dan Controller'lar alınır:                 │
│    - ProductsController                                     │
│    - CategoriesController                                   │
│ 2. Route attribute'ları taranır:                            │
│    - [Route("api/[controller]")]                           │
│    - [HttpGet], [HttpPost], [HttpPut], [HttpDelete]        │
│ 3. Route'lar oluşturulur:                                   │
│    - GET /api/products                                      │
│    - GET /api/products/{id}                                 │
│    - POST /api/products                                     │
│    - ... (diğer route'lar)                                  │
│ 4. Route'lar HTTP pipeline'a eklenir                       │
└─────────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────────┐
│ app.Run(); (Satır 113)                                      │
│                                                              │
│ Uygulama başlar, HTTP istekleri dinlenir                   │
└─────────────────────────────────────────────────────────────┘
```

---

### 2. HTTP İsteği Geldiğinde

**Örnek Senaryo: GET /api/products**

```
┌─────────────────────────────────────────────────────────────┐
│ Client: GET /api/products                                    │
│                                                              │
│ HTTP Request gelir                                           │
└─────────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────────┐
│ ASP.NET Core HTTP Pipeline                                  │
│                                                              │
│ 1. Exception Handler Middleware (Satır 58)                 │
│ 2. Swagger Middleware (Development'ta, Satır 78-83)        │
│ 3. HTTPS Redirection (Satır 86)                             │
│ 4. Route Matching (MapControllers tarafından oluşturulan)   │
└─────────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────────┐
│ Route Matching                                               │
│                                                              │
│ URL: /api/products                                           │
│ Method: GET                                                  │
│                                                              │
│ MapControllers() tarafından oluşturulan route'lar taranır:  │
│ - GET /api/products → ✅ EŞLEŞTİ!                           │
│   Controller: ProductsController                            │
│   Action: GetProducts()                                      │
└─────────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────────┐
│ Controller Instance Oluşturulur                              │
│                                                              │
│ DI Container'dan ProductsController alınır (Scoped)        │
│                                                              │
│ Constructor çalışır:                                        │
│ public ProductsController(IMediator mediator)               │
│ {                                                            │
│     _mediator = mediator;  // DI container'dan inject edilir│
│ }                                                            │
└─────────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────────┐
│ Action Method Çalışır                                        │
│                                                              │
│ ProductsController.GetProducts([FromQuery] GetProductsQuery)│
│                                                              │
│ 1. Model Binding:                                            │
│    Query string'den GetProductsQuery oluşturulur            │
│    (PageNumber, PageSize, CategoryId)                        │
│                                                              │
│ 2. Method çalışır:                                           │
│    var products = await _mediator.Send(query);              │
│    return Ok(products);                                      │
└─────────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────────┐
│ HTTP Response                                                │
│                                                              │
│ HTTP/1.1 200 OK                                              │
│ Content-Type: application/json                              │
│                                                              │
│ [                                                             │
│   { "id": "...", "name": "iPhone 15", ... },                │
│   { "id": "...", "name": "Samsung S24", ... }               │
│ ]                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Detaylı Kod Analizi

### ProductsController - Route Oluşturma

**Controller Seviyesi:**
```csharp
// ProductsController.cs - Satır 13-14
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
```

**Route Oluşturma:**
1. `[Route("api/[controller]")]` → Base route
2. `[controller]` token'ı → `ProductsController` → `products` (Controller kısmı çıkarılır)
3. Base route: `api/products`

**Action Method Seviyesi - GetProducts:**
```csharp
// ProductsController.cs - Satır 24-30
[HttpGet]
public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts([FromQuery] GetProductsQuery query)
```

**Route Oluşturma:**
1. Base route: `api/products`
2. Action route: `[HttpGet]` → Boş (base route kullanılır)
3. Final route: `GET /api/products`
4. Parametre: `[FromQuery] GetProductsQuery query` → Query string'den alınır

**Action Method Seviyesi - GetProductById:**
```csharp
// ProductsController.cs - Satır 32-38
[HttpGet("{id}")]
public async Task<ActionResult<ProductDto>> GetProductById(Guid id)
```

**Route Oluşturma:**
1. Base route: `api/products`
2. Action route: `[HttpGet("{id}")]` → `{id}`
3. Final route: `GET /api/products/{id}`
4. Parametre: `Guid id` → Route'tan alınır (URL'deki `{id}` değeri)

**Action Method Seviyesi - GetProductsByCategory:**
```csharp
// ProductsController.cs - Satır 40-46
[HttpGet("category/{categoryId}")]
public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsByCategory(Guid categoryId)
```

**Route Oluşturma:**
1. Base route: `api/products`
2. Action route: `[HttpGet("category/{categoryId}")]` → `category/{categoryId}`
3. Final route: `GET /api/products/category/{categoryId}`
4. Parametre: `Guid categoryId` → Route'tan alınır

**Action Method Seviyesi - CreateProduct:**
```csharp
// ProductsController.cs - Satır 48-54
[HttpPost]
public async Task<ActionResult<Guid>> CreateProduct([FromBody] CreateProductCommand command)
```

**Route Oluşturma:**
1. Base route: `api/products`
2. Action route: `[HttpPost]` → Boş (base route kullanılır)
3. Final route: `POST /api/products`
4. Parametre: `[FromBody] CreateProductCommand command` → Request body'den alınır (JSON)

---

### CategoriesController - Route Oluşturma

**Controller Seviyesi:**
```csharp
// CategoriesController.cs - Satır 10-11
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
```

**Route Oluşturma:**
1. `[Route("api/[controller]")]` → Base route
2. `[controller]` token'ı → `CategoriesController` → `categories`
3. Base route: `api/categories`

**Action Method Seviyesi - GetCategories:**
```csharp
// CategoriesController.cs - Satır 21-27
[HttpGet]
public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
```

**Route Oluşturma:**
1. Base route: `api/categories`
2. Action route: `[HttpGet]` → Boş
3. Final route: `GET /api/categories`
4. Parametre: Yok

**Action Method Seviyesi - GetCategoryById:**
```csharp
// CategoriesController.cs - Satır 29-35
[HttpGet("{id}")]
public async Task<ActionResult<CategoryDto>> GetCategoryById(Guid id)
```

**Route Oluşturma:**
1. Base route: `api/categories`
2. Action route: `[HttpGet("{id}")]` → `{id}`
3. Final route: `GET /api/categories/{id}`
4. Parametre: `Guid id` → Route'tan alınır

**Action Method Seviyesi - CreateCategory:**
```csharp
// CategoriesController.cs - Satır 37-43
[HttpPost]
public async Task<ActionResult<Guid>> CreateCategory([FromBody] CreateCategoryCommand command)
```

**Route Oluşturma:**
1. Base route: `api/categories`
2. Action route: `[HttpPost]` → Boş
3. Final route: `POST /api/categories`
4. Parametre: `[FromBody] CreateCategoryCommand command` → Request body'den alınır

---

## AddControllers() vs MapControllers() Karşılaştırması

| Özellik | AddControllers() | MapControllers() |
|---------|------------------|------------------|
| **Ne zaman çağrılır?** | `builder.Services` aşamasında | `app` aşamasında |
| **Ne yapar?** | Controller'ları DI container'a kaydeder | Route'ları HTTP pipeline'a ekler |
| **Hangi Controller'ları bulur?** | `[ApiController]` + `ControllerBase` | DI container'daki Controller'lar |
| **Reflection kullanır mı?** | Evet (Controller'ları bulmak için) | Evet (Route attribute'larını bulmak için) |
| **DI Container kullanır mı?** | Evet (Controller'ları kaydetmek için) | Evet (Controller instance'ları almak için) |
| **Route oluşturur mu?** | Hayır | Evet |
| **HTTP pipeline'a ekler mi?** | Hayır | Evet |

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

---

### 2. MapControllers() Olmadan Endpoint'ler Çalışmaz

**Neden?**
- `AddControllers()` Controller'ları kaydeder ama route oluşturmaz
- `MapControllers()` route'ları HTTP pipeline'a ekler
- Route olmadan HTTP istekleri Controller'lara ulaşamaz

**Örnek:**
```csharp
// ❌ YANLIŞ - MapControllers() yok
builder.Services.AddControllers();  // Controller'lar kayıtlı
// MapControllers() yok!
// Sonuç: GET /api/products → 404 Not Found

// ✅ DOĞRU - İkisi de var
builder.Services.AddControllers();  // Controller'lar kayıtlı
app.MapControllers();  // Route'lar oluşturulur
// Sonuç: GET /api/products → 200 OK
```

---

### 3. Controller Lifetime (Scoped)

**Ne Anlama Gelir?**
- Her HTTP request'te yeni Controller instance oluşturulur
- Request bitince instance dispose olur
- Aynı request içinde aynı instance kullanılır

**Neden Scoped?**
- Thread-safe değildir (her request'te yeni instance güvenli)
- DbContext gibi scoped servislerle uyumlu
- Memory kullanımı optimize edilir

**Örnek:**
```
Request 1: GET /api/products
  → ProductsController instance 1 oluşturulur
  → Request bitince dispose olur

Request 2: GET /api/categories
  → CategoriesController instance 1 oluşturulur
  → Request bitince dispose olur

Request 3: GET /api/products
  → ProductsController instance 2 oluşturulur (yeni instance)
  → Request bitince dispose olur
```

---

### 4. Route Token Değiştirme ([controller])

**Nasıl Çalışır?**
```csharp
[Route("api/[controller]")]
public class ProductsController : ControllerBase
```

**Adımlar:**
1. `[controller]` token'ı bulunur
2. Controller adı alınır: `ProductsController`
3. "Controller" kısmı çıkarılır: `Products`
4. İlk harf küçük yapılır: `products`
5. Token değiştirilir: `api/products`

**Örnekler:**
- `ProductsController` → `api/products`
- `CategoriesController` → `api/categories`
- `OrdersController` → `api/orders`

---

## Özet

**AddControllers():**
- Controller'ları DI container'a kaydeder
- Reflection ile Controller'ları bulur
- Controller özelliklerini etkinleştirir
- **Program.cs'de:** `builder.Services.AddControllers();` (Satır 16)

**MapControllers():**
- Route'ları HTTP pipeline'a ekler
- Route attribute'larını tarar
- HTTP isteklerini Controller'lara yönlendirir
- **Program.cs'de:** `app.MapControllers();` (Satır 89)

**İkisi Birlikte:**
- Controller'lar kayıtlı ve kullanılabilir
- Route'lar oluşturulmuş ve erişilebilir
- API endpoint'leri çalışır

---

**Son Güncelleme:** Aralık 2024

