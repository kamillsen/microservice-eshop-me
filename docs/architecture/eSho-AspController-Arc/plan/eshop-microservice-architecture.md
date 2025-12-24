# 🏗️ E-Shop Microservice Mimarisi

> **API Türü:** ASP.NET Core Controller-based API  
> **Pattern:** CQRS + MediatR  
> **Seviye:** Junior-Mid Developer  
> **Tarih:** Aralık 2024

---

## 📋 İçindekiler

1. [Genel Bakış](#-genel-bakış)
2. [Proje Yapısı](#-proje-yapısı)
3. [CQRS ve MediatR](#-cqrs-ve-mediatr)
4. [Servisler](#-servisler)
5. [Teknoloji Stack](#-teknoloji-stack)
6. [Docker Yapısı](#-docker-yapısı)
7. [API Gateway (YARP)](#-api-gateway-yarp)
8. [Servisler Arası İletişim](#-servisler-arası-i̇letişim)
9. [RabbitMQ + MassTransit](#-rabbitmq--masstransit)
10. [gRPC Kullanımı](#-grpc-kullanımı)
11. [Veritabanları](#-veritabanları)
12. [Exception Handling](#-exception-handling-hata-yönetimi)
13. [Health Checks](#-health-checks)
14. [Portlar & UI Erişim Tablosu](#-portlar--ui-erişim-tablosu)
15. [Dockerfile Stratejisi](#-dockerfile-stratejisi)
16. [appsettings.json Stratejisi](#️-appsettingsjson-stratejisi)

---

## 🎯 Genel Bakış

Bu proje, microservice mimarisini öğrenmek için tasarlanmış bir e-ticaret uygulamasıdır.

### Kararlar

| Karar | Seçim | Neden |
|-------|-------|-------|
| API Türü | **Controller-based API** | Daha organize, MultiShop ile uyumlu |
| Pattern | **CQRS + MediatR** | Separation of concerns, test edilebilirlik |
| API Gateway | **YARP** | Microsoft destekli, performanslı |
| Message Broker | **RabbitMQ + MassTransit** | Kolay kullanım, otomatik retry |
| Sync İletişim | **gRPC** | Yüksek performans |
| Container | **Docker Compose** | Tek komut ile tüm sistem |

### Servis Protokolleri

| Servis | Protokol | Kullanım | Neden? |
|--------|----------|----------|--------|
| **Catalog.API** | REST | Dışarıya açık | CRUD operasyonları, Swagger desteği |
| **Basket.API** | REST | Dışarıya açık | CRUD operasyonları, Swagger desteği |
| **Ordering.API** | REST | Dışarıya açık | CRUD operasyonları, Swagger desteği |
| **Discount.Grpc** | gRPC | Internal (Basket → Discount) | Yüksek performans, binary format |
| **Gateway.API** | REST (YARP) | Dışarıya açık | Reverse proxy, routing |

### İletişim Akışı

```
                              REST API
    İstemci ─────────────────────────────────► Gateway (:5000)
    (Web/Mobile)                                   │
                                                   │ REST API (YARP routing)
                    ┌──────────────────────────────┼──────────────────────────────┐
                    │                              │                              │
                    ▼                              ▼                              ▼
             ┌──────────┐                   ┌──────────┐                   ┌──────────┐
             │ Catalog  │                   │  Basket  │                   │ Ordering │
             │   API    │                   │   API    │                   │   API    │
             │ (REST)   │                   │ (REST)   │                   │ (REST)   │
             └──────────┘                   └────┬─────┘                   └────┬─────┘
                                                 │                              │
                                       ┌─────────┤                              │
                                       │  gRPC   │                              │
                                       │ (sync)  │                              │
                                       ▼         │                              │
                                 ┌──────────┐    │                              │
                                 │ Discount │    │         RabbitMQ             │
                                 │  (gRPC)  │    │         (async)              │
                                 └──────────┘    └──────────────────────────────┘
```

---

## 📁 Proje Yapısı

```
microservice-mrt-practice/
│
├── src/
│   ├── Services/
│   │   │
│   │   ├── Catalog/                           # 📦 Ürün Kataloğu Servisi
│   │   │   └── Catalog.API/
│   │   │       ├── Controllers/
│   │   │       │   ├── ProductsController.cs
│   │   │       │   └── CategoriesController.cs
│   │   │       ├── Features/                  # CQRS - Vertical Slice
│   │   │       │   ├── Products/
│   │   │       │   │   ├── Commands/
│   │   │       │   │   │   ├── CreateProduct/
│   │   │       │   │   │   │   ├── CreateProductCommand.cs
│   │   │       │   │   │   │   ├── CreateProductHandler.cs
│   │   │       │   │   │   │   └── CreateProductValidator.cs
│   │   │       │   │   │   ├── UpdateProduct/
│   │   │       │   │   │   │   ├── UpdateProductCommand.cs
│   │   │       │   │   │   │   ├── UpdateProductHandler.cs
│   │   │       │   │   │   │   └── UpdateProductValidator.cs
│   │   │       │   │   │   └── DeleteProduct/
│   │   │       │   │   │       ├── DeleteProductCommand.cs
│   │   │       │   │   │       └── DeleteProductHandler.cs
│   │   │       │   │   └── Queries/
│   │   │       │   │       ├── GetProducts/
│   │   │       │   │       │   ├── GetProductsQuery.cs
│   │   │       │   │       │   └── GetProductsHandler.cs
│   │   │       │   │       └── GetProductById/
│   │   │       │   │           ├── GetProductByIdQuery.cs
│   │   │       │   │           └── GetProductByIdHandler.cs
│   │   │       │   └── Categories/
│   │   │       │       ├── Commands/
│   │   │       │       └── Queries/
│   │   │       ├── Data/
│   │   │       │   ├── CatalogDbContext.cs
│   │   │       │   └── Migrations/
│   │   │       ├── Entities/
│   │   │       │   ├── Product.cs
│   │   │       │   └── Category.cs
│   │   │       ├── Dtos/
│   │   │       │   ├── ProductDto.cs
│   │   │       │   ├── CreateProductDto.cs
│   │   │       │   └── UpdateProductDto.cs
│   │   │       ├── Mapping/
│   │   │       │   └── MappingProfile.cs
│   │   │       ├── Program.cs
│   │   │       ├── appsettings.json
│   │   │       ├── Dockerfile
│   │   │       └── Catalog.API.csproj
│   │   │
│   │   ├── Basket/                            # 🛒 Sepet Servisi (Redis)
│   │   │   └── Basket.API/
│   │   │       ├── Controllers/
│   │   │       │   └── BasketsController.cs
│   │   │       ├── Features/
│   │   │       │   └── Basket/
│   │   │       │       ├── Commands/
│   │   │       │       │   ├── StoreBasket/
│   │   │       │       │   │   ├── StoreBasketCommand.cs
│   │   │       │       │   │   ├── StoreBasketHandler.cs
│   │   │       │       │   │   └── StoreBasketValidator.cs
│   │   │       │       │   ├── DeleteBasket/
│   │   │       │       │   │   ├── DeleteBasketCommand.cs
│   │   │       │       │   │   └── DeleteBasketHandler.cs
│   │   │       │       │   └── CheckoutBasket/
│   │   │       │       │       ├── CheckoutBasketCommand.cs
│   │   │       │       │       ├── CheckoutBasketHandler.cs
│   │   │       │       │       └── CheckoutBasketValidator.cs
│   │   │       │       └── Queries/
│   │   │       │           └── GetBasket/
│   │   │       │               ├── GetBasketQuery.cs
│   │   │       │               └── GetBasketHandler.cs
│   │   │       ├── Data/
│   │   │       │   └── BasketRepository.cs    # Redis
│   │   │       ├── Entities/
│   │   │       │   ├── ShoppingCart.cs
│   │   │       │   └── ShoppingCartItem.cs
│   │   │       ├── GrpcServices/
│   │   │       │   └── DiscountGrpcService.cs
│   │   │       ├── Program.cs
│   │   │       ├── appsettings.json
│   │   │       ├── Dockerfile
│   │   │       └── Basket.API.csproj
│   │   │
│   │   ├── Ordering/                          # 📋 Sipariş Servisi
│   │   │   └── Ordering.API/
│   │   │       ├── Controllers/
│   │   │       │   └── OrdersController.cs
│   │   │       ├── Features/
│   │   │       │   └── Orders/
│   │   │       │       ├── Commands/
│   │   │       │       │   ├── CreateOrder/
│   │   │       │       │   │   ├── CreateOrderCommand.cs
│   │   │       │       │   │   ├── CreateOrderHandler.cs
│   │   │       │       │   │   └── CreateOrderValidator.cs
│   │   │       │       │   ├── UpdateOrder/
│   │   │       │       │   └── DeleteOrder/
│   │   │       │       └── Queries/
│   │   │       │           ├── GetOrders/
│   │   │       │           └── GetOrdersByUser/
│   │   │       ├── Data/
│   │   │       │   ├── OrderingDbContext.cs
│   │   │       │   └── Migrations/
│   │   │       ├── Entities/
│   │   │       │   ├── Order.cs
│   │   │       │   └── OrderItem.cs
│   │   │       ├── EventHandlers/
│   │   │       │   └── BasketCheckoutConsumer.cs  # RabbitMQ Consumer
│   │   │       ├── Program.cs
│   │   │       ├── appsettings.json
│   │   │       ├── Dockerfile
│   │   │       └── Ordering.API.csproj
│   │   │
│   │   └── Discount/                          # 🏷️ İndirim Servisi (gRPC)
│   │       └── Discount.Grpc/
│   │           ├── Protos/
│   │           │   └── discount.proto
│   │           ├── Services/
│   │           │   └── DiscountService.cs
│   │           ├── Data/
│   │           │   └── DiscountDbContext.cs
│   │           ├── Entities/
│   │           │   └── Coupon.cs
│   │           ├── Program.cs
│   │           ├── appsettings.json
│   │           ├── Dockerfile
│   │           └── Discount.Grpc.csproj
│   │
│   ├── ApiGateway/                            # 🚪 API Gateway (YARP)
│   │   └── Gateway.API/
│   │       ├── Program.cs
│   │       ├── appsettings.json               # YARP route config
│   │       ├── Dockerfile
│   │       └── Gateway.API.csproj
│   │
│   └── BuildingBlocks/                        # 🧱 Paylaşılan Kod
│       ├── BuildingBlocks.Messaging/
│       │   ├── Events/
│       │   │   ├── IntegrationEvent.cs
│       │   │   └── BasketCheckoutEvent.cs
│       │   ├── Extensions/
│       │   │   └── MassTransitExtensions.cs
│       │   └── BuildingBlocks.Messaging.csproj
│       │
│       └── BuildingBlocks.Behaviors/          # MediatR Pipeline Behaviors
│           ├── ValidationBehavior.cs
│           ├── LoggingBehavior.cs
│           └── BuildingBlocks.Behaviors.csproj
│
├── tests/
│   ├── Catalog.API.Tests/
│   ├── Basket.API.Tests/
│   └── Ordering.API.Tests/
│
├── docker-compose.yml                         # 🐳 Ana compose dosyası
├── docker-compose.override.yml                # Development ayarları
├── .env                                       # Environment variables
│
├── global.json                                # .NET 9 SDK
├── Directory.Build.props                      # Ortak MSBuild ayarları
├── Directory.Packages.props                   # Central Package Management
├── EShop.sln                                  # Solution dosyası
└── README.md
```

---

## 🔄 CQRS ve MediatR

### CQRS Nedir?

**Command Query Responsibility Segregation** - Okuma ve yazma işlemlerini ayırma.

```
┌─────────────────────────────────────────────────────────────┐
│                        CQRS Pattern                          │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│   COMMAND (Yazma)              QUERY (Okuma)                │
│   ─────────────────            ─────────────                │
│   • CreateProduct              • GetProducts                │
│   • UpdateProduct              • GetProductById             │
│   • DeleteProduct              • GetProductsByCategory      │
│                                                              │
│   Veriyi DEĞİŞTİRİR            Veriyi OKUR                  │
│   Sonuç: ID veya void          Sonuç: DTO                   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### MediatR Nedir?

Controller ile Handler arasında **aracı** görevi gören kütüphane.

```
┌────────────┐     ┌────────────┐     ┌────────────┐     ┌────────────┐
│ Controller │────►│  MediatR   │────►│  Handler   │────►│  Database  │
│            │     │ (Mediator) │     │            │     │            │
└────────────┘     └────────────┘     └────────────┘     └────────────┘
       │                 │                  │
       │   Send(Query)   │                  │
       │────────────────►│                  │
       │                 │  Find Handler    │
       │                 │─────────────────►│
       │                 │                  │  Execute
       │                 │                  │─────────►
       │                 │     Result       │
       │◄────────────────│◄─────────────────│
```

### MediatR Akışı (Pipeline)

```
Request ──► Logging ──► Validation ──► Handler ──► Response
               │              │
               │              └── FluentValidation ile doğrulama
               └── Serilog ile loglama
```

---

### Command Örneği

```csharp
// Features/Products/Commands/CreateProduct/CreateProductCommand.cs
public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    string ImageUrl,
    Guid CategoryId
) : IRequest<Guid>;  // Guid döner (Product ID)
```

```csharp
// Features/Products/Commands/CreateProduct/CreateProductHandler.cs
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
        // 1. Command'dan Entity oluştur
        var product = _mapper.Map<Product>(request);
        product.Id = Guid.NewGuid();

        // 2. Veritabanına kaydet
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        // 3. ID döndür
        return product.Id;
    }
}
```

```csharp
// Features/Products/Commands/CreateProduct/CreateProductValidator.cs
public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ürün adı boş olamaz")
            .MaximumLength(100).WithMessage("Ürün adı en fazla 100 karakter olabilir");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Fiyat 0'dan büyük olmalı");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Kategori seçilmeli");
    }
}
```

---

### Query Örneği

```csharp
// Features/Products/Queries/GetProducts/GetProductsQuery.cs
public record GetProductsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    Guid? CategoryId = null
) : IRequest<IEnumerable<ProductDto>>;
```

```csharp
// Features/Products/Queries/GetProducts/GetProductsHandler.cs
public class GetProductsHandler : IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>
{
    private readonly CatalogDbContext _context;
    private readonly IMapper _mapper;

    public GetProductsHandler(CatalogDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductDto>> Handle(
        GetProductsQuery request, 
        CancellationToken cancellationToken)
    {
        var query = _context.Products.AsQueryable();

        // Filtreleme
        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId);
        }

        // Sayfalama
        var products = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }
}
```

---

### Controller Kullanımı

```csharp
// Controllers/ProductsController.cs
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(
        [FromQuery] GetProductsQuery query)
    {
        var products = await _mediator.Send(query);
        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProductById(Guid id)
    {
        var product = await _mediator.Send(new GetProductByIdQuery(id));
        return Ok(product);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateProduct(CreateProductCommand command)
    {
        var productId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetProductById), new { id = productId }, productId);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(Guid id, UpdateProductCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID uyuşmuyor");

        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        await _mediator.Send(new DeleteProductCommand(id));
        return NoContent();
    }
}
```

---

### Pipeline Behaviors

```csharp
// BuildingBlocks.Behaviors/ValidationBehavior.cs
public class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Any())
                throw new ValidationException(failures);
        }

        return await next();
    }
}
```

```csharp
// BuildingBlocks.Behaviors/LoggingBehavior.cs
public class LoggingBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        _logger.LogInformation("Handling {RequestName}: {@Request}", requestName, request);
        
        var response = await next();
        
        _logger.LogInformation("Handled {RequestName}: {@Response}", requestName, response);
        
        return response;
    }
}
```

---

### MediatR Konfigürasyonu (Program.cs)

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// DbContext
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

var app = builder.Build();

app.MapControllers();
app.Run();
```

---

## 🔧 Servisler

### 1. Catalog Service (Ürün Kataloğu)

#### Ne İşe Yarar?

Catalog Service, e-ticaret sitesindeki **ürün ve kategori bilgilerini** yönetir. Kullanıcılar ürünleri görüntüler, admin ürün ekler/düzenler.

**Temel İşlevler:**
- Ürün listesini gösterme (sayfalama, filtreleme)
- Ürün detaylarını gösterme
- Kategori bazlı ürün arama
- Admin: Ürün ekleme, güncelleme, silme
- Admin: Kategori yönetimi

#### Örnek Senaryolar

**Senaryo 1: Kullanıcı Ürün Arıyor**
```
1. Kullanıcı: "Elektronik kategorisindeki ürünleri göster"
2. Catalog Service: Kategorideki tüm ürünleri döner
3. Response: [iPhone 15, Samsung S24, MacBook Pro...]
```

**Senaryo 2: Admin Yeni Ürün Ekliyor**
```
1. Admin: POST /api/products { name: "iPad Pro", price: 35000, categoryId: "..." }
2. Catalog Service: Ürünü veritabanına kaydeder
3. Response: { id: "guid", ... }
```

**Senaryo 3: Ürün Detayı**
```
1. Kullanıcı: "iPhone 15'in detaylarını göster"
2. Catalog Service: Ürün bilgilerini döner
3. Response: { id, name, description, price, imageUrl, category }
```

#### Teknik Detaylar

| Özellik | Değer |
|---------|-------|
| **Port** | 8080 (internal) |
| **Database** | PostgreSQL |
| **ORM** | Entity Framework Core |
| **Pattern** | CQRS + MediatR |
| **Endpoints** | CRUD Products, Categories |

```
GET    /api/products                    # GetProductsQuery (Liste, sayfalama)
GET    /api/products/{id}               # GetProductByIdQuery (Detay)
GET    /api/products/category/{id}      # GetProductsByCategoryQuery (Kategoriye göre)
POST   /api/products                    # CreateProductCommand (Yeni ürün)
PUT    /api/products/{id}               # UpdateProductCommand (Güncelle)
DELETE /api/products/{id}               # DeleteProductCommand (Sil)

GET    /api/categories                  # GetCategoriesQuery (Tüm kategoriler)
GET    /api/categories/{id}             # GetCategoryByIdQuery (Kategori detayı)
POST   /api/categories                  # CreateCategoryCommand (Yeni kategori)
```

---

### 2. Basket Service (Sepet)

#### Ne İşe Yarar?

Basket Service, kullanıcıların **alışveriş sepetini** yönetir. Sepete ürün ekleme, çıkarma, güncelleme ve ödeme işlemlerini yapar.

**Temel İşlevler:**
- Sepete ürün ekleme
- Sepetten ürün çıkarma
- Sepetteki ürün miktarını güncelleme
- Sepeti görüntüleme (toplam fiyat, indirimler dahil)
- Sepeti temizleme
- **Checkout (Ödeme):** Sepeti siparişe dönüştürme (RabbitMQ event gönderir)

#### Örnek Senaryolar

**Senaryo 1: Kullanıcı Sepete Ürün Ekliyor**
```
1. Kullanıcı: "iPhone 15'i sepete ekle, adet: 2"
2. Basket Service: 
   - Sepeti Redis'ten al (cache) veya PostgreSQL'den al (cache-aside pattern)
   - Ürünü sepete ekle
   - Discount Service'e gRPC ile bağlan → İndirim var mı kontrol et
   - Toplam fiyatı hesapla (indirim dahil)
   - PostgreSQL'e kaydet (source of truth)
   - Redis'e kaydet (cache)
3. Response: { userName, items: [...], totalPrice: 95000 }
```

**Senaryo 2: Kullanıcı Sepeti Görüntülüyor**
```
1. Kullanıcı: "Sepetimi göster"
2. Basket Service: 
   - Önce Redis'e bak (cache)
   - Redis'te yoksa PostgreSQL'den al (cache-aside pattern)
   - PostgreSQL'den aldıktan sonra Redis'e yaz (cache)
3. Response: { 
     userName: "user1",
     items: [
       { productId: "guid", productName: "iPhone 15", quantity: 2, price: 50000 },
       { productId: "guid", productName: "MacBook", quantity: 1, price: 45000 }
     ],
     totalPrice: 145000,
     discount: 5000
   }
```

**Senaryo 3: Kullanıcı Checkout Yapıyor (Ödeme)**
```
1. Kullanıcı: POST /api/baskets/checkout { shippingAddress, paymentInfo }
2. Basket Service:
   - Sepeti Redis'ten veya PostgreSQL'den al (cache-aside pattern)
   - BasketCheckoutEvent oluştur (tüm bilgilerle)
   - RabbitMQ'ya event gönder (Ordering Service dinleyecek)
   - Sepeti PostgreSQL'den sil (source of truth)
   - Sepeti Redis'ten sil (cache)
3. Response: { success: true, message: "Sipariş oluşturuldu" }
```

#### Teknik Detaylar

| Özellik | Değer |
|---------|-------|
| **Port** | 8080 (internal) |
| **Database** | Redis + PostgreSQL (Cache-aside pattern) |
| **Pattern** | CQRS + MediatR |
| **gRPC Client** | Discount.Grpc (indirim sorgulama) |
| **Publishes** | BasketCheckoutEvent (RabbitMQ) |

```
GET    /api/baskets/{userName}          # GetBasketQuery (Sepeti getir)
POST   /api/baskets                     # StoreBasketCommand (Sepeti kaydet/güncelle)
DELETE /api/baskets/{userName}          # DeleteBasketCommand (Sepeti sil)
POST   /api/baskets/checkout            # CheckoutBasketCommand (Ödeme - RabbitMQ event)
```

**Neden Redis + PostgreSQL?**
- **Redis (Cache):** Hızlı okuma/yazma için (kullanıcı deneyimi)
- **PostgreSQL (Source of Truth):** Veri kalıcılığı için (veri kaybı riski düşük)
- **Cache-aside Pattern:** Önce Redis'e bak, yoksa PostgreSQL'den al ve cache'le
- Redis down olsa bile PostgreSQL'den okur (yavaş ama çalışır)
- Sepet geçmişi tutulabilir (analiz için)

---

### 3. Ordering Service (Sipariş)

#### Ne İşe Yarar?

Ordering Service, kullanıcıların **siparişlerini** yönetir. Basket Service'ten gelen checkout event'ini dinleyerek otomatik sipariş oluşturur.

**Temel İşlevler:**
- **Otomatik sipariş oluşturma:** Basket checkout event'ini dinler
- Sipariş listesini gösterme
- Sipariş detaylarını gösterme
- Kullanıcıya göre sipariş arama
- Sipariş durumunu güncelleme (Admin)
- Sipariş iptal etme

#### Örnek Senaryolar

**Senaryo 1: Basket Checkout → Sipariş Oluşuyor (Otomatik)**
```
1. Basket Service: Checkout yapıldı → BasketCheckoutEvent RabbitMQ'ya gönderildi
2. Ordering Service: Event'i dinledi (BasketCheckoutConsumer)
3. Ordering Service:
   - Event'ten CreateOrderCommand oluştur
   - MediatR ile CreateOrderHandler'ı çağır
   - Siparişi veritabanına kaydet
   - Sipariş numarası oluştur
4. Sonuç: Sipariş oluştu, kullanıcıya bildirim gönderilebilir
```

**Senaryo 2: Kullanıcı Siparişlerini Görüntülüyor**
```
1. Kullanıcı: "Tüm siparişlerimi göster"
2. Ordering Service: Kullanıcıya ait siparişleri getir
3. Response: [
     { orderId: "guid", orderDate: "2024-12-01", totalPrice: 145000, status: "Pending" },
     { orderId: "guid", orderDate: "2024-11-28", totalPrice: 50000, status: "Shipped" }
   ]
```

**Senaryo 3: Admin Sipariş Durumunu Güncelliyor**
```
1. Admin: PUT /api/orders/{id} { status: "Shipped" }
2. Ordering Service: Sipariş durumunu güncelle
3. Response: { success: true }
```

#### Teknik Detaylar

| Özellik | Değer |
|---------|-------|
| **Port** | 8080 (internal) |
| **Database** | PostgreSQL |
| **ORM** | Entity Framework Core |
| **Pattern** | CQRS + MediatR |
| **Consumes** | BasketCheckoutEvent (RabbitMQ) |

```
GET    /api/orders                      # GetOrdersQuery (Tüm siparişler)
GET    /api/orders/{id}                 # GetOrderByIdQuery (Sipariş detayı)
GET    /api/orders/user/{userName}      # GetOrdersByUserQuery (Kullanıcı siparişleri)
POST   /api/orders                      # CreateOrderCommand (Manuel sipariş - opsiyonel)
PUT    /api/orders/{id}                 # UpdateOrderCommand (Durum güncelleme)
DELETE /api/orders/{id}                 # DeleteOrderCommand (Sipariş iptal)
```

**Event Akışı:**
```
Basket Service → RabbitMQ → Ordering Service (Consumer)
     ↓              ↓              ↓
  Checkout      Event Queue    Create Order
```

---

### 4. Discount Service (İndirim - gRPC)

#### Ne İşe Yarar?

Discount Service, ürünlere özel **indirim kuponlarını** yönetir. Basket Service, sepetteki ürünler için indirim olup olmadığını bu servisten öğrenir.

**Temel İşlevler:**
- Ürün için indirim sorgulama (gRPC ile hızlı)
- Yeni indirim kuponu oluşturma
- İndirim kuponu güncelleme
- İndirim kuponu silme

#### Örnek Senaryolar

**Senaryo 1: Basket Service İndirim Sorguluyor**
```
1. Basket Service: "iPhone 15 için indirim var mı?"
2. Discount Service (gRPC): 
   - Veritabanında kupon ara
   - Varsa: { productName: "iPhone 15", amount: 5000, description: "Yılbaşı indirimi" }
   - Yoksa: { amount: 0 }
3. Basket Service: İndirimi sepete uygula
```

**Senaryo 2: Admin Yeni İndirim Oluşturuyor**
```
1. Admin: "iPhone 15 için %10 indirim oluştur"
2. Discount Service: Kuponu veritabanına kaydet
3. Response: { id: 1, productName: "iPhone 15", amount: 5000 }
```

#### Teknik Detaylar

| Özellik | Değer |
|---------|-------|
| **Port** | 8080 (gRPC - HTTP/2 only), 8081 (Health check - HTTP/1.1 only) |
| **Protocol** | gRPC (REST değil!) - HTTP/2 cleartext (h2c) |
| **Database** | PostgreSQL |
| **HTTP/2 Cleartext** | Prior Knowledge mode (sadece Http2 protokolü) |

> **Not:** Discount servisi gRPC kullandığı için MediatR kullanmıyor. gRPC servisleri doğrudan repository ile çalışır.

```protobuf
service DiscountProtoService {
  rpc GetDiscount (GetDiscountRequest) returns (CouponModel);      # İndirim sorgula
  rpc CreateDiscount (CreateDiscountRequest) returns (CouponModel); # Yeni kupon
  rpc UpdateDiscount (UpdateDiscountRequest) returns (CouponModel); # Kupon güncelle
  rpc DeleteDiscount (DeleteDiscountRequest) returns (DeleteDiscountResponse); # Kupon sil
}
```

**Neden gRPC?**
- Basket Service sürekli indirim sorguluyor (her sepet işleminde)
- gRPC çok hızlı (binary format, HTTP/2)
- Internal servis (dışarıya açık değil)
- Yüksek performans gerekiyor

---

### 5. Gateway Service (API Gateway - YARP)

#### Ne İşe Yarar?

Gateway Service, tüm servislere **tek giriş noktası** sağlar. Kullanıcılar farklı servislerin portlarını bilmek zorunda kalmaz.

**Temel İşlevler:**
- Tüm servislere tek URL üzerinden erişim
- Request routing (hangi istek hangi servise gidecek)
- Load balancing (ileride birden fazla instance olursa)
- CORS yönetimi
- Rate limiting (ileride)

#### Örnek Senaryolar

**Senaryo 1: Kullanıcı Ürün Listesi İstiyor**
```
1. Kullanıcı: GET http://localhost:5000/catalog-service/api/products
2. Gateway (YARP):
   - Route'u kontrol et → "/catalog-service/**" → Catalog.API'ye yönlendir
   - Path'i dönüştür → "/catalog-service" prefix'ini kaldır
   - Request'i Catalog.API'ye gönder → http://catalog.api:8080/api/products
3. Catalog.API: Response döner
4. Gateway: Response'u kullanıcıya iletir
```

**Senaryo 2: Kullanıcı Sepeti Görüntülüyor**
```
1. Kullanıcı: GET http://localhost:5000/basket-service/api/baskets/user1
2. Gateway: Basket.API'ye yönlendir
3. Basket.API: Sepeti döner
```

#### Teknik Detaylar

| Özellik | Değer |
|---------|-------|
| **Port** | 5000 (external - kullanıcılar buraya bağlanır) |
| **Teknoloji** | YARP (Yet Another Reverse Proxy) |
| **Pattern** | Reverse Proxy, Routing |

**Route Yapısı:**
```
/catalog-service/**  → http://catalog.api:8080
/basket-service/**   → http://basket.api:8080
/ordering-service/** → http://ordering.api:8080
```

**Avantajları:**
- Kullanıcı tek URL bilir (localhost:5000)
- Servisler internal port'ta çalışır (güvenlik)
- İleride authentication/authorization eklenebilir
- Rate limiting, logging merkezi yapılabilir

---

## 🛠️ Teknoloji Stack

| Katman | Teknoloji | Versiyon |
|--------|-----------|----------|
| **Framework** | .NET | 9.0 |
| **API** | ASP.NET Core Web API | Controller-based |
| **Pattern** | CQRS + MediatR | Latest |
| **API Gateway** | YARP | Latest |
| **Database (Relational)** | PostgreSQL | 16 |
| **Database (Cache)** | Redis | 7 |
| **ORM** | Entity Framework Core | 9.0 |
| **Message Broker** | RabbitMQ | 3-management |
| **Message Abstraction** | MassTransit | Latest |
| **Sync Communication** | gRPC | Latest |
| **Object Mapping** | AutoMapper | Latest |
| **Validation** | FluentValidation | Latest |
| **Logging** | Serilog | Latest |
| **Container** | Docker + Docker Compose | Latest |

---

## 🐳 Docker Yapısı

### docker-compose.yml

```yaml
version: '3.8'

services:
  # ==================== INFRASTRUCTURE ====================
  
  catalogdb:
    image: postgres:16-alpine
    container_name: catalogdb
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: CatalogDb
    ports:
      - "5436:5432"  # Host port 5436 (sistem PostgreSQL port 5432'de çalıştığı için çakışmayı önlemek için)
    volumes:
      - catalogdb_data:/var/lib/postgresql/data

  basketdb:
    image: redis/redis-stack:latest
    container_name: basketdb
    ports:
      - "6379:6379"      # Redis
      - "8001:8001"      # RedisInsight UI
    volumes:
      - basketdb_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  basketpostgres:
    image: postgres:16-alpine
    container_name: basketpostgres
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: BasketDb
    ports:
      - "5437:5432"  # Host port 5437 (diğer PostgreSQL'lerle çakışmaması için)
    volumes:
      - basketpostgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  orderingdb:
    image: postgres:16-alpine
    container_name: orderingdb
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: OrderingDb
    ports:
      - "5435:5432"  # 5433 kullanılıyordu, 5435'e değiştirildi
    volumes:
      - orderingdb_data:/var/lib/postgresql/data

  discountdb:
    image: postgres:16-alpine
    container_name: discountdb
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: DiscountDb
    ports:
      - "5434:5432"
    volumes:
      - discountdb_data:/var/lib/postgresql/data

  messagebroker:
    image: rabbitmq:3-management-alpine
    container_name: messagebroker
    hostname: ecommerce-mq
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    ports:
      - "5673:5672"      # AMQP (5672 kullanılıyordu, 5673'e değiştirildi)
      - "15673:15672"    # Management UI (15672 kullanılıyordu, 15673'e değiştirildi)
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq

  pgadmin:
    image: dpage/pgadmin4:latest
    container_name: pgadmin
    environment:
      PGADMIN_DEFAULT_EMAIL: admin@admin.com
      PGADMIN_DEFAULT_PASSWORD: admin
      PGADMIN_CONFIG_SERVER_MODE: 'False'
    ports:
      - "5050:80"        # pgAdmin Web UI
    volumes:
      - pgadmin_data:/var/lib/pgadmin
    depends_on:
      - catalogdb
      - orderingdb
      - discountdb
      - basketpostgres

  # ==================== SERVICES ====================

  catalog.api:
    image: ${DOCKER_REGISTRY-}catalogapi
    container_name: catalog.api
    build:
      context: .
      dockerfile: src/Services/Catalog/Catalog.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__Database=Host=catalogdb;Port=5432;Database=CatalogDb;Username=postgres;Password=postgres
      # Not: Container network içinde Port=5432 (container içindeki port)
      # Localhost'tan bağlanırken: Host=localhost;Port=5436 (host port)
    depends_on:
      - catalogdb
    ports:
      - "5001:8080"

  basket.api:
    image: ${DOCKER_REGISTRY-}basketapi
    container_name: basket.api
    build:
      context: .
      dockerfile: src/Services/Basket/Basket.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__Redis=basketdb:6379
      - ConnectionStrings__Database=Host=basketpostgres;Port=5432;Database=BasketDb;Username=postgres;Password=postgres
      - GrpcSettings__DiscountUrl=http://discount.grpc:8080
      - MessageBroker__Host=amqp://guest:guest@messagebroker:5673
    depends_on:
      - basketdb
      - basketpostgres
      - discount.grpc
      - messagebroker
    ports:
      - "5002:8080"

  ordering.api:
    image: ${DOCKER_REGISTRY-}orderingapi
    container_name: ordering.api
    build:
      context: .
      dockerfile: src/Services/Ordering/Ordering.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__Database=Host=orderingdb;Port=5432;Database=OrderingDb;Username=postgres;Password=postgres
      - MessageBroker__Host=amqp://guest:guest@messagebroker:5673
    depends_on:
      - orderingdb
      - messagebroker
    ports:
      - "5003:8080"

  discount.grpc:
    image: ${DOCKER_REGISTRY-}discountgrpc
    container_name: discount.grpc
    build:
      context: .
      dockerfile: src/Services/Discount/Discount.Grpc/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__Database=Host=discountdb;Port=5432;Database=DiscountDb;Username=postgres;Password=postgres
    depends_on:
      - discountdb
    ports:
      - "5004:8080"  # gRPC port (HTTP/2 only)
      - "5005:8081"  # Health check port (HTTP/1.1 only)
    healthcheck:
      test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8081/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  gateway.api:
    image: ${DOCKER_REGISTRY-}gatewayapi
    container_name: gateway.api
    build:
      context: .
      dockerfile: src/ApiGateway/Gateway.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    depends_on:
      - catalog.api
      - basket.api
      - ordering.api
    ports:
      - "5000:8080"

volumes:
  catalogdb_data:
  basketdb_data:
  basketpostgres_data:
  orderingdb_data:
  discountdb_data:
  rabbitmq_data:
  pgadmin_data:
```

### Docker Komutları

```bash
# Tüm sistemi başlat (arka planda)
docker-compose up -d

# Logları izle
docker-compose logs -f

# Belirli servisin logunu izle
docker-compose logs -f catalog.api

# Sistemi durdur
docker-compose down

# Sistemi durdur + volume'ları sil
docker-compose down -v

# Yeniden build et ve başlat
docker-compose up -d --build

# Çalışan container'ları listele
docker-compose ps
```

---

## 🚪 API Gateway (YARP)

### YARP Nedir?

**YARP** (Yet Another Reverse Proxy) - Microsoft'un geliştirdiği yüksek performanslı reverse proxy.

### Nasıl Çalışır?

```
                         ┌─────────────────────────────────┐
                         │         API Gateway             │
     İstemci ──────────► │           (YARP)                │
     :5000               │         :5000                   │
                         └───────────────┬─────────────────┘
                                         │
                    ┌────────────────────┼────────────────────┐
                    │                    │                    │
                    ▼                    ▼                    ▼
             ┌──────────┐         ┌──────────┐         ┌──────────┐
             │ /catalog │         │ /basket  │         │ /orders  │
             │   ───►   │         │   ───►   │         │   ───►   │
             │ Catalog  │         │  Basket  │         │ Ordering │
             │   API    │         │   API    │         │   API    │
             └──────────┘         └──────────┘         └──────────┘
```

### YARP Konfigürasyonu (appsettings.json)

```json
{
  "ReverseProxy": {
    "Routes": {
      "catalog-route": {
        "ClusterId": "catalog-cluster",
        "Match": {
          "Path": "/catalog-service/{**catch-all}"
        },
        "Transforms": [
          { "PathRemovePrefix": "/catalog-service" }
        ]
      },
      "basket-route": {
        "ClusterId": "basket-cluster",
        "Match": {
          "Path": "/basket-service/{**catch-all}"
        },
        "Transforms": [
          { "PathRemovePrefix": "/basket-service" }
        ]
      },
      "ordering-route": {
        "ClusterId": "ordering-cluster",
        "Match": {
          "Path": "/ordering-service/{**catch-all}"
        },
        "Transforms": [
          { "PathRemovePrefix": "/ordering-service" }
        ]
      }
    },
    "Clusters": {
      "catalog-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://catalog.api:8080"
          }
        }
      },
      "basket-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://basket.api:8080"
          }
        }
      },
      "ordering-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://ordering.api:8080"
          }
        }
      }
    }
  }
}
```

### Gateway Üzerinden Erişim

| İstek | Yönlendirildiği Yer |
|-------|---------------------|
| `GET http://localhost:5000/catalog-service/api/products` | `http://catalog.api:8080/api/products` |
| `GET http://localhost:5000/basket-service/api/baskets/user1` | `http://basket.api:8080/api/baskets/user1` |
| `GET http://localhost:5000/ordering-service/api/orders` | `http://ordering.api:8080/api/orders` |

---

## 📡 Servisler Arası İletişim

### İletişim Türleri

```
┌─────────────────────────────────────────────────────────────────┐
│                      GATEWAY (:5000)                             │
│                         (YARP)                                   │
└──────────┬──────────────┬──────────────┬───────────────────────┘
           │              │              │
           ▼              ▼              ▼
    ┌──────────┐   ┌──────────┐   ┌──────────┐
    │ Catalog  │   │  Basket  │   │ Ordering │
    │   API    │   │   API    │   │   API    │
    │  :8080   │   │  :8080   │   │  :8080   │
    └──────────┘   └────┬─────┘   └────┬─────┘
                        │              │
              ┌─────────┤              │
              │  gRPC   │              │
              │ (sync)  │              │
              ▼         │              │
        ┌──────────┐    │              │
        │ Discount │    │              │
        │   gRPC   │    │              │
        └──────────┘    │              │
                        │              │
                        ▼              ▼
                   ┌─────────────────────┐
                   │      RabbitMQ       │
                   │   (async events)    │
                   │                     │
                   │  BasketCheckout ────┼──► Ordering
                   │      Event          │    Consumer
                   └─────────────────────┘
```

### Sync vs Async İletişim

| Tür | Kullanım | Örnek |
|-----|----------|-------|
| **Sync (gRPC)** | Hemen cevap lazım | Basket → Discount (indirim sorgula) |
| **Async (RabbitMQ)** | Fire & Forget | Basket → Ordering (checkout event) |

---

## 🐰 RabbitMQ + MassTransit

### RabbitMQ Nedir?

Servisler arası mesaj taşıyan **Message Broker**.

### MassTransit Nedir?

RabbitMQ'yu kolaylaştıran **abstraction layer**. Otomatik:
- Serialization/Deserialization
- Retry mekanizması
- Error handling
- Dead letter queue

### Event Tanımlama

```csharp
// BuildingBlocks.Messaging/Events/BasketCheckoutEvent.cs
public record BasketCheckoutEvent : IntegrationEvent
{
    public string UserName { get; init; } = default!;
    public decimal TotalPrice { get; init; }
    
    // Shipping Address
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string EmailAddress { get; init; } = default!;
    public string AddressLine { get; init; } = default!;
    public string Country { get; init; } = default!;
    public string State { get; init; } = default!;
    public string ZipCode { get; init; } = default!;
    
    // Payment
    public string CardName { get; init; } = default!;
    public string CardNumber { get; init; } = default!;
    public string Expiration { get; init; } = default!;
    public string CVV { get; init; } = default!;
    public int PaymentMethod { get; init; }
}
```

### CheckoutBasket Handler (MediatR + RabbitMQ)

```csharp
// Basket.API/Features/Basket/Commands/CheckoutBasket/CheckoutBasketHandler.cs
public class CheckoutBasketHandler : IRequestHandler<CheckoutBasketCommand, bool>
{
    private readonly IBasketRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMapper _mapper;

    public CheckoutBasketHandler(
        IBasketRepository repository,
        IPublishEndpoint publishEndpoint,
        IMapper mapper)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _mapper = mapper;
    }

    public async Task<bool> Handle(CheckoutBasketCommand request, CancellationToken cancellationToken)
    {
        // 1. Sepeti al
        var basket = await _repository.GetBasket(request.UserName);
        if (basket == null)
            throw new NotFoundException($"Basket for {request.UserName} not found");

        // 2. Event oluştur
        var eventMessage = _mapper.Map<BasketCheckoutEvent>(request);
        eventMessage.TotalPrice = basket.TotalPrice;

        // 3. RabbitMQ'ya gönder
        await _publishEndpoint.Publish(eventMessage, cancellationToken);

        // 4. Sepeti sil
        await _repository.DeleteBasket(request.UserName);

        return true;
    }
}
```

### Event Consume (Ordering.API)

```csharp
// Ordering.API/EventHandlers/BasketCheckoutConsumer.cs
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
        _logger.LogInformation("BasketCheckoutEvent consumed. UserName: {UserName}", 
            context.Message.UserName);

        // Event'ten Command oluştur ve MediatR'a gönder
        var command = _mapper.Map<CreateOrderCommand>(context.Message);
        await _mediator.Send(command);
    }
}
```

### MassTransit Konfigürasyonu

```csharp
// Program.cs
builder.Services.AddMassTransit(config =>
{
    // Consumer'ları ekle
    config.AddConsumer<BasketCheckoutConsumer>();
    
    config.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["MessageBroker:Host"]);
        
        // Endpoint'leri otomatik configure et
        cfg.ConfigureEndpoints(context);
    });
});
```

### Event Akışı

```
┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│   Basket    │      │  RabbitMQ   │      │  Ordering   │
│    API      │      │             │      │    API      │
└──────┬──────┘      └──────┬──────┘      └──────┬──────┘
       │                    │                    │
       │  1. Checkout       │                    │
       │─────────────────►  │                    │
       │                    │                    │
       │  2. Publish Event  │                    │
       │─────────────────►  │                    │
       │                    │                    │
       │                    │  3. Deliver Event  │
       │                    │─────────────────►  │
       │                    │                    │
       │                    │                    │  4. Create Order
       │                    │                    │─────────────────►
       │                    │                    │
```

---

## 📞 gRPC Kullanımı

### gRPC Nedir?

Google'ın geliştirdiği yüksek performanslı RPC framework'ü.

### Neden gRPC?

| HTTP/REST | gRPC |
|-----------|------|
| JSON (text) | Protocol Buffers (binary) |
| Yavaş | Çok hızlı |
| Büyük payload | Küçük payload |
| Herkes anlar | Öğrenmek lazım |

### Proto Dosyası

```protobuf
// Discount.Grpc/Protos/discount.proto
syntax = "proto3";

option csharp_namespace = "Discount.Grpc.Protos";

package discount;

service DiscountProtoService {
  rpc GetDiscount (GetDiscountRequest) returns (CouponModel);
  rpc CreateDiscount (CreateDiscountRequest) returns (CouponModel);
  rpc UpdateDiscount (UpdateDiscountRequest) returns (CouponModel);
  rpc DeleteDiscount (DeleteDiscountRequest) returns (DeleteDiscountResponse);
}

message GetDiscountRequest {
  string productName = 1;
}

message CouponModel {
  int32 id = 1;
  string productName = 2;
  string description = 3;
  int32 amount = 4;
}

message CreateDiscountRequest {
  CouponModel coupon = 1;
}

message UpdateDiscountRequest {
  CouponModel coupon = 1;
}

message DeleteDiscountRequest {
  string productName = 1;
}

message DeleteDiscountResponse {
  bool success = 1;
}
```

### gRPC Server (Discount.Grpc)

```csharp
// Discount.Grpc/Services/DiscountService.cs
public class DiscountService : DiscountProtoService.DiscountProtoServiceBase
{
    private readonly DiscountDbContext _context;
    private readonly ILogger<DiscountService> _logger;

    public DiscountService(DiscountDbContext context, ILogger<DiscountService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public override async Task<CouponModel> GetDiscount(
        GetDiscountRequest request, 
        ServerCallContext context)
    {
        var coupon = await _context.Coupons
            .FirstOrDefaultAsync(c => c.ProductName == request.ProductName);
        
        if (coupon == null)
        {
            throw new RpcException(new Status(
                StatusCode.NotFound, 
                $"Discount for {request.ProductName} not found"));
        }

        _logger.LogInformation("Discount retrieved for {ProductName}: {Amount}", 
            coupon.ProductName, coupon.Amount);

        return new CouponModel
        {
            Id = coupon.Id,
            ProductName = coupon.ProductName,
            Description = coupon.Description,
            Amount = coupon.Amount
        };
    }
}
```

### gRPC Client (Basket.API)

**HTTP/2 Cleartext (h2c) Konfigürasyonu:**

Docker container içinde TLS olmadan HTTP/2 kullanımı için özel konfigürasyon gerekir:

```csharp
// Basket.API/Program.cs
// HTTP/2 cleartext desteği için AppContext switch'i
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

// gRPC Client konfigürasyonu
builder.Services.AddSingleton<DiscountProtoService.DiscountProtoServiceClient>(sp =>
{
    var address = builder.Configuration["GrpcSettings:DiscountUrl"]!;
    
    // HTTP/2 cleartext için SocketsHttpHandler kullan (HttpClientHandler desteklemez)
    var socketsHandler = new System.Net.Http.SocketsHttpHandler
    {
        EnableMultipleHttp2Connections = true
    };
    
    var httpClient = new HttpClient(socketsHandler)
    {
        DefaultVersionPolicy = System.Net.Http.HttpVersionPolicy.RequestVersionOrHigher,
        DefaultRequestVersion = System.Net.HttpVersion.Version20
    };
    
    var channelOptions = new GrpcChannelOptions
    {
        HttpClient = httpClient,
        // HTTP/2 cleartext için Insecure credentials
        Credentials = Grpc.Core.ChannelCredentials.Insecure
    };
    
    var channel = GrpcChannel.ForAddress(address, channelOptions);
    return new DiscountProtoService.DiscountProtoServiceClient(channel);
});
```

**Wrapper Service:**

```csharp
// Basket.API/GrpcServices/DiscountGrpcService.cs
public class DiscountGrpcService
{
    private readonly DiscountProtoService.DiscountProtoServiceClient _client;
    private readonly ILogger<DiscountGrpcService> _logger;

    public DiscountGrpcService(
        DiscountProtoService.DiscountProtoServiceClient client,
        ILogger<DiscountGrpcService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<CouponModel> GetDiscount(string productName)
    {
        var request = new GetDiscountRequest { ProductName = productName };
        
        _logger.LogInformation("Getting discount for {ProductName}", productName);
        
        return await _client.GetDiscountAsync(request);
    }
}
```

**Önemli Notlar:**
- **Http2UnencryptedSupport:** Docker container içinde HTTP/2 cleartext için gerekli
- **SocketsHttpHandler:** HttpClientHandler HTTP/2 cleartext'i desteklemez, SocketsHttpHandler kullanılmalı
- **ChannelCredentials.Insecure:** HTTP/2 cleartext için insecure credentials gerekli
- **Prior Knowledge Mode:** Discount.Grpc servisi sadece Http2 protokolünü kullanır (8080 portu)

### gRPC Akışı

```
┌─────────────┐                    ┌─────────────┐
│   Basket    │                    │  Discount   │
│    API      │                    │    gRPC     │
└──────┬──────┘                    └──────┬──────┘
       │                                  │
       │  1. GetDiscount("iPhone")        │
       │─────────────────────────────────►│
       │                                  │
       │                                  │  2. Query DB
       │                                  │─────────────►
       │                                  │
       │  3. CouponModel { Amount: 100 }  │
       │◄─────────────────────────────────│
       │                                  │
       │  4. Apply discount to basket     │
       │                                  │
```

---

## 💾 Veritabanları

### Veritabanı Dağılımı

| Servis | Veritabanı | Port | Neden? |
|--------|------------|------|--------|
| Catalog | PostgreSQL | 5436 | İlişkisel veri (Products, Categories) (Host port: 5436, container port: 5432) |
| Basket | Redis + PostgreSQL | 6379, 5437 | Redis (cache) + PostgreSQL (source of truth) - Cache-aside pattern |
| Ordering | PostgreSQL | 5435 | İlişkisel veri (Orders, OrderItems) (5433 kullanılıyordu, 5435'e değiştirildi) |
| Discount | PostgreSQL | 5434 | İlişkisel veri (Coupons) |

### Database per Service Pattern

Her microservice'in kendi veritabanı var:

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Catalog    │     │    Basket    │     │   Ordering   │
│     API      │     │     API      │     │     API      │
└──────┬───────┘     └──────┬───────┘     └──────┬───────┘
       │                    │                    │
       ▼                    ▼                    ▼
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  PostgreSQL  │     │  Redis +     │     │  PostgreSQL  │
│  (CatalogDb) │     │  PostgreSQL  │     │ (OrderingDb) │
│              │     │  (Cache +    │     │              │
│              │     │   Source)    │     │              │
└──────────────┘     └──────────────┘     └──────────────┘
```

**Avantajları:**
- Servisler bağımsız ölçeklenebilir
- Bir servisin DB'si çökerse diğerleri etkilenmez
- Her servis için en uygun DB teknolojisi seçilebilir

### Seed Data Stratejisi

GitHub'dan projeyi clone yapan herkes aynı başlangıç verileriyle çalışabilmeli.

**Çözüm:**

| Yöntem | Açıklama | Kullanım |
|--------|----------|----------|
| **SeedData Class** | Program.cs'de uygulama başlangıcında çalışır | ✅ Kullanacağız |
| **Docker Volume** | Veri kalıcı, container silinse bile durur | ✅ Kullanacağız |
| **EF Core Migration** | Tablo yapısını oluşturur | ✅ Kullanacağız |

**Nasıl Çalışır:**

```
1. Kullanıcı projeyi clone eder
   └── git clone https://github.com/xxx/microservice-mrt-practice.git

2. Docker Compose çalıştırır
   └── docker-compose up -d

3. Servisler başlarken:
   └── Migration uygulanır (tablolar oluşur)
   └── SeedData.InitializeAsync() çalışır
   └── Veri yoksa seed data eklenir

4. Hazır! Örnek ürünler, kategoriler, kuponlar mevcut
```

**Program.cs Konfigürasyonu:**

```csharp
// Program.cs
var app = builder.Build();

// Uygulama başlarken seed data kontrol et
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    
    // 1. Migration uygula (tablo oluştur)
    await context.Database.MigrateAsync();
    
    // 2. Seed data ekle (eğer boşsa)
    await SeedData.InitializeAsync(context);
}

app.Run();
```

**SeedData Class Örneği:**

```csharp
// Data/SeedData.cs
public static class SeedData
{
    public static async Task InitializeAsync(CatalogDbContext context)
    {
        // Zaten veri varsa ekleme
        if (await context.Products.AnyAsync())
            return;

        // Kategoriler
        var categories = new List<Category>
        {
            new() { Id = Guid.NewGuid(), Name = "Elektronik" },
            new() { Id = Guid.NewGuid(), Name = "Giyim" },
            new() { Id = Guid.NewGuid(), Name = "Ev & Yaşam" }
        };
        await context.Categories.AddRangeAsync(categories);

        // Ürünler
        var products = new List<Product>
        {
            new() 
            { 
                Id = Guid.NewGuid(), 
                Name = "iPhone 15", 
                Description = "Apple iPhone 15 128GB",
                Price = 50000,
                ImageUrl = "iphone15.jpg",
                CategoryId = categories[0].Id 
            },
            new() 
            { 
                Id = Guid.NewGuid(), 
                Name = "Samsung S24", 
                Description = "Samsung Galaxy S24 256GB",
                Price = 45000,
                ImageUrl = "s24.jpg",
                CategoryId = categories[0].Id 
            }
        };
        await context.Products.AddRangeAsync(products);

        await context.SaveChangesAsync();
    }
}
```

**Avantajları:**
- ✅ Koşullu ekleme (veri varsa tekrar eklemez)
- ✅ Esnek ve dinamik
- ✅ eShopOnContainers ile aynı yaklaşım
- ✅ Büyük veri setleri için uygun
- ✅ JSON dosyasından okuyabilir (opsiyonel)

---

## 🚨 Exception Handling (Hata Yönetimi)

### Nedir ve Neden Gerekli?

**Exception Handling** = Uygulamada oluşan **hataları yakalama ve yönetme** mekanizması.

```
Kullanıcı: GET /api/products/999 (olmayan ürün)

❌ Exception Handling OLMADAN:
─────────────────────────────────
500 Internal Server Error
Stack trace: NullReferenceException at ProductService.cs line 45...
(Kullanıcı ne olduğunu anlamaz, güvenlik açığı)

✅ Exception Handling İLE:
─────────────────────────────────
404 Not Found
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Not Found",
  "status": 404,
  "detail": "Product with id 999 not found"
}
(Temiz, anlaşılır, güvenli)
```

### Strateji

| Katman | Yöntem | Açıklama |
|--------|--------|----------|
| **Global** | Exception Middleware | Tüm hataları merkezi yakalar |
| **Validation** | FluentValidation + MediatR Pipeline | Request doğrulama hataları |
| **Business** | Custom Exception Classes | Domain-specific hatalar |

### Akış

```
İstek Geldi
    │
    ▼
┌─────────────────────────────────────────────┐
│         Global Exception Middleware          │  ← TÜM hataları yakalar
└─────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────┐
│         MediatR Validation Pipeline          │  ← Validation hatalarını yakalar
└─────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────┐
│              Handler (İş Mantığı)            │  ← Business hataları fırlatır
└─────────────────────────────────────────────┘
```

### Kullanılacak Yapılar

- **Global Exception Middleware** → Tüm servislerde ortak, `BuildingBlocks` içinde
- **Custom Exception Types:**
  - `NotFoundException` → Kayıt bulunamadığında (404)
  - `BadRequestException` → Geçersiz istek (400)
  - `ValidationException` → FluentValidation hataları (400)
  - `InternalServerException` → Beklenmeyen hatalar (500)
- **ProblemDetails Format** → RFC 7807 standardı (API hata response formatı)

### Konum

```
src/BuildingBlocks/
└── BuildingBlocks.Exceptions/
    ├── Exceptions/
    │   ├── NotFoundException.cs
    │   ├── BadRequestException.cs
    │   └── InternalServerException.cs
    ├── Handler/
    │   └── GlobalExceptionHandler.cs
    └── BuildingBlocks.Exceptions.csproj
```

---

## 🏥 Health Checks

### Neden Gerekli?

- Docker container'ların sağlık durumunu izlemek
- Kubernetes/Orchestrator için liveness/readiness probe
- Bağımlılıkların (DB, Redis, RabbitMQ) erişilebilirliğini kontrol

### Kullanılacak Yapılar

| Servis | Health Check | Paket |
|--------|--------------|-------|
| **Catalog.API** | PostgreSQL | `AspNetCore.HealthChecks.NpgSql` |
| **Basket.API** | Redis + PostgreSQL | `AspNetCore.HealthChecks.Redis`, `AspNetCore.HealthChecks.NpgSql` |
| **Ordering.API** | PostgreSQL | `AspNetCore.HealthChecks.NpgSql` (RabbitMQ health check kaldırıldı - MassTransit zaten RabbitMQ'yu yönetiyor) |
| **Discount.Grpc** | PostgreSQL | `AspNetCore.HealthChecks.NpgSql` |
| **Gateway.API** | Downstream services | `AspNetCore.HealthChecks.Uris` (Container network adresleri: `http://catalog.api:8080/health`, `http://basket.api:8080/health`, `http://ordering.api:8080/health`) |

### Endpoint'ler

| Endpoint | Kullanım |
|----------|----------|
| `/health` | Genel sağlık durumu |
| `/health/ready` | Readiness (bağımlılıklar hazır mı?) |
| `/health/live` | Liveness (servis çalışıyor mu?) |

### Docker Compose Entegrasyonu

```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
  interval: 30s
  timeout: 10s
  retries: 3
```

---

## 🌐 Portlar & UI Erişim Tablosu

### Servis Portları

| Servis | Internal Port | External Port | Açıklama |
|--------|---------------|---------------|----------|
| **Gateway.API** | 8080 | 5000 | Ana giriş noktası |
| **Catalog.API** | 8080 | 5001 | Ürün servisi |
| **Basket.API** | 8080 | 5278 | Sepet servisi (launchSettings.json'da 5278) |
| **Ordering.API** | 8080 | 5003 | Sipariş servisi |
| **Discount.Grpc** | 8080 | 5004 | İndirim servisi (gRPC) |

### Veritabanı Portları

| Veritabanı | Port | Container Adı |
|------------|------|---------------|
| CatalogDb (PostgreSQL) | 5436 | catalogdb (Host port: 5436, container port: 5432 - sistem PostgreSQL ile çakışmayı önlemek için) |
| OrderingDb (PostgreSQL) | 5435 | orderingdb (5433 kullanılıyordu, 5435'e değiştirildi) |
| DiscountDb (PostgreSQL) | 5434 | discountdb |
| BasketDb (Redis) | 6379 | basketdb |
| BasketDb (PostgreSQL) | 5437 | basketpostgres (Host port: 5437, container port: 5432) |

### UI & Yönetim Panelleri

| UI | URL | Kullanıcı/Şifre |
|----|-----|-----------------|
| **RabbitMQ Management** | http://localhost:15673 | guest / guest (15672 kullanılıyordu, 15673'e değiştirildi) |
| **RedisInsight** | http://localhost:8001 | - |
| **pgAdmin** | http://localhost:5050 | admin@admin.com / admin |
| **Swagger (Catalog)** | http://localhost:5001/ | - |
| **Swagger (Basket)** | http://localhost:5278/swagger | - |
| **Swagger (Ordering)** | http://localhost:5003/swagger | - |
| **Gateway** | http://localhost:5000 | - |

---

## 🐳 Dockerfile Stratejisi

### Yapı

Her servis için **multi-stage Dockerfile** kullanılacak.

| Stage | Amaç |
|-------|------|
| **base** | Runtime image (aspnet) |
| **build** | SDK ile derleme |
| **publish** | Release build |
| **final** | Minimal production image |

### Konum

```
src/Services/Catalog/Catalog.API/Dockerfile
src/Services/Basket/Basket.API/Dockerfile
src/Services/Ordering/Ordering.API/Dockerfile
src/Services/Discount/Discount.Grpc/Dockerfile
src/ApiGateway/Gateway.API/Dockerfile
```

### Kullanılacak Base Image'lar

| Amaç | Image |
|------|-------|
| **Build** | `mcr.microsoft.com/dotnet/sdk:9.0` |
| **Runtime** | `mcr.microsoft.com/dotnet/aspnet:9.0` |

### Build Context

Tüm Dockerfile'lar **solution root**'tan build edilecek (shared projelere erişim için).

```bash
docker build -f src/Services/Catalog/Catalog.API/Dockerfile -t catalogapi .
```

---

## ⚙️ appsettings.json Stratejisi

### Dosya Yapısı

Her serviste:

| Dosya | Kullanım |
|-------|----------|
| `appsettings.json` | Varsayılan ayarlar |
| `appsettings.Development.json` | Local geliştirme |
| `appsettings.Docker.json` | Docker ortamı (opsiyonel) |

### İçerik Yapısı

| Bölüm | Açıklama |
|-------|----------|
| `ConnectionStrings` | Database bağlantıları |
| `GrpcSettings` | gRPC servis URL'leri (Basket için) |
| `MessageBroker` | RabbitMQ ayarları |
| `Logging` | Serilog konfigürasyonu |
| `ReverseProxy` | YARP routing (sadece Gateway) |

### Environment Variables ile Override

Docker Compose'da `environment` ile override edilecek:

```yaml
environment:
  - ConnectionStrings__Database=Host=catalogdb;...
  - MessageBroker__Host=amqp://guest:guest@messagebroker:5672
```

> **Not:** `__` (çift alt çizgi) = JSON'daki `:` (nested property)

---

## 📝 Sonraki Adımlar

1. [ ] Solution ve projeleri oluştur
2. [ ] Docker Compose dosyasını yaz
3. [ ] BuildingBlocks projelerini oluştur (Messaging, Behaviors)
4. [ ] Catalog Service'i geliştir (CQRS + MediatR)
5. [ ] Discount Service'i geliştir (gRPC)
6. [ ] Basket Service'i geliştir (Redis + gRPC client + MediatR)
7. [ ] Ordering Service'i geliştir (RabbitMQ consumer + MediatR)
8. [ ] API Gateway'i konfigüre et (YARP)
9. [ ] Tüm sistemi Docker ile test et

---

## 🔗 Kaynaklar

- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
- [MassTransit Documentation](https://masstransit.io/)
- [gRPC for .NET](https://learn.microsoft.com/en-us/aspnet/core/grpc/)
- [Docker Compose](https://docs.docker.com/compose/)
- [FluentValidation](https://docs.fluentvalidation.net/)
