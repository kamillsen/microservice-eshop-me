# EF Core Veri Dönüşümleri Rehberi

Bu rehber, Entity Framework Core kullanarak veritabanına yazma ve veritabanından okuma işlemlerinde veri dönüşümlerinin nasıl yapılması gerektiğini açıklar.

---

## 📋 İçindekiler

1. [Genel Prensipler](#genel-prensipler)
2. [DB'ye Yazma (Write/Command)](#dbye-yazma-writecommand)
3. [DB'den Okuma (Read/Query)](#dbden-okuma-readquery)
4. [Mapping Profile Konfigürasyonu](#mapping-profile-konfigürasyonu)
5. [Örnek Kodlar](#örnek-kodlar)
6. [Best Practices](#best-practices)
7. [Hatalı Yaklaşımlar](#hatalı-yaklaşımlar)

---

## 🎯 Genel Prensipler

### Temel Kural
- **DB'ye Yazma:** `DTO/Command → Entity → DB`
- **DB'den Okuma:** `DB → Entity → DTO → API`

### Neden Entity Gerekli?
1. **EF Core DbSet tip güvenliği:** `DbSet<Order>` sadece `Order` entity tipini kabul eder
2. **Navigation property'ler:** Entity ilişkileri (Foreign Key, One-to-Many) tanımlıdır
3. **Migration'lar:** EF Core migration'ları entity'lere göre oluşturulur
4. **Change tracking:** EF Core entity'leri izler ve değişiklikleri takip eder

---

## ✍️ DB'ye Yazma (Write/Command)

### Akış
```
Frontend/API → Command/DTO → Entity → DB
```

### Adımlar

#### 1. Command/DTO → Entity Mapping
```csharp
var order = _mapper.Map<Order>(request);
//                          ↑
//                    Command'dan Entity'ye dönüştür
```

#### 2. Entity'ye Manuel Atamalar (Gerekirse)
```csharp
// Entity'de default değerler varsa manuel atama gereksiz:
// ❌ Gereksiz (entity'de zaten default var):
// order.Id = Guid.NewGuid();
// order.OrderDate = DateTime.UtcNow;
// order.Status = OrderStatus.Pending;

// ✅ Gerekli (ilişki için):
orderItem.OrderId = order.Id; // Foreign key
```

#### 3. İlişkili Entity'leri Ekle
```csharp
foreach (var itemDto in request.Items)
{
    var orderItem = _mapper.Map<OrderItem>(itemDto);
    orderItem.OrderId = order.Id; // Foreign key ayarla
    order.Items.Add(orderItem);    // Navigation property'e ekle
}
```

#### 4. Entity'yi DB'ye Kaydet
```csharp
_context.Orders.Add(order);  // ← Entity tipi zorunlu!
await _context.SaveChangesAsync(cancellationToken);
```

### Örnek: CreateOrderHandler
```csharp
public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
{
    // 1. Command → Entity mapping
    var order = _mapper.Map<Order>(request);
    
    // 2. OrderItems'ları ekle
    foreach (var itemDto in request.Items)
    {
        var orderItem = _mapper.Map<OrderItem>(itemDto);
        orderItem.OrderId = order.Id; // Foreign key
        order.Items.Add(orderItem);
    }
    
    // 3. Veritabanına kaydet
    _context.Orders.Add(order);
    await _context.SaveChangesAsync(cancellationToken);
    
    return order.Id;
}
```

### Kurallar
- ✅ **YAPIN:** Command/DTO → Entity mapping
- ✅ **YAPIN:** Entity tipini kullanın (`DbSet<Order>`)
- ✅ **YAPIN:** Navigation property'leri ve foreign key'leri doğru ayarlayın
- ❌ **YAPMAYIN:** DTO'yu direkt DB'ye yazmayın
- ❌ **YAPMAYIN:** Entity'de default değer varsa tekrar atama yapmayın

---

## 📖 DB'den Okuma (Read/Query)

### Akış
```
DB → Entity → DTO → Frontend/API
```

### Adımlar

#### 1. Entity'yi DB'den Çek (Include ile)
```csharp
var order = await _context.Orders
    .Include(o => o.Items)  // ← Navigation property'leri yükle
    .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
```

#### 2. Entity → DTO Mapping
```csharp
return _mapper.Map<OrderDto>(order);
//                          ↑
//                    Entity'den DTO'ya dönüştür
```

### Örnek: GetOrderByIdHandler
```csharp
public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
{
    // 1. Entity'yi DB'den çek (navigation property'leri Include et)
    var order = await _context.Orders
        .Include(o => o.Items)
        .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
    
    if (order == null)
        return null;
    
    // 2. Entity → DTO mapping
    return _mapper.Map<OrderDto>(order);
}
```

### Örnek: GetOrdersHandler (Liste)
```csharp
public async Task<IEnumerable<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
{
    // 1. Entity listesini DB'den çek
    var orders = await _context.Orders
        .Include(o => o.Items)
        .OrderByDescending(o => o.OrderDate)
        .ToListAsync(cancellationToken);
    
    // 2. Entity listesi → DTO listesi mapping
    return _mapper.Map<IEnumerable<OrderDto>>(orders);
    //                                         ↑
    //                              AutoMapper otomatik list mapping yapar
}
```

### Kurallar
- ✅ **YAPIN:** Entity → DTO mapping
- ✅ **YAPIN:** `Include()` ile navigation property'leri yükleyin
- ✅ **YAPIN:** DTO'yu API response olarak döndürün
- ❌ **YAPMAYIN:** Entity'yi direkt API'den döndürmeyin
- ❌ **YAPMAYIN:** Navigation property'leri yüklemeden mapping yapmayın

---

## ⚙️ Mapping Profile Konfigürasyonu

### MappingProfile.cs
```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ✅ OKUMA: Entity → DTO
        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        //                               ↑
        //                    Enum → string dönüşümü
        
        CreateMap<OrderItem, OrderItemDto>().ReverseMap();
        
        // ✅ YAZMA: Command/DTO → Entity
        CreateMap<CreateOrderCommand, Order>()
            .ForMember(dest => dest.Items, opt => opt.Ignore());
        //                    ↑
        //         Manuel işlenecek (foreach ile)
        
        CreateMap<OrderItemDto, OrderItem>();
        
        // Event → Command mapping (RabbitMQ için)
        CreateMap<BasketCheckoutEvent, CreateOrderCommand>();
    }
}
```

### Mapping Kuralları
1. **Entity → DTO:** Okuma işlemleri için
2. **Command/DTO → Entity:** Yazma işlemleri için
3. **Enum → String:** DTO'da string olarak göster
4. **Ignore:** Manuel işlenecek property'leri ignore et
5. **List Mapping:** AutoMapper otomatik yapar (`List<A> → List<B>`)

---

## 📊 Özet Tablo

| İşlem | Kaynak | Hedef | Yer | Açıklama |
|-------|--------|-------|-----|----------|
| **Yazma** | `CreateOrderCommand` | `Order` | Handler | Command → Entity mapping |
| **Yazma** | `OrderItemDto` | `OrderItem` | Handler | DTO → Entity mapping |
| **Okuma** | `Order` | `OrderDto` | Handler | Entity → DTO mapping |
| **Okuma** | `OrderItem` | `OrderItemDto` | Handler | Entity → DTO mapping |

---

## ✅ Best Practices

### 1. DB'ye Yazma İşlemleri
```csharp
// ✅ DOĞRU:
var entity = _mapper.Map<Entity>(dto);
_context.Entities.Add(entity);
await _context.SaveChangesAsync();

// ❌ YANLIŞ:
_context.Entities.Add(dto); // Tip uyuşmazlığı hatası!
```

### 2. DB'den Okuma İşlemleri
```csharp
// ✅ DOĞRU:
var entities = await _context.Entities
    .Include(e => e.RelatedEntity)
    .ToListAsync();
return _mapper.Map<IEnumerable<Dto>>(entities);

// ❌ YANLIŞ:
var entities = await _context.Entities.ToListAsync();
return entities; // Entity'yi direkt döndürme!
```

### 3. Navigation Property'leri Yükleme
```csharp
// ✅ DOĞRU:
var order = await _context.Orders
    .Include(o => o.Items)  // Items yüklendi
    .FirstOrDefaultAsync(...);
var dto = _mapper.Map<OrderDto>(order); // Items dahil map edildi

// ❌ YANLIŞ:
var order = await _context.Orders.FirstOrDefaultAsync(...);
var dto = _mapper.Map<OrderDto>(order); // Items null olabilir!
```

### 4. Entity'de Default Değerler
```csharp
// ✅ Entity'de default değer varsa:
public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
}

// ✅ Handler'da manuel atama GEREKSIZ:
var order = _mapper.Map<Order>(command);
// order.Id = Guid.NewGuid(); // ❌ Gereksiz, zaten default var
// order.OrderDate = DateTime.UtcNow; // ❌ Gereksiz, zaten default var
```

---

## ❌ Hatalı Yaklaşımlar

### Hata 1: DTO'yu DB'ye Yazmaya Çalışmak
```csharp
// ❌ HATALI:
var orderDto = new OrderDto { ... };
_context.Orders.Add(orderDto); // ❌ Type mismatch hatası!

// ✅ DOĞRU:
var order = _mapper.Map<Order>(orderDto);
_context.Orders.Add(order);
```

### Hata 2: Entity'yi API'den Döndürmek
```csharp
// ❌ HATALI:
public async Task<Order> GetOrder(Guid id)
{
    return await _context.Orders.FindAsync(id);
    //     ↑ Entity'yi direkt döndürme!
}

// ✅ DOĞRU:
public async Task<OrderDto> GetOrder(Guid id)
{
    var order = await _context.Orders.FindAsync(id);
    return _mapper.Map<OrderDto>(order);
}
```

### Hata 3: Navigation Property'leri Yüklemeden Mapping
```csharp
// ❌ HATALI:
var order = await _context.Orders.FindAsync(id);
var dto = _mapper.Map<OrderDto>(order);
// Items null olabilir!

// ✅ DOĞRU:
var order = await _context.Orders
    .Include(o => o.Items)
    .FirstOrDefaultAsync(o => o.Id == id);
var dto = _mapper.Map<OrderDto>(order);
```

### Hata 4: AutoMapper Olmadan Manuel Mapping
```csharp
// ❌ HATALI (uzun ve hata yapmaya açık):
var dto = new OrderDto
{
    Id = entity.Id,
    UserName = entity.UserName,
    TotalPrice = entity.TotalPrice,
    OrderDate = entity.OrderDate,
    Status = entity.Status.ToString(), // Enum → string
    Items = entity.Items.Select(i => new OrderItemDto { ... }).ToList()
    // Çok fazla kod, hata yapma riski yüksek
};

// ✅ DOĞRU (kısa ve güvenli):
var dto = _mapper.Map<OrderDto>(entity);
```

---

## 🔑 Anahtar Kurallar

1. **DB'ye Yazarken:** DTO/Command → Entity → `context.Add(entity)` → `SaveChanges()`
2. **DB'den Okurken:** `context.Query()` → Entity → DTO → API Response
3. **Her zaman:** Entity ↔ DTO mapping yapın
4. **Asla:** Entity'yi API katmanına çıkarmayın (DTO kullanın)
5. **Her zaman:** Navigation property'leri `Include()` ile yükleyin
6. **Her zaman:** MappingProfile'da tüm dönüşümleri tanımlayın

---

## 📝 Özet

- **Yazma:** `DTO/Command → Entity` (AutoMapper ile)
- **Okuma:** `Entity → DTO` (AutoMapper ile)
- **Entity:** Hiçbir zaman API'den dışarı çıkmamalı
- **DTO:** Hiçbir zaman DB'ye direkt yazılmamalı
- **Include:** Navigation property'leri mutlaka yükle
- **Mapping:** AutoMapper kullan, manuel mapping yapma

---

**Son Güncelleme:** 2024-12-15

