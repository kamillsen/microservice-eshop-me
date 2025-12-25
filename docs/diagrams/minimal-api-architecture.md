# 🏗️ E-Shop Microservice Mimarisi

## 📁 Proje Yapısı

```
microservice-practice-me/
│
├── src/
│   ├── Services/
│   │   ├── Catalog/                       # Ürün Kataloğu
│   │   │   └── Catalog.API/              # Controller-based API
│   │   │       ├── Controllers/           # REST API Controllers
│   │   │       ├── Features/              # CQRS (MediatR Handlers)
│   │   │       ├── Data/                  # EF Core DbContext
│   │   │       ├── Entities/             # Domain modelleri
│   │   │       ├── Dtos/                 # Data Transfer Objects
│   │   │       ├── Mapping/              # AutoMapper Profiles
│   │   │       ├── Migrations/           # EF Core Migrations
│   │   │       ├── Program.cs
│   │   │       ├── Dockerfile
│   │   │       └── appsettings.json
│   │   │
│   │   ├── Basket/                        # Sepet (Redis + PostgreSQL)
│   │   │   └── Basket.API/
│   │   │       ├── Controllers/
│   │   │       ├── Features/              # CQRS (MediatR Handlers)
│   │   │       ├── Data/                  # EF Core + Redis Repository
│   │   │       ├── Entities/
│   │   │       ├── Dtos/
│   │   │       ├── GrpcServices/         # gRPC Client (Discount)
│   │   │       ├── Mapping/
│   │   │       ├── Migrations/
│   │   │       ├── Protos/               # gRPC Proto files
│   │   │       ├── Program.cs
│   │   │       ├── Dockerfile
│   │   │       └── appsettings.json
│   │   │
│   │   ├── Ordering/                      # Sipariş
│   │   │   └── Ordering.API/
│   │   │       ├── Controllers/
│   │   │       ├── Features/              # CQRS (MediatR Handlers)
│   │   │       ├── Data/
│   │   │       ├── Entities/
│   │   │       ├── Dtos/
│   │   │       ├── EventHandlers/        # RabbitMQ Event Handlers
│   │   │       ├── Mapping/
│   │   │       ├── Migrations/
│   │   │       ├── Program.cs
│   │   │       ├── Dockerfile
│   │   │       └── appsettings.json
│   │   │
│   │   └── Discount/                      # İndirim (gRPC)
│   │       └── Discount.Grpc/
│   │           ├── Protos/               # gRPC Proto files
│   │           ├── Services/             # gRPC Service Implementation
│   │           ├── Data/
│   │           ├── Entities/
│   │           ├── Migrations/
│   │           ├── Program.cs
│   │           ├── Dockerfile
│   │           └── appsettings.json
│   │
│   ├── ApiGateway/                        # API Gateway (YARP)
│   │   └── Gateway.API/
│   │       ├── Program.cs
│   │       ├── Dockerfile
│   │       └── appsettings.json
│   │
│   └── BuildingBlocks/                    # Paylaşılan Kod
│       ├── BuildingBlocks.Messaging/      # RabbitMQ/MassTransit
│       │   └── Events/                    # Integration Events
│       ├── BuildingBlocks.Behaviors/      # MediatR Behaviors
│       │   └── Behaviors/
│       │       ├── ValidationBehavior.cs
│       │       └── LoggingBehavior.cs
│       └── BuildingBlocks.Exceptions/     # Exception Handling
│           ├── Exceptions/
│           └── Handler/
│
├── tests/
│   └── ... (test projeleri)
│
├── docs/                                  # Dokümantasyon
│   ├── architecture/
│   ├── diagrams/
│   └── ...
│
├── docker-compose.yml                     # 🐳 TEK KOMUT İLE AYAĞA KALDIR
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
    image: postgres:16-alpine
    ports:
      - "5436:5432"     # Host:Container
    
  basketdb:            # Redis for Basket (Cache)
    image: redis/redis-stack:latest
    ports:
      - "6379:6379"     # Redis
      - "8001:8001"     # RedisInsight UI
    
  basketpostgres:      # PostgreSQL for Basket (Source of Truth)
    image: postgres:16-alpine
    ports:
      - "5437:5432"
    
  orderingdb:          # PostgreSQL for Ordering
    image: postgres:16-alpine
    ports:
      - "5435:5432"
    
  discountdb:          # PostgreSQL for Discount
    image: postgres:16-alpine
    ports:
      - "5434:5432"
    
  messagebroker:       # RabbitMQ
    image: rabbitmq:3-management-alpine
    ports:
      - "5673:5672"     # AMQP
      - "15673:15672"   # Management UI
    
  pgadmin:             # PostgreSQL Yönetim Arayüzü
    image: dpage/pgadmin4:latest
    ports:
      - "5050:80"
    
  # ===== SERVİSLER =====
  catalog.api:
    build:
      context: .
      dockerfile: src/Services/Catalog/Catalog.API/Dockerfile
    ports:
      - "5001:8080"
    depends_on:
      catalogdb:
        condition: service_healthy
      
  basket.api:
    build:
      context: .
      dockerfile: src/Services/Basket/Basket.API/Dockerfile
    ports:
      - "5002:8080"
    depends_on:
      basketdb:
        condition: service_healthy
      basketpostgres:
        condition: service_healthy
      discount.grpc:
        condition: service_healthy
      messagebroker:
        condition: service_healthy
      
  ordering.api:
    build:
      context: .
      dockerfile: src/Services/Ordering/Ordering.API/Dockerfile
    ports:
      - "5003:8080"
    depends_on:
      orderingdb:
        condition: service_healthy
      messagebroker:
        condition: service_healthy
      
  discount.grpc:
    build:
      context: .
      dockerfile: src/Services/Discount/Discount.Grpc/Dockerfile
    ports:
      - "5004:8080"     # gRPC (HTTP/2)
      - "5005:8081"     # Health Check (HTTP/1.1)
    depends_on:
      discountdb:
        condition: service_healthy
      
  gateway.api:
    build:
      context: .
      dockerfile: src/ApiGateway/Gateway.API/Dockerfile
    ports:
      - "5000:8080"     # Dış dünyaya açık tek port
    depends_on:
      catalog.api:
        condition: service_healthy
      basket.api:
        condition: service_healthy
      ordering.api:
        condition: service_healthy
```

### Docker Komutları

```bash
# Tüm sistemi başlat
docker compose up -d

# Logları izle
docker compose logs -f

# Durdur
docker compose down

# Yeniden build et
docker compose up -d --build

# Container durumlarını kontrol et
docker compose ps
```

---

## 📊 Servisler Arası İletişim

```
┌─────────────────────────────────────────────────────────────┐
│                      GATEWAY (Port: 5000)                    │
│                         (YARP)                               │
└──────────┬──────────────┬──────────────┬───────────────────┘
           │              │              │
           ▼              ▼              ▼
    ┌──────────┐   ┌──────────┐   ┌──────────┐
    │ Catalog  │   │  Basket  │   │ Ordering │
    │   API    │   │   API    │   │   API    │
    │ Port:    │   │ Port:    │   │ Port:    │
    │  5001   │   │  5002    │   │  5003    │
    └────┬─────┘   └────┬─────┘   └────┬─────┘
         │              │              │
         │              │              │
         │         ┌────┼────┐         │
         │         │    │    │         │
         ▼         ▼    ▼    ▼         ▼
    ┌─────────┐ ┌─────┐ ┌──────────┐ ┌─────────────────┐
    │PostgreSQL│ │Redis│ │PostgreSQL│ │    RabbitMQ     │
    │ Catalog │ │Cache│ │  Basket  │ │  (Async Events) │
    │  (5436) │ │(6379)│ │  (5437)  │ │   (5673/15673)  │
    └─────────┘ └─────┘ └──────────┘ └─────────────────┘
         ▲              │                      │
         │              │              ┌───────┴───────┐
         │              │              │               │
    ┌────┴────┐         │              ▼               ▼
    │Discount │◄────────┼───────  BasketCheckout   OrderCreated
    │  gRPC   │  gRPC   │          Event            Event
    │ Port:   │ (sync)  │
    │ 5004/   │         │
    │ 5005    │         │
    └────┬────┘         │
         │              │
         ▼              │
    ┌──────────┐        │
    │PostgreSQL│        │
    │ Discount │        │
    │  (5434)  │        │
    └──────────┘        │
                        │
                        ▼
                 ┌──────────┐
                 │PostgreSQL│
                 │ Ordering │
                 │  (5435)  │
                 └──────────┘
```

---

## 🛠️ Teknoloji Stack'i

| Katman | Teknoloji |
|--------|-----------|
| **API Türü** | Controller-based API (ASP.NET Core) |
| **Mimari** | CQRS + MediatR (Feature-based) |
| **CQRS/Mediator** | MediatR |
| **Validation** | FluentValidation |
| **Mapping** | AutoMapper |
| **Sync İletişim** | gRPC (Discount servisi, HTTP/2 cleartext) |
| **Async İletişim** | RabbitMQ + MassTransit |
| **Gateway** | YARP (Yet Another Reverse Proxy) |
| **Veritabanları** | PostgreSQL (5 adet) + Redis (Cache-aside pattern) |
| **ORM** | Entity Framework Core |
| **Exception Handling** | Global Exception Handler (RFC 7807) |
| **Health Checks** | AspNetCore.HealthChecks |
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
1. Basket.API → Discount.Grpc: GetDiscount(productName) (gRPC call)
2. Discount.Grpc → Basket.API: DiscountResponse
3. Basket.API: Apply discount to basket item
```

**Not:** gRPC HTTP/2 cleartext (h2c) kullanılıyor. Docker container network içinde TLS olmadan çalışır.

---

## 📝 Notlar

- Tüm servisler Docker container'ları içinde çalışır
- Tek `docker compose up -d` komutu ile tüm sistem ayağa kalkar
- Gateway üzerinden tek port (5000) ile tüm servislere erişilir
- Servisler arası async iletişim RabbitMQ ile sağlanır
- Discount servisi gRPC ile sync iletişim kurar (performans için)
- Basket Service hem Redis (cache) hem PostgreSQL (source of truth) kullanır
- Discount.Grpc iki port kullanır: 8080 (gRPC/HTTP/2) ve 8081 (Health Check/HTTP/1.1)
- Health checks tüm servislerde aktif (PostgreSQL, Redis, RabbitMQ, Downstream services)
- Gateway health check'i downstream servislerden ayrı tutulmuştur (circular dependency önlemek için)

