# MediatR Pipeline Behavior Mekanizması - Detaylı Açıklama

> Bu dokümantasyon, MediatR Pipeline Behavior'ların nasıl çalıştığını, Program.cs'de yaptığımız kayıtların ne sağladığını ve arka planda olanları detaylı olarak açıklar.

---

## Soru: Behavior'ları Yazdık Ama Hiçbir Yerde Kullanmıyoruz, Nasıl Otomatik Çalışıyor?

Bu soru çok önemli! Behavior'ları yazdık ama Controller'da veya Handler'da manuel olarak çağırmıyoruz. Peki nasıl otomatik çalışıyor?

**Cevap:** MediatR, her `_mediator.Send()` çağrısında otomatik olarak pipeline'ı çalıştırıyor. Biz sadece behavior'ları pipeline'a kaydediyoruz, MediatR gerisini hallediyor.

---

## 1. Program.cs'de Ne Yapıyoruz? (Kayıtlar - Uygulama Başlangıcı)

### Kod (Program.cs, Satır 14-24):

```csharp
// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);
```

### Bu Kod Ne Yapıyor?

1. **`RegisterServicesFromAssembly`**: `IRequestHandler<TRequest, TResponse>` interface'ini implement eden handler'ları otomatik bulup DI container'a kaydediyor
2. **`AddBehavior<LoggingBehavior>`**: LoggingBehavior'ı pipeline'a ekliyor
3. **`AddBehavior<ValidationBehavior>`**: ValidationBehavior'ı pipeline'a ekliyor
4. **`AddValidatorsFromAssembly`**: `AbstractValidator<T>` türeyen validator'ları otomatik bulup DI container'a kaydediyor
5. **`AddAutoMapper`**: `Profile` sınıfını miras alan class'ları bulup mapping kurallarını kaydediyor

**Önemli:** Bu satırlar sadece **kayıt** yapıyor. Behavior'lar, validator'lar ve mapping'ler henüz çalışmıyor. Sadece "şu servisler var, kaydedilsin" diyoruz.

---

## 2. Uygulama Başlangıcında Ne Oluyor? (Kayıt Süreci)

### 2.1. Handler'ların Bulunması ve Kaydedilmesi

**Kod:** `cfg.RegisterServicesFromAssembly(typeof(Program).Assembly)`

#### Ne Yapıyor?

**Neye Bakıyor?**
1. **Assembly**: `typeof(Program).Assembly` → Catalog.API assembly'si
2. **Class'lar**: Assembly'deki tüm public, non-abstract class'lar
3. **Interface**: `IRequestHandler<TRequest, TResponse>` implement eden class'lar

**Nasıl Kontrol Ediyor? (Reflection ile)**

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

**Gerçek Örnekler:**

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

**DI container'a kayıt:**
```csharp
services.AddScoped<IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>, GetProductsHandler>();
```

**Scoped lifetime:** Her HTTP request'te yeni bir instance oluşturulur.

**Sonuç:**
- `CreateProductHandler` → `IRequestHandler<CreateProductCommand, Guid>` olarak kaydedilir
- `GetProductsHandler` → `IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>` olarak kaydedilir
- `CreateCategoryHandler` → `IRequestHandler<CreateCategoryCommand, Guid>` olarak kaydedilir
- ... ve diğer tüm handler'lar otomatik kaydedilir

---

### 2.2. Behavior'ların Pipeline'a Eklenmesi

**Kod:** 
```csharp
cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

**Ne Yapıyor:**
- Behavior'ları MediatR pipeline'ına ekler
- Her `_mediator.Send()` çağrısında otomatik çalışır

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

**Önemli:** Ekleme sırası = Pipeline sırası. İlk eklenen behavior ilk çalışır.

---

### 2.3. Validator'ların Bulunması ve Kaydedilmesi

**Kod:** `builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly)`

**Ne Yapıyor:**
- Catalog.API assembly'sindeki tüm `AbstractValidator<T>` türeyen class'ları bulur
- DI container'a `IValidator<T>` olarak kaydeder

**Nasıl Çalışıyor:**

```csharp
// FluentValidation içinde (basitleştirilmiş)
// Assembly içinde AbstractValidator<T> türevlerini arar
// Mesela şunu bulur:
public class CreateProductValidator : AbstractValidator<CreateProductCommand>

// Ve DI container'a şuna eşdeğer şekilde kaydeder:
services.AddScoped<IValidator<CreateProductCommand>, CreateProductValidator>();
```

**Sonuç:** DI artık `IValidator<CreateProductCommand>` istenirse `CreateProductValidator` döndürebilir.

---

### 2.4. AutoMapper'ın Kurulumu

**Kod:** `builder.Services.AddAutoMapper(typeof(Program).Assembly)`

#### Ne Yapıyor?

**1. Assembly'yi Alıyor**
```csharp
// AutoMapper içinde
Assembly assembly = typeof(Program).Assembly;
// → Catalog.API.dll assembly'si alınır
```

**2. Assembly'deki Tüm Class'ları Tarıyor**
```csharp
// AutoMapper içinde
var allTypes = assembly.GetTypes()
    .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
    .ToList();
// → [Program, MappingProfile, Product, ProductDto, CreateProductHandler, ...]
```

**3. Profile Sınıfını Miras Alan Class'ları Arıyor**
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

**4. MappingProfile Instance'ı Oluşturuyor**
```csharp
// AutoMapper içinde
var profileInstance = Activator.CreateInstance(typeof(MappingProfile));
// → new MappingProfile() çağrısı yapılır
```

Bu noktada `MappingProfile` constructor çalışır.

**5. MappingProfile Constructor Çalışıyor**

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

**6. IMapper Servisi DI Container'a Ekleniyor**

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
│  ✅ IMediator → Mediator instance          │
│  ✅ IMapper → Mapper instance              │
│     └─ İçinde tüm mapping kuralları var   │
│  ✅ IValidator<CreateProductCommand> →      │
│     CreateProductValidator                 │
│  ✅ IRequestHandler<CreateProductCommand,   │
│     Guid> → CreateProductHandler           │
└─────────────────────────────────────────────┘
```

---

## 3. HTTP Request Geldiğinde Ne Oluyor? (Çalışma Zamanı)

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

### 3.1. MediatR Handler Bulma Mekanizması: Doğru Handler Nasıl Bulunuyor?

**Soru:** Controller'dan `_mediator.Send(command)` çağrıldığında, MediatR doğru handler'ı nasıl buluyor?

**Cevap:** Generic type matching ile. Command'in tipine göre `IRequestHandler<TRequest, TResponse>` tipindeki handler'ı arıyor.

#### 1. Command Tipi

```csharp
// Controller'dan
var command = new CreateProductCommand { ... };
// command'in tipi: CreateProductCommand

await _mediator.Send(command);
// MediatR'a CreateProductCommand tipinde bir request gönderiliyor
```

#### 2. MediatR'ın İçinde Ne Oluyor?

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

#### 3. Nasıl Eşleşiyor? (Adım Adım)

**Adım 1: Command Tipi**
```csharp
var command = new CreateProductCommand { ... };
// Tip: CreateProductCommand
```

**Adım 2: MediatR Handler Interface'ini Oluşturur**
```csharp
// MediatR içinde
IRequestHandler<CreateProductCommand, Guid>
//              ↑ TRequest (Command tipi)
//                              ↑ TResponse (dönecek tip)
```

**Adım 3: DI Container'dan Handler'ı Bulur**
```csharp
// DI container'da kayıtlı olan (RegisterServicesFromAssembly sayesinde):
IRequestHandler<CreateProductCommand, Guid> → CreateProductHandler

// MediatR arıyor:
IRequestHandler<CreateProductCommand, Guid> → ✅ BULUNDU! → CreateProductHandler
```

#### 4. Gerçek Örnek: Projemizdeki Handler

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

#### 5. Farklı Command'lar → Farklı Handler'lar

**Örnek 1: CreateProductCommand**
```csharp
var command = new CreateProductCommand { Name = "iPhone 15", ... };
await _mediator.Send(command);
// → IRequestHandler<CreateProductCommand, Guid> aranır
// → CreateProductHandler bulunur ✅
```

**Örnek 2: GetProductsQuery**
```csharp
var query = new GetProductsQuery { PageNumber = 1, PageSize = 10 };
var products = await _mediator.Send(query);
// → IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>> aranır
// → GetProductsHandler bulunur ✅
```

#### 6. Generic Type Matching (Tip Eşleştirmesi)

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

#### 7. Özet: Handler Nasıl Bulunuyor?

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

### 3.2. Pipeline'ın Oluşturulması

`_mediator.Send(command)` çağrıldığında MediatR şu adımları izler:

1. **Handler'ı bulur** (Yukarıdaki bölümde açıklandı)
2. **Pipeline behavior'ları bulur** (Program.cs'deki `AddBehavior` sayesinde)
3. **Pipeline zincirini oluşturur**
4. **Pipeline'ı çalıştırır**

**Sonuç:** Şöyle bir zincir oluşuyor:
```
LoggingBehavior → ValidationBehavior → CreateProductHandler
```

#### Pipeline Sırasını Ne Belirliyor?

**Cevap:** Program.cs'deki `AddBehavior` çağrılarının sırası

**Program.cs'deki Gerçek Kod (Satır 18-19):**

```csharp
// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));     // Satır 18: 1. Eklendi → İlk çalışır
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)); // Satır 19: 2. Eklendi → İkinci çalışır
});
```

**Ekleme Sırası:**
1. **Satır 18**: `LoggingBehavior<,>` eklendi → **İlk çalışır** (dışta)
2. **Satır 19**: `ValidationBehavior<,>` eklendi → **İkinci çalışır** (ortada)
3. **Handler**: Otomatik bulunur → **En son çalışır** (içte)

#### MediatR İçinde Nasıl Wrap Ediliyor?

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

#### Sırayı Değiştirirsek Ne Olur?

**Senaryo 1: Mevcut Sıra (Logging → Validation) ✅ Önerilen**

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

**Senaryo 2: Ters Sıra (Validation → Logging)**

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

**Özet: Pipeline Sırasını Ne Belirliyor?**

- **İlk eklenen** → İlk çalışır (dışta)
- **İkinci eklenen** → İkinci çalışır (ortada)
- **Handler** → En son çalışır (içte)

**Pipeline Sırası:**
```
LoggingBehavior (Satır 18) → ValidationBehavior (Satır 19) → Handler
```

---

## 4. Pipeline Nasıl Çalışıyor? (Detaylı Akış)

### 4.1. Gerçek Kod Akışı

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

### 4.2. Handler İçinde AutoMapper Nasıl Çalışıyor?

**Soru:** `_mapper.Map<Product>(request)` Nasıl Doğru Mapping Kuralını Buluyor?

Handler içinde (Satır 483) şu kod var:

```csharp
var product = _mapper.Map<Product>(request);
```

Bu satır çalıştığında AutoMapper nasıl `CreateProductCommand → Product` mapping kuralını buluyor?

#### Handler'da `_mapper.Map<Product>(request)` Çağrıldığında Ne Oluyor?

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

**Özet: AutoMapper'ın Çalışma Süreci**

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

### 5.1. ValidationBehavior Nasıl Validator'ı Buluyor?

**Soru:** ValidationBehavior içindeyken CreateProductValidator nasıl "kendiliğinden" çalışıyor?

**Kısa Cevap:**

`ValidationBehavior` **CreateProductValidator'ı hiç tanımaz**. Ama **DI (Dependency Injection) container** onu `IValidator<CreateProductCommand>` olarak kaydettiği için, MediatR `Send(CreateProductCommand)` çalıştırırken `ValidationBehavior`'a otomatik **enjekte eder**. Behavior da elindeki tüm validator'ları `ValidateAsync` ile çalıştırır.

#### 1) Bu Otomatikliği Sağlayan Şey Ne?

İki ayrı kayıt var ve ikisi birlikte "magic"i oluşturuyor:

**A) FluentValidation Tarafı (Validator Kayıtları)**

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

**B) MediatR Tarafı (Behavior Kayıtları)**

```csharp
cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

Bu kayıt sayesinde MediatR her `Send()` çağrısında pipeline oluştururken `ValidationBehavior<TRequest,TResponse>`'ı da zincire dahil eder.

✅ Sonuç: Her request için MediatR, ilgili generic tiplerle behavior instance'ını DI'dan çözer.

#### 2) "Tam Olarak Ne Zaman" CreateProductValidator Geliyor?

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

#### 3) ValidationBehavior O Validator'ı Nasıl Çalıştırıyor?

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

#### 4) Neden ValidationBehavior "Hangi Validator" Olduğunu Bilmek Zorunda Değil?

Çünkü tasarım şu:

- `ValidationBehavior` sadece şunu bilir:
  "Bu request için DI bana hangi `IValidator<TRequest>` verdiyse onları çalıştırırım."
- Hangi request gelirse:
  - `CreateProductCommand` → `CreateProductValidator`
  - `UpdateProductCommand` → `UpdateProductValidator`
  - vs.

✅ Bu sayede yeni validator eklediğinde behavior'a dokunmazsın.

#### 5) En Kısa "Tek Cümle" Özet

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

### Program.cs'de Yaptığımız (Satır 14-24):

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

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);  // 5. MappingProfile'ı bul ve mapping kurallarını kaydet
```

### Bu Kodun Sağladıkları:

1. **Handler'lar otomatik bulunuyor** → Manuel kayıt yok
2. **Pipeline otomatik çalışıyor** → Her `_mediator.Send()` çağrısında
3. **Logging otomatik yapılıyor** → Her request/response loglanıyor
4. **Validation otomatik yapılıyor** → Her request validate ediliyor
5. **Validator'lar otomatik bulunuyor** → Manuel kayıt yok
6. **Mapping kuralları otomatik kaydediliyor** → AutoMapper otomatik çalışıyor

### Arka Planda Olanlar:

1. **Reflection** → Assembly'deki class'ları tarıyor
2. **DI Container** → Servisleri kaydediyor ve çözüyor
3. **Pipeline Pattern** → Behavior'ları zincir halinde çalıştırıyor
4. **Decorator Pattern** → Her behavior bir decorator gibi çalışıyor
5. **Generic Type Matching** → Handler'lar ve mapping kuralları otomatik eşleşiyor

---

## 9. Sonuç

**Kısa Cevap:** Behavior'ları yazdık ama kullanmıyoruz çünkü MediatR otomatik kullanıyor. `_mediator.Send()` çağrıldığında MediatR pipeline'ı çalıştırıyor ve behavior'lar otomatik devreye giriyor.

**Uzun Cevap:** Program.cs'de yaptığımız kayıtlar sayesinde:
- Handler'lar ve validator'lar otomatik bulunuyor (reflection)
- Behavior'lar pipeline'a ekleniyor
- Mapping kuralları otomatik kaydediliyor
- Her `_mediator.Send()` çağrısında MediatR pipeline'ı otomatik çalıştırıyor
- Behavior'lar `next()` delegate'i ile zincir halinde çalışıyor
- Handler en son çalışıyor
- AutoMapper kaynak ve hedef tiplere göre doğru mapping kuralını buluyor

**Felsefe:** Separation of Concerns, DRY, Open/Closed Principle ve Decorator Pattern sayesinde kod daha temiz, test edilebilir ve genişletilebilir oluyor.

---

**Son Güncelleme:** Aralık 2024
