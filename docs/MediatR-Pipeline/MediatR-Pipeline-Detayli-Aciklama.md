# MediatR Pipeline Behavior Mekanizması - Detaylı Açıklama

> Bu dokümantasyon, MediatR Pipeline'ın nasıl çalıştığını, validator'ların nasıl bulunduğunu, handler'ların nasıl kaydedildiğini ve arka planda ne olduğunu detaylı olarak açıklar.

---

## Soru: Behavior'ları Yazdık Ama Hiçbir Yerde Kullanmıyoruz, Nasıl Otomatik Çalışıyor?

Bu soru çok önemli! Behavior'ları yazdık ama Controller'da veya Handler'da manuel olarak çağırmıyoruz. Peki nasıl otomatik çalışıyor?

**Cevap:** MediatR, her `_mediator.Send()` çağrısında otomatik olarak pipeline'ı çalıştırıyor. Biz sadece behavior'ları pipeline'a kaydediyoruz, MediatR gerisini hallediyor.

---

## 1. Program.cs'de Ne Yapıyoruz? (Kayıtlar - Uygulama Başlangıcı)

### Kod (Program.cs, Satır 30-36):

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
```

### Bu Kod Ne Yapıyor?

1. **`RegisterServicesFromAssembly`**: `IRequestHandler<TRequest, TResponse>` interface'ini implement eden handler'ları otomatik bulup DI container'a kaydediyor
2. **`AddBehavior<LoggingBehavior>`**: LoggingBehavior'ı pipeline'a ekliyor
3. **`AddBehavior<ValidationBehavior>`**: ValidationBehavior'ı pipeline'a ekliyor
4. **`AddValidatorsFromAssembly`**: `AbstractValidator<T>` türeyen validator'ları otomatik bulup DI container'a kaydediyor

**Önemli:** Bu satırlar sadece **kayıt** yapıyor. Behavior'lar, validator'lar ve handler'lar henüz çalışmıyor. Sadece "şu servisler var, kaydedilsin" diyoruz.

---

## 2. Uygulama Başlangıcında Ne Oluyor? (Kayıt Süreci)

### 2.1. Handler'ların Bulunması ve Kaydedilmesi

**Kod:** `cfg.RegisterServicesFromAssembly(typeof(Program).Assembly)`

#### Ne Yapıyor?

**Neye Bakıyor?**
1. **Assembly**: `typeof(Program).Assembly` → Basket.API assembly'si
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

##### Örnek 1: StoreBasketHandler

**Dosya:** `Basket.API/Features/Basket/Commands/StoreBasket/StoreBasketHandler.cs`

```csharp
public class StoreBasketHandler : IRequestHandler<StoreBasketCommand, ShoppingCartDto>
//                                ↑ Bu interface'i implement ediyor
{
    // ...
}
```

**RegisterServicesFromAssembly kontrolü:**
1. **Class adı**: `StoreBasketHandler`
2. **Interface kontrolü**: `IRequestHandler<StoreBasketCommand, ShoppingCartDto>` implement ediyor mu? → ✅ Evet
3. **Generic parametreler**:
   - `TRequest` = `StoreBasketCommand`
   - `TResponse` = `ShoppingCartDto`
4. **DI container'a kayıt**:
   ```csharp
   services.AddScoped<IRequestHandler<StoreBasketCommand, ShoppingCartDto>, StoreBasketHandler>();
   ```

##### Örnek 2: GetBasketHandler

**Dosya:** `Basket.API/Features/Basket/Queries/GetBasket/GetBasketHandler.cs`

```csharp
public class GetBasketHandler : IRequestHandler<GetBasketQuery, ShoppingCartDto>
//                              ↑ Bu interface'i implement ediyor
{
    // ...
}
```

**DI container'a kayıt:**
```csharp
services.AddScoped<IRequestHandler<GetBasketQuery, ShoppingCartDto>, GetBasketHandler>();
```

**Scoped lifetime:** Her HTTP request'te yeni bir instance oluşturulur.

**Sonuç:**
- `StoreBasketHandler` → `IRequestHandler<StoreBasketCommand, ShoppingCartDto>` olarak kaydedilir
- `GetBasketHandler` → `IRequestHandler<GetBasketQuery, ShoppingCartDto>` olarak kaydedilir
- `CheckoutBasketHandler` → `IRequestHandler<CheckoutBasketCommand, bool>` olarak kaydedilir
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
1. **Satır 34**: `LoggingBehavior<,>` eklendi → **İlk çalışır** (dışta)
2. **Satır 35**: `ValidationBehavior<,>` eklendi → **İkinci çalışır** (ortada)
3. **Handler**: Otomatik bulunur → **En son çalışır** (içte)

**Pipeline Sırası (Çalışma Sırası):**
```
1. LoggingBehavior (Satır 34'te eklendi → İlk çalışır - dışta)
2. ValidationBehavior (Satır 35'te eklendi → İkinci çalışır - ortada)
3. Handler (RegisterServicesFromAssembly ile bulunur → En son çalışır - içte)
```

**Önemli:** Ekleme sırası = Pipeline sırası. İlk eklenen behavior ilk çalışır.

---

### 2.3. Validator'ların Bulunması ve Kaydedilmesi

**Kod:** `builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly)`

**Ne Yapıyor:**
- Basket.API assembly'sindeki tüm `AbstractValidator<T>` türeyen class'ları bulur
- DI container'a `IValidator<T>` olarak kaydeder

**Nasıl Çalışıyor:**

```csharp
// FluentValidation içinde (basitleştirilmiş)
// Assembly içinde AbstractValidator<T> türevlerini arar
// Mesela şunu bulur:
public class StoreBasketValidator : AbstractValidator<StoreBasketCommand>

// Ve DI container'a şuna eşdeğer şekilde kaydeder:
services.AddScoped<IValidator<StoreBasketCommand>, StoreBasketValidator>();
```

**Sonuç:** DI artık `IValidator<StoreBasketCommand>` istenirse `StoreBasketValidator` döndürebilir.

**Gerçek Örnek: StoreBasketValidator**

**Dosya:** `Basket.API/Features/Basket/Commands/StoreBasket/StoreBasketValidator.cs`

```csharp
public class StoreBasketValidator : AbstractValidator<StoreBasketCommand>
{
    public StoreBasketValidator()
    {
        RuleFor(x => x.Basket)
            .NotNull().WithMessage("Basket boş olamaz");

        RuleFor(x => x.Basket.UserName)
            .NotEmpty().WithMessage("UserName boş olamaz");

        RuleFor(x => x.Basket.Items)
            .NotNull().WithMessage("Items null olamaz");

        RuleForEach(x => x.Basket.Items)
            .SetValidator(new ShoppingCartItemValidator());
    }
}
```

**FluentValidation ne yapar:**

1. **Reflection ile bulur:**
   ```csharp
   // Assembly'de StoreBasketValidator class'ını bulur
   var validatorType = typeof(StoreBasketValidator);
   
   // Base class'ı kontrol eder
   var baseType = validatorType.BaseType;
   // → AbstractValidator<StoreBasketCommand>
   
   // Generic parametreyi alır
   var requestType = typeof(StoreBasketCommand);
   ```

2. **DI container'a kaydeder:**
   ```csharp
   services.AddScoped(
       typeof(IValidator<StoreBasketCommand>),
       typeof(StoreBasketValidator));
   ```

---

## 3. HTTP Request Geldiğinde Ne Oluyor? (Çalışma Zamanı)

### Senaryo: Kullanıcı sepete ürün ekliyor

**HTTP Request:**
```
POST /api/baskets
Content-Type: application/json

{
  "userName": "ali",
  "items": [
    {
      "productId": "1",
      "productName": "iPhone 15",
      "quantity": 2,
      "price": 35000.00
    }
  ]
}
```

### Controller (BasketsController.cs):

```csharp
using MediatR;
using Basket.API.Features.Basket.Commands.StoreBasket;

namespace Basket.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BasketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BasketsController(IMediator mediator)
    {
        _mediator = mediator;  // DI container'dan IMediator alınıyor
    }

    [HttpPost]
    public async Task<ActionResult<ShoppingCartDto>> StoreBasket([FromBody] ShoppingCartDto basket)
    {
        // 1. DTO'dan Command oluştur
        var command = new StoreBasketCommand { Basket = basket };

        // 2. MediatR'a gönder ← BURADA MAGIC BAŞLIYOR!
        var result = await _mediator.Send(command);

        // 3. Response döndür
        return Ok(result);
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
var command = new StoreBasketCommand { Basket = basket };
// command'in tipi: StoreBasketCommand

await _mediator.Send(command);
// MediatR'a StoreBasketCommand tipinde bir request gönderiliyor
```

#### 2. MediatR'ın İçinde Ne Oluyor?

```csharp
// MediatR içinde (basitleştirilmiş)
public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
{
    // 1. Request'in tipini al
    var requestType = request.GetType();  
    // → typeof(StoreBasketCommand)
    
    // 2. Handler interface tipini oluştur
    var handlerInterfaceType = typeof(IRequestHandler<,>)
        .MakeGenericType(requestType, typeof(TResponse));
    // → IRequestHandler<StoreBasketCommand, ShoppingCartDto>
    
    // 3. DI container'dan bu tipi al
    var handler = serviceProvider.GetRequiredService(handlerInterfaceType);
    // → StoreBasketHandler instance'ı döner
    
    // 4. Handler'ı çalıştır
    return await handler.Handle(request, cancellationToken);
}
```

#### 3. Nasıl Eşleşiyor? (Adım Adım)

**Adım 1: Command Tipi**
```csharp
var command = new StoreBasketCommand { Basket = basket };
// Tip: StoreBasketCommand
```

**Adım 2: MediatR Handler Interface'ini Oluşturur**
```csharp
// MediatR içinde
IRequestHandler<StoreBasketCommand, ShoppingCartDto>
//              ↑ TRequest (Command tipi)
//                              ↑ TResponse (dönecek tip)
```

**Adım 3: DI Container'dan Handler'ı Bulur**
```csharp
// DI container'da kayıtlı olan (RegisterServicesFromAssembly sayesinde):
IRequestHandler<StoreBasketCommand, ShoppingCartDto> → StoreBasketHandler

// MediatR arıyor:
IRequestHandler<StoreBasketCommand, ShoppingCartDto> → ✅ BULUNDU! → StoreBasketHandler
```

#### 4. Gerçek Örnek: Projemizdeki Handler

**Command (Dosya: `Basket.API/Features/Basket/Commands/StoreBasket/StoreBasketCommand.cs`):**
```csharp
public class StoreBasketCommand : IRequest<ShoppingCartDto>
{
    public ShoppingCartDto Basket { get; set; } = null!;
}
```

**Handler (Dosya: `Basket.API/Features/Basket/Commands/StoreBasket/StoreBasketHandler.cs`):**
```csharp
public class StoreBasketHandler : IRequestHandler<StoreBasketCommand, ShoppingCartDto>
//                                ↑ TRequest = StoreBasketCommand
//                                                ↑ TResponse = ShoppingCartDto
{
    public async Task<ShoppingCartDto> Handle(StoreBasketCommand request, CancellationToken cancellationToken)
    {
        // İş mantığı
        return basketDto;
    }
}
```

**Eşleşme:**
- Command tipi: `StoreBasketCommand`
- Handler interface: `IRequestHandler<StoreBasketCommand, ShoppingCartDto>`
- Handler class: `StoreBasketHandler`
- **Eşleşme:** ✅ Evet

#### 5. Farklı Command'lar → Farklı Handler'lar

**Örnek 1: StoreBasketCommand**
```csharp
var command = new StoreBasketCommand { Basket = basket };
await _mediator.Send(command);
// → IRequestHandler<StoreBasketCommand, ShoppingCartDto> aranır
// → StoreBasketHandler bulunur ✅
```

**Örnek 2: GetBasketQuery**
```csharp
var query = new GetBasketQuery(userName);
var basket = await _mediator.Send(query);
// → IRequestHandler<GetBasketQuery, ShoppingCartDto> aranır
// → GetBasketHandler bulunur ✅
```

#### 6. Generic Type Matching (Tip Eşleştirmesi)

MediatR, **generic type matching** kullanır:

```csharp
// Command tipi: StoreBasketCommand
// Handler interface: IRequestHandler<StoreBasketCommand, ShoppingCartDto>
// Handler class: StoreBasketHandler : IRequestHandler<StoreBasketCommand, ShoppingCartDto>

// Eşleşme kontrolü:
typeof(StoreBasketCommand) == typeof(StoreBasketCommand) ✅
typeof(ShoppingCartDto) == typeof(ShoppingCartDto) ✅
// → Handler bulundu!
```

**Neden Generic Type Matching?**
- ✅ **Type-safe**: Compile-time'da tip kontrolü
- ✅ **Otomatik**: Manuel eşleştirme gerekmez
- ✅ **Esnek**: Her Command/Query için farklı Handler olabilir

#### 7. Özet: Handler Nasıl Bulunuyor?

1. **Command tipi**: `StoreBasketCommand`
2. **MediatR handler interface'ini oluşturur**: `IRequestHandler<StoreBasketCommand, ShoppingCartDto>`
3. **DI container'dan bu interface tipini arar**
4. **`StoreBasketHandler` bulunur** (çünkü `IRequestHandler<StoreBasketCommand, ShoppingCartDto>` implement ediyor)
5. **Handler çalıştırılır**

**Sonuç:** MediatR, command'in tipine göre doğru handler'ı bulur:
- `StoreBasketCommand` → `IRequestHandler<StoreBasketCommand, ShoppingCartDto>` → `StoreBasketHandler`
- `GetBasketQuery` → `IRequestHandler<GetBasketQuery, ShoppingCartDto>` → `GetBasketHandler`

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
LoggingBehavior → ValidationBehavior → StoreBasketHandler
```

#### Pipeline Sırasını Ne Belirliyor?

**Cevap:** Program.cs'deki `AddBehavior` çağrılarının sırası

**Program.cs'deki Gerçek Kod (Satır 34-35):**

```csharp
// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));     // Satır 34: 1. Eklendi → İlk çalışır
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)); // Satır 35: 2. Eklendi → İkinci çalışır
});
```

**Ekleme Sırası:**
1. **Satır 34**: `LoggingBehavior<,>` eklendi → **İlk çalışır** (dışta)
2. **Satır 35**: `ValidationBehavior<,>` eklendi → **İkinci çalışır** (ortada)
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
LoggingBehavior (Satır 34) → ValidationBehavior (Satır 35) → Handler
```

---

## 4. Pipeline Nasıl Çalışıyor? (Detaylı Akış)

### 4.1. Gerçek Kod Akışı

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. LoggingBehavior.Handle() başlar                             │
│    Dosya: BuildingBlocks.Behaviors/Behaviors/LoggingBehavior.cs │
│                                                                   │
│    var requestName = typeof(StoreBasketCommand).Name;            │
│    // → "StoreBasketCommand"                                     │
│                                                                   │
│    _logger.LogInformation(                                       │
│        "Handling StoreBasketCommand: {@Request}",                │
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
│    │    // → [StoreBasketValidator] döner                      │ │
│    │                                                            │ │
│    │    // StoreBasketValidator'ı çalıştır                     │ │
│    │    var validationResults = await Task.WhenAll(            │ │
│    │        _validators.Select(v => v.ValidateAsync(context)));│ │
│    │                                                            │ │
│    │    // Validation kuralları kontrol edilir:                │ │
│    │    // - Basket: NotNull ✅                                │ │
│    │    // - Basket.UserName: NotEmpty ✅                      │ │
│    │    // - Basket.Items: NotNull ✅                          │ │
│    │    // - RuleForEach(x => x.Basket.Items) ✅               │ │
│    │                                                            │ │
│    │    var failures = validationResults                        │ │
│    │        .SelectMany(r => r.Errors)                         │ │
│    │        .Where(f => f != null)                             │ │
│    │        .ToList();                                          │ │
│    │                                                            │ │
│    │    if (failures.Any())                                    │ │
│    │    {                                                       │ │
│    │        throw new ValidationException(failures);  // ← Hata varsa │
│    │    }                                                       │ │
│    │                                                            │ │
│    │    // ✅ Validation başarılı, next() çağrılır → Handler'a │ │
│    │                                                            │ │
│    │    ┌─────────────────────────────────────────────────┐  │ │
│    │    │ 3. StoreBasketHandler.Handle() başlar           │  │ │
│    │    │    Dosya: Basket.API/Features/Basket/           │  │ │
│    │    │           Commands/StoreBasket/                 │  │ │
│    │    │           StoreBasketHandler.cs                  │  │ │
│    │    │                                                    │  │ │
│    │    │    // 1. DTO'dan Entity'ye map et                │  │ │
│    │    │    var basket = _mapper.Map<ShoppingCart>(request.Basket);│ │
│    │    │                                                    │  │ │
│    │    │    // 2. Repository'ye kaydet                     │  │ │
│    │    │    var savedBasket = await _repository.SaveBasket(basket);│ │
│    │    │    // → PostgreSQL'e kaydedilir                   │  │ │
│    │    │    // → Redis'e cache'lenir                        │  │ │
│    │    │                                                    │  │ │
│    │    │    // 3. Entity'den DTO'ya map et                 │  │ │
│    │    │    var basketDto = _mapper.Map<ShoppingCartDto>(savedBasket);│ │
│    │    │                                                    │  │ │
│    │    │    // 4. ShoppingCartDto döndür                    │  │ │
│    │    │    return basketDto;  // → ShoppingCartDto döner  │  │ │
│    │    └─────────────────────────────────────────────────┘  │ │
│    │                                                            │ │
│    │    // Handler'dan dönen ShoppingCartDto alınır            │ │
│    │    return await next();  // → ShoppingCartDto döner    │ │
│    └─────────────────────────────────────────────────────────┘ │
│                                                                   │
│    // ValidationBehavior'dan dönen ShoppingCartDto alınır       │
│    var response = await next();  // → ShoppingCartDto döner     │
│                                                                   │
│    _logger.LogInformation(                                       │
│        "Handled StoreBasketCommand: {@Response}",              │
│        response);  // ← RESPONSE LOGLANIR                        │
│                                                                   │
│    return response;  // → ShoppingCartDto döner                 │
└─────────────────────────────────────────────────────────────────┘
```

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

            // Tüm validator'ları paralel çalıştır
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

**Soru:** ValidationBehavior içindeyken StoreBasketValidator nasıl "kendiliğinden" çalışıyor?

**Kısa Cevap:**

`ValidationBehavior` **StoreBasketValidator'ı hiç tanımaz**. Ama **DI (Dependency Injection) container** onu `IValidator<StoreBasketCommand>` olarak kaydettiği için, MediatR `Send(StoreBasketCommand)` çalıştırırken `ValidationBehavior`'a otomatik **enjekte eder**. Behavior da elindeki tüm validator'ları `ValidateAsync` ile çalıştırır.

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
public class StoreBasketValidator : AbstractValidator<StoreBasketCommand>
```

- Ve DI container'a şuna eşdeğer şekilde kaydeder:

```csharp
services.AddScoped<IValidator<StoreBasketCommand>, StoreBasketValidator>();
```

✅ Sonuç: DI artık `IValidator<StoreBasketCommand>` istenirse `StoreBasketValidator` döndürebilir.

**B) MediatR Tarafı (Behavior Kayıtları)**

```csharp
cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

Bu kayıt sayesinde MediatR her `Send()` çağrısında pipeline oluştururken `ValidationBehavior<TRequest,TResponse>`'ı da zincire dahil eder.

✅ Sonuç: Her request için MediatR, ilgili generic tiplerle behavior instance'ını DI'dan çözer.

#### 2) "Tam Olarak Ne Zaman" StoreBasketValidator Geliyor?

Şu anda istek gönderiliyor:

```csharp
await _mediator.Send(new StoreBasketCommand(...));
```

Bu anda:

- `TRequest = StoreBasketCommand`
- `TResponse = ShoppingCartDto`

MediatR pipeline kurarken DI'dan şunu ister:

- `IPipelineBehavior<StoreBasketCommand, ShoppingCartDto>`
  - bu da `ValidationBehavior<StoreBasketCommand, ShoppingCartDto>` demektir.

DI, `ValidationBehavior`'ı oluştururken constructor'a bakar:

```csharp
public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
```

Burada `TRequest = StoreBasketCommand` olduğuna göre DI şu listeyi üretmek zorunda:

👉 `IEnumerable<IValidator<StoreBasketCommand>>`

DI container'da daha önce şunu kayıt etmiştik:

- `IValidator<StoreBasketCommand> -> StoreBasketValidator`

O yüzden `_validators` listesi içinde **StoreBasketValidator** gelir.

#### 3) ValidationBehavior O Validator'ı Nasıl Çalıştırıyor?

Behavior içindeki şu satır:

```csharp
_validators.Select(v => v.ValidateAsync(context, cancellationToken))
```

`_validators` içinde `StoreBasketValidator` olduğu için şu çalışmış olur:

```csharp
storeBasketValidator.ValidateAsync(context, cancellationToken);
```

Ve validator'ın constructor'ında yazdığın:

```csharp
RuleFor(x => x.Basket).NotNull()...
RuleFor(x => x.Basket.UserName).NotEmpty()...
RuleForEach(x => x.Basket.Items).SetValidator(...)...
```

kuralları işletilir.

#### 4) Neden ValidationBehavior "Hangi Validator" Olduğunu Bilmek Zorunda Değil?

Çünkü tasarım şu:

- `ValidationBehavior` sadece şunu bilir:
  "Bu request için DI bana hangi `IValidator<TRequest>` verdiyse onları çalıştırırım."
- Hangi request gelirse:
  - `StoreBasketCommand` → `StoreBasketValidator`
  - `CheckoutBasketCommand` → `CheckoutBasketValidator`
  - vs.

✅ Bu sayede yeni validator eklediğinde behavior'a dokunmazsın.

#### 5) En Kısa "Tek Cümle" Özet

**StoreBasketValidator, `AddValidatorsFromAssembly` ile DI'a `IValidator<StoreBasketCommand>` olarak kaydolduğu için; MediatR `Send(StoreBasketCommand)` sırasında `ValidationBehavior<StoreBasketCommand, ShoppingCartDto>` oluşturulurken DI tarafından otomatik enjekte edilir ve `ValidateAsync` ile çalıştırılır.**

---

### StoreBasketHandler.cs (Gerçek Kod):

```csharp
public class StoreBasketHandler : IRequestHandler<StoreBasketCommand, ShoppingCartDto>
{
    private readonly IBasketRepository _repository;
    private readonly IMapper _mapper;

    public StoreBasketHandler(IBasketRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ShoppingCartDto> Handle(StoreBasketCommand request, CancellationToken cancellationToken)
    {
        // 1. DTO'dan Entity'ye map et
        var basket = _mapper.Map<ShoppingCart>(request.Basket);

        // 2. Repository'ye kaydet
        var savedBasket = await _repository.SaveBasket(basket);

        // 3. Entity'den DTO'ya map et
        var basketDto = _mapper.Map<ShoppingCartDto>(savedBasket);

        // 4. DTO döndür
        return basketDto;
    }
}
```

**Kritik Nokta:** Handler sadece iş mantığını içeriyor. Logging veya validation yok. Bunlar pipeline'da otomatik yapılıyor.

---

## 6. Program.cs'de Yaptığımızın Bize Ne Sağladığı

### 6.1. RegisterServicesFromAssembly - Handler'ları Otomatik Bulma

**Ne Yapıyor:**
- Basket.API assembly'sindeki tüm `IRequestHandler<TRequest, TResponse>` implement eden class'ları bulur
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
public class StoreBasketHandler : IRequestHandler<StoreBasketCommand, ShoppingCartDto>
{
    public async Task<ShoppingCartDto> Handle(...)
    {
        _logger.LogInformation("Handling StoreBasketCommand");  // ← Her handler'da tekrar
        // ... iş mantığı
        _logger.LogInformation("Handled StoreBasketCommand");
    }
}

// ✅ Otomatik logging (LoggingBehavior yapıyor)
public class StoreBasketHandler : IRequestHandler<StoreBasketCommand, ShoppingCartDto>
{
    public async Task<ShoppingCartDto> Handle(...)
    {
        // Sadece iş mantığı, logging yok
        // LoggingBehavior otomatik loglar
    }
}
```

### 6.3. AddValidatorsFromAssembly - Validator'ları Otomatik Bulma

**Ne Yapıyor:**
- Basket.API assembly'sindeki tüm `AbstractValidator<T>` türeyen class'ları bulur
- DI container'a `IValidator<T>` olarak kaydeder

**Bize Ne Sağlıyor:**
- ✅ Her validator için manuel kayıt yapmaya gerek yok
- ✅ ValidationBehavior validator'ları otomatik bulur
- ✅ Yeni validator eklendiğinde otomatik çalışır

**Örnek:**
```csharp
// ❌ Manuel kayıt (yapmıyoruz)
builder.Services.AddScoped<IValidator<StoreBasketCommand>, StoreBasketValidator>();
builder.Services.AddScoped<IValidator<CheckoutBasketCommand>, CheckoutBasketValidator>();
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

### Program.cs'de Yaptığımız (Satır 30-36):

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
5. **Generic Type Matching** → Handler'lar otomatik eşleşiyor

---

## 9. Sonuç

**Kısa Cevap:** Behavior'ları yazdık ama kullanmıyoruz çünkü MediatR otomatik kullanıyor. `_mediator.Send()` çağrıldığında MediatR pipeline'ı çalıştırıyor ve behavior'lar otomatik devreye giriyor.

**Uzun Cevap:** Program.cs'de yaptığımız kayıtlar sayesinde:
- Handler'lar ve validator'lar otomatik bulunuyor (reflection)
- Behavior'lar pipeline'a ekleniyor
- Her `_mediator.Send()` çağrısında MediatR pipeline'ı otomatik çalıştırıyor
- Behavior'lar `next()` delegate'i ile zincir halinde çalışıyor
- Handler en son çalışıyor
- ValidationBehavior, DI container'dan validator'ları otomatik alıyor ve çalıştırıyor

**Felsefe:** Separation of Concerns, DRY, Open/Closed Principle ve Decorator Pattern sayesinde kod daha temiz, test edilebilir ve genişletilebilir oluyor.

---

**Son Güncelleme:** Aralık 2024
