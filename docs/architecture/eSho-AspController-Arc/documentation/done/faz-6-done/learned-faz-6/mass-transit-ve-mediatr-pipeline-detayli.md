# MassTransit ve MediatR Pipeline Detaylı Açıklama

> Bu dosya, Faz 6.4'te öğrendiğim MassTransit Consumer'ın nasıl çalıştığını ve MediatR pipeline'ına nasıl entegre edildiğini detaylı olarak açıklar.

---

## IConsumer<BasketCheckoutEvent> Nedir?

`IConsumer<BasketCheckoutEvent>`, MassTransit'ın RabbitMQ'dan gelen event'leri işlemek için kullandığı interface'dir. `BasketCheckoutConsumer` bu interface'i implement eder.

**Ne işe yarar:**
- RabbitMQ'dan `BasketCheckoutEvent` geldiğinde `Consume` metodu otomatik çağrılır
- MassTransit, consumer'ı RabbitMQ queue'suna bağlar
- Event geldiğinde consumer çalışır

**Örnek:**
```csharp
public class BasketCheckoutConsumer : IConsumer<BasketCheckoutEvent>
{
    public async Task Consume(ConsumeContext<BasketCheckoutEvent> context)
    {
        // Event geldiğinde bu metod otomatik çağrılır
    }
}
```

---

## Program.cs'de MassTransit Konfigürasyonu

### Kod:

```csharp
// MassTransit (Consumer)
builder.Services.AddMassTransit(config =>
{
    // Consumer'ı ekle
    config.AddConsumer<BasketCheckoutConsumer>();

    config.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["MessageBroker:Host"], "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Endpoint'leri otomatik configure et
        cfg.ConfigureEndpoints(context);
    });
});
```

### Adım Adım Ne Oluyor:

#### 1. AddMassTransit(config => ...)

**Ne yapar:**
- MassTransit servislerini DI container'a ekler
- Consumer'ları, bus'ı ve diğer MassTransit bileşenlerini kaydeder
- MassTransit'in temel yapılandırmasını başlatır

**Arka planda ne oluyor:**
```csharp
// MassTransit otomatik olarak şunları yapar:
services.AddSingleton<IBusControl, ...>();
services.AddSingleton<IBus, ...>();
services.AddScoped<IConsumerFactory<BasketCheckoutEvent>, ...>();
// ... diğer MassTransit servisleri
```

#### 2. config.AddConsumer<BasketCheckoutConsumer>()

**Ne yapar:**
- Consumer'ı MassTransit'a kaydeder
- MassTransit, `IConsumer<T>` implement eden class'ları bulur
- DI container'a **scoped** olarak kaydeder
- Her mesaj için yeni bir consumer instance oluşturulur

**Arka planda ne oluyor:**
```csharp
// MassTransit otomatik olarak şunu yapar:
services.AddScoped<IConsumer<BasketCheckoutEvent>, BasketCheckoutConsumer>();

// Consumer factory oluşturulur:
services.AddScoped<IConsumerFactory<BasketCheckoutEvent>>(sp =>
{
    return new ConsumerFactory<BasketCheckoutConsumer>(sp);
});
```

**Neden Scoped?**
- Her RabbitMQ mesajı için yeni bir consumer instance oluşturulur
- Consumer içinde DbContext gibi scoped servisler kullanılabilir
- Thread-safe değildir (her mesaj ayrı thread'de işlenir)

#### 3. config.UsingRabbitMq((context, cfg) => ...)

**Ne yapar:**
- RabbitMQ provider'ı yapılandırır
- RabbitMQ host, username, password ayarları yapılır
- Connection string `appsettings.json`'dan okunur: `amqp://guest:guest@localhost:5673`

**Arka planda ne oluyor:**
```csharp
// RabbitMQ connection factory oluşturulur:
var connectionFactory = new ConnectionFactory
{
    Uri = new Uri("amqp://guest:guest@localhost:5673"),
    AutomaticRecoveryEnabled = true
};

// RabbitMQ connection oluşturulur:
var connection = connectionFactory.CreateConnection();
```

#### 4. cfg.ConfigureEndpoints(context)

**Ne yapar:**
- Consumer'ları RabbitMQ queue'larına bağlar
- Queue adı: `BasketCheckoutEvent` (event adından türetilir)
- Exchange: `BasketCheckoutEvent` (topic exchange)
- Routing key: `BasketCheckoutEvent`
- MassTransit otomatik olarak queue, exchange ve binding'leri oluşturur

**Arka planda ne oluyor:**
```
RabbitMQ'da şunlar oluşturulur:

1. Exchange: BasketCheckoutEvent
   - Type: topic
   - Durable: true
   - Auto-delete: false

2. Queue: BasketCheckoutEvent
   - Durable: true
   - Auto-delete: false
   - Exclusive: false

3. Binding:
   - Exchange: BasketCheckoutEvent
   - Queue: BasketCheckoutEvent
   - Routing Key: BasketCheckoutEvent
```

**Consumer nasıl queue'ya bağlanır:**
```csharp
// MassTransit otomatik olarak şunu yapar:
cfg.ReceiveEndpoint("BasketCheckoutEvent", e =>
{
    e.Consumer<BasketCheckoutConsumer>(context);
});
```

**Uygulama başladığında ne oluyor:**
```csharp
// Uygulama başladığında (app.Run()):
1. MassTransit bus başlatılır
2. RabbitMQ'ya bağlanılır
3. Queue'lar oluşturulur (yoksa)
4. Exchange'ler oluşturulur (yoksa)
5. Binding'ler oluşturulur (yoksa)
6. Queue dinlemeye başlanır (consumer aktif olur)
```

---

## RabbitMQ Mesaj Alma Mekanizması

### Mesaj Gönderme (Basket Service)

**Basket Service'te ne oluyor:**
```csharp
// Basket.API/Features/Basket/Commands/CheckoutBasket/CheckoutBasketHandler.cs
public async Task<bool> Handle(CheckoutBasketCommand request, CancellationToken cancellationToken)
{
    // 1. Sepeti al
    var basket = await _repository.GetBasket(request.UserName);
    
    // 2. Event oluştur
    var eventMessage = _mapper.Map<BasketCheckoutEvent>(request);
    eventMessage = eventMessage with { TotalPrice = basket.TotalPrice };
    
    // 3. RabbitMQ'ya gönder
    await _publishEndpoint.Publish(eventMessage, cancellationToken);
    
    // 4. Sepeti sil
    await _repository.DeleteBasket(request.UserName);
    
    return true;
}
```

**Publish işlemi ne yapar:**
```
1. IPublishEndpoint.Publish() çağrılır
2. MassTransit, event'i serialize eder (JSON)
3. RabbitMQ Exchange'e gönderilir
4. Exchange, routing key'e göre queue'ya yönlendirir
5. Mesaj queue'ya yazılır
```

### RabbitMQ'da Mesajın Yolculuğu

```
1. Basket Service → Publish
   └─> BasketCheckoutEvent serialize edilir (JSON)
   └─> RabbitMQ Exchange'e gönderilir

2. Exchange (BasketCheckoutEvent)
   └─> Routing key: "BasketCheckoutEvent"
   └─> Binding'e göre queue'ya yönlendirilir

3. Queue (BasketCheckoutEvent)
   └─> Mesaj queue'ya yazılır
   └─> Mesaj beklemeye alınır (consumer dinliyor mu?)

4. Consumer (Ordering Service)
   └─> Queue'yu dinliyor
   └─> Mesaj geldiğinde alır
```

### MassTransit'in Queue'yu Dinlemesi

**Uygulama başladığında:**

```csharp
// Program.cs'de app.Run() çağrıldığında:

1. MassTransit bus başlatılır
   └─> IBusControl.StartAsync() çağrılır

2. RabbitMQ'ya bağlanılır
   └─> Connection oluşturulur
   └─> Channel oluşturulur

3. Queue dinlemeye başlanır
   └─> BasicConsume() çağrılır
   └─> Queue'dan mesaj beklenir
```

**Arka planda ne oluyor:**
```csharp
// MassTransit otomatik olarak şunu yapar:
var channel = connection.CreateModel();
channel.BasicConsume(
    queue: "BasketCheckoutEvent",
    autoAck: false,  // Manuel acknowledgment (başarılı olursa ack edilir)
    consumer: new EventingBasicConsumer(channel)
);

// Mesaj geldiğinde:
consumer.Received += async (model, ea) =>
{
    // 1. Mesaj alınır
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    
    // 2. Deserialize edilir
    var event = JsonSerializer.Deserialize<BasketCheckoutEvent>(message);
    
    // 3. Consumer bulunur
    var consumer = serviceProvider.GetService<IConsumer<BasketCheckoutEvent>>();
    
    // 4. Consume metodu çağrılır
    var consumeContext = new ConsumeContext<BasketCheckoutEvent>(event);
    await consumer.Consume(consumeContext);
    
    // 5. Başarılı olursa acknowledgment gönderilir
    channel.BasicAck(ea.DeliveryTag, false);
};
```

### Mesaj Geldiğinde Ne Oluyor?

**Adım adım süreç:**

```
1. RabbitMQ Queue'dan Mesaj Gelir
   └─> Queue: BasketCheckoutEvent
   └─> Mesaj: { UserName: "john", TotalPrice: 1000, ... }

2. MassTransit Mesajı Alır
   └─> BasicConsume callback tetiklenir
   └─> Mesaj deserialize edilir
   └─> BasketCheckoutEvent object'i oluşturulur

3. Consumer Bulunur
   └─> DI container'dan IConsumer<BasketCheckoutEvent> alınır
   └─> BasketCheckoutConsumer instance oluşturulur (scoped)
   └─> Dependency'ler inject edilir (IMediator, IMapper, ILogger)

4. Consume Metodu Çağrılır
   └─> BasketCheckoutConsumer.Consume(context) çağrılır
   └─> context.Message → RabbitMQ'dan gelen event

5. Consumer İşlemi Yapar
   └─> Event → Command mapping
   └─> MediatR.Send(command)
   └─> Handler çalışır
   └─> Sipariş oluşturulur

6. Acknowledgment (ACK)
   └─> Başarılı olursa: channel.BasicAck() → Mesaj queue'dan silinir
   └─> Hata olursa: throw → Mesaj queue'da kalır, retry yapılır
```

### Mesaj Acknowledgment Mekanizması

**Neden önemli?**
- Mesaj işlendikten sonra queue'dan silinmesi için gerekli
- Mesaj kaybını önler
- Retry mekanizması için gerekli

**Nasıl çalışır:**
```csharp
// Başarılı olursa:
try
{
    await consumer.Consume(context);
    channel.BasicAck(deliveryTag, false); // Mesaj queue'dan silinir
}
catch (Exception ex)
{
    channel.BasicNack(deliveryTag, false, true); // Mesaj queue'da kalır, retry yapılır
}
```

**MassTransit otomatik yapar:**
- Consumer başarılı olursa → Otomatik ACK
- Consumer exception fırlatırsa → Otomatik NACK (retry)

**Retry mekanizması:**
```
1. İlk deneme: Hemen
2. İkinci deneme: 1 saniye sonra
3. Üçüncü deneme: 2 saniye sonra
4. Dördüncü deneme: 4 saniye sonra
5. Başarısız olursa: Dead Letter Queue'ya gönderilir
```

### Consumer'ın Consume Metodunun Çağrılması

**Tam akış:**

```csharp
// 1. RabbitMQ'dan mesaj geldiğinde:
MassTransit → BasicConsume callback → Mesaj alındı

// 2. MassTransit consumer'ı bulur:
var consumerFactory = serviceProvider.GetService<IConsumerFactory<BasketCheckoutEvent>>();
var consumer = consumerFactory.Create(); // Yeni instance (scoped)

// 3. ConsumeContext oluşturulur:
var context = new ConsumeContext<BasketCheckoutEvent>
{
    Message = deserializedEvent,
    CancellationToken = cancellationToken,
    // ... diğer context bilgileri
};

// 4. Consumer.Consume() çağrılır:
await consumer.Consume(context);

// 5. Başarılı olursa:
channel.BasicAck(deliveryTag, false); // Mesaj silinir

// 6. Hata olursa:
throw; // MassTransit retry yapar
```

**Consumer instance oluşturma:**
```csharp
// Her mesaj için yeni consumer instance:
var scope = serviceProvider.CreateScope(); // Scoped lifetime
var consumer = scope.ServiceProvider.GetService<BasketCheckoutConsumer>();

// Dependency'ler inject edilir:
// - IMediator (singleton)
// - IMapper (singleton)
// - ILogger<BasketCheckoutConsumer> (singleton)
// - DbContext (scoped) - eğer kullanılıyorsa
```

---

## BasketCheckoutConsumer.cs Detaylı Açıklama

### Kod:

```csharp
using AutoMapper;
using BuildingBlocks.Messaging.Events;
using MassTransit;
using MediatR;
using Ordering.API.Features.Orders.Commands.CreateOrder;

namespace Ordering.API.EventHandlers;

public class BasketCheckoutConsumer : IConsumer<BasketCheckoutEvent>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger<BasketCheckoutConsumer> _logger;

    public BasketCheckoutConsumer(
        IMediator mediator,
        IMapper mapper,
        ILogger<BasketCheckoutConsumer> logger)
    {
        _mediator = mediator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BasketCheckoutEvent> context)
    {
        _logger.LogInformation("BasketCheckoutEvent consumed. UserName: {UserName}, TotalPrice: {TotalPrice}",
            context.Message.UserName, context.Message.TotalPrice);

        try
        {
            // 1. Event'ten Command oluştur
            var command = _mapper.Map<CreateOrderCommand>(context.Message);

            // 2. MediatR ile CreateOrderHandler'ı çağır
            var orderId = await _mediator.Send(command);

            _logger.LogInformation("Order created from BasketCheckoutEvent. OrderId: {OrderId}, UserName: {UserName}",
                orderId, context.Message.UserName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing BasketCheckoutEvent for UserName: {UserName}",
                context.Message.UserName);
            throw; // MassTransit otomatik retry yapar
        }
    }
}
```

### Adım Adım Ne Oluyor:

#### 1. Constructor Injection

**Dependency'ler:**
- `IMediator` → MediatR mediator (handler'ları çağırmak için)
- `IMapper` → AutoMapper (Event → Command mapping için)
- `ILogger<BasketCheckoutConsumer>` → Logging için

**Neden bu dependency'ler?**
- **IMediator:** Consumer, business logic'i direkt yapmaz, MediatR üzerinden handler'ı çağırır
- **IMapper:** Event'i Command'e çevirmek için gerekli
- **ILogger:** İşlemleri loglamak için

#### 2. Consume Metodu

**Ne zaman çağrılır:**
- RabbitMQ'dan `BasketCheckoutEvent` mesajı geldiğinde
- MassTransit otomatik olarak `Consume` metodunu çağırır
- Her mesaj için yeni bir consumer instance oluşturulur

**ConsumeContext<BasketCheckoutEvent> context:**
- `context.Message` → RabbitMQ'dan gelen event mesajı
- `context.CancellationToken` → İptal token'ı
- `context.Respond<T>()` → Response göndermek için (request-response pattern)
- `context.Publish<T>()` → Başka event publish etmek için

#### 3. Event → Command Mapping

```csharp
var command = _mapper.Map<CreateOrderCommand>(context.Message);
```

**Ne yapar:**
- `BasketCheckoutEvent` → `CreateOrderCommand` mapping yapılır
- AutoMapper, `MappingProfile`'daki mapping'i kullanır
- `Items` boş liste olarak atanır (event'te Items yok)

**Mapping detayı:**
```csharp
// MappingProfile.cs'de:
CreateMap<BasketCheckoutEvent, CreateOrderCommand>()
    .ForMember(dest => dest.Items, opt => opt.MapFrom(src => new List<OrderItemDto>()));
```

#### 4. MediatR ile Handler Çağırma

```csharp
var orderId = await _mediator.Send(command);
```

**Ne yapar:**
- MediatR, `CreateOrderCommand` için handler'ı bulur
- MediatR pipeline'ından geçirir (LoggingBehavior → ValidationBehavior → Handler)
- Handler çalışır ve OrderId döner

**Arka planda ne oluyor:**
```
1. MediatR, CreateOrderCommand için handler'ı bulur:
   → CreateOrderHandler : IRequestHandler<CreateOrderCommand, Guid>

2. Pipeline behavior'ları çalışır:
   → LoggingBehavior (request loglanır)
   → ValidationBehavior (FluentValidation çalışır)
   → CreateOrderHandler.Handle() çalışır

3. Handler sonucu döner:
   → OrderId (Guid)
```

#### 5. Exception Handling

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error processing BasketCheckoutEvent for UserName: {UserName}",
        context.Message.UserName);
    throw; // MassTransit otomatik retry yapar
}
```

**Ne yapar:**
- Hata olursa log yazar
- Exception'ı tekrar fırlatır
- MassTransit otomatik retry mekanizması devreye girer

**MassTransit Retry Mekanizması:**
- İlk retry: 1 saniye sonra
- İkinci retry: 2 saniye sonra
- Üçüncü retry: 4 saniye sonra
- Başarısız olursa Dead Letter Queue'ya gönderilir

---

## Program.cs'de MediatR Konfigürasyonu (Karşılaştırma)

### Kod:

```csharp
// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});
```

### Adım Adım Ne Oluyor:

#### 1. AddMediatR(cfg => ...)

**Ne yapar:**
- MediatR servislerini DI container'a ekler
- MediatR'ın temel yapılandırmasını başlatır

**Arka planda ne oluyor:**
```csharp
// MediatR otomatik olarak şunları yapar:
services.AddSingleton<IMediator, Mediator>();
services.AddTransient<ServiceFactory>(sp => sp.GetService);
// ... diğer MediatR servisleri
```

#### 2. cfg.RegisterServicesFromAssembly(typeof(Program).Assembly)

**Ne yapar:**
- Assembly'deki tüm `IRequestHandler<TRequest, TResponse>` implement eden class'ları bulur
- DI container'a kaydeder
- Handler'lar otomatik olarak bulunur ve kaydedilir

**Arka planda ne oluyor:**
```csharp
// MediatR otomatik olarak şunu yapar:
// Assembly'de IRequestHandler<TRequest, TResponse> implement eden tüm class'ları bulur

// Örnek:
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    // Bu class otomatik bulunur ve kaydedilir:
    // services.AddScoped<IRequestHandler<CreateOrderCommand, Guid>, CreateOrderHandler>();
}
```

**Handler nasıl bulunur:**
```csharp
// Reflection ile:
var handlerTypes = assembly.GetTypes()
    .Where(t => t.GetInterfaces()
        .Any(i => i.IsGenericType && 
                  i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
    .ToList();

// Her handler için:
foreach (var handlerType in handlerTypes)
{
    var interfaceType = handlerType.GetInterfaces()
        .First(i => i.IsGenericType && 
                    i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
    
    services.AddScoped(interfaceType, handlerType);
}
```

#### 3. cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))

**Ne yapar:**
- Pipeline behavior'ları ekler
- `LoggingBehavior` → Tüm request'leri loglar
- `ValidationBehavior` → FluentValidation ile doğrulama yapar

**Arka planda ne oluyor:**
```csharp
// MediatR otomatik olarak şunu yapar:
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

**Pipeline nasıl çalışır:**
```csharp
// MediatR.Send(command) çağrıldığında:

1. Handler bulunur:
   → CreateOrderHandler : IRequestHandler<CreateOrderCommand, Guid>

2. Pipeline oluşturulur:
   → LoggingBehavior → ValidationBehavior → CreateOrderHandler

3. Pipeline çalışır:
   → LoggingBehavior.Handle() → ValidationBehavior.Handle() → CreateOrderHandler.Handle()
```

---

## İki Pipeline'ın Birlikte Çalışması

### Tam Akış:

```
1. Basket Service → CheckoutBasketHandler
   └─> BasketCheckoutEvent publish edilir
   └─> RabbitMQ Queue'ya gönderilir

2. RabbitMQ Queue → MassTransit
   └─> BasketCheckoutEvent mesajı alınır
   └─> MassTransit, consumer'ı bulur (IConsumer<BasketCheckoutEvent>)
   └─> BasketCheckoutConsumer.Consume() çağrılır

3. BasketCheckoutConsumer.Consume()
   └─> Event loglanır
   └─> Event → Command mapping (AutoMapper)
   └─> MediatR.Send(CreateOrderCommand) çağrılır

4. MediatR Pipeline
   └─> Handler bulunur (IRequestHandler<CreateOrderCommand, Guid>)
   └─> LoggingBehavior çalışır (request loglanır)
   └─> ValidationBehavior çalışır (FluentValidation)
   └─> CreateOrderHandler.Handle() çalışır (business logic)
   └─> OrderId döner

5. Consumer
   └─> OrderId alır
   └─> Log yazar
   └─> Başarılı olursa mesaj acknowledge edilir
   └─> Hata olursa throw edilir (MassTransit retry yapar)
```

### Pipeline Karşılaştırması:

| Özellik | MassTransit Pipeline | MediatR Pipeline |
|---------|---------------------|------------------|
| **Amaç** | RabbitMQ event'lerini işlemek | CQRS pattern (Commands/Queries) |
| **Interface** | `IConsumer<T>` | `IRequestHandler<TRequest, TResponse>` |
| **Kayıt** | `AddConsumer<T>()` | `RegisterServicesFromAssembly()` |
| **Nasıl Bulunur** | `IConsumer<T>` implement eden class'lar | `IRequestHandler<TRequest, TResponse>` implement eden class'lar |
| **Çalışma** | RabbitMQ'dan event geldiğinde | `MediatR.Send()` çağrıldığında |
| **Behavior'lar** | MassTransit middleware | MediatR pipeline behaviors |
| **Retry** | MassTransit otomatik retry | Manuel retry (gerekirse) |
| **Lifetime** | Scoped (her mesaj için yeni instance) | Scoped (her request için yeni instance) |

---

## Neden Bu Yapı?

### Separation of Concerns (Sorumlulukların Ayrılması)

**Consumer'ın Sorumluluğu:**
- RabbitMQ'dan event almak
- Event'i Command'e çevirmek (AutoMapper ile)
- MediatR'ı çağırmak
- Exception handling (MassTransit retry için)

**Handler'ın Sorumluluğu:**
- Business logic (sipariş oluşturma)
- Veritabanı işlemleri
- Entity mapping

### Business Logic Handler'da

**Avantajlar:**
- Handler'ı ayrı test edebiliriz
- REST API ve Consumer aynı handler'ı kullanır
- Code duplication yok
- Single Responsibility Principle

**Örnek:**
```csharp
// REST API'den:
POST /api/orders
→ OrdersController → MediatR.Send(CreateOrderCommand) → CreateOrderHandler

// RabbitMQ'dan:
BasketCheckoutEvent
→ BasketCheckoutConsumer → MediatR.Send(CreateOrderCommand) → CreateOrderHandler
```

### Test Edilebilirlik

**Handler'ı test etmek:**
```csharp
// Handler'ı direkt test edebiliriz
var handler = new CreateOrderHandler(context, mapper, logger);
var result = await handler.Handle(command, cancellationToken);
```

**Consumer'ı test etmek:**
```csharp
// Consumer'ı test ederken sadece MediatR mock'lanır
var mediator = new Mock<IMediator>();
var consumer = new BasketCheckoutConsumer(mediator.Object, mapper, logger);
```

### Consistency (Tutarlılık)

**Aynı iş mantığı:**
- REST API'den sipariş oluşturma → `CreateOrderHandler`
- RabbitMQ'dan sipariş oluşturma → `CreateOrderHandler`
- Her ikisi de aynı validation, logging, business logic'i kullanır

---

## Sonuç

**Event-Driven Architecture + CQRS Pattern:**

1. **MassTransit Pipeline** → RabbitMQ event'lerini alır ve Consumer'a yönlendirir
2. **Consumer** → Event'i Command'e çevirir ve MediatR'ı çağırır
3. **MediatR Pipeline** → Handler'ı bulur, behavior'lardan geçirir ve çalıştırır
4. **Handler** → Business logic'i uygular ve sonucu döner

Bu yapı sayesinde:
- ✅ Loose coupling (Basket Service, Ordering Service'i bilmez)
- ✅ Separation of concerns (Consumer sadece event alır, Handler iş mantığını yapar)
- ✅ Test edilebilirlik (Handler'ı ayrı test edebiliriz)
- ✅ Consistency (REST API ve Consumer aynı handler'ı kullanır)
- ✅ Scalability (Birden fazla Ordering Service instance çalışabilir)

---

**Tarih:** Aralık 2024  
**Faz:** Faz 6.4 - Ordering RabbitMQ Consumer  
**Konu:** MassTransit ve MediatR Pipeline Detaylı Açıklama

