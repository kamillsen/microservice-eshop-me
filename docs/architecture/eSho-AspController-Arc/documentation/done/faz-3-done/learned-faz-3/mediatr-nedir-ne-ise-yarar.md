# MediatR Nedir? Ne İşe Yarar? Neden Kullanılır?

> Bu dokümantasyon, MediatR'ın ne olduğunu, neden kullanıldığını, hangi sorunları çözdüğünü ve alternatiflerini detaylı olarak açıklar.

---

## 📖 İçindekiler

1. [MediatR Nedir?](#-mediatr-nedir)
2. [Ne İşe Yarar?](#-ne-işe-yarar)
3. [Neden Kullanılır?](#-neden-kullanılır)
4. [Hangi Sorunları Çözer?](#-hangi-sorunları-çözer)
5. [Neyi Kolaylaştırır?](#-neyi-kolaylaştırır)
6. [MediatR Nasıl Çalışır?](#-mediatr-nasıl-çalışır)
7. [Projelerde Kullanım](#-projelerde-kullanım)
8. [Alternatifler](#-alternatifler)
9. [Sonuç](#-sonuç)

---

## 🎯 MediatR Nedir?

**MediatR**, .NET uygulamalarında **Mediator Pattern**'i uygulayan açık kaynaklı bir kütüphanedir. Jimmy Bogard tarafından geliştirilmiştir ve .NET ekosisteminde yaygın olarak kullanılır.

### Kısa Tanım

MediatR, **Controller ile Handler arasında aracı (mediator) görevi gören bir kütüphanedir**. Controller'lar doğrudan business logic'e bağımlı olmak yerine, MediatR üzerinden Command/Query gönderir ve Handler'lar bu işlemleri yürütür.

### Temel Konsept

```
┌──────────────┐
│  Controller  │  ← HTTP isteklerini alır
└──────┬───────┘
       │
       │ Send(Command/Query)
       │
       ▼
┌──────────────┐
│   MediatR    │  ← Aracı (Mediator Pattern)
│  (Mediator)  │
└──────┬───────┘
       │
       │ Find Handler
       │ Execute
       │
       ▼
┌──────────────┐
│   Handler    │  ← Business Logic (İş mantığı)
└──────┬───────┘
       │
       │ Save/Query
       │
       ▼
┌──────────────┐
│  Database    │  ← Veritabanı işlemleri
└──────────────┘
```

---

## 🔧 Ne İşe Yarar?

### 1. In-Process Messaging (Süreç İçi Mesajlaşma)

MediatR, **aynı uygulama içinde** mesajlaşma sağlar. Controller'dan Handler'a komut/query gönderme işlemini yönetir.

**Örnek:**
```csharp
// Controller
await _mediator.Send(new CreateProductCommand { Name = "iPhone", Price = 35000 });

// MediatR → CreateProductHandler'ı bulur ve çalıştırır
```

### 2. CQRS Pattern Desteği

**CQRS (Command Query Responsibility Segregation)** pattern'ini kolayca uygulamanıza olanak sağlar:

- **Command**: Veriyi değiştiren işlemler (Create, Update, Delete)
- **Query**: Veriyi okuyan işlemler (Get, GetAll)

```csharp
// Command (Veriyi değiştirir)
public class CreateProductCommand : IRequest<Guid> { }
public class CreateProductHandler : IRequestHandler<CreateProductCommand, Guid> { }

// Query (Veriyi okur)
public class GetProductsQuery : IRequest<IEnumerable<ProductDto>> { }
public class GetProductsHandler : IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>> { }
```

### 3. Pipeline Behavior'lar

MediatR, **pipeline behavior** mekanizması ile cross-cutting concerns'leri (logging, validation, caching) merkezi olarak yönetmenizi sağlar.

```
Request
  │
  ────────────────┐
│ LoggingBehavior │  ← Request loglanır
└────────┬────────┘
         │
         ▼
┌──────────────────┐
│ValidationBehavior│  ← Request validate edilir
└────────┬─────────┘
         │
         ▼
┌─────────────────┐
│     Handler     │  ← İş mantığı çalışır
└────────┬────────┘
         │
         ▼
┌──────────────────┐
│ValidationBehavior│  ← Response validate edilir
└────────┬─────────┘
         │
         ▼
┌─────────────────┐
│ LoggingBehavior │  ← Response loglanır
└────────┬────────┘
         │
         ▼
Response
```▼
┌─

### 4. Decoupling (Bağlantıları Azaltma)

Controller'lar, Handler'ları doğrudan bilmez. Sadece MediatR üzerinden Command/Query gönderir.

**MediatR Olmadan (Tight Coupling):**
```csharp
public class ProductsController : ControllerBase
{
    private readonly CreateProductHandler _createHandler;  // ❌ Doğrudan bağımlı
    private readonly GetProductsHandler _getHandler;       // ❌ Doğrudan bağımlı
    
    public ProductsController(
        CreateProductHandler createHandler,
        GetProductsHandler getHandler)
    {
        _createHandler = createHandler;
        _getHandler = getHandler;
    }
    
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var command = new CreateProductCommand { ... };
        var result = await _createHandler.Handle(command, cancellationToken);  // ❌ Doğrudan çağrı
        return Ok(result);
    }
}
```

**MediatR ile (Loose Coupling):**
```csharp
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;  // ✅ Sadece MediatR'a bağımlı
    
    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var command = new CreateProductCommand { ... };
        var result = await _mediator.Send(command);  // ✅ MediatR üzerinden
        return Ok(result);
    }
}
```

---

## 🤔 Neden Kullanılır?

### 1. Separation of Concerns (Sorumlulukların Ayrılması)

Controller'lar sadece HTTP isteklerini yönetir, business logic Handler'larda toplanır.

```
┌─────────────────────────────────────────────────────┐
│              MediatR Olmadan                        │
├─────────────────────────────────────────────────────┤
│ Controller                                           │
│   ├─ HTTP isteklerini alır                         │
│   ├─ Business logic (❌ burası olmamalı)           │
│   ├─ Validation (❌ burası olmamalı)               │
│   ├─ Logging (❌ burası olmamalı)                  │
│   └─ Database işlemleri (❌ burası olmamalı)       │
│                                                     │
│ ❌ Her şey bir arada, test etmek zor               │
│ ❌ Kod tekrarı fazla                                │
│ ❌ Bakımı zor                                        │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│              MediatR ile                            │
├─────────────────────────────────────────────────────┤
│ Controller                                           │
│   └─ Sadece HTTP isteklerini yönetir (✅)          │
│                                                     │
│ Handler                                             │
│   └─ Business logic burada (✅)                    │
│                                                     │
│ ValidationBehavior                                  │
│   └─ Validation burada (✅)                        │
│                                                     │
│ LoggingBehavior                                     │
│   └─ Logging burada (✅)                           │
│                                                     │
│ ✅ Her şey ayrı, test etmek kolay                  │
│ ✅ Kod tekrarı yok                                   │
│ ✅ Bakımı kolay                                       │
└─────────────────────────────────────────────────────┘
```

### 2. Test Edilebilirlik (Testability)

Handler'lar bağımsız olarak test edilebilir, Controller'lara bağımlı değildir.

**Test Örneği:**
```csharp
// Handler testi (Controller'a ihtiyaç yok)
[Fact]
public async Task Handle_ValidCommand_ReturnsProductId()
{
    // Arrange
    var context = CreateInMemoryDbContext();
    var mapper = CreateMapper();
    var handler = new CreateProductHandler(context, mapper);
    var command = new CreateProductCommand { Name = "iPhone", Price = 35000 };
    
    // Act
    var productId = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    Assert.NotEqual(Guid.Empty, productId);
    var product = await context.Products.FindAsync(productId);
    Assert.NotNull(product);
    Assert.Equal("iPhone", product.Name);
}
```

### 3. Kod Organizasyonu (Code Organization)

CQRS pattern'i ile kod daha organize ve okunabilir hale gelir.

```
Proje Yapısı (MediatR ile):

Features/
├── Products/
│   ├── Commands/
│   │   ├── CreateProduct/
│   │   │   ├── CreateProductCommand.cs
│   │   │   ├── CreateProductHandler.cs
│   │   │   └── CreateProductValidator.cs
│   │   ├── UpdateProduct/
│   │   └── DeleteProduct/
│   └── Queries/
│       ├── GetProducts/
│       │   ├── GetProductsQuery.cs
│       │   └── GetProductsHandler.cs
│       └── GetProductById/
└── Categories/
    ├── Commands/
    └── Queries/
```

### 4. Cross-Cutting Concerns

Logging, validation, caching gibi işlemler merkezi olarak yönetilir.

**Avantaj:**
- ✅ Her handler'da ayrı ayrı logging yazmaya gerek yok
- ✅ Validation otomatik çalışır
- ✅ Yeni behavior eklemek kolay (sadece `AddBehavior` ile)

### 5. Type Safety (Tip Güvenliği)

Generic type system sayesinde compile-time'da tip kontrolü yapılır.

```csharp
// ✅ Tip güvenli
await _mediator.Send(new CreateProductCommand { ... });
// → CreateProductHandler bulunur

// ❌ Compile-time hatası
await _mediator.Send(new CreateProductCommand { ... });
// → GetProductsHandler bulunamaz (tip uyuşmazlığı)
```

---

## 🎯 Hangi Sorunları Çözer?

### Sorun 1: Controller'da Business Logic

**Sorun:**
```csharp
public class ProductsController : ControllerBase
{
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        // ❌ Business logic Controller'da
        if (dto.Price <= 0)
            return BadRequest("Price must be greater than 0");
        
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Price = dto.Price
        };
        
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        // ❌ Logging Controller'da
        _logger.LogInformation("Product created: {ProductId}", product.Id);
        
        return Ok(product.Id);
    }
}
```

**Çözüm (MediatR ile):**
```csharp
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var command = new CreateProductCommand 
        { 
            Name = dto.Name, 
            Price = dto.Price 
        };
        
        // ✅ Business logic Handler'da
        // ✅ Validation ValidationBehavior'da
        // ✅ Logging LoggingBehavior'da
        var productId = await _mediator.Send(command);
        
        return Ok(productId);
    }
}
```

### Sorun 2: Kod Tekrarı (DRY Violation)

**Sorun:**
Her endpoint'te aynı logging, validation kodları tekrar eder.

```csharp
// CreateProduct endpoint
_logger.LogInformation("Handling CreateProduct");
// validation...
// business logic...

// UpdateProduct endpoint
_logger.LogInformation("Handling UpdateProduct");  // ❌ Tekrar
// validation...  // ❌ Tekrar
// business logic...

// DeleteProduct endpoint
_logger.LogInformation("Handling DeleteProduct");  // ❌ Tekrar
// validation...  // ❌ Tekrar
// business logic...
```

**Çözüm (MediatR ile):**
```csharp
// LoggingBehavior → Tüm request'ler otomatik loglanır
// ValidationBehavior → Tüm request'ler otomatik validate edilir
// Handler → Sadece business logic
```

### Sorun 3: Test Edilebilirlik

**Sorun:**
Controller'da business logic varsa, test etmek için HTTP server başlatmak gerekir (integration test).

**Çözüm (MediatR ile):**
Handler'ları unit test ile test edebilirsiniz (daha hızlı, daha kolay).

### Sorun 4: Dependency Injection Karmaşası

**Sorun:**
Controller'da çok fazla dependency inject edilmesi gerekir.

```csharp
public class ProductsController : ControllerBase
{
    private readonly CreateProductHandler _createHandler;
    private readonly UpdateProductHandler _updateHandler;
    private readonly DeleteProductHandler _deleteHandler;
    private readonly GetProductsHandler _getHandler;
    private readonly GetProductByIdHandler _getByIdHandler;
    // ... daha fazla handler
    
    // ❌ Constructor çok kalabalık
    public ProductsController(
        CreateProductHandler createHandler,
        UpdateProductHandler updateHandler,
        DeleteProductHandler deleteHandler,
        GetProductsHandler getHandler,
        GetProductByIdHandler getByIdHandler
        // ...)
    {
        // ...
    }
}
```

**Çözüm (MediatR ile):**
```csharp
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;  // ✅ Sadece bir dependency
    
    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }
}
```

---

## ✨ Neyi Kolaylaştırır?

### 1. Handler Bulma ve Çalıştırma

MediatR, doğru Handler'ı otomatik bulur ve çalıştırır. Manuel eşleştirme yapmaya gerek yok.

```
┌─────────────────────────────────────────────────────┐
│   MediatR Olmadan (Manuel Eşleştirme)              │
├─────────────────────────────────────────────────────┤
│                                                      │
│  switch (commandType)                               │
│  {                                                  │
│      case CreateProductCommand:                     │
│          await _createHandler.Handle(command);      │
│          break;                                     │
│      case UpdateProductCommand:                     │
│          await _updateHandler.Handle(command);      │
│          break;                                     │
│      // ... her command için tekrar                 │
│  }                                                  │
│                                                      │
│  ❌ Manuel eşleştirme                               │
│  ❌ Yeni command eklenince switch'e ekleme gerekir │
│                                                      │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│   MediatR ile (Otomatik Eşleştirme)                │
├─────────────────────────────────────────────────────┤
│                                                      │
│  await _mediator.Send(command);                     │
│                                                      │
│  ✅ Otomatik Handler bulma (Generic type matching)  │
│  ✅ Yeni command eklenince otomatik çalışır         │
│                                                      │
└─────────────────────────────────────────────────────┘
```

### 2. Pipeline Behavior Ekleme

Yeni bir cross-cutting concern eklemek için sadece bir behavior yazıp pipeline'a eklemeniz yeterli.

**Örnek: Caching Behavior Ekleme**

```csharp
// 1. Behavior yaz
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Cache kontrolü
        if (cache.Contains(request))
            return cache.Get(request);
        
        // Handler çalıştır
        var response = await next();
        
        // Cache'e kaydet
        cache.Set(request, response);
        
        return response;
    }
}

// 2. Pipeline'a ekle (Program.cs)
cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

// ✅ Tüm handler'lar otomatik cache kullanır!
```

### 3. Validation

FluentValidation ile validation otomatik çalışır. Handler'da validation kodu yazmaya gerek yok.

```csharp
// ValidationBehavior otomatik çalışır
// CreateProductValidator otomatik bulunur ve çalıştırılır
var command = new CreateProductCommand { Name = "", Price = -1 };
await _mediator.Send(command);
// → ValidationException fırlatılır (Handler'a gitmez)
```

### 4. Logging

LoggingBehavior ile tüm request/response'lar otomatik loglanır.

```csharp
// LoggingBehavior otomatik çalışır
// Her request loglanır:
// "Handling CreateProductCommand: {Request}"
// Her response loglanır:
// "Handled CreateProductCommand: {Response}"
```

---

## ⚙️ MediatR Nasıl Çalışır?

### 1. Kayıt Süreci (Uygulama Başlangıcı)

```
┌────────────────────────────────────────────────────────┐
│  Program.cs - AddMediatR()                            │
├────────────────────────────────────────────────────────┤
│                                                         │
│  builder.Services.AddMediatR(cfg =>                    │
│  {                                                      │
│      cfg.RegisterServicesFromAssembly(assembly);       │
│      cfg.AddBehavior<LoggingBehavior>();               │
│      cfg.AddBehavior<ValidationBehavior>();            │
│  });                                                   │
│                                                         │
│  ═══════════════════════════════════════════════════   │
│                                                         │
│  1. Handler'ları Bul (Reflection)                     │
│     ┌─────────────────────────────────────────┐        │
│     │  Assembly'yi tara                      │        │
│     │  IRequestHandler<,> implement edenleri │        │
│     │  bul ve DI container'a kaydet          │        │
│     │                                         │        │
│     │  CreateProductHandler →                │        │
│     │    IRequestHandler<CreateProductCommand│        │
│     │                      , Guid>           │        │
│     │                                         │        │
│     │  GetProductsHandler →                  │        │
│     │    IRequestHandler<GetProductsQuery,   │        │
│     │                     IEnumerable<...>>  │        │
│     └─────────────────────────────────────────┘        │
│                                                         │
│  2. Behavior'ları Pipeline'a Ekle                     │
│     ┌─────────────────────────────────────────┐        │
│     │  LoggingBehavior (1. sıra)             │        │
│     │  ValidationBehavior (2. sıra)          │        │
│     │  Handler (3. sıra - otomatik bulunur)  │        │
│     └─────────────────────────────────────────┘        │
│                                                         │
└────────────────────────────────────────────────────────┘
```

### 2. Çalışma Zamanı (Runtime)

```
┌────────────────────────────────────────────────────────┐
│  Controller → MediatR.Send(Command)                    │
├────────────────────────────────────────────────────────┤
│                                                         │
│  1. Command Tipini Al                                  │
│     ┌─────────────────────────────────────────┐        │
│     │  command.GetType()                     │        │
│     │  → typeof(CreateProductCommand)        │        │
│     └─────────────────────────────────────────┘        │
│                                                         │
│  2. Handler Interface Tipini Oluştur                   │
│     ┌─────────────────────────────────────────┐        │
│     │  IRequestHandler<CreateProductCommand, │        │
│     │                  Guid>                 │        │
│     └─────────────────────────────────────────┘        │
│                                                         │
│  3. DI Container'dan Handler'ı Bul                     │
│     ┌─────────────────────────────────────────┐        │
│     │  serviceProvider.GetRequiredService(    │        │
│     │    IRequestHandler<CreateProductCommand,│        │
│     │                      Guid>)             │        │
│     │  → CreateProductHandler instance        │        │
│     └─────────────────────────────────────────┘        │
│                                                         │
│  4. Pipeline'ı Oluştur ve Çalıştır                     │
│     ┌─────────────────────────────────────────┐        │
│     │  Pipeline:                              │        │
│     │                                         │        │
│     │  LoggingBehavior                        │        │
│     │    ↓                                    │        │
│     │  ValidationBehavior                     │        │
│     │    ↓                                    │        │
│     │  CreateProductHandler                   │        │
│     │                                         │        │
│     │  Her behavior next() delegate'ini       │        │
│     │  çağırarak bir sonrakine geçer          │        │
│     └─────────────────────────────────────────┘        │
│                                                         │
│  5. Response Döndür                                    │
│     ┌─────────────────────────────────────────┐        │
│     │  Handler → Guid (Product ID)           │        │
│     │  ↓                                      │        │
│     │  ValidationBehavior → Guid              │        │
│     │  ↓                                      │        │
│     │  LoggingBehavior → Guid                 │        │
│     │  ↓                                      │        │
│     │  Controller → Ok(Guid)                  │        │
│     └─────────────────────────────────────────┘        │
│                                                         │
└────────────────────────────────────────────────────────┘
```

### 3. Pipeline Akışı (Detaylı)

```
Request (CreateProductCommand)
  │
  ▼
┌─────────────────────────────────┐
│  LoggingBehavior.Handle()       │
│                                  │
│  1. Request loglanır            │
│     "Handling CreateProductCommand: {...}" │
│                                  │
│  2. next() çağrılır             │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│  ValidationBehavior.Handle()    │
│                                  │
│  1. Validator'ı bul             │
│     IValidator<CreateProductCommand> │
│                                  │
│  2. ValidateAsync() çağrılır    │
│                                  │
│  3. Hata varsa → Exception      │
│     Hata yoksa → next() çağrılır │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│  CreateProductHandler.Handle()  │
│                                  │
│  1. Command'den Entity oluştur  │
│     _mapper.Map<Product>(command) │
│                                  │
│  2. Veritabanına kaydet         │
│     _context.Products.Add(product) │
│     await _context.SaveChangesAsync() │
│                                  │
│  3. Product ID döndür           │
│     return product.Id; (Guid)   │
└──────────────┬──────────────────┘
               │
               ▼
Response (Guid)
  │
  ▼ (Geri dönüş - ters sıra)
┌─────────────────────────────────┐
│  ValidationBehavior              │
│  → Response geçer (loglanmaz)   │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│  LoggingBehavior                │
│  → Response loglanır            │
│  "Handled CreateProductCommand: {Guid}" │
└──────────────┬──────────────────┘
               │
               ▼
Controller'a döner
```

---

## 🏢 Projelerde Kullanım

### Ne Zaman Kullanılmalı?

#### ✅ Kullanılması Gereken Durumlar

1. **CQRS Pattern Uygulanıyorsa**
   - Command ve Query ayrımı yapılıyorsa
   - Örnek: E-ticaret, CRM, ERP sistemleri

2. **Orta-Büyük Ölçekli Projeler**
   - 5+ endpoint'ten fazla
   - Karmaşık business logic
   - Çok sayıda handler

3. **Cross-Cutting Concerns Varsa**
   - Logging, validation, caching gibi işlemler merkezi yönetilecekse
   - Pipeline behavior'lardan yararlanılacaksa

4. **Test Edilebilirlik Önemliyse**
   - Handler'lar bağımsız test edilecekse
   - Unit test coverage yüksek olacaksa

5. **Kod Organizasyonu Önemliyse**
   - Feature-based klasör yapısı kullanılıyorsa
   - Vertical Slice Architecture tercih ediliyorsa

#### ❌ Kullanılmaması Gereken Durumlar

1. **Küçük/Basit Projeler**
   - 1-2 endpoint
   - Basit CRUD işlemleri
   - MediatR overhead'i gereksiz olur

2. **Performans Kritik Uygulamalar**
   - Reflection kullanımı minimal overhead yaratır (genelde önemsiz ama çok kritik ise dikkat)
   - Direct call daha hızlı olabilir

3. **Ekip MediatR Bilmiyorsa**
   - Öğrenme eğrisi var
   - Karmaşıklık artabilir (yanlış kullanılırsa)

### Endüstrideki Kullanım

MediatR, .NET ekosisteminde **yaygın olarak kullanılır**:

- ✅ **Microsoft** (bazı internal projelerinde)
- ✅ **Stack Overflow** (birçok projede)
- ✅ **GitHub Stars**: ~9.5k+ ⭐
- ✅ **NuGet Downloads**: Milyonlarca
- ✅ **Enterprise projelerde** sıkça kullanılır

### Avantajları (Proje Bazında)

| Avantaj | Açıklama |
|---------|----------|
| **Separation of Concerns** | Controller ve business logic ayrılır |
| **Testability** | Handler'lar kolay test edilir |
| **Code Organization** | Feature-based yapı kolaylaşır |
| **DRY Principle** | Kod tekrarı azalır |
| **Pipeline Behaviors** | Cross-cutting concerns merkezi yönetilir |
| **Type Safety** | Compile-time tip kontrolü |
| **Maintainability** | Kod bakımı kolaylaşır |

### Dezavantajları

| Dezavantaj | Açıklama |
|------------|----------|
| **Learning Curve** | Yeni ekip üyelerinin öğrenmesi gerekir |
| **Slight Overhead** | Reflection kullanımı minimal overhead yaratır (genelde önemsiz) |
| **Abstraction Layer** | Ek bir abstraction katmanı (debug zorlaşabilir) |
| **NuGet Dependency** | Harici bir kütüphane bağımlılığı |

---

## 🔄 Alternatifler

### 1. Direct Handler Injection

**Yaklaşım:** Handler'ları doğrudan DI container'dan inject etmek.

```csharp
public class ProductsController : ControllerBase
{
    private readonly CreateProductHandler _handler;
    
    public ProductsController(CreateProductHandler handler)
    {
        _handler = handler;
    }
    
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var command = new CreateProductCommand { ... };
        var result = await _handler.Handle(command, cancellationToken);
        return Ok(result);
    }
}
```

**Avantajlar:**
- ✅ Daha basit (ek kütüphane yok)
- ✅ Daha az abstraction
- ✅ Daha hızlı (reflection yok)

**Dezavantajlar:**
- ❌ Controller'da çok fazla dependency
- ❌ Pipeline behavior yok
- ❌ Manual handler bulma gerekir

**Ne Zaman Kullanılır:**
- Küçük projeler
- Basit CRUD işlemleri
- Pipeline behavior'a ihtiyaç yoksa

---

### 2. Custom Mediator Implementation

**Yaklaşım:** Kendi mediator pattern implementasyonunuzu yazmak.

```csharp
public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request);
}

public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        var handlerType = typeof(IRequestHandler<,>)
            .MakeGenericType(request.GetType(), typeof(TResponse));
        var handler = _serviceProvider.GetRequiredService(handlerType);
        // ... reflection ile Handle metodunu çağır
    }
}
```

**Avantajlar:**
- ✅ Tam kontrol (istediğiniz özelliği ekleyebilirsiniz)
- ✅ Harici bağımlılık yok

**Dezavantajlar:**
- ❌ Çok fazla kod yazmanız gerekir
- ❌ Test etmek zaman alır
- ❌ Pipeline behavior mekanizması yazmak karmaşık

**Ne Zaman Kullanılır:**
- Çok özel gereksinimler varsa
- MediatR'ın yeteneklerini aşan bir şey gerekiyorsa

---

### 3. Minimal API + Endpoint Handlers

**Yaklaşım:** ASP.NET Core Minimal API ile endpoint handler'lar kullanmak.

```csharp
app.MapPost("/api/products", async (CreateProductCommand command, CreateProductHandler handler) =>
{
    return await handler.Handle(command, cancellationToken);
});
```

**Avantajlar:**
- ✅ Daha az boilerplate kod
- ✅ Daha performanslı (minimal overhead)

**Dezavantajlar:**
- ❌ Pipeline behavior mekanizması yok (manuel eklenmeli)
- ❌ CQRS pattern'i manuel uygulanmalı
- ❌ Controller pattern'inin avantajları yok

**Ne Zaman Kullanılır:**
- Minimal API tercih ediliyorsa
- Çok basit endpoint'ler varsa

---

### 4. Service Locator Pattern

**Yaklaşım:** Handler'ları service locator ile bulmak (anti-pattern).

```csharp
public class ProductsController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var handler = _serviceProvider.GetRequiredService<CreateProductHandler>();
        // ...
    }
}
```

**Avantajlar:**
- ❌ Yok (anti-pattern)

**Dezavantajlar:**
- ❌ Dependency'ler gizlenir (test zorlaşır)
- ❌ Service locator anti-pattern
- ❌ Compile-time kontrolü yok

**Ne Zaman Kullanılır:**
- ❌ Hiçbir zaman (anti-pattern)

---

### 5. MassTransit (Message Bus)

**Yaklaşım:** Message bus pattern ile command/query göndermek.

```csharp
// Command gönderme
await _publishEndpoint.Publish(new CreateProductCommand { ... });

// Command işleme
public class CreateProductConsumer : IConsumer<CreateProductCommand>
{
    public async Task Consume(ConsumeContext<CreateProductCommand> context)
    {
        // Handler logic
    }
}
```

**Avantajlar:**
- ✅ Distributed systems için uygun
- ✅ Asenkron mesajlaşma
- ✅ Service-to-service iletişim

**Dezavantajlar:**
- ❌ Overkill (tek bir process içinde gereksiz)
- ❌ Daha karmaşık setup
- ❌ Message broker gerektirir (RabbitMQ, Azure Service Bus, vb.)

**Ne Zaman Kullanılır:**
- Microservice mimarisinde service-to-service iletişim
- Asenkron işlemler
- Event-driven architecture

**Not:** MediatR (in-process) ve MassTransit (distributed) **farklı amaçlar** için kullanılır. Birlikte de kullanılabilir:
- **MediatR**: Aynı service içinde (Controller → Handler)
- **MassTransit**: Service'ler arası (Basket Service → Ordering Service)

---

### Karşılaştırma Tablosu

| Yaklaşım | Karmaşıklık | Pipeline Behavior | CQRS Desteği | Kullanım Senaryosu |
|----------|-------------|-------------------|--------------|-------------------|
| **MediatR** | Orta | ✅ | ✅ | Orta-büyük projeler, CQRS |
| **Direct Handler Injection** | Düşük | ❌ | ❌ | Küçük projeler |
| **Custom Mediator** | Yüksek | Manuel | Manuel | Özel gereksinimler |
| **Minimal API** | Düşük | Manuel | Manuel | Minimal API projeleri |
| **MassTransit** | Yüksek | ✅ (farklı) | ✅ (farklı) | Distributed systems |

---

## 📊 MediatR vs Alternatifler - Senaryo Bazında

### Senaryo 1: Küçük REST API (5-10 endpoint)

```
┌──────────────────────────────────────────────┐
│  Öneri: Direct Handler Injection            │
├──────────────────────────────────────────────┤
│  ✅ Basit ve hızlı                           │
│  ✅ MediatR overhead'i gereksiz              │
│  ✅ Pipeline behavior'a ihtiyaç yok          │
│                                              │
│  Controller                                  │
│    └─ Handler (direct injection)            │
└──────────────────────────────────────────────┘
```

### Senaryo 2: Orta-Büyük REST API (20+ endpoint)

```
┌──────────────────────────────────────────────┐
│  Öneri: MediatR                              │
├──────────────────────────────────────────────┤
│  ✅ CQRS pattern                             │
│  ✅ Pipeline behaviors                       │
│  ✅ Code organization                        │
│                                              │
│  Controller                                  │
│    └─ MediatR                                │
│        └─ Pipeline (Logging, Validation)    │
│            └─ Handler                        │
└──────────────────────────────────────────────┘
```

### Senaryo 3: Microservice (Service-to-Service)

```
┌──────────────────────────────────────────────┐
│  Öneri: MediatR + MassTransit                │
├──────────────────────────────────────────────┤
│  ✅ MediatR: In-process (Controller→Handler) │
│  ✅ MassTransit: Service-to-Service          │
│                                              │
│  Service A                                   │
│    Controller → MediatR → Handler            │
│                ↓                             │
│         MassTransit Publish                  │
│                ↓                             │
│  Service B                                   │
│         MassTransit Consume                  │
│                ↓                             │
│         MediatR → Handler                    │
└──────────────────────────────────────────────┘
```

---

## ✅ Sonuç

### MediatR Ne Zaman Kullanılmalı?

**Kullanın:**
- ✅ CQRS pattern uyguluyorsanız
- ✅ Orta-büyük ölçekli projeler
- ✅ Cross-cutting concerns (logging, validation) merkezi yönetilecekse
- ✅ Test edilebilirlik önemliyse
- ✅ Kod organizasyonu önemliyse

**Kullanmayın:**
- ❌ Çok küçük/basit projeler (1-2 endpoint)
- ❌ Performans kritik uygulamalar (ama genelde sorun olmaz)
- ❌ Ekip MediatR bilmiyorsa (ama öğrenilebilir)

### Özet

MediatR, **Mediator Pattern**'i .NET'e getiren, **CQRS pattern**'ini kolaylaştıran, **pipeline behavior** mekanizması ile cross-cutting concerns'leri merkezi olarak yönetmenizi sağlayan bir kütüphanedir.

**Temel Avantajlar:**
- ✅ Separation of Concerns
- ✅ Testability
- ✅ Code Organization
- ✅ Pipeline Behaviors
- ✅ Type Safety

**Endüstride Yaygın Kullanım:**
- ✅ Enterprise projelerde sıkça kullanılır
- ✅ .NET ekosisteminde popüler
- ✅ GitHub'da 9.5k+ ⭐

**Alternatifler:**
- Direct Handler Injection (küçük projeler)
- Custom Mediator (özel gereksinimler)
- Minimal API (minimal API projeleri)
- MassTransit (distributed systems - farklı amaç)

MediatR, doğru kullanıldığında kod kalitesini ve maintainability'yi artıran güçlü bir araçtır.

---

**Son Güncelleme:** Aralık 2024

