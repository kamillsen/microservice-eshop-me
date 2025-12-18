# DI Container (Dependency Injection Container) — Notlar

Bu not, projede sık geçen **DI Container** kavramını ve bu sohbet boyunca sorduğun soruların cevaplarını tek yerde toplamak için yazıldı.

---

## DI Container nedir?

**DI Container**, uygulamadaki sınıfların ihtiyaç duyduğu bağımlılıkları (**dependency**) otomatik olarak:

- **oluşturan** (instantiate),
- **doğru yere veren** (inject),
- **yaşam döngüsünü yöneten** (lifetime: transient/scoped/singleton),
- gerekirse **dispose eden**

mekanizmadır.

En basit cümleyle: **“`new` zincirlerini ve wiring/kablolama işini framework’e bırakma”** yöntemidir.

ASP.NET Core’da DI container’a eriştiğin yer çoğunlukla şurasıdır:

- `builder.Services`  → “servis kaydı / register”
- Constructor parametreleri → “servis çözümleme / resolve + inject”

---

## .NET’e özgü mü?

Hayır. **Kavram .NET’e özgü değil.** Farklı ekosistemlerde de vardır (Java Spring, Node/NestJS vb.).

Ama **ASP.NET Core** ile birlikte DI, framework’ün içine **built‑in** olarak geldi (`Microsoft.Extensions.DependencyInjection`).

> Eski **.NET Framework** dünyasında built‑in DI yoktu; genelde Autofac/Ninject/Unity gibi container’lar kullanılırdı.

---

## Neden ortaya çıkmış? Ne problemi çözer?

DI olmadan bağımlılıklar çoğunlukla sınıfın içinde “`new`” ile üretilir:

- Kod **sıkı bağlı** olur (değişiklikler dalga dalga yayılır)
- Test etmek zorlaşır (mock/fake takmak zor)
- Lifecycle/dispose yönetimi karışır

DI/DI Container ile:

- Sınıf sadece **“neye ihtiyacım var?”** der (constructor)
- Container **“nasıl oluşturulur?”** işini çözer

Sonuç: **daha esnek, daha test edilebilir, daha bakımı kolay** yapı.

---

## Neyi kolaylaştırıyor?

- **Loose coupling (gevşek bağlılık)**: interface üzerinden çalışmak (`IMediator` gibi)
- **Test edilebilirlik**: fake/mock bağımlılık takabilmek
- **Tek merkezden yönetim**: bağımlılıkların kayıtları `Program.cs` benzeri yerde
- **Lifecycle yönetimi**: `Transient/Scoped/Singleton`
- **Cross-cutting concerns**: pipeline/middleware gibi yapıları sisteme entegre etmek

---

## Bu projede DI Container nerede kullanılıyor?

### 1) Controller → IMediator (constructor injection)

`ProductsController`, `IMediator` ister:

```csharp
public ProductsController(IMediator mediator)
{
    _mediator = mediator;
}
```

Bu şu demek:

- Controller, `IMediator`’ı **kendisi üretmez**
- DI container, request geldiğinde controller’ı oluştururken `IMediator`’ı **resolve edip verir**

### 2) Program.cs’de servis kayıtları

`builder.Services.*` ile yapılan çağrıların çoğu DI container’a kayıttır:

- `AddControllers()`
- `AddMediatR(...)`
- `AddValidatorsFromAssembly(...)`
- `AddAutoMapper(...)`
- `AddDbContext<CatalogDbContext>(...)`
- `AddExceptionHandler<...>()`
- `AddHealthChecks()...`

---

## “Program.cs’deki AddMediatR bloğu DI Container için mi?”

Evet. Şu blok DI container’a MediatR altyapısını ve pipeline behavior’ları kaydeder:

- `builder.Services.AddMediatR(...)`
- `cfg.RegisterServicesFromAssembly(...)` → handler’ları bul/kaydet
- `cfg.AddBehavior(...)` → pipeline’a behavior ekle

**Bunu yapmasak ne olur?**

- Controller `IMediator` istediği için DI container `IMediator` üretemez → uygulama runtime’da “service resolve edilemedi” hatası verir.
- Handler’lar register edilmemiş olur → `Send(...)` yaptığında request’in handler’ı bulunamaz.
- Logging/Validation behavior’ları pipeline’da çalışmaz.

Özet: Bu projede MediatR/CQRS yaklaşımını kullanıyorsan bu kayıtlar **pratikte zorunlu**.

---

## “Kendi pipeline’ında ne var?” (HTTP pipeline vs MediatR pipeline)

Projede iki farklı “pipeline” kavramı var:

### 1) ASP.NET Core HTTP pipeline (middleware)

HTTP request geldiğinde middleware zincirinden geçer (exception handler, swagger, routing vs.).

### 2) MediatR pipeline (in-process)

Controller `IMediator.Send(...)` deyince MediatR kendi iç zincirini çalıştırır:

- `LoggingBehavior`
- `ValidationBehavior`
- sonra ilgili `Handler`

Basit görünüm:

```
HTTP -> Controller -> IMediator.Send
                 -> LoggingBehavior
                 -> ValidationBehavior
                 -> Handler
```

---

## “Controller Scoped” ne demek? Her istek için instance konusu

Bu kısım genelde kafa karıştırıyor çünkü **tek bir controller class’ı içinde birden fazla action (endpoint)** var.

Önemli gerçek: **HTTP’de her action çağrısı ayrı bir request’tir.**  
Yani “Controller’da 6 tane fonksiyon var” demek, “tek bir instance hepsini beraber çalıştırır” demek değildir.

Dokümanda geçen ifade doğru şekilde şöyle okunur:

- **Her HTTP request geldiğinde** controller için **yeni bir instance** oluşturulur.
- **Aynı request içinde** aynı controller instance kullanılır.
- Request bitince instance **dispose** edilir.

Önemli nokta: Controller’daki endpoint sayısı (GET/POST/PUT/DELETE) **“tek instance hepsine ortak”** demek değildir.

### Bunu senin kodundan okuyalım (ProductsController)

Senin `ProductsController` içinde DI ile gelen dependency şu:

```csharp
public ProductsController(IMediator mediator)
{
    _mediator = mediator;
}
```

Bu şu anlama gelir:

- Controller `IMediator`’ı kendi `new`’lemez.
- Request geldiğinde **DI Container**, controller’ı oluştururken `IMediator`’ı resolve eder ve verir.
- Hangi action çağrıldıysa **sadece o action** çalışır (diğer action’lar “orada durur”, çağrılmadıkça çalışmaz).

### Senaryo A — GET /api/products (listeleme) [Diagram 1]

Bu endpoint senin controller’da şuna karşılık gelir:

```csharp
[HttpGet]
public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts([FromQuery] GetProductsQuery query)
{
    var products = await _mediator.Send(query);
    return Ok(products);
}
```

ASCII akış:

```
HTTP Request #A: GET /api/products?pageNumber=1&pageSize=10
   |
   v
DI Container (scope = bu request)
   - ProductsController oluşturur (instance #A)
   - IMediator resolve eder ve ctor'a verir
   |
   v
ProductsController.GetProducts(query)
   |
   v
IMediator.Send(GetProductsQuery)
   |
   v
MediatR pipeline:
  LoggingBehavior -> ValidationBehavior -> GetProductsHandler
   |
   v
HTTP Response: 200 OK
   |
   v
Request biter -> scope dispose (controller + scoped servisler)
```

Burada “scoped” mantık: **Request #A boyunca** controller (ve genelde `DbContext` gibi servisler) tek scope içinde yaşar; request bitince biter.

### Senaryo B — POST /api/products (ürün oluşturma) [Diagram 2]

Bu endpoint senin controller’da şuna karşılık gelir:

```csharp
[HttpPost]
public async Task<ActionResult<Guid>> CreateProduct([FromBody] CreateProductCommand command)
{
    var productId = await _mediator.Send(command);
    return CreatedAtAction(nameof(GetProductById), new { id = productId }, productId);
}
```

ASCII akış:

```
HTTP Request #B: POST /api/products
Body: { ... CreateProductCommand ... }
   |
   v
DI Container (scope = bu request)
   - ProductsController oluşturur (instance #B)   <-- #A'dan farklı!
   - IMediator resolve eder
   |
   v
ProductsController.CreateProduct(command)
   |
   v
IMediator.Send(CreateProductCommand)
   |
   v
MediatR pipeline:
  LoggingBehavior -> ValidationBehavior -> CreateProductHandler
   |
   v
HTTP Response: 201 Created (+ Location header)
   |
   v
Request biter -> scope dispose
```

### “Controller’da çok action var” konusu neden sorun değil?

- Controller bir “menü” gibi düşün: içinde **bir sürü endpoint tanımı** var.
- Ama runtime’da **tek request sadece tek endpoint’i çağırır**.
- Aynı anda 3 farklı istek gelirse bile, genelde her biri **kendi controller instance’ını** alır.

### Aynı anda 3 request örneği (farklı endpoint’ler)

```
Request A: GET /api/products
  -> ProductsController #1 -> GetProducts() -> 200 -> dispose

Request B: GET /api/products/{id}
  -> ProductsController #2 -> GetProductById() -> 200/404 -> dispose

Request C: POST /api/products
  -> ProductsController #3 -> CreateProduct() -> 201 -> dispose
```

Yani: **Her request = ayrı controller instance** (genelde).

---

## Neden “Scoped” mantıklı?

Çünkü tipik olarak `DbContext` gibi bileşenler de request scope’ta yaşar:

- Thread-safety
- doğru dispose
- request boyunca tutarlı transaction/connection kullanımı

Bu yüzden “her request yeni instance” modeli web uygulamalarında standarttır.

---

## Lifetime türleri (Transient / Scoped / Singleton) — “A1/A2” ile net örnek

Bu kısım “özellikle transient” için kafa karıştırır. En net kural şu:

- **Transient**: *Resolve ettiğin her seferinde yeni instance*
- **Scoped**: *1 HTTP request boyunca tek instance*
- **Singleton**: *Uygulama ayağa kalktığından kapanana kadar tek instance*

Kısa şema:

```
Transient : aynı request içinde bile her resolve -> yeni (A1, A2, A3...)
Scoped    : 1 request = 1 instance (Request1->A1, Request2->A2)
Singleton : uygulama boyunca 1 instance (her yerde A1)
```

### Transient’i neden “aynı request içinde bile A1/A2” diye söylüyoruz?

Çünkü transient’te DI Container, **aynı request scope’u içinde olsan bile** her “isteyene” ayrı bir nesne üretir.

Somut düşün:

- Controller içinde `_mediator` zaten ctor’da 1 kere enjekte edildi (bu injection anında resolve oldu).
- Ama **controller içinden değil de handler içinde** tekrar başka bir transient servisi istersek, o da “yeni” olur.

Örnek senaryo (tamamen mantığı göstermek için):

```
HTTP Request #A
   |
   v
DI Container:
  - ProductsController yaratılırken IMyTransientService resolve -> A1
  - Handler yaratılırken IMyTransientService resolve -> A2   <-- aynı request ama farklı instance!
```

Yani transient “request’e bağlı kalmaz”, **resolve sayısına bağlıdır**.

#### Transient ne zaman mantıklı?

- **Stateless** (durum tutmayan) küçük yardımcılar: formatter, mapper wrapper, küçük hesaplayıcılar
- Her kullanımda “taze” olsun istediğin nesneler

#### Transient ne zaman tehlikeli?

- İçinde “paylaşılması gereken state” varsa (örn. request boyunca aynı kalması gereken bir şey)
- İçinde pahalı kaynak yönetimi varsa ve çok sık oluşturuluyorsa
- `DbContext` gibi thread-safe olmayan, scope’la yaşaması gereken tipler için (bunlar transient yapılmaz)

### Scoped’ı nasıl okumalıyım?

Scoped, web için en “doğal” olanıdır:

```
Request #A boyunca: aynı servis instance'ı -> A1 (hep aynı)
Request #B boyunca: yeni instance -> A2
```

Bu yüzden `DbContext` çoğunlukla scoped’tur: **request biter, context dispose olur**.

### Singleton’ı nasıl okumalıyım?

Singleton tek instance’dır:

```
Uygulama start -> A1 oluşturulur
Tüm request'ler -> hep aynı A1 kullanılır
Uygulama stop -> A1 dispose edilir
```

#### Singleton ne zaman mantıklı?

- Cache tutan, config tutan, thread-safe “paylaşımlı” servisler
- Çok pahalı kurulumu olan ve paylaşılabilen servisler

#### Singleton’da en kritik kural (çok önemli)

- Singleton içinde **Scoped** bir şeyi doğrudan kullanamazsın (örn. singleton içine `DbContext` almak yanlış).
- Çünkü singleton bütün uygulama boyunca yaşar; scoped ise request bitince ölür. Bu ikisi çakışır.

---

## Kısa özet

- **DI container**: bağımlılıkları üretir + enjekte eder + yaşam döngüsünü yönetir.
- **.NET’e özgü değil**, ama ASP.NET Core’da built‑in.
- `builder.Services.*` çağrıları: **DI kayıtları**.
- `AddMediatR(...)`: MediatR + handler + pipeline behavior kayıtları; MediatR kullanıyorsan **gerekli**.
- **Controller Scoped**: her request yeni controller instance; aynı request içinde aynı instance.


