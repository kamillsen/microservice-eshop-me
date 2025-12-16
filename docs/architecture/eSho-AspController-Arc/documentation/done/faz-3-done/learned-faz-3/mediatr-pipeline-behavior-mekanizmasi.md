# MediatR Pipeline Behavior Mekanizması - Detaylı Açıklama

> Bu dokümantasyon, MediatR Pipeline Behavior'ların nasıl çalıştığını, Program.cs'de yaptığımız kayıtların ne sağladığını ve arka planda olanları detaylı olarak açıklar.

---

## Soru: Behavior'ları Yazdık Ama Hiçbir Yerde Kullanmıyoruz, Nasıl Otomatik Çalışıyor?

Bu soru çok önemli! Behavior'ları yazdık ama Controller'da veya Handler'da manuel olarak çağırmıyoruz. Peki nasıl otomatik çalışıyor?

**Cevap:** MediatR, her `_mediator.Send()` çağrısında otomatik olarak pipeline'ı çalıştırıyor. Biz sadece behavior'ları pipeline'a kaydediyoruz, MediatR gerisini hallediyor.

---

## 1. Program.cs'de Ne Yapıyoruz?

### Kod (Program.cs, Satır 14-20):

```csharp
// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});
```

### Bu Kod Ne Yapıyor?

1. **`RegisterServicesFromAssembly`**: `IRequestHandler<TRequest, TResponse>` interface'ini implement eden handler'ları otomatik bulup DI container'a kaydediyor
2. **`AddBehavior<LoggingBehavior>`**: LoggingBehavior'ı pipeline'a ekliyor
3. **`AddBehavior<ValidationBehavior>`**: ValidationBehavior'ı pipeline'a ekliyor

**Önemli:** Bu satırlar sadece **kayıt** yapıyor. Behavior'lar henüz çalışmıyor. Sadece "şu behavior'lar var, pipeline'a eklensin" diyoruz.

---

## 2. Gerçek Senaryo: CreateProduct İsteği

### Senaryo: Kullanıcı yeni bir ürün oluşturmak istiyor

**HTTP Request:**
```
POST /api/products
Content-Type: application/json

{
  "name": "iPhone 15",
  "description": "Apple iPhone 15 128GB",
  "price": 35000.00,
  "imageUrl": "https://example.com/images/iphone15.jpg",
  "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### Controller (Gelecekte oluşturulacak - ProductsController.cs):

```csharp
using MediatR;
using Catalog.API.Features.Products.Commands.CreateProduct;
using Catalog.API.Dtos;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;  // DI container'dan IMediator alınıyor
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateProduct(CreateProductDto dto)
    {
        // 1. DTO'dan Command oluştur
        var command = new CreateProductCommand
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            ImageUrl = dto.ImageUrl,
            CategoryId = dto.CategoryId
        };

        // 2. MediatR'a gönder ← BURADA MAGIC BAŞLIYOR!
        var productId = await _mediator.Send(command);

        // 3. Response döndür
        return CreatedAtAction(nameof(GetProductById), new { id = productId }, productId);
    }
}
```

**Kritik Nokta:** `await _mediator.Send(command)` çağrıldığında MediatR pipeline'ı devreye giriyor.

---

## 3. MediatR'ın İçinde Ne Oluyor? (Arka Plan)

`_mediator.Send(command)` çağrıldığında MediatR şu adımları izler:

1. **Handler'ı bulur** (Bkz: [3.1. MediatR Handler Bulma Mekanizması](#31-mediatr-handler-bulma-mekanizması-doğru-handler-nasıl-bulunuyor))
2. **Pipeline behavior'ları bulur** (Program.cs'deki `AddBehavior` sayesinde)
3. **Pipeline zincirini oluşturur** (Bkz: [3.2. Pipeline Sırasını Ne Belirliyor?](#32-pipeline-sırasını-ne-belirliyor))
4. **Pipeline'ı çalıştırır**

**Sonuç:** Şöyle bir zincir oluşuyor:
```
LoggingBehavior → ValidationBehavior → CreateProductHandler
```

Detaylı açıklamalar için aşağıdaki bölümlere bakın.

---

## 3.1. MediatR Handler Bulma Mekanizması: Doğru Handler Nasıl Bulunuyor?

**Cevap:** Program.cs'deki `AddBehavior` çağrılarının sırası pipeline sırasını belirler.

#### 1. Ekleme Sırası = Pipeline Sırası

**Program.cs'deki Gerçek Kod (Satır 14-20):**

```csharp
// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));     // Satır 18: 1. Eklendi → İlk çalışır
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)); // Satır 19: 2. Eklendi → İkinci çalışır
});
```

**Dosya:** `src/Services/Catalog/Catalog.API/Program.cs`

**Ekleme Sırası:**
1. **Satır 18**: `LoggingBehavior<,>` eklendi → **İlk çalışır** (dışta)
2. **Satır 19**: `ValidationBehavior<,>` eklendi → **İkinci çalışır** (ortada)
3. **Handler**: Otomatik bulunur → **En son çalışır** (içte)

**Pipeline Sırası (Çalışma Sırası):**
```
1. LoggingBehavior (Satır 18'de eklendi → İlk çalışır - dışta)
2. ValidationBehavior (Satır 19'da eklendi → İkinci çalışır - ortada)
3. Handler (RegisterServicesFromAssembly ile bulunur → En son çalışır - içte)
```

#### 2. MediatR İçinde Nasıl Wrap Ediliyor?

MediatR, behavior'ları **ters sırada wrap eder** (içten dışa, Decorator Pattern):

```csharp
// MediatR içinde (basitleştirilmiş)
// Behavior'lar ekleme sırasına göre: [LoggingBehavior, ValidationBehavior]

// 1. Handler delegate'i oluştur (en içte)
var handlerDelegate = () => handler.Handle(request);

// 2. ValidationBehavior'ı wrap et (ikinci eklendi, ortada)
handlerDelegate = () => validationBehavior.Handle(request, handlerDelegate);

// 3. LoggingBehavior'ı wrap et (ilk eklendi, dışta)
handlerDelegate = () => loggingBehavior.Handle(request, handlerDelegate);

// Sonuç: LoggingBehavior → ValidationBehavior → Handler
```

**Görsel:**
```
┌─────────────────┐
│ LoggingBehavior │  ← Dışta (ilk çalışır)
└────────┬────────┘
         │ wraps
┌────────▼────────┐
│ValidationBehavior│  ← Ortada (ikinci çalışır)
└────────┬────────┘
         │ wraps
┌────────▼────────┐
│     Handler     │  ← İçte (en son çalışır)
└─────────────────┘
```

#### 3. Sırayı Değiştirirsek Ne Olur?

##### Senaryo 1: Mevcut Sıra (Logging → Validation) ✅ Önerilen

**Program.cs (Satır 18-19) - Mevcut Kod:** (Bkz: [Bölüm 3.2.1 - Ekleme Sırası](#1-ekleme-sırası--pipeline-sırası))

**Pipeline sırası:**
```
1. LoggingBehavior → Request loglanır
2. ValidationBehavior → Request validate edilir
3. Handler → İş mantığı çalışır
4. ValidationBehavior → Response geçer (loglanmaz)
5. LoggingBehavior → Response loglanır
```

**Avantajları:**
- ✅ Her request loglanır (validation hatası olsa bile)
- ✅ Hatalı request'ler de loglanır (debug için yararlı)
- ✅ Validation hatası olsa bile request görülebilir

##### Senaryo 2: Ters Sıra (Validation → Logging)

**Program.cs (Satır 18-19) - Alternatif Sıra:**
```csharp
// Program.cs - src/Services/Catalog/Catalog.API/Program.cs
// Not: Mevcut kodun tersi - sadece sıra değişiyor
cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)); // Satır 18: 1. (değişiklik)
cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));     // Satır 19: 2. (değişiklik)
```

**Pipeline sırası:**
```
1. ValidationBehavior → Request validate edilir
2. LoggingBehavior → Request loglanır
3. Handler → İş mantığı çalışır
4. LoggingBehavior → Response loglanır
5. ValidationBehavior → Response geçer (loglanmaz)
```

**Fark:** Validation önce çalışır, hata varsa LoggingBehavior'a geçmez (hatalı request'ler loglanmaz).

**Avantajları:**
- ✅ Geçersiz request'ler loglanmaz (performans)
- ✅ Sadece geçerli request'ler loglanır

**Dezavantajları:**
- ❌ Hatalı request'ler loglanmaz (debug zorlaşır)
- ❌ Validation hatası olsa bile request görülemez

#### 4. Özet: Pipeline Sırasını Ne Belirliyor?

**Cevap:** Program.cs'deki `AddBehavior` çağrılarının sırası

- **İlk eklenen** → İlk çalışır (dışta)
- **İkinci eklenen** → İkinci çalışır (ortada)
- **Handler** → En son çalışır (içte)

**Program.cs'deki Gerçek Kod:** (Bkz: [Bölüm 3.2.1 - Ekleme Sırası](#1-ekleme-sırası--pipeline-sırası))

**Pipeline Sırası:**
```
LoggingBehavior (Satır 18) → ValidationBehavior (Satır 19) → Handler
```

---

## 3.1. MediatR Handler Bulma Mekanizması: Doğru Handler Nasıl Bulunuyor?

### Soru: Command Tipine Göre Handler Nasıl Bulunuyor?

Controller'dan `_mediator.Send(command)` çağrıldığında, MediatR doğru handler'ı nasıl buluyor?

**Cevap:** Generic type matching ile. Command'in tipine göre `IRequestHandler<TRequest, TResponse>` tipindeki handler'ı arıyor.

---

### 1. Command Tipi

```csharp
// Controller'dan
var command = new CreateProductCommand { ... };
// command'in tipi: CreateProductCommand

await _mediator.Send(command);
// MediatR'a CreateProductCommand tipinde bir request gönderiliyor
```

---

### 2. MediatR'ın İçinde Ne Oluyor?

```csharp
// MediatR içinde (basitleştirilmiş)
public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
{
    // 1. Request'in tipini al
    var requestType = request.GetType();  
    // → typeof(CreateProductCommand)
    
    // 2. Handler interface tipini oluştur
    var handlerInterfaceType = typeof(IRequestHandler<,>)
        .MakeGenericType(requestType, typeof(TResponse));
    // → IRequestHandler<CreateProductCommand, Guid>
    
    // 3. DI container'dan bu tipi al
    var handler = serviceProvider.GetRequiredService(handlerInterfaceType);
    // → CreateProductHandler instance'ı döner
    
    // 4. Handler'ı çalıştır
    return await handler.Handle(request, cancellationToken);
}
```

---

### 3. Nasıl Eşleşiyor? (Adım Adım)

#### Adım 1: Command Tipi
```csharp
var command = new CreateProductCommand { ... };
// Tip: CreateProductCommand
```

#### Adım 2: MediatR Handler Interface'ini Oluşturur
```csharp
// MediatR içinde
IRequestHandler<CreateProductCommand, Guid>
//              ↑ TRequest (Command tipi)
//                              ↑ TResponse (dönecek tip)
```

#### Adım 3: DI Container'dan Handler'ı Bulur
```csharp
// DI container'da kayıtlı olan (RegisterServicesFromAssembly sayesinde):
IRequestHandler<CreateProductCommand, Guid> → CreateProductHandler

// MediatR arıyor:
IRequestHandler<CreateProductCommand, Guid> → ✅ BULUNDU! → CreateProductHandler
```

---

### 4. Gerçek Örnek: Projemizdeki Handler

#### CreateProductCommand → CreateProductHandler

**Command (Dosya: `Catalog.API/Features/Products/Commands/CreateProduct/CreateProductCommand.cs`):**
```csharp
public class CreateProductCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryId { get; set; }
}
```

**Handler (Dosya: `Catalog.API/Features/Products/Commands/CreateProduct/CreateProductHandler.cs`):**
```csharp
public class CreateProductHandler : IRequestHandler<CreateProductCommand, Guid>
//                                  ↑ TRequest = CreateProductCommand
//                                                  ↑ TResponse = Guid
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // İş mantığı
        return product.Id;
    }
}
```

**Eşleşme:**
- Command tipi: `CreateProductCommand`
- Handler interface: `IRequestHandler<CreateProductCommand, Guid>`
- Handler class: `CreateProductHandler`
- **Eşleşme:** ✅ Evet

---

### 5. Farklı Command'lar → Farklı Handler'lar

#### Örnek 1: CreateProductCommand
```csharp
var command = new CreateProductCommand { Name = "iPhone 15", ... };
await _mediator.Send(command);
// → IRequestHandler<CreateProductCommand, Guid> aranır
// → CreateProductHandler bulunur ✅
```

#### Örnek 2: GetProductsQuery
```csharp
var query = new GetProductsQuery { PageNumber = 1, PageSize = 10 };
var products = await _mediator.Send(query);
// → IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>> aranır
// → GetProductsHandler bulunur ✅
```

#### Örnek 3: CreateCategoryCommand
```csharp
var command = new CreateCategoryCommand { Name = "Elektronik" };
var categoryId = await _mediator.Send(command);
// → IRequestHandler<CreateCategoryCommand, Guid> aranır
// → CreateCategoryHandler bulunur ✅
```

---

### 6. Generic Type Matching (Tip Eşleştirmesi)

MediatR, **generic type matching** kullanır:

```csharp
// Command tipi: CreateProductCommand
// Handler interface: IRequestHandler<CreateProductCommand, Guid>
// Handler class: CreateProductHandler : IRequestHandler<CreateProductCommand, Guid>

// Eşleşme kontrolü:
typeof(CreateProductCommand) == typeof(CreateProductCommand) ✅
typeof(Guid) == typeof(Guid) ✅
// → Handler bulundu!
```

**Neden Generic Type Matching?**
- ✅ **Type-safe**: Compile-time'da tip kontrolü
- ✅ **Otomatik**: Manuel eşleştirme gerekmez
- ✅ **Esnek**: Her Command/Query için farklı Handler olabilir

---

### 7. Özet: Handler Nasıl Bulunuyor?

1. **Command tipi**: `CreateProductCommand`
2. **MediatR handler interface'ini oluşturur**: `IRequestHandler<CreateProductCommand, Guid>`
3. **DI container'dan bu interface tipini arar**
4. **`CreateProductHandler` bulunur** (çünkü `IRequestHandler<CreateProductCommand, Guid>` implement ediyor)
5. **Handler çalıştırılır**

**Sonuç:** MediatR, command'in tipine göre doğru handler'ı bulur:
- `CreateProductCommand` → `IRequestHandler<CreateProductCommand, Guid>` → `CreateProductHandler`
- `GetProductsQuery` → `IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>` → `GetProductsHandler`

Generic type matching sayesinde otomatik eşleşir.

---

## 3.2. Pipeline Sırasını Ne Belirliyor?

### Gerçek Kod Akışı:

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. LoggingBehavior.Handle() başlar                             │
│    Dosya: BuildingBlocks.Behaviors/Behaviors/LoggingBehavior.cs │
│                                                                   │
│    var requestName = typeof(CreateProductCommand).Name;          │
│    // → "CreateProductCommand"                                   │
│                                                                   │
│    _logger.LogInformation(                                       │
│        "Handling CreateProductCommand: {@Request}",              │
│        command);  // ← REQUEST LOGLANIR                          │
│                                                                   │
│    // ↓ next() çağrılır → ValidationBehavior'a geçer            │
│                                                                   │
│    ┌─────────────────────────────────────────────────────────┐ │
│    │ 2. ValidationBehavior.Handle() başlar                    │ │
│    │    Dosya: BuildingBlocks.Behaviors/Behaviors/            │ │
│    │           ValidationBehavior.cs                          │ │
│    │                                                            │ │
│    │    // Validator'ları DI container'dan al                 │ │
│    │    var validators = _validators;                          │ │
│    │    // → [CreateProductValidator] döner                    │ │
│    │                                                            │ │
│    │    // CreateProductValidator'ı çalıştır                  │ │
│    │    var result = await validator.ValidateAsync(command);   │ │
│    │                                                            │ │
│    │    // Validation kuralları kontrol edilir:                │ │
│    │    // - Name: NotEmpty, MaximumLength(100) ✅            │ │
│    │    // - Price: GreaterThan(0) ✅                          │ │
│    │    // - CategoryId: NotEmpty ✅                           │ │
│    │                                                            │ │
│    │    if (result.IsValid == false)                           │ │
│    │    {                                                       │ │
│    │        throw new ValidationException(...);  // ← Hata varsa │
│    │    }                                                       │ │
│    │                                                            │ │
│    │    // ✅ Validation başarılı, next() çağrılır → Handler'a │ │
│    │                                                            │ │
│    │    ┌─────────────────────────────────────────────────┐  │ │
│    │    │ 3. CreateProductHandler.Handle() başlar          │  │ │
│    │    │    Dosya: Catalog.API/Features/Products/         │  │ │
│    │    │           Commands/CreateProduct/                │  │ │
│    │    │           CreateProductHandler.cs                 │  │ │
│    │    │                                                    │  │ │
│    │    │    // 1. Command'den Entity oluştur              │  │ │
│    │    │    var product = _mapper.Map<Product>(request);   │  │ │
│    │    │    product.Id = Guid.NewGuid();                   │  │ │
│    │    │                                                    │  │ │
│    │    │    // 2. Veritabanına ekle                        │  │ │
│    │    │    _context.Products.Add(product);                │  │ │
│    │    │    await _context.SaveChangesAsync();              │  │ │
│    │    │    // → PostgreSQL'e INSERT SQL çalışır           │  │ │
│    │    │                                                    │  │ │
│    │    │    // 3. Product ID döndür                         │  │ │
│    │    │    return product.Id;  // → Guid döner            │  │ │
│    │    └─────────────────────────────────────────────────┘  │ │
│    │                                                            │ │
│    │    // Handler'dan dönen Guid alınır                      │ │
│    │    return await next();  // → Guid döner               │ │
│    └─────────────────────────────────────────────────────────┘ │
│                                                                   │
│    // ValidationBehavior'dan dönen Guid alınır                   │
│    var response = await next();  // → Guid döner                 │
│                                                                   │
│    _logger.LogInformation(                                       │
│        "Handled CreateProductCommand: {@Response}",              │
│        response);  // ← RESPONSE LOGLANIR                        │
│                                                                   │
│    return response;  // → Guid döner                             │
└─────────────────────────────────────────────────────────────────┘
```

---

## 3.3. Handler İçinde AutoMapper Nasıl Çalışıyor?

### Soru: `_mapper.Map<Product>(request)` Nasıl Doğru Mapping Kuralını Buluyor?

Handler içinde (Satır 483) şu kod var:

```csharp
var product = _mapper.Map<Product>(request);
```

Bu satır çalıştığında AutoMapper nasıl `CreateProductCommand → Product` mapping kuralını buluyor? Program.cs'de ne yapıldı ki bu otomatik çalışıyor?

---

### 1. Program.cs'de Ne Yapıyoruz? (Satır 24)

**Kod:**
```csharp
builder.Services.AddAutoMapper(typeof(Program).Assembly);
```

**Bu Satır Ne Yapıyor?**

#### 1.1. Assembly'yi Alıyor

```csharp
// AutoMapper içinde
Assembly assembly = typeof(Program).Assembly;
// → Catalog.API.dll assembly'si alınır
```

#### 1.2. Assembly'deki Tüm Class'ları Tarıyor

```csharp
// AutoMapper içinde
var allTypes = assembly.GetTypes()
    .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
    .ToList();
// → [Program, MappingProfile, Product, ProductDto, CreateProductHandler, ...]
```

#### 1.3. Profile Sınıfını Miras Alan Class'ları Arıyor

```csharp
// AutoMapper içinde
foreach (var type in allTypes)
{
    // Bu class Profile sınıfını miras alıyor mu?
    if (typeof(Profile).IsAssignableFrom(type))
    {
        // ✅ EVET! MappingProfile bulundu
        // → new MappingProfile() yapılır
    }
}
```

**Kontrol:**
- `typeof(Profile).IsAssignableFrom(typeof(MappingProfile))` → `true` ✅
  - Çünkü: `MappingProfile : Profile` (kalıtım var)
- `typeof(Profile).IsAssignableFrom(typeof(Product))` → `false` ❌
  - Çünkü: `Product` Profile'dan türemiyor

**Sonuç:** AutoMapper, assembly'deki tüm class'ları tarar ve **Profile sınıfını miras alan (inherit eden) class'ları** bulur. Bizim projemizde sadece `MappingProfile` bu kriteri sağlar.

#### 1.4. MappingProfile Instance'ı Oluşturuyor

```csharp
// AutoMapper içinde
var profileInstance = Activator.CreateInstance(typeof(MappingProfile));
// → new MappingProfile() çağrısı yapılır
```

Bu noktada `MappingProfile` constructor çalışır.

---

### 2. MappingProfile Constructor Çalışıyor

**Ne Zaman Çalışır?**

`AddAutoMapper` içinde `new MappingProfile()` çağrıldığında.

**Kod (MappingProfile.cs, Satır 12-23):**

```csharp
public MappingProfile()
{
    // Command → Entity
    CreateMap<CreateProductCommand, Product>();
    CreateMap<UpdateProductCommand, Product>();
    CreateMap<CreateCategoryCommand, Category>();
    
    // Entity → DTO
    CreateMap<Product, ProductDto>()
        .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty));
    CreateMap<Category, CategoryDto>();
}
```

**Her `CreateMap` Ne Yapıyor?**

#### Satır 15: `CreateMap<CreateProductCommand, Product>();`

**Ne yapıyor:**
- `CreateProductCommand` → `Product` mapping kuralını kaydeder.

**Nasıl çalışıyor:**
```csharp
// AutoMapper içinde (basitleştirilmiş)
public IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
{
    // 1. Mapping kuralını oluştur
    var mappingExpression = new MappingExpression<CreateProductCommand, Product>();
    
    // 2. Property eşleştirmelerini otomatik yap (convention-based)
    // → Aynı isimli property'leri eşleştir:
    //   CreateProductCommand.Name → Product.Name
    //   CreateProductCommand.Description → Product.Description
    //   CreateProductCommand.Price → Product.Price
    //   CreateProductCommand.ImageUrl → Product.ImageUrl
    //   CreateProductCommand.CategoryId → Product.CategoryId
    
    // 3. Mapping kuralını configuration'a kaydet
    // → Kaynak tip: CreateProductCommand
    // → Hedef tip: Product
    configuration.AddMapping(mappingExpression);
    
    return mappingExpression;
}
```

**Nereye kaydediliyor:**
```
┌─────────────────────────────────────────────┐
│ AutoMapper Configuration (Memory'de)        │
│                                             │
│ Mapping Kuralları:                         │
│  ✅ CreateProductCommand → Product         │
│     Kaynak Tip: CreateProductCommand        │
│     Hedef Tip: Product                      │
│     Property Eşleştirmeleri:                │
│       • Name → Name                         │
│       • Description → Description            │
│       • Price → Price                       │
│       • ImageUrl → ImageUrl                  │
│       • CategoryId → CategoryId             │
└─────────────────────────────────────────────┘
```

---

### 3. IMapper Servisi DI Container'a Ekleniyor

**Ne Zaman?**

Constructor çalıştıktan ve tüm `CreateMap` kuralları kaydedildikten sonra.

**Ne Yapıyor?**

```csharp
// AutoMapper içinde
builder.Services.AddSingleton<IMapper>(sp =>
{
    // 1. Tüm mapping kurallarını içeren configuration'ı oluştur
    var configuration = new MapperConfiguration(cfg =>
    {
        cfg.AddProfile(profileInstance); // MappingProfile'daki tüm kurallar
    });
    
    // 2. IMapper instance'ı oluştur
    var mapper = configuration.CreateMapper();
    
    // 3. DI container'a ekle
    return mapper;
});
```

**Sonuç:**
```
┌─────────────────────────────────────────────┐
│ DI Container (Service Provider)            │
│                                             │
│ Servisler:                                 │
│  ✅ IMapper → Mapper instance              │
│     └─ İçinde tüm mapping kuralları var   │
│        • CreateProductCommand → Product     │
│        • UpdateProductCommand → Product    │
│        • CreateCategoryCommand → Category   │
│        • Product → ProductDto              │
│        • Category → CategoryDto            │
└─────────────────────────────────────────────┘
```

---

### 4. Handler'da `_mapper.Map<Product>(request)` Çağrıldığında Ne Oluyor?

**Kod (CreateProductHandler.cs, Satır 22):**

```csharp
var product = _mapper.Map<Product>(request);
//    ↑ IMapper (DI'dan geliyor, içinde tüm mapping kuralları var)
//              ↑ Hedef tip (Product)
//                        ↑ Kaynak (CreateProductCommand)
```

**Kritik Soru: AutoMapper Hangi Mapping Kuralını Kullanacak?**

AutoMapper, kaynak ve hedef tiplere göre doğru mapping kuralını bulur:

```csharp
// AutoMapper içinde
public TDestination Map<TDestination>(object source)
{
    // 1. Kaynak tipi al
    Type sourceType = source.GetType();
    // → typeof(CreateProductCommand)
    
    // 2. Hedef tipi al
    Type destinationType = typeof(TDestination);
    // → typeof(Product)
    
    // 3. Mapping kuralını bul (configuration'dan)
    // → Kaynak tip: CreateProductCommand
    // → Hedef tip: Product
    // → Bu iki tip eşleşen kuralı ara
    var mapping = configuration.GetAllTypeMaps()
        .FirstOrDefault(m => 
            m.SourceType == typeof(CreateProductCommand) && 
            m.DestinationType == typeof(Product));
    // → ✅ BULUNDU! (MappingProfile constructor'ında kaydedilmişti)
    
    // 4. Yeni Product instance'ı oluştur
    var product = new Product();
    
    // 5. Property'leri eşleştir (kaydedilmiş kurallara göre)
    product.Name = request.Name;
    // → "iPhone 15"
    
    product.Description = request.Description;
    // → "Apple iPhone 15 128GB"
    
    product.Price = request.Price;
    // → 35000.00m
    
    product.ImageUrl = request.ImageUrl;
    // → "https://example.com/iphone15.jpg"
    
    product.CategoryId = request.CategoryId;
    // → Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6")
    
    // 6. Product instance'ı döndür
    return product;
}
```

**Görsel: Mapping Kuralı Bulma Süreci**

```
┌─────────────────────────────────────────────────────────┐
│ _mapper.Map<Product>(request) çağrıldı                  │
│ (Handler içinde, Satır 483)                              │
│                                                           │
│ 1. Kaynak tip: CreateProductCommand                      │
│ 2. Hedef tip: Product                                    │
│                                                           │
│ 3. Configuration'daki kuralları ara:                     │
│  ┌────────────────────────────────────────────────┐     │
│  │ Kural 1: CreateProductCommand → Product        │     │
│  │   Kaynak: CreateProductCommand ✅                │     │
│  │   Hedef: Product ✅                              │     │
│  │   → EŞLEŞTİ! ✅                                  │     │
│  └────────────────────────────────────────────────┘     │
│                                                           │
│  ┌────────────────────────────────────────────────┐     │
│  │ Kural 2: UpdateProductCommand → Product         │     │
│  │   Kaynak: UpdateProductCommand ❌                │     │
│  │   Hedef: Product ✅                              │     │
│  │   → EŞLEŞMEDİ! ❌                                │     │
│  └────────────────────────────────────────────────┘     │
│                                                           │
│  ┌────────────────────────────────────────────────┐     │
│  │ Kural 3: Product → ProductDto                  │     │
│  │   Kaynak: Product ❌                            │     │
│  │   Hedef: ProductDto ❌                           │     │
│  │   → EŞLEŞMEDİ! ❌                                │     │
│  └────────────────────────────────────────────────┘     │
│                                                           │
│ ✅ SONUÇ: Kural 1 kullanılacak!                         │
│    CreateProductCommand → Product                       │
│                                                           │
│ 4. Property'ler eşleştirilir:                           │
│    • Name → Name                                        │
│    • Description → Description                          │
│    • Price → Price                                      │
│    • ImageUrl → ImageUrl                                 │
│    • CategoryId → CategoryId                            │
│                                                           │
│ 5. Product instance'ı döndürülür                        │
└─────────────────────────────────────────────────────────┘
```

**Sonuç:**

```csharp
var product = new Product
{
    Id = Guid.Empty,  // (henüz set edilmedi)
    Name = "iPhone 15",
    Description = "Apple iPhone 15 128GB",
    Price = 35000.00m,
    ImageUrl = "https://example.com/iphone15.jpg",
    CategoryId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    Category = null  // (Command'de yok, ignore edilir)
};
```

---

### 5. Özet: AutoMapper'ın Çalışma Süreci

**Kodun Çalışma Sırası:**

```
1. Uygulama Başlar
   └─ Program.cs çalışır

2. Satır 24: AddAutoMapper(typeof(Program).Assembly)
   └─ Assembly'yi alır
   └─ Tüm class'ları tarar
   └─ Profile sınıfını miras alan class'ları bulur (MappingProfile)
   └─ new MappingProfile() yapar

3. MappingProfile() Constructor Çalışır
   └─ CreateMap<CreateProductCommand, Product>() → Kural kaydedilir
   └─ CreateMap<UpdateProductCommand, Product>() → Kural kaydedilir
   └─ CreateMap<CreateCategoryCommand, Category>() → Kural kaydedilir
   └─ CreateMap<Product, ProductDto>() → Kural kaydedilir
   └─ CreateMap<Category, CategoryDto>() → Kural kaydedilir

4. IMapper Servisi DI Container'a Eklenir
   └─ Tüm mapping kuralları içinde

5. Uygulama Hazır (app.Run())

6. HTTP Request Gelir
   └─ Handler çalışır
   └─ _mapper.Map<TDestination>(source) çağrılır
   └─ AutoMapper kaynak ve hedef tiplere göre doğru kuralı bulur
   └─ Mapping yapılır
```

**Mapping Kuralı Seçimi:**

AutoMapper, `_mapper.Map<TDestination>(source)` çağrıldığında:

1. **Kaynak tipi alır:** `source.GetType()` → `typeof(CreateProductCommand)`
2. **Hedef tipi alır:** `typeof(TDestination)` → `typeof(Product)`
3. **Configuration'daki tüm mapping kurallarını tarar**
4. **Kaynak tip ve hedef tip eşleşen kuralı bulur:**
   - `m.SourceType == typeof(CreateProductCommand)` ✅
   - `m.DestinationType == typeof(Product)` ✅
   - → Bu kural kullanılır!
5. **Bulunan kurala göre mapping yapar**

**Sonuç:** AutoMapper, kaynak ve hedef tiplere göre doğru mapping kuralını otomatik bulur ve kullanır. Bu sayede Handler içinde sadece `_mapper.Map<Product>(request)` yazmak yeterli olur.

---

## 5. Gerçek Kod İncelemesi

### LoggingBehavior.cs (Gerçek Kod):

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,  // ← Bu "sonraki adım"
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        // 1. REQUEST LOGLANIR
        _logger.LogInformation("Handling {RequestName}: {@Request}", requestName, request);

        // 2. SONRAKI ADIMA GEÇ (ValidationBehavior veya Handler)
        var response = await next();  // ← BURADA MAGIC! Pipeline devam ediyor

        // 3. RESPONSE LOGLANIR (geri dönüş)
        _logger.LogInformation("Handled {RequestName}: {@Response}", requestName, response);

        return response;
    }
}
```

**Kritik Nokta:** `next()` bir delegate. Çağrıldığında bir sonraki adım (ValidationBehavior veya Handler) çalışır.

### ValidationBehavior.cs (Gerçek Kod):

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;  // DI container'dan validator'lar alınıyor
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,  // ← Bu "sonraki adım" (Handler)
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            // Tüm validator'ları çalıştır
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            // Hataları topla
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            // Hata varsa exception fırlat
            if (failures.Any())
            {
                throw new ValidationException(failures);  // ← Handler'a gitmez
            }
        }

        // Validation başarılıysa Handler'a geç
        return await next();  // ← Handler çalışır
    }
}
```

**Kritik Nokta:** 
- `_validators` DI container'dan geliyor (Program.cs'deki `AddValidatorsFromAssembly` sayesinde)
- Hata varsa `ValidationException` fırlatılır, `next()` çağrılmaz → Handler çalışmaz

---

## 5.1. ValidationBehavior Nasıl Validator'ı Buluyor?

### Soru: ValidationBehavior içindeyken CreateProductValidator nasıl "kendiliğinden" çalışıyor?

**Kısa Cevap:**

`ValidationBehavior` **CreateProductValidator'ı hiç tanımaz**. Ama **DI (Dependency Injection) container** onu `IValidator<CreateProductCommand>` olarak kaydettiği için, MediatR `Send(CreateProductCommand)` çalıştırırken `ValidationBehavior`'a otomatik **enjekte eder**. Behavior da elindeki tüm validator'ları `ValidateAsync` ile çalıştırır.

---

### 1) Bu Otomatikliği Sağlayan Şey Ne?

İki ayrı kayıt var ve ikisi birlikte "magic"i oluşturuyor:

#### A) FluentValidation Tarafı (Validator Kayıtları)

Genelde şunu yapıyoruz:

```csharp
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
```

Bu satır şunu yapar:

- Assembly içinde `AbstractValidator<T>` türevlerini arar
- Mesela şunu bulur:

```csharp
public class CreateProductValidator : AbstractValidator<CreateProductCommand>
```

- Ve DI container'a şuna eşdeğer şekilde kaydeder:

```csharp
services.AddScoped<IValidator<CreateProductCommand>, CreateProductValidator>();
```

✅ Sonuç: DI artık `IValidator<CreateProductCommand>` istenirse `CreateProductValidator` döndürebilir.

#### B) MediatR Tarafı (Behavior Kayıtları)

Şunu yaptık:

```csharp
cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

Bu kayıt sayesinde MediatR her `Send()` çağrısında pipeline oluştururken `ValidationBehavior<TRequest,TResponse>`'ı da zincire dahil eder.

✅ Sonuç: Her request için MediatR, ilgili generic tiplerle behavior instance'ını DI'dan çözer.

---

### 2) "Tam Olarak Ne Zaman" CreateProductValidator Geliyor?

Şu anda istek gönderiliyor:

```csharp
await _mediator.Send(new CreateProductCommand(...));
```

Bu anda:

- `TRequest = CreateProductCommand`
- `TResponse = Guid` (örnek)

MediatR pipeline kurarken DI'dan şunu ister:

- `IPipelineBehavior<CreateProductCommand, Guid>`
  - bu da `ValidationBehavior<CreateProductCommand, Guid>` demektir.

DI, `ValidationBehavior`'ı oluştururken constructor'a bakar:

```csharp
public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
```

Burada `TRequest = CreateProductCommand` olduğuna göre DI şu listeyi üretmek zorunda:

👉 `IEnumerable<IValidator<CreateProductCommand>>`

DI container'da daha önce şunu kayıt etmiştik:

- `IValidator<CreateProductCommand> -> CreateProductValidator`

O yüzden `_validators` listesi içinde **CreateProductValidator** gelir.

---

### 3) ValidationBehavior O Validator'ı Nasıl Çalıştırıyor?

Behavior içindeki şu satır:

```csharp
_validators.Select(v => v.ValidateAsync(context, cancellationToken))
```

`_validators` içinde `CreateProductValidator` olduğu için şu çalışmış olur:

```csharp
createProductValidator.ValidateAsync(context, cancellationToken);
```

Ve validator'ın constructor'ında yazdığın:

```csharp
RuleFor(x => x.Name).NotEmpty()...
RuleFor(x => x.Price).GreaterThan(0)...
RuleFor(x => x.CategoryId).NotEmpty()...
```

kuralları işletilir.

---

### 4) Neden ValidationBehavior "Hangi Validator" Olduğunu Bilmek Zorunda Değil?

Çünkü tasarım şu:

- `ValidationBehavior` sadece şunu bilir:
  "Bu request için DI bana hangi `IValidator<TRequest>` verdiyse onları çalıştırırım."
- Hangi request gelirse:
  - `CreateProductCommand` → `CreateProductValidator`
  - `UpdateProductCommand` → `UpdateProductValidator`
  - vs.

✅ Bu sayede yeni validator eklediğinde behavior'a dokunmazsın.

---

### 5) En Kısa "Tek Cümle" Özet

**CreateProductValidator, `AddValidatorsFromAssembly` ile DI'a `IValidator<CreateProductCommand>` olarak kaydolduğu için; MediatR `Send(CreateProductCommand)` sırasında `ValidationBehavior<CreateProductCommand, Guid>` oluşturulurken DI tarafından otomatik enjekte edilir ve `ValidateAsync` ile çalıştırılır.**

---

### CreateProductHandler.cs (Gerçek Kod):

```csharp
public class CreateProductHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly CatalogDbContext _context;
    private readonly IMapper _mapper;

    public CreateProductHandler(CatalogDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Command'den Entity oluştur
        var product = _mapper.Map<Product>(request);
        product.Id = Guid.NewGuid();

        // 2. Veritabanına ekle
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        // 3. ID döndür
        return product.Id;
    }
}
```

**Kritik Nokta:** Handler sadece iş mantığını içeriyor. Logging veya validation yok. Bunlar pipeline'da otomatik yapılıyor.

---

## 6. Program.cs'de Yaptığımızın Bize Ne Sağladığı

### 6.1. RegisterServicesFromAssembly - Handler'ları Otomatik Bulma

**Ne Yapıyor:**
- Catalog.API assembly'sindeki tüm `IRequestHandler<TRequest, TResponse>` implement eden class'ları bulur
- DI container'a kaydeder

**Bize Ne Sağlıyor:**
- ✅ Her handler için manuel kayıt yapmaya gerek yok
- ✅ Yeni handler eklendiğinde otomatik bulunur
- ✅ Kod tekrarı yok

**Detaylı Açıklama:** Aşağıdaki bölüme bakın.

---

#### 6.1.1. RegisterServicesFromAssembly: Neye Bakıyor ve Nasıl Ekleme Yapıyor?

#### 1. Neye Bakıyor?

```csharp
cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
```

Bu satır şunu yapar:
- `typeof(Program).Assembly` → Catalog.API assembly'sini alır
- **Reflection** ile assembly'deki tüm class'ları tarar
- Her class için şu kontrolü yapar: **Bu class `IRequestHandler<TRequest, TResponse>` interface'ini implement ediyor mu?**

---

#### 2. Nasıl Kontrol Ediyor? (Reflection ile)

MediatR içinde (basitleştirilmiş pseudo-code):

```csharp
// MediatR içinde (basitleştirilmiş)
public void RegisterServicesFromAssembly(Assembly assembly)
{
    // 1. Assembly'deki tüm public class'ları al
    var types = assembly.GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
        .ToList();
    
    // 2. Her class için kontrol et
    foreach (var type in types)
    {
        // 3. Bu class'ın implement ettiği interface'leri al
        var interfaces = type.GetInterfaces();
        
        // 4. IRequestHandler<,> interface'ini implement ediyor mu?
        var handlerInterface = interfaces
            .FirstOrDefault(i => 
                i.IsGenericType && 
                i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
        
        if (handlerInterface != null)
        {
            // 5. Generic parametreleri al
            var genericArgs = handlerInterface.GetGenericArguments();
            var requestType = genericArgs[0];   // TRequest
            var responseType = genericArgs[1];   // TResponse
            
            // 6. DI container'a kaydet
            services.AddScoped(
                typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType),
                type);
        }
    }
}
```

---

#### 3. Gerçek Örnekler: Projemizdeki Handler'lar

##### Örnek 1: CreateProductHandler

**Dosya:** `Catalog.API/Features/Products/Commands/CreateProduct/CreateProductHandler.cs`

```csharp
public class CreateProductHandler : IRequestHandler<CreateProductCommand, Guid>
//                                    ↑ Bu interface'i implement ediyor
{
    // ...
}
```

**RegisterServicesFromAssembly kontrolü:**
1. **Class adı**: `CreateProductHandler`
2. **Interface kontrolü**: `IRequestHandler<CreateProductCommand, Guid>` implement ediyor mu? → ✅ Evet
3. **Generic parametreler**:
   - `TRequest` = `CreateProductCommand`
   - `TResponse` = `Guid`
4. **DI container'a kayıt**:
   ```csharp
   services.AddScoped<IRequestHandler<CreateProductCommand, Guid>, CreateProductHandler>();
   ```

##### Örnek 2: GetProductsHandler

**Dosya:** `Catalog.API/Features/Products/Queries/GetProducts/GetProductsHandler.cs`

```csharp
public class GetProductsHandler : IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>
//                                  ↑ Bu interface'i implement ediyor
{
    // ...
}
```

**RegisterServicesFromAssembly kontrolü:**
1. **Class adı**: `GetProductsHandler`
2. **Interface kontrolü**: `IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>` implement ediyor mu? → ✅ Evet
3. **Generic parametreler**:
   - `TRequest` = `GetProductsQuery`
   - `TResponse` = `IEnumerable<ProductDto>`
4. **DI container'a kayıt**:
   ```csharp
   services.AddScoped<IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>, GetProductsHandler>();
   ```

##### Örnek 3: CreateCategoryHandler

**Dosya:** `Catalog.API/Features/Categories/Commands/CreateCategory/CreateCategoryHandler.cs`

```csharp
public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Guid>
//                                     ↑ Bu interface'i implement ediyor
{
    // ...
}
```

**RegisterServicesFromAssembly kontrolü:**
1. **Class adı**: `CreateCategoryHandler`
2. **Interface kontrolü**: `IRequestHandler<CreateCategoryCommand, Guid>` implement ediyor mu? → ✅ Evet
3. **Generic parametreler**:
   - `TRequest` = `CreateCategoryCommand`
   - `TResponse` = `Guid`
4. **DI container'a kayıt**:
   ```csharp
   services.AddScoped<IRequestHandler<CreateCategoryCommand, Guid>, CreateCategoryHandler>();
   ```

---

#### 4. Bulunmayan Class'lar (IRequestHandler implement etmeyenler)

##### Örnek: ProductDto

**Dosya:** `Catalog.API/Dtos/ProductDto.cs`

```csharp
public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // ...
}
```

**RegisterServicesFromAssembly kontrolü:**
1. **Class adı**: `ProductDto`
2. **Interface kontrolü**: `IRequestHandler<,>` implement ediyor mu? → ❌ Hayır
3. **Sonuç**: Kaydedilmez (DTO, handler değil)

##### Örnek: CreateProductValidator

**Dosya:** `Catalog.API/Features/Products/Commands/CreateProduct/CreateProductValidator.cs`

```csharp
public class CreateProductValidator : AbstractValidator<CreateProductCommand>
//                                     ↑ IRequestHandler değil, AbstractValidator
{
    // ...
}
```

**RegisterServicesFromAssembly kontrolü:**
1. **Class adı**: `CreateProductValidator`
2. **Interface kontrolü**: `IRequestHandler<,>` implement ediyor mu? → ❌ Hayır
3. **Sonuç**: Kaydedilmez (Validator, handler değil)

---

#### 5. DI Container'a Nasıl Ekleniyor?

RegisterServicesFromAssembly bulduğu her handler için şunu yapar:

```csharp
// Manuel kayıt (yapmıyoruz, RegisterServicesFromAssembly otomatik yapıyor)
services.AddScoped<IRequestHandler<CreateProductCommand, Guid>, CreateProductHandler>();
services.AddScoped<IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>, GetProductsHandler>();
services.AddScoped<IRequestHandler<CreateCategoryCommand, Guid>, CreateCategoryHandler>();
// ... ve diğer tüm handler'lar
```

**Scoped lifetime:** Her HTTP request'te yeni bir instance oluşturulur.

---

#### 6. Özet: Neye Bakıyor ve Nasıl Ekleme Yapıyor?

**Neye Bakıyor?**
1. **Assembly**: `typeof(Program).Assembly` → Catalog.API assembly'si
2. **Class'lar**: Assembly'deki tüm public, non-abstract class'lar
3. **Interface**: `IRequestHandler<TRequest, TResponse>` implement eden class'lar

**Nasıl Ekleme Yapıyor?**
1. **Reflection** ile tüm class'ları tarar
2. Her class için `IRequestHandler<,>` kontrolü yapar
3. Bulunan handler'ları DI container'a kaydeder:
   ```csharp
   services.AddScoped<IRequestHandler<TRequest, TResponse>, HandlerClass>();
   ```

**Sonuç:**
- `CreateProductHandler` → `IRequestHandler<CreateProductCommand, Guid>` olarak kaydedilir
- `GetProductsHandler` → `IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>` olarak kaydedilir
- `CreateCategoryHandler` → `IRequestHandler<CreateCategoryCommand, Guid>` olarak kaydedilir
- ... ve diğer tüm handler'lar otomatik kaydedilir

---

### 6.2. AddBehavior - Pipeline'a Behavior Ekleme

**Ne Yapıyor:**
- Behavior'ları MediatR pipeline'ına ekler
- Her `_mediator.Send()` çağrısında otomatik çalışır

**Bize Ne Sağlıyor:**
- ✅ Cross-cutting concerns (logging, validation) merkezi olarak yönetilir
- ✅ Her handler'da ayrı ayrı logging/validation yazmaya gerek yok
- ✅ Yeni behavior eklemek kolay (sadece `AddBehavior` ile ekle)
- ✅ Behavior'ları test etmek kolay (handler'dan bağımsız)

**Örnek:**
```csharp
// ❌ Her handler'da manuel logging (yapmıyoruz)
public class CreateProductHandler : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(...)
    {
        _logger.LogInformation("Handling CreateProductCommand");  // ← Her handler'da tekrar
        // ... iş mantığı
        _logger.LogInformation("Handled CreateProductCommand");
    }
}

// ✅ Otomatik logging (LoggingBehavior yapıyor)
public class CreateProductHandler : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(...)
    {
        // Sadece iş mantığı, logging yok
        // LoggingBehavior otomatik loglar
    }
}
```

### 6.3. AddValidatorsFromAssembly - Validator'ları Otomatik Bulma

**Ne Yapıyor:**
- Catalog.API assembly'sindeki tüm `AbstractValidator<T>` türeyen class'ları bulur
- DI container'a `IValidator<T>` olarak kaydeder

**Bize Ne Sağlıyor:**
- ✅ Her validator için manuel kayıt yapmaya gerek yok
- ✅ ValidationBehavior validator'ları otomatik bulur
- ✅ Yeni validator eklendiğinde otomatik çalışır

**Örnek:**
```csharp
// ❌ Manuel kayıt (yapmıyoruz)
builder.Services.AddScoped<IValidator<CreateProductCommand>, CreateProductValidator>();
builder.Services.AddScoped<IValidator<UpdateProductCommand>, UpdateProductValidator>();
// ... her validator için tek tek yazmak gerekir

// ✅ Otomatik kayıt (AddValidatorsFromAssembly yapıyor)
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
// → Tüm validator'lar otomatik bulunur ve kaydedilir
```

---

## 7. Felsefe ve Mantık: Neden Bu Yaklaşım?

### 7.1. Separation of Concerns (Sorumlulukların Ayrılması)

**Sorun:** Handler'da hem iş mantığı hem logging hem validation olursa:
- ❌ Kod karmaşık olur
- ❌ Test etmek zor olur
- ❌ Değişiklik yapmak zor olur

**Çözüm:** Pipeline Behavior Pattern
- ✅ Handler sadece iş mantığını içerir
- ✅ Logging ayrı behavior'da
- ✅ Validation ayrı behavior'da
- ✅ Her biri bağımsız test edilebilir

### 7.2. DRY (Don't Repeat Yourself)

**Sorun:** Her handler'da aynı logging/validation kodu tekrar eder:
```csharp
// Her handler'da aynı kod
_logger.LogInformation("Handling...");
// validation
// iş mantığı
_logger.LogInformation("Handled...");
```

**Çözüm:** Pipeline Behavior
- ✅ Logging kodu bir yerde (LoggingBehavior)
- ✅ Validation kodu bir yerde (ValidationBehavior)
- ✅ Handler'da sadece iş mantığı

### 7.3. Open/Closed Principle

**Sorun:** Yeni bir cross-cutting concern (örnek: caching) eklemek istersek:
- ❌ Tüm handler'ları değiştirmek gerekir

**Çözüm:** Pipeline Behavior
- ✅ Sadece yeni behavior ekle (`AddBehavior<CachingBehavior>`)
- ✅ Handler'ları değiştirmeye gerek yok
- ✅ Mevcut kod açık (extension için) ama kapalı (modification için)

### 7.4. Decorator Pattern

Pipeline Behavior aslında **Decorator Pattern**'in bir uygulaması:

```
┌─────────────────┐
│  LoggingBehavior│  ← Decorator (logging ekler)
└────────┬────────┘
         │ wraps
┌────────▼────────┐
│ValidationBehavior│  ← Decorator (validation ekler)
└────────┬────────┘
         │ wraps
┌────────▼────────┐
│     Handler     │  ← Core (iş mantığı)
└─────────────────┘
```

Her behavior bir decorator gibi çalışır, core functionality'yi (handler) wrap eder.

---

## 8. Özet: Program.cs'de Yaptığımızın Özeti

### Program.cs'de Yaptığımız (Satır 14-23):

**Tam Kod:** (Bkz: [Bölüm 1 - Program.cs'de Ne Yapıyoruz?](#1-programcsde-ne-yapıyoruz))

```csharp
// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);  // 1. Handler'ları bul
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));  // 2. Logging ekle
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));  // 3. Validation ekle
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);  // 4. Validator'ları bul
```

### Bu Kodun Sağladıkları:

1. **Handler'lar otomatik bulunuyor** → Manuel kayıt yok
2. **Pipeline otomatik çalışıyor** → Her `_mediator.Send()` çağrısında
3. **Logging otomatik yapılıyor** → Her request/response loglanıyor
4. **Validation otomatik yapılıyor** → Her request validate ediliyor
5. **Validator'lar otomatik bulunuyor** → Manuel kayıt yok

### Arka Planda Olanlar:

1. **Reflection** → Assembly'deki class'ları tarıyor
2. **DI Container** → Servisleri kaydediyor ve çözüyor
3. **Pipeline Pattern** → Behavior'ları zincir halinde çalıştırıyor
4. **Decorator Pattern** → Her behavior bir decorator gibi çalışıyor

---

## 9. Sonuç

**Kısa Cevap:** Behavior'ları yazdık ama kullanmıyoruz çünkü MediatR otomatik kullanıyor. `_mediator.Send()` çağrıldığında MediatR pipeline'ı çalıştırıyor ve behavior'lar otomatik devreye giriyor.

**Uzun Cevap:** Program.cs'de yaptığımız kayıtlar sayesinde:
- Handler'lar ve validator'lar otomatik bulunuyor (reflection)
- Behavior'lar pipeline'a ekleniyor
- Her `_mediator.Send()` çağrısında MediatR pipeline'ı otomatik çalıştırıyor
- Behavior'lar `next()` delegate'i ile zincir halinde çalışıyor
- Handler en son çalışıyor

**Felsefe:** Separation of Concerns, DRY, Open/Closed Principle ve Decorator Pattern sayesinde kod daha temiz, test edilebilir ve genişletilebilir oluyor.

---

**Son Güncelleme:** Aralık 2024

