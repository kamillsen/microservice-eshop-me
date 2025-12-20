# Faz 6 - Ordering Service (RabbitMQ Consumer)

## Servis Hakkında

**Ne İşe Yarar?**
- Kullanıcıların siparişlerini yönetir
- **Otomatik sipariş oluşturma:** Basket checkout event'ini dinler (RabbitMQ)
- Sipariş listesini gösterme
- Sipariş detaylarını gösterme
- Kullanıcıya göre sipariş arama
- Sipariş durumunu güncelleme (Admin)
- Sipariş iptal etme

**Örnek Kullanım Senaryosu:**
```
1. Kullanıcı: Basket Service'te "Ödeme yap" (Checkout)
   → Basket Service: BasketCheckoutEvent RabbitMQ'ya gönderir

2. Ordering Service: Event'i dinledi (BasketCheckoutConsumer)
   → Event'ten CreateOrderCommand oluştur
   → MediatR ile CreateOrderHandler'ı çağır
   → Siparişi PostgreSQL'e kaydet
   → Sipariş numarası oluştur
   → Response: { orderId, orderDate, totalPrice, status: "Pending" }

3. Kullanıcı: "Siparişlerimi göster"
   → Ordering Service: Kullanıcıya ait siparişleri getir
   → Response: [
       { orderId: "guid", orderDate: "2024-12-01", totalPrice: 145000, status: "Pending" },
       { orderId: "guid", orderDate: "2024-11-28", totalPrice: 50000, status: "Shipped" }
     ]
```

**Neden şimdi?** 
- ✅ Basket hazır (checkout event gönderiyor)
- ✅ RabbitMQ hazır (event queue çalışıyor)
- ✅ Artık sipariş işlemleri yapılabilir
- ✅ En karmaşık servis (Consumer + CQRS + MediatR)

**Neden RabbitMQ Consumer?**
- Basket Service checkout yaptığında asenkron olarak sipariş oluşturulmalı
- Decoupling (Basket Service, Ordering Service'i bilmez)
- Event-driven architecture (loosely coupled)
- Retry mekanizması (event başarısız olursa tekrar dener)
- Scalability (birden fazla Ordering Service instance çalışabilir)

**Neden PostgreSQL?**
- Siparişler kalıcı veri (silinmemeli)
- İlişkisel yapı gerekli (Order → OrderItems)
- Sorgulama ve raporlama için ideal
- ACID garantisi (transaction güvenliği)

---

## 6.1 Ordering.API Projesi Oluştur

**Hedef:** RabbitMQ consumer REST API projesi

### Görevler:

#### Ordering klasör yapısını oluştur
**Ne işe yarar:** Ordering servisi için klasör oluşturur.

```bash
cd src/Services
mkdir Ordering
cd Ordering
```

**Açıklama:**
- `src/Services/Ordering/` klasörü oluşturulur
- Catalog ve Basket gibi aynı yapıda olacak

#### Web API projesi oluştur
**Ne işe yarar:** Ordering Service için REST API projesi oluşturur.

```bash
cd src/Services/Ordering
dotnet new webapi -n Ordering.API
```

**Açıklama:**
- `webapi` template'i ile proje oluşturulur
- Otomatik olarak `Controllers/`, `Program.cs`, `appsettings.json` oluşturulur
- Swagger konfigürasyonu hazır gelir

#### Projeyi solution'a ekle
**Ne işe yarar:** Projeyi solution'a ekler, böylece diğer projeler referans verebilir.

```bash
cd ../../..
dotnet sln add src/Services/Ordering/Ordering.API/Ordering.API.csproj
```

#### NuGet paketlerini ekle
**Ne işe yarar:** PostgreSQL, MediatR, MassTransit ve diğer gerekli paketleri ekler.

```bash
cd src/Services/Ordering/Ordering.API
dotnet add package MediatR
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
dotnet add package AutoMapper
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package MassTransit
dotnet add package MassTransit.RabbitMQ
dotnet add package AspNetCore.HealthChecks.NpgSql
dotnet add package AspNetCore.HealthChecks.RabbitMQ
```

**Paketler:**
- `MediatR` → CQRS pattern için
- `FluentValidation` → Request validation için
- `AutoMapper` → Object mapping için
- `Microsoft.EntityFrameworkCore` → EF Core (PostgreSQL için)
- `Microsoft.EntityFrameworkCore.Design` → EF Core migrations için
- `Npgsql.EntityFrameworkCore.PostgreSQL` → PostgreSQL provider
- `MassTransit` → RabbitMQ abstraction
- `MassTransit.RabbitMQ` → RabbitMQ provider (Consumer için)
- `AspNetCore.HealthChecks.NpgSql` → PostgreSQL health check
- `AspNetCore.HealthChecks.RabbitMQ` → RabbitMQ health check

#### Project References ekle
**Ne işe yarar:** BuildingBlocks projelerini referans olarak ekler.

```bash
dotnet add reference ../../../BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj
dotnet add reference ../../../BuildingBlocks/BuildingBlocks.Behaviors/BuildingBlocks.Behaviors.csproj
dotnet add reference ../../../BuildingBlocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj
```

**Açıklama:**
- `BuildingBlocks.Exceptions` → Exception handling için
- `BuildingBlocks.Behaviors` → MediatR pipeline behaviors (Validation, Logging)
- `BuildingBlocks.Messaging` → BasketCheckoutEvent için

#### Klasör yapısını oluştur
**Ne işe yarar:** Entity, Data, Features, EventHandlers için klasör yapısını oluşturur.

```bash
mkdir -p Entities
mkdir -p Data
mkdir -p Features/Orders
mkdir -p Features/Orders/Commands
mkdir -p Features/Orders/Queries
mkdir -p EventHandlers
mkdir -p Dtos
mkdir -p Mapping
```

**Klasör Açıklamaları:**

1. **`Entities/`** → Sipariş entity'leri
   - **`Order.cs`**: Sipariş entity (Id, UserName, TotalPrice, OrderDate, Status, Items)
   - **`OrderItem.cs`**: Sipariş item entity (Id, OrderId FK, ProductId, ProductName, Quantity, Price)
   - **Neden?**: PostgreSQL'de saklanacak veri yapısı

2. **`Data/`** → EF Core DbContext
   - **`OrderingDbContext.cs`**: PostgreSQL DbContext
   - **`Migrations/`**: EF Core migrations
   - **Neden?**: Veritabanı işlemleri için

3. **`Features/Orders/`** → CQRS (Vertical Slice)
   - **`Commands/`**: Yazma işlemleri (CreateOrder, UpdateOrder, DeleteOrder)
   - **`Queries/`**: Okuma işlemleri (GetOrders, GetOrderById, GetOrdersByUser)
   - **Neden?**: CQRS pattern, her feature kendi klasöründe

4. **`EventHandlers/`** → RabbitMQ Consumer
   - **`BasketCheckoutConsumer.cs`**: BasketCheckoutEvent'i dinleyen consumer
   - **Neden?**: RabbitMQ event'lerini işlemek için

5. **`Dtos/`** → Data Transfer Objects
   - **`OrderDto.cs`**: Sipariş DTO
   - **`OrderItemDto.cs`**: Sipariş item DTO
   - **Neden?**: API response'ları için

6. **`Mapping/`** → AutoMapper profiles
   - **`MappingProfile.cs`**: Entity ↔ DTO, Event → Command mapping
   - **Neden?**: Object mapping için

#### Order ve OrderItem Entity'lerini oluştur
**Ne işe yarar:** Sipariş verilerini temsil eden entity class'larını oluşturur.

**Entities/Order.cs:**

```csharp
namespace Ordering.API.Entities;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserName { get; set; } = default!;
    public decimal TotalPrice { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    public List<OrderItem> Items { get; set; } = new();
}

public enum OrderStatus
{
    Pending = 0,
    Shipped = 1,
    Delivered = 2,
    Cancelled = 3
}
```

**Entities/OrderItem.cs:**

```csharp
namespace Ordering.API.Entities;

public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

**Açıklama:**
- `Order` → Sipariş (Id, UserName, TotalPrice, OrderDate, Status enum, Items navigation property)
- `OrderItem` → Sipariş item'ı (Id, OrderId FK, Order navigation, ProductId, ProductName, Quantity, Price)
- `OrderStatus` → Enum (Pending, Shipped, Delivered, Cancelled)

**Neden enum?**
- Status değerleri sabit (type-safe)
- Veritabanında integer olarak saklanır
- Kod okunabilirliği artar

#### appsettings.json'a connection string ve ayarları ekle
**Ne işe yarar:** PostgreSQL ve RabbitMQ bağlantı bilgilerini ekler.

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5435;Database=OrderingDb;Username=postgres;Password=postgres"
  },
  "MessageBroker": {
    "Host": "amqp://guest:guest@localhost:5673"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**Açıklama:**
- `ConnectionStrings:Database` → PostgreSQL bağlantı string'i (localhost:5435 - host port)
- `MessageBroker:Host` → RabbitMQ bağlantı string'i (localhost:5673 - host port)

**Not:** 
- Localhost'tan bağlanırken: `localhost:5435`, `localhost:5673`
- Container network içinde: `orderingdb:5432`, `amqp://guest:guest@messagebroker:5672`

### Kontrol:
- Proje build oluyor mu? (`dotnet build`)
- Solution'da görünüyor mu? (`dotnet sln list`)
- Tüm paketler eklendi mi? (`cat Ordering.API.csproj`)

---

## 6.2 Ordering Database & Seed Data

**Hedef:** PostgreSQL veritabanı ve tablo yapısı

### Görevler:

#### OrderingDbContext oluştur
**Ne işe yarar:** EF Core ile PostgreSQL veritabanı bağlantısını sağlar.

**Data/OrderingDbContext.cs:**

```csharp
using Ordering.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ordering.API.Data;

public class OrderingDbContext : DbContext
{
    public OrderingDbContext(DbContextOptions<OrderingDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserName);
            entity.Property(e => e.Status)
                .HasConversion<int>(); // Enum'u integer'a çevir
            entity.HasMany(e => e.Items)
                .WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        base.OnModelCreating(modelBuilder);
    }
}
```

**Açıklama:**
- `DbSet<Order>` → Orders tablosu
- `DbSet<OrderItem>` → OrderItems tablosu
- `HasConversion<int>()` → OrderStatus enum'u integer'a çevirir (PostgreSQL'de integer olarak saklanır)
- `OnDelete(DeleteBehavior.Cascade)` → Order silinince OrderItems da silinir

#### Program.cs'de DbContext kaydı
**Ne işe yarar:** PostgreSQL DbContext'i DI container'a kaydeder.

**Program.cs güncellemesi:**

```csharp
using Ordering.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL
builder.Services.AddDbContext<OrderingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// ... diğer servisler

var app = builder.Build();

// Migration
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
    await context.Database.MigrateAsync();
}

// ... diğer middleware'ler

app.Run();
```

**Açıklama:**
- `AddDbContext<OrderingDbContext>` → PostgreSQL DbContext scoped (her request için yeni)
- `MigrateAsync()` → Migration'ları otomatik uygular (container başladığında)

#### EF Core Migration oluştur
**Ne işe yarar:** Veritabanı tablolarını oluşturur.

```bash
cd src/Services/Ordering/Ordering.API
export DOTNET_ROOT=/usr/lib64/dotnet  # Fedora için
dotnet ef migrations add InitialCreate --project . --startup-project .
```

**Açıklama:**
- `InitialCreate` → İlk migration (Orders ve OrderItems tabloları)
- Migration dosyası `Data/Migrations/` klasöründe oluşur

**Not:** Seed data gerekmez (siparişler kullanıcı işlemleriyle oluşur)

### Test:
- Migration oluştu mu? (`ls Data/Migrations/`)
- Container'da DB oluştu mu? (`docker exec orderingdb psql -U postgres -d OrderingDb -c "\dt"`)
- Tablolar doğru mu? (Orders, OrderItems)

---

## 6.3 Ordering CQRS - Commands & Queries

**Hedef:** Sipariş işlemleri (CQRS + MediatR)

### Görevler:

#### MediatR, FluentValidation, AutoMapper konfigürasyonu
**Ne işe yarar:** CQRS pattern için gerekli servisleri kaydeder.

**Program.cs güncellemesi:**

```csharp
using BuildingBlocks.Behaviors.Behaviors;
using FluentValidation;
using MediatR;

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

**Açıklama:**
- `RegisterServicesFromAssembly` → Handler'ları otomatik bulur
- `AddBehavior` → Pipeline behaviors ekler (Logging, Validation)
- `AddValidatorsFromAssembly` → Validator'ları otomatik bulur
- `AddAutoMapper` → AutoMapper profile'ları otomatik bulur

#### CreateOrderCommand + CreateOrderHandler + CreateOrderValidator
**Ne işe yarar:** Yeni sipariş oluşturur.

**Features/Orders/Commands/CreateOrder/CreateOrderCommand.cs:**

```csharp
using MediatR;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Commands.CreateOrder;

public record CreateOrderCommand(
    string UserName,
    decimal TotalPrice,
    List<OrderItemDto> Items,
    string FirstName,
    string LastName,
    string EmailAddress,
    string AddressLine,
    string Country,
    string State,
    string ZipCode,
    string CardName,
    string CardNumber,
    string Expiration,
    string CVV,
    int PaymentMethod
) : IRequest<Guid>;
```

**Features/Orders/Commands/CreateOrder/CreateOrderValidator.cs:**

```csharp
using FluentValidation;

namespace Ordering.API.Features.Orders.Commands.CreateOrder;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("UserName boş olamaz");

        RuleFor(x => x.TotalPrice)
            .GreaterThan(0).WithMessage("TotalPrice 0'dan büyük olmalı");

        RuleFor(x => x.Items)
            .NotNull().WithMessage("Items null olamaz")
            .NotEmpty().WithMessage("Items boş olamaz");

        RuleFor(x => x.EmailAddress)
            .NotEmpty().EmailAddress().WithMessage("Geçerli email adresi gerekli");

        RuleFor(x => x.AddressLine)
            .NotEmpty().WithMessage("Adres boş olamaz");
    }
}
```

**Features/Orders/Commands/CreateOrder/CreateOrderHandler.cs:**

```csharp
using AutoMapper;
using MediatR;
using Ordering.API.Data;
using Ordering.API.Entities;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Commands.CreateOrder;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly OrderingDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(
        OrderingDbContext context,
        IMapper mapper,
        ILogger<CreateOrderHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Command'dan Entity oluştur
        var order = _mapper.Map<Order>(request);
        order.Id = Guid.NewGuid();
        order.OrderDate = DateTime.UtcNow;
        order.Status = OrderStatus.Pending;

        // 2. OrderItems'ları ekle
        foreach (var itemDto in request.Items)
        {
            var orderItem = _mapper.Map<OrderItem>(itemDto);
            orderItem.Id = Guid.NewGuid();
            orderItem.OrderId = order.Id;
            order.Items.Add(orderItem);
        }

        // 3. Veritabanına kaydet
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order created. OrderId: {OrderId}, UserName: {UserName}, TotalPrice: {TotalPrice}",
            order.Id, order.UserName, order.TotalPrice);

        return order.Id;
    }
}
```

**Açıklama:**
- `CreateOrder` → Yeni sipariş oluşturur
- Validation → FluentValidation ile doğrulama
- Mapping → AutoMapper ile Command → Entity
- OrderItems → Her item için OrderItem entity oluşturulur

#### UpdateOrderCommand + UpdateOrderHandler
**Ne işe yarar:** Sipariş durumunu günceller (Admin).

**Features/Orders/Commands/UpdateOrder/UpdateOrderCommand.cs:**

```csharp
using MediatR;
using Ordering.API.Entities;

namespace Ordering.API.Features.Orders.Commands.UpdateOrder;

public record UpdateOrderCommand(
    Guid Id,
    OrderStatus Status
) : IRequest<bool>;
```

**Features/Orders/Commands/UpdateOrder/UpdateOrderHandler.cs:**

```csharp
using MediatR;
using Ordering.API.Data;
using Ordering.API.Entities;

namespace Ordering.API.Features.Orders.Commands.UpdateOrder;

public class UpdateOrderHandler : IRequestHandler<UpdateOrderCommand, bool>
{
    private readonly OrderingDbContext _context;
    private readonly ILogger<UpdateOrderHandler> _logger;

    public UpdateOrderHandler(
        OrderingDbContext context,
        ILogger<UpdateOrderHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (order == null)
        {
            _logger.LogWarning("Order not found. OrderId: {OrderId}", request.Id);
            return false;
        }

        order.Status = request.Status;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order updated. OrderId: {OrderId}, NewStatus: {Status}",
            request.Id, request.Status);

        return true;
    }
}
```

#### DeleteOrderCommand + DeleteOrderHandler
**Ne işe yarar:** Siparişi iptal eder (silmez, status'u Cancelled yapar).

**Features/Orders/Commands/DeleteOrder/DeleteOrderCommand.cs:**

```csharp
using MediatR;

namespace Ordering.API.Features.Orders.Commands.DeleteOrder;

public record DeleteOrderCommand(Guid Id) : IRequest<bool>;
```

**Features/Orders/Commands/DeleteOrder/DeleteOrderHandler.cs:**

```csharp
using MediatR;
using Ordering.API.Data;
using Ordering.API.Entities;

namespace Ordering.API.Features.Orders.Commands.DeleteOrder;

public class DeleteOrderHandler : IRequestHandler<DeleteOrderCommand, bool>
{
    private readonly OrderingDbContext _context;
    private readonly ILogger<DeleteOrderHandler> _logger;

    public DeleteOrderHandler(
        OrderingDbContext context,
        ILogger<DeleteOrderHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (order == null)
        {
            _logger.LogWarning("Order not found. OrderId: {OrderId}", request.Id);
            return false;
        }

        // Siparişi silme, sadece iptal et
        order.Status = OrderStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order cancelled. OrderId: {OrderId}", request.Id);

        return true;
    }
}
```

**Açıklama:**
- Siparişler silinmez, sadece `Cancelled` status'u verilir
- Veri bütünlüğü için (raporlama, analiz)

#### GetOrdersQuery + GetOrdersHandler
**Ne işe yarar:** Tüm siparişleri getirir (Admin).

**Features/Orders/Queries/GetOrders/GetOrdersQuery.cs:**

```csharp
using MediatR;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Queries.GetOrders;

public record GetOrdersQuery : IRequest<IEnumerable<OrderDto>>;
```

**Features/Orders/Queries/GetOrders/GetOrdersHandler.cs:**

```csharp
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ordering.API.Data;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Queries.GetOrders;

public class GetOrdersHandler : IRequestHandler<GetOrdersQuery, IEnumerable<OrderDto>>
{
    private readonly OrderingDbContext _context;
    private readonly IMapper _mapper;

    public GetOrdersHandler(OrderingDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<OrderDto>>(orders);
    }
}
```

#### GetOrderByIdQuery + GetOrderByIdHandler
**Ne işe yarar:** Belirli bir siparişi getirir.

**Features/Orders/Queries/GetOrderById/GetOrderByIdQuery.cs:**

```csharp
using MediatR;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Queries.GetOrderById;

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto?>;
```

**Features/Orders/Queries/GetOrderById/GetOrderByIdHandler.cs:**

```csharp
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ordering.API.Data;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Queries.GetOrderById;

public class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly OrderingDbContext _context;
    private readonly IMapper _mapper;

    public GetOrderByIdHandler(OrderingDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order == null)
            return null;

        return _mapper.Map<OrderDto>(order);
    }
}
```

#### GetOrdersByUserQuery + GetOrdersByUserHandler
**Ne işe yarar:** Kullanıcıya ait siparişleri getirir.

**Features/Orders/Queries/GetOrdersByUser/GetOrdersByUserQuery.cs:**

```csharp
using MediatR;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Queries.GetOrdersByUser;

public record GetOrdersByUserQuery(string UserName) : IRequest<IEnumerable<OrderDto>>;
```

**Features/Orders/Queries/GetOrdersByUser/GetOrdersByUserHandler.cs:**

```csharp
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ordering.API.Data;
using Ordering.API.Dtos;

namespace Ordering.API.Features.Orders.Queries.GetOrdersByUser;

public class GetOrdersByUserHandler : IRequestHandler<GetOrdersByUserQuery, IEnumerable<OrderDto>>
{
    private readonly OrderingDbContext _context;
    private readonly IMapper _mapper;

    public GetOrdersByUserHandler(OrderingDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<OrderDto>> Handle(GetOrdersByUserQuery request, CancellationToken cancellationToken)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserName == request.UserName)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<OrderDto>>(orders);
    }
}
```

#### DTO'ları oluştur
**Ne işe yarar:** API response'ları için DTO class'larını oluşturur.

**Dtos/OrderDto.cs:**

```csharp
namespace Ordering.API.Dtos;

public class OrderDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = default!;
    public decimal TotalPrice { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = default!;
    public List<OrderItemDto> Items { get; set; } = new();
}
```

**Dtos/OrderItemDto.cs:**

```csharp
namespace Ordering.API.Dtos;

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

#### AutoMapper MappingProfile oluştur
**Ne işe yarar:** Entity ↔ DTO ve Event → Command mapping'lerini tanımlar.

**Mapping/MappingProfile.cs:**

```csharp
using AutoMapper;
using Ordering.API.Dtos;
using Ordering.API.Entities;
using Ordering.API.Features.Orders.Commands.CreateOrder;
using BuildingBlocks.Messaging.Events;

namespace Ordering.API.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Entity ↔ DTO
        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        CreateMap<OrderItem, OrderItemDto>().ReverseMap();

        // Command → Entity
        CreateMap<CreateOrderCommand, Order>()
            .ForMember(dest => dest.Items, opt => opt.Ignore()); // Items manuel eklenir

        CreateMap<OrderItemDto, OrderItem>();

        // Event → Command (Consumer için)
        CreateMap<BasketCheckoutEvent, CreateOrderCommand>();
    }
}
```

**Açıklama:**
- `Order.Status` → Enum'u string'e çevirir (DTO'da string olarak döner)
- `BasketCheckoutEvent → CreateOrderCommand` → Consumer'da kullanılacak

### Test:
- Handler'lar çalışıyor mu? (Manuel test veya unit test)
- Validation çalışıyor mu? (Geçersiz request gönder)
- Mapping doğru mu? (Entity ↔ DTO)

---

## 6.4 Ordering RabbitMQ Consumer

**Hedef:** BasketCheckoutEvent'i dinle ve sipariş oluştur

### Görevler:

#### BasketCheckoutConsumer oluştur
**Ne işe yarar:** RabbitMQ'dan BasketCheckoutEvent'i dinler ve sipariş oluşturur.

**EventHandlers/BasketCheckoutConsumer.cs:**

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

**Açıklama:**
- `IConsumer<BasketCheckoutEvent>` → MassTransit consumer interface
- `Consume` → Event geldiğinde çalışır
- `IMediator` → CreateOrderCommand'ı MediatR'ye gönderir
- `IMapper` → Event'ten Command'e map eder
- Exception → MassTransit otomatik retry yapar

**Neden MediatR kullanıyoruz?**
- Consumer sadece event'i alır ve Command'e çevirir
- İş mantığı Handler'da (separation of concerns)
- Test edilebilirlik artar

#### MassTransit konfigürasyonu (Consumer)
**Ne işe yarar:** RabbitMQ consumer'ı DI container'a kaydeder.

**Program.cs güncellemesi:**

```csharp
using MassTransit;
using Ordering.API.EventHandlers;

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

**Açıklama:**
- `AddConsumer<BasketCheckoutConsumer>` → Consumer'ı kaydeder
- `ConfigureEndpoints` → Consumer endpoint'lerini otomatik oluşturur
- Queue adı: `BasketCheckoutEvent` (MassTransit otomatik oluşturur)

**MassTransit Queue Yapısı:**
- Queue adı: `BasketCheckoutEvent` (event adından türetilir)
- Exchange: `BasketCheckoutEvent` (topic exchange)
- Routing key: `BasketCheckoutEvent`

#### AutoMapper Event → Command mapping
**Ne işe yarar:** BasketCheckoutEvent'i CreateOrderCommand'e map eder.

**Mapping/MappingProfile.cs güncellemesi:**

```csharp
// Event → Command (Consumer için)
CreateMap<BasketCheckoutEvent, CreateOrderCommand>()
    .ForMember(dest => dest.Items, opt => opt.Ignore()); // Items Basket'ten gelmez, event'te yok
```

**Önemli:** `BasketCheckoutEvent`'te `Items` yok (sadece toplam bilgiler var). Items'ları Basket Service'ten almak için:
- Ya Basket Service event'e Items ekler
- Ya da Ordering Service Basket Service'e gRPC/REST ile sorar

**Şimdilik:** Items'ları event'ten almayacağız (sadece toplam bilgilerle sipariş oluşturulur).

### Test:
- Consumer RabbitMQ'dan event alıyor mu?
- Event geldiğinde sipariş oluşuyor mu?
- RabbitMQ Management UI'da queue görünüyor mu? (http://localhost:15673)
- Queue'da mesaj var mı? (RabbitMQ Management UI'da kontrol et)

---

## 6.5 Ordering Controller & Entegrasyon

**Hedef:** REST API endpoint'leri

### Görevler:

#### OrdersController oluştur
**Ne işe yarar:** REST API endpoint'lerini oluşturur (MediatR ile).

**Controllers/OrdersController.cs:**

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ordering.API.Dtos;
using Ordering.API.Features.Orders.Commands.CreateOrder;
using Ordering.API.Features.Orders.Commands.DeleteOrder;
using Ordering.API.Features.Orders.Commands.UpdateOrder;
using Ordering.API.Features.Orders.Queries.GetOrderById;
using Ordering.API.Features.Orders.Queries.GetOrders;
using Ordering.API.Features.Orders.Queries.GetOrdersByUser;

namespace Ordering.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
    {
        var orders = await _mediator.Send(new GetOrdersQuery());
        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrderById(Guid id)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(id));
        if (order == null)
            return NotFound();
        return Ok(order);
    }

    [HttpGet("user/{userName}")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByUser(string userName)
    {
        var orders = await _mediator.Send(new GetOrdersByUserQuery(userName));
        return Ok(orders);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateOrder(CreateOrderCommand command)
    {
        var orderId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetOrderById), new { id = orderId }, orderId);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrder(Guid id, UpdateOrderCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID uyuşmuyor");

        var result = await _mediator.Send(command);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrder(Guid id)
    {
        var result = await _mediator.Send(new DeleteOrderCommand(id));
        if (!result)
            return NotFound();

        return NoContent();
    }
}
```

**Açıklama:**
- `GET /api/orders` → Tüm siparişleri getirir (Admin)
- `GET /api/orders/{id}` → Sipariş detayını getirir
- `GET /api/orders/user/{userName}` → Kullanıcı siparişlerini getirir
- `POST /api/orders` → Manuel sipariş oluşturur (opsiyonel, genelde Consumer kullanılır)
- `PUT /api/orders/{id}` → Sipariş durumunu günceller (Admin)
- `DELETE /api/orders/{id}` → Siparişi iptal eder (Cancelled)

#### Exception middleware ekle
**Ne işe yarar:** Global exception handling ekler.

**Program.cs güncellemesi:**

```csharp
using BuildingBlocks.Exceptions.Handler;

var app = builder.Build();

// Exception Middleware
app.UseExceptionHandler();

// ... diğer middleware'ler
```

**Açıklama:**
- `UseExceptionHandler` → BuildingBlocks.Exceptions'dan gelen middleware
- Tüm hataları yakalar ve ProblemDetails formatında döner

#### Health Checks ekle
**Ne işe yarar:** PostgreSQL ve RabbitMQ bağlantılarını kontrol eder.

**Program.cs güncellemesi:**

```csharp
// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!)
    .AddRabbitMQ(builder.Configuration["MessageBroker:Host"]!);

// ...

app.MapHealthChecks("/health");
```

**Açıklama:**
- `AddNpgSql` → PostgreSQL health check ekler
- `AddRabbitMQ` → RabbitMQ health check ekler
- `/health` endpoint'i → PostgreSQL ve RabbitMQ bağlantılarını kontrol eder

#### Swagger konfigürasyonu
**Ne işe yarar:** Swagger UI'ı yapılandırır.

**Program.cs güncellemesi:**

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Ordering API",
        Version = "v1",
        Description = "E-ticaret Ordering Service API - Sipariş yönetimi için REST API"
    });
});

// ...

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ordering API v1");
        c.RoutePrefix = "swagger";
    });
}
```

### Test:
- Swagger açılıyor mu? (http://localhost:5003/swagger)
- Endpoint'ler çalışıyor mu?
  - `GET /api/orders` → Tüm siparişleri getirir
  - `GET /api/orders/{id}` → Sipariş detayını getirir
  - `GET /api/orders/user/{userName}` → Kullanıcı siparişlerini getirir
  - `POST /api/orders` → Manuel sipariş oluşturur
  - `PUT /api/orders/{id}` → Sipariş durumunu günceller
  - `DELETE /api/orders/{id}` → Siparişi iptal eder
- Health check çalışıyor mu? (http://localhost:5003/health)
- Consumer çalışıyor mu? (Basket checkout yap, sipariş oluştu mu?)

**Sonuç:** ✅ Ordering Service çalışıyor (Port 5003)

---

## Özet: Faz 6 adımlar sırası

1. Ordering klasörünü oluştur
2. Ordering.API Web API projesi oluştur (`dotnet new webapi`)
3. Projeyi solution'a ekle
4. NuGet paketlerini ekle (MediatR, EF Core, MassTransit, vb.)
5. Project References ekle (BuildingBlocks)
6. Klasör yapısını oluştur (Entities, Data, Features, EventHandlers, Dtos, Mapping)
7. Order ve OrderItem Entity'lerini oluştur
8. appsettings.json'a connection string ve ayarları ekle (PostgreSQL, RabbitMQ)
9. OrderingDbContext oluştur
10. Program.cs'de DbContext kaydı
11. EF Core Migration oluştur (`dotnet ef migrations add InitialCreate`)
12. MediatR, FluentValidation, AutoMapper konfigürasyonu
13. CreateOrderCommand + CreateOrderHandler + CreateOrderValidator oluştur
14. UpdateOrderCommand + UpdateOrderHandler oluştur
15. DeleteOrderCommand + DeleteOrderHandler oluştur
16. GetOrdersQuery + GetOrdersHandler oluştur
17. GetOrderByIdQuery + GetOrderByIdHandler oluştur
18. GetOrdersByUserQuery + GetOrdersByUserHandler oluştur
19. DTO'ları oluştur (OrderDto, OrderItemDto)
20. AutoMapper MappingProfile oluştur
21. BasketCheckoutConsumer oluştur
22. MassTransit konfigürasyonu (Consumer)
23. OrdersController oluştur
24. Exception middleware ekle
25. Health Checks ekle (PostgreSQL + RabbitMQ)
26. Swagger konfigürasyonu
27. Tüm endpoint'leri test et
28. Consumer'ı test et (Basket checkout yap, sipariş oluştu mu?)

**Bu adımlar tamamlandıktan sonra Faz 7'ye (API Gateway) geçilebilir.**

**Not:** Ordering Service, Basket Service'ten gelen BasketCheckoutEvent'i dinleyecek, bu yüzden Basket Service'in çalışır durumda olması önemlidir.

