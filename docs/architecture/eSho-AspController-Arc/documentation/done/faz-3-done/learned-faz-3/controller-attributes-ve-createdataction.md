# Controller Attribute'ları ve CreatedAtAction - Detaylı Açıklama

> Bu dokümanda, ASP.NET Core Controller'larında kullanılan attribute'lar (`[ApiController]`, `[Route]`, `[HttpGet]`) ve `CreatedAtAction()` metodunun nasıl çalıştığı detaylı olarak açıklanmaktadır.

---

## [ApiController] Attribute'u

**Kullanım:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    // ...
}
```

**Ne İşe Yarar:**
- Controller'ın bir API Controller olduğunu belirtir
- ASP.NET Core'a bu controller'ın REST API için kullanıldığını söyler
- Otomatik davranışları etkinleştirir

**Otomatik Davranışlar:**
1. **Otomatik Model Validation:**
   - Model binding sırasında validation hataları varsa otomatik 400 Bad Request döner
   - `ModelState.IsValid` kontrolüne gerek yok (otomatik yapılır)

2. **ProblemDetails Formatı:**
   - Hata yanıtları RFC 7807 standardına uygun ProblemDetails formatında döner
   - `type`, `title`, `status`, `detail`, `instance` alanları içerir

3. **Attribute Routing:**
   - `[FromBody]`, `[FromQuery]`, `[FromRoute]` gibi attribute'lar otomatik çalışır
   - Model binding daha akıllı çalışır

4. **400 Bad Request:**
   - Model binding hatalarında otomatik 400 Bad Request döner
   - `ModelState` içindeki hatalar otomatik ProblemDetails formatına dönüştürülür

**Neden Gerekli:**
- API Controller'lar için standart davranışları etkinleştirir
- Kod tekrarını önler (manuel validation kontrolü yazmaya gerek yok)
- Tutarlı hata yanıt formatı sağlar
- REST API best practices'e uygun davranış

**Nasıl Çalışır:**
- ASP.NET Core runtime, `[ApiController]` attribute'unu görünce otomatik davranışları etkinleştirir
- Controller'ın `ControllerBase`'den türemesi gerekir (MVC Controller değil)
- Uygulama başlangıcında bu attribute'lar tarama yapılır ve davranışlar kaydedilir

**Örnek Senaryo:**
```csharp
// [ApiController] OLMADAN
[HttpPost]
public async Task<ActionResult> CreateCategory([FromBody] CreateCategoryCommand command)
{
    if (!ModelState.IsValid)  // Manuel kontrol gerekir
    {
        return BadRequest(ModelState);
    }
    // ...
}

// [ApiController] İLE
[ApiController]
[HttpPost]
public async Task<ActionResult> CreateCategory([FromBody] CreateCategoryCommand command)
{
    // ModelState.IsValid kontrolüne gerek yok
    // Hata varsa otomatik 400 Bad Request döner
    // ...
}
```

---

## [Route("api/[controller]")] Attribute'u

**Kullanım:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    // ...
}
```

**Ne İşe Yarar:**
- Controller'ın base route'unu tanımlar
- Tüm action method'lar için ortak route prefix'i oluşturur
- `[controller]` token'ı controller adıyla değiştirilir

**Token Değiştirme:**
- `[controller]` → Controller adından "Controller" kısmı çıkarılır
- `CategoriesController` → `categories`
- `ProductsController` → `products`
- Final route: `api/categories`, `api/products`

**Neden Gerekli:**
- HTTP isteklerinin hangi controller'a gideceğini belirler
- REST API URL yapısını oluşturur
- Tüm endpoint'ler için tutarlı URL yapısı sağlar

**Nasıl Çalışır:**
1. ASP.NET Core runtime, `[Route("api/[controller]")]` attribute'unu görür
2. Controller adını alır: `CategoriesController`
3. "Controller" kısmını çıkarır: `Categories`
4. İlk harfi küçük yapar: `categories`
5. Route template'ine yerleştirir: `api/categories`
6. Tüm action method'lar bu base route'u kullanır

**Örnek:**
```csharp
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    [HttpGet]  // → GET /api/categories
    public async Task<ActionResult> GetCategories() { }

    [HttpGet("{id}")]  // → GET /api/categories/{id}
    public async Task<ActionResult> GetCategoryById(Guid id) { }

    [HttpPost]  // → POST /api/categories
    public async Task<ActionResult> CreateCategory([FromBody] CreateCategoryCommand command) { }
}
```

**Route Birleştirme:**
- Base route: `api/categories`
- Action route: `{id}` (GetCategoryById için)
- Final route: `api/categories/{id}`

---

## [HttpGet("{id}")] Attribute'u

**Kullanım:**
```csharp
[HttpGet("{id}")]
public async Task<ActionResult<CategoryDto>> GetCategoryById(Guid id)
{
    // ...
}
```

**Ne İşe Yarar:**
- HTTP GET isteği için route tanımlar
- `{id}` route parametresini method parametresine bağlar
- URL'deki `{id}` değeri `Guid id` parametresine otomatik atanır

**Route Parametresi:**
- `{id}` → Route template'inde parametre tanımı
- `Guid id` → Method parametresi, route parametresinden otomatik alınır
- Model binding ile otomatik dönüşüm yapılır (string → Guid)

**Neden Gerekli:**
- RESTful API'de kaynak ID'si ile erişim için
- URL'den parametre almak için
- Route parametrelerini method parametrelerine bağlamak için

**Nasıl Çalışır:**
1. Client isteği: `GET /api/categories/abc-123-def-456`
2. ASP.NET Core route matching yapar:
   - Base route: `api/categories`
   - Action route: `{id}`
   - URL: `api/categories/abc-123-def-456`
   - Eşleşme: `{id}` = `abc-123-def-456`
3. Model binding:
   - `abc-123-def-456` string'i `Guid` tipine dönüştürülür
   - `GetCategoryById(Guid id)` metoduna `id` parametresi olarak geçilir
4. Method çalışır

**Örnek Senaryo:**
```
Client İsteği:
GET /api/categories/123e4567-e89b-12d3-a456-426614174000

Route Eşleştirme:
api/categories/{id}
         ↓
{id} = "123e4567-e89b-12d3-a456-426614174000"

Model Binding:
"123e4567-e89b-12d3-a456-426614174000" → Guid.Parse() → Guid instance

Method Çağrısı:
GetCategoryById(Guid id)
id = Guid("123e4567-e89b-12d3-a456-426614174000")
```

**Diğer HTTP Method Attribute'ları:**
- `[HttpGet]` → GET isteği
- `[HttpPost]` → POST isteği
- `[HttpPut]` → PUT isteği
- `[HttpDelete]` → DELETE isteği
- `[HttpPatch]` → PATCH isteği

---

## CreatedAtAction() Metodu

**Kullanım:**
```csharp
[HttpPost]
public async Task<ActionResult<Guid>> CreateCategory([FromBody] CreateCategoryCommand command)
{
    var categoryId = await _mediator.Send(command);
    return CreatedAtAction(nameof(GetCategoryById), new { id = categoryId }, categoryId);
}
```

**Ne İşe Yarar:**
- Yeni oluşturulan kaynağın URL'ini Location header'ına ekler
- HTTP 201 Created status code döner
- REST API best practice'e uygun response oluşturur

**Parametreler:**
1. **`nameof(GetCategoryById)`**: Action method adı (string)
   - Metod çalıştırılmaz, sadece route bilgisi alınır
   - `nameof()` compile-time'da string'e dönüştürülür
   - Type-safe (metod adı değişirse compile hatası verir)

2. **`new { id = categoryId }`**: Route parametreleri (anonymous object)
   - Action method'un route parametrelerini doldurur
   - `GetCategoryById(Guid id)` için `{ id = categoryId }` gerekir

3. **`categoryId`**: Response body (opsiyonel)
   - HTTP response body'sinde dönecek değer
   - Genellikle oluşturulan kaynağın ID'si

**Neden Gerekli:**
- REST API standardına uygun response döndürmek için
- Client'ın yeni oluşturulan kaynağın nerede olduğunu bilmesi için
- Location header'ında kaynağın URL'ini göstermek için

**Nasıl Çalışır? (Adım Adım):**

**1. nameof(GetCategoryById) İşlemi:**
```csharp
nameof(GetCategoryById)  // → "GetCategoryById" string'i döner
```
- Compile-time'da metod adı string'e dönüştürülür
- Metod çalıştırılmaz, sadece adı alınır

**2. Action Method Bulma:**
```csharp
// ASP.NET Core, controller'da "GetCategoryById" adında action method arar
[HttpGet("{id}")]
public async Task<ActionResult<CategoryDto>> GetCategoryById(Guid id)
{
    // Bu metod bulunur
}
```

**3. Route Bilgisi Alma:**
```csharp
// GetCategoryById metodunun route'u:
[HttpGet("{id}")]  // → Route: "{id}"
// Base route: "api/categories"
// Final route: "api/categories/{id}"
```

**4. Route Parametrelerini Doldurma:**
```csharp
new { id = categoryId }  // → { id = "abc-123-def-456" }
// Route template: "api/categories/{id}"
// Doldurulmuş route: "api/categories/abc-123-def-456"
```

**5. URL Oluşturma:**
```csharp
// Final URL:
"/api/categories/abc-123-def-456"
```

**6. HTTP Response Oluşturma:**
```http
HTTP/1.1 201 Created
Location: /api/categories/abc-123-def-456
Content-Type: application/json

"abc-123-def-456"
```

**Önemli Not:**
- `nameof(GetCategoryById)` sadece string döndürür, metod çalıştırılmaz
- Sadece route bilgisi alınır, URL oluşturulur
- Handler'ın çalışması gerekmez (zaten çalışmış, categoryId dönmüş)

**Akış Diyagramı:**
```
┌─────────────────────────────────────────────────────────────┐
│ CreateCategory() Metodu Çalışır                             │
│                                                              │
│ 1. var categoryId = await _mediator.Send(command);         │
│    → Handler çalışır                                         │
│    → categoryId = "abc-123" döner                            │
│                                                              │
│ 2. return CreatedAtAction(                                   │
│        nameof(GetCategoryById),  ← SADECE STRING!           │
│        new { id = categoryId },                              │
│        categoryId                                             │
│    );                                                         │
└─────────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────────┐
│ CreatedAtAction İçinde (ASP.NET Core)                       │
│                                                              │
│ 1. nameof(GetCategoryById) → "GetCategoryById" string'i    │
│                                                              │
│ 2. Controller'da "GetCategoryById" adında action arar      │
│    → GetCategoryById metodu bulunur                          │
│                                                              │
│ 3. GetCategoryById'in route'unu alır:                       │
│    [HttpGet("{id}")]                                         │
│    → Route: "api/categories/{id}"                            │
│                                                              │
│ 4. Route parametrelerini doldurur:                           │
│    { id = "abc-123" }                                        │
│    → URL: "api/categories/abc-123"                           │
│                                                              │
│ 5. Location header'ına ekler:                                │
│    Location: /api/categories/abc-123                         │
│                                                              │
│ ⚠️ ÖNEMLİ: GetCategoryById metodu ÇALIŞTIRILMAZ!            │
│    Sadece route bilgisi alınır, URL oluşturulur.            │
└─────────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────────┐
│ HTTP Response                                                │
│                                                              │
│ HTTP/1.1 201 Created                                         │
│ Location: /api/categories/abc-123                            │
│                                                              │
│ "abc-123"  ← Response body                                   │
└─────────────────────────────────────────────────────────────┘
```

**Örnek Senaryo:**
```
1. Client: POST /api/categories
   Body: { "name": "Elektronik" }
   
2. Server: Category oluşturulur
   categoryId = "abc-123"
   
3. Server: CreatedAtAction ile response döner
   Location: /api/categories/abc-123
   Body: "abc-123"
   
4. Client: Location header'ı görür
   → "Ah, yeni kategori şurada: /api/categories/abc-123"
   
5. Client (isteğe bağlı): GET /api/categories/abc-123
   → Yeni oluşturulan kategoriyi getirir
```

**Alternatif Kullanımlar:**
```csharp
// Sadece Location header (body yok)
return CreatedAtAction(nameof(GetCategoryById), new { id = categoryId }, null);

// Farklı action method
return CreatedAtAction(nameof(GetProductById), new { id = productId }, productId);

// Manuel URL (CreatedAtAction yerine)
return Created($"/api/categories/{categoryId}", categoryId);
```

---

## Özet

| Özellik | Açıklama |
|---------|----------|
| **`[ApiController]`** | API Controller olduğunu belirtir, otomatik validation ve ProblemDetails sağlar |
| **`[Route("api/[controller]")]`** | Base route tanımlar, `[controller]` token'ı controller adıyla değiştirilir |
| **`[HttpGet("{id}")]`** | HTTP GET isteği için route tanımlar, route parametresini method parametresine bağlar |
| **`CreatedAtAction()`** | HTTP 201 Created response döner, Location header'ında yeni kaynağın URL'ini gösterir |

---

**Son Güncelleme:** Aralık 2024

