# 🏗️ E-Shop Microservice Mimarisi (Minimal API)

## 📁 Proje Yapısı

```
microservice-mrt-practice/
│
├── src/
│   ├── Services/
│   │   ├── Catalog/                       # Ürün Kataloğu
│   │   │   ├── Catalog.API/               # Minimal API + Carter
│   │   │   │   ├── Endpoints/             # Endpoint modülleri
│   │   │   │   ├── Features/              # CQRS (Vertical Slice)
│   │   │   │   ├── Data/                  # EF Core DbContext
│   │   │   │   ├── Entities/              # Domain modelleri
│   │   │   │   ├── Program.cs
│   │   │   │   ├── Dockerfile
│   │   │   │   └── appsettings.json
│   │   │   └── Catalog.API.Tests/
│   │   │
│   │   ├── Basket/                        # Sepet (Redis)
│   │   │   ├── Basket.API/
│   │   │   │   ├── Endpoints/
│   │   │   │   ├── Features/
│   │   │   │   ├── Data/                  # Redis Repository
│   │   │   │   ├── Entities/
│   │   │   │   ├── Program.cs
│   │   │   │   ├── Dockerfile
│   │   │   │   └── appsettings.json
│   │   │   └── Basket.API.Tests/
│   │   │
│   │   ├── Ordering/                      # Sipariş
│   │   │   ├── Ordering.API/
│   │   │   │   ├── Endpoints/
│   │   │   │   ├── Features/
│   │   │   │   ├── Data/
│   │   │   │   ├── Entities/
│   │   │   │   ├── Program.cs
│   │   │   │   ├── Dockerfile
│   │   │   │   └── appsettings.json
│   │   │   └── Ordering.API.Tests/
│   │   │
│   │   └── Discount/                      # İndirim (gRPC)
│   │       ├── Discount.Grpc/
│   │       │   ├── Protos/
│   │       │   ├── Services/
│   │       │   ├── Program.cs
│   │       │   ├── Dockerfile
│   │       │   └── appsettings.json
│   │       └── Discount.Grpc.Tests/
│   │
│   ├── ApiGateway/                        # Tek Gateway (YARP)
│   │   └── Gateway.API/
│   │       ├── Program.cs
│   │       ├── Dockerfile
│   │       └── appsettings.json
│   │
│   └── BuildingBlocks/                    # Paylaşılan Kod
│       ├── BuildingBlocks.Messaging/      # RabbitMQ/MassTransit
│       │   ├── Events/                    # Integration Events
│       │   └── Extensions/
│       └── BuildingBlocks.Behaviors/      # MediatR Behaviors
│           ├── ValidationBehavior.cs
│           └── LoggingBehavior.cs
│
├── tests/
│   └── ... (test projeleri)
│
├── docker-compose.yml                     # 🐳 TEK KOMUT İLE AYAĞA KALDIR
├── docker-compose.override.yml            # Development ayarları
├── .env                                   # Environment variables
│
├── global.json
├── Directory.Build.props
├── Directory.Packages.props
├── EShop.sln
└── README.md
```

---

## 🐳 Docker Compose Yapısı

```yaml
# docker-compose.yml
services:
  # ===== ALTYAPI =====
  catalogdb:           # PostgreSQL for Catalog
    image: postgres:16
    
  basketdb:            # Redis for Basket
    image: redis:7
    
  orderingdb:          # PostgreSQL for Ordering
    image: postgres:16
    
  discountdb:          # PostgreSQL for Discount
    image: postgres:16
    
  messagebroker:       # RabbitMQ
    image: rabbitmq:3-management
    
  # ===== SERVİSLER =====
  catalog.api:
    build: ./src/Services/Catalog/Catalog.API
    depends_on:
      - catalogdb
      - messagebroker
      
  basket.api:
    build: ./src/Services/Basket/Basket.API
    depends_on:
      - basketdb
      - messagebroker
      
  ordering.api:
    build: ./src/Services/Ordering/Ordering.API
    depends_on:
      - orderingdb
      - messagebroker
      
  discount.grpc:
    build: ./src/Services/Discount/Discount.Grpc
    depends_on:
      - discountdb
      
  gateway.api:
    build: ./src/ApiGateway/Gateway.API
    depends_on:
      - catalog.api
      - basket.api
      - ordering.api
    ports:
      - "5000:8080"    # Dış dünyaya açık tek port
```

### Docker Komutları

```bash
# Tüm sistemi başlat
docker-compose up -d

# Logları izle
docker-compose logs -f

# Durdur
docker-compose down

# Yeniden build et
docker-compose up -d --build
```

---

## 📊 Servisler Arası İletişim

```
┌─────────────────────────────────────────────────────────────┐
│                      GATEWAY (:5000)                         │
│                         (YARP)                               │
└──────────┬──────────────┬──────────────┬───────────────────┘
           │              │              │
           ▼              ▼              ▼
    ┌──────────┐   ┌──────────┐   ┌──────────┐
    │ Catalog  │   │  Basket  │   │ Ordering │
    │   API    │   │   API    │◄──│   API    │
    │  :8080   │   │  :8080   │   │  :8080   │
    └────┬─────┘   └────┬─────┘   └────┬─────┘
         │              │              │
         │         ┌────┴────┐         │
         │         │         │         │
         ▼         ▼         ▼         ▼
    ┌─────────┐ ┌─────┐ ┌─────────────────┐
    │PostgreSQL│ │Redis│ │    RabbitMQ     │
    │ Catalog │ │     │ │  (Async Events) │
    └─────────┘ └─────┘ └─────────────────┘
         ▲                      │
         │              ┌───────┴───────┐
    ┌────┴────┐         ▼               ▼
    │Discount │   BasketCheckout   OrderCreated
    │  gRPC   │      Event            Event
    └─────────┘
```

---

## 🛠️ Teknoloji Stack'i

| Katman | Teknoloji |
|--------|-----------|
| **API Türü** | Minimal API + Carter |
| **Mimari** | Vertical Slice (Feature-based) |
| **CQRS/Mediator** | MediatR |
| **Validation** | FluentValidation |
| **Sync İletişim** | gRPC (Discount servisi) |
| **Async İletişim** | RabbitMQ + MassTransit |
| **Gateway** | YARP |
| **Veritabanları** | PostgreSQL + Redis |
| **ORM** | Entity Framework Core, Dapper |
| **Logging** | Serilog |
| **Container** | Docker Compose |

---

## 🔄 Event Akışı

### Basket Checkout Flow
```
1. User → Basket.API: POST /basket/checkout
2. Basket.API → RabbitMQ: Publish BasketCheckoutEvent
3. Ordering.API ← RabbitMQ: Consume BasketCheckoutEvent
4. Ordering.API: Create Order
5. Ordering.API → RabbitMQ: Publish OrderCreatedEvent
```

### Discount Check Flow (Sync - gRPC)
```
1. Basket.API → Discount.Grpc: GetDiscount(productName)
2. Discount.Grpc → Basket.API: DiscountResponse
3. Basket.API: Apply discount to basket item
```

---

## 📝 Notlar

- Tüm servisler Docker container'ları içinde çalışır
- Tek `docker-compose up` komutu ile tüm sistem ayağa kalkar
- Gateway üzerinden tek port (5000) ile tüm servislere erişilir
- Servisler arası async iletişim RabbitMQ ile sağlanır
- Discount servisi gRPC ile sync iletişim kurar (performans için)

