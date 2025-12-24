Aşağıdaki README, içeriğini bozmadan **daha okunabilir**, **daha “GitHub-friendly”**, daha düzenli başlıklar + görsel bloklar + hızlı başlangıç akışıyla yenilenmiş bir versiyon. Kopyalayıp `README.md` olarak kullanabilirsin.

````md
# 🏪 E-Shop Microservice Practice Project

> **Eğitim amaçlı microservice mimarisi e-ticaret projesi**  
> Microservice, Redis, RabbitMQ, gRPC, Docker, CQRS ve API Gateway gibi modern yaklaşımları pratik etmek için tasarlanmıştır.

<p align="left">
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-9.0-purple.svg" /></a>
  <a href="https://www.docker.com/"><img src="https://img.shields.io/badge/Docker-Compose-blue.svg" /></a>
  <a href="https://www.postgresql.org/"><img src="https://img.shields.io/badge/PostgreSQL-16-blue.svg" /></a>
  <a href="https://redis.io/"><img src="https://img.shields.io/badge/Redis-Stack-red.svg" /></a>
  <a href="https://www.rabbitmq.com/"><img src="https://img.shields.io/badge/RabbitMQ-3-orange.svg" /></a>
</p>

---

## ✨ Neler Öğreneceksin?

- ✅ **Microservice mimarisi** (service decomposition, database-per-service)
- ✅ **Redis** ile cache yönetimi (**cache-aside**)
- ✅ **RabbitMQ + MassTransit** ile **event-driven** asenkron iletişim
- ✅ **gRPC (h2c)** ile yüksek performanslı senkron servisler arası haberleşme
- ✅ **Docker & Docker Compose** ile containerization
- ✅ **CQRS + MediatR** pattern’i
- ✅ **API Gateway (YARP)** ile merkezi routing / tek giriş noktası
- ✅ **Health Checks** ile servis izleme

---

## 📌 İçindekiler

- [Mimari](#-mimari)
- [Servisler](#-servisler)
- [Teknoloji Stack](#-teknoloji-stack)
- [Hızlı Başlangıç](#-hızlı-başlangıç)
- [Portlar ve Yönetim Panelleri](#-portlar-ve-yönetim-panelleri)
- [Kullanım Örnekleri](#-kullanım-örnekleri)
- [API Endpoints](#-api-endpoints)
- [Test Senaryosu (E2E)](#-test-senaryosu-e2e)
- [Proje Yapısı](#-proje-yapısı)
- [Durdurma](#-durdurma)
- [Dokümantasyon](#-dokümantasyon)

---

## 🏗️ Mimari

### Genel Bakış

```text
┌─────────────────────────────────────────────────────────────┐
│                      API Gateway (YARP)                     │
│                         Port: 5000                          │
└───────────────────────┬─────────────────────────────────────┘
                        │
        ┌───────────────┼───────────────┐
        │               │               │
        ▼               ▼               ▼
┌──────────┐      ┌──────────┐      ┌──────────┐
│ Catalog  │      │  Basket  │      │ Ordering │
│   API    │      │   API    │      │   API    │
│  5001    │      │  5002    │      │  5003    │
└────┬─────┘      └────┬─────┘      └────┬─────┘
     │                 │                  │
     │                 │                  │
     │           ┌─────┴─────┐            │
     │           ▼           ▼            │
     │      PostgreSQL      Redis         │
     │      (BasketDb)     (Cache)        │
     │                 │
     │                 ▼
     │           Discount.Grpc (5004)
     │                 │
     │            PostgreSQL (DiscountDb)
     │
     └──────────────► PostgreSQL (CatalogDb)

                 Basket.API ──(async)──► RabbitMQ ──► Ordering.API
````

### Servisler Arası İletişim

* **Synchronous (gRPC):** `Basket.API → Discount.Grpc` (indirim sorgulama)
* **Asynchronous (RabbitMQ):** `Basket.API → Ordering.API` (checkout event)

### Database Per Service

* **Catalog.API** → PostgreSQL (**CatalogDb**)
* **Basket.API** → PostgreSQL (**BasketDb**) + Redis (**Cache**)
* **Ordering.API** → PostgreSQL (**OrderingDb**)
* **Discount.Grpc** → PostgreSQL (**DiscountDb**)

### Cache Stratejisi (Basket)

**Cache-aside pattern:**

```text
Sepet Getirme:
1) Redis'te var mı? → Evet: Redis'ten döner
2) Yok: PostgreSQL'den alır → Redis'e yazar → Döner

Sepet Kaydetme:
1) PostgreSQL'e yazar (source of truth)
2) Redis'e yazar (cache)
```

---

## 🔧 Servisler

### 1) Catalog Service (Ürün Kataloğu)

* **Port:** `5001`
* **DB:** PostgreSQL (CatalogDb)
* Ürün & kategori yönetimi, CRUD, Swagger

### 2) Basket Service (Sepet)

* **Port:** `5002`
* **DB:** PostgreSQL (BasketDb) + Redis (Cache)
* Sepet yönetimi, Redis cache, gRPC ile indirim, RabbitMQ ile checkout event

### 3) Ordering Service (Sipariş)

* **Port:** `5003`
* **DB:** PostgreSQL (OrderingDb)
* RabbitMQ consumer, sipariş oluşturma/sorgulama, MassTransit ile event handling

### 4) Discount Service (İndirim - gRPC)

* **Port:** `5004` (gRPC), `5005` (Health)
* **DB:** PostgreSQL (DiscountDb)
* Kupon yönetimi & indirim sorgulama (HTTP/2 cleartext - h2c)

### 5) Gateway Service (API Gateway - YARP)

* **Port:** `5000`
* Tek giriş noktası, routing, health aggregation

---

## 🧰 Teknoloji Stack

**Backend**

* ASP.NET Core 9.0, C# 13
* EF Core, AutoMapper, FluentValidation
* CQRS + MediatR

**Data / Cache**

* PostgreSQL 16
* Redis Stack (cache-aside)

**Messaging**

* RabbitMQ 3
* MassTransit

**Communication**

* gRPC (h2c)
* YARP (Reverse Proxy / Gateway)

**Infrastructure**

* Docker, Docker Compose
* Health Checks

---

## 🚀 Hızlı Başlangıç

### Önkoşullar

* .NET 9.0 SDK+
* Docker & Docker Compose (v2.x)
* Git

### Kurulum

```bash
git clone <repository-url>
cd microservice-practice-me
docker compose up -d
docker compose ps
```

### Health Check

```bash
curl http://localhost:5000/health  # Gateway
curl http://localhost:5001/health  # Catalog
curl http://localhost:5002/health  # Basket
curl http://localhost:5003/health  # Ordering
curl http://localhost:5005/health  # Discount
```

---

## 🔌 Portlar ve Yönetim Panelleri

### Servis Portları

| Servis          | Port | Açıklama               |
| --------------- | ---- | ---------------------- |
| Gateway.API     | 5000 | API Gateway            |
| Catalog.API     | 5001 | Ürün servisi           |
| Basket.API      | 5002 | Sepet servisi          |
| Ordering.API    | 5003 | Sipariş servisi        |
| Discount.Grpc   | 5004 | İndirim servisi (gRPC) |
| Discount Health | 5005 | İndirim health check   |

### Management UI

| Araç                | URL                                              | Kullanıcı/Şifre                                   |
| ------------------- | ------------------------------------------------ | ------------------------------------------------- |
| RabbitMQ Management | [http://localhost:15673](http://localhost:15673) | guest / guest                                     |
| RedisInsight        | [http://localhost:8001](http://localhost:8001)   | -                                                 |
| pgAdmin             | [http://localhost:5050](http://localhost:5050)   | [admin@admin.com](mailto:admin@admin.com) / admin |

---

## 💻 Kullanım Örnekleri

> Tüm endpoint’lere **Gateway** üzerinden erişilir.

### Ürünleri Listele

```bash
curl http://localhost:5000/catalog-service/api/products
```

### Sepete Ürün Ekle / Güncelle

```bash
curl -X POST http://localhost:5000/basket-service/api/baskets \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "testuser",
    "items": [{
      "productId": "product-id",
      "productName": "iPhone 15",
      "quantity": 1,
      "price": 55000
    }]
  }'
```

### Sepeti Getir (indirim uygulanmış)

```bash
curl http://localhost:5000/basket-service/api/baskets/testuser
```

### Checkout (Sipariş oluştur)

```bash
curl -X POST http://localhost:5000/basket-service/api/baskets/checkout \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "testuser",
    "firstName": "Test",
    "lastName": "User",
    "emailAddress": "test@example.com",
    "addressLine": "Test Address",
    "country": "Turkey",
    "state": "Istanbul",
    "zipCode": "34000",
    "cardName": "Test Card",
    "cardNumber": "1234567890123456",
    "expiration": "12/25",
    "cvv": "123",
    "paymentMethod": 1
  }'
```

---

## 📡 API Endpoints

### Catalog Service

```text
GET    /api/products
GET    /api/products/{id}
POST   /api/products
PUT    /api/products/{id}
DELETE /api/products/{id}

GET    /api/categories
GET    /api/categories/{id}
```

### Basket Service

```text
GET    /api/baskets/{userName}
POST   /api/baskets
DELETE /api/baskets/{userName}
POST   /api/baskets/checkout
```

### Ordering Service

```text
GET    /api/orders
GET    /api/orders/{id}
GET    /api/orders/user/{userName}
```

### Discount Service (gRPC)

```text
rpc GetDiscount(GetDiscountRequest) returns (CouponModel)
rpc CreateDiscount(CreateDiscountRequest) returns (CouponModel)
rpc UpdateDiscount(UpdateDiscountRequest) returns (CouponModel)
rpc DeleteDiscount(DeleteDiscountRequest) returns (DeleteDiscountResponse)
```

---

## 🧪 Test Senaryosu (E2E)

1. Ürünleri al:

```bash
curl http://localhost:5000/catalog-service/api/products
```

2. Sepete ürün ekle:

```bash
curl -X POST http://localhost:5000/basket-service/api/baskets \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "testuser",
    "items": [{
      "productId": "macbook-id",
      "productName": "MacBook Pro",
      "quantity": 1,
      "price": 55000
    }]
  }'
```

3. Sepeti getir:

```bash
curl http://localhost:5000/basket-service/api/baskets/testuser
```

4. Checkout:

```bash
curl -X POST http://localhost:5000/basket-service/api/baskets/checkout \
  -H "Content-Type: application/json" \
  -d '{...checkout data...}'
```

5. Siparişleri kontrol et:

```bash
curl http://localhost:5000/ordering-service/api/orders/user/testuser
```

---

## 🗂️ Proje Yapısı

```text
microservice-practice-me/
│
├── src/
│   ├── Services/
│   │   ├── Catalog/
│   │   │   └── Catalog.API/
│   │   ├── Basket/
│   │   │   └── Basket.API/
│   │   ├── Ordering/
│   │   │   └── Ordering.API/
│   │   └── Discount/
│   │       └── Discount.Grpc/
│   │
│   ├── ApiGateway/
│   │   └── Gateway.API/
│   │
│   └── BuildingBlocks/
│       ├── BuildingBlocks.Exceptions/
│       ├── BuildingBlocks.Behaviors/
│       └── BuildingBlocks.Messaging/
│
├── docs/
├── docker-compose.yml
├── global.json
├── Directory.Build.props
├── Directory.Packages.props
└── README.md
```

---

## 🛑 Durdurma

```bash
docker compose down
```

> Volume’ları da silmek (DB verileri silinir):

```bash
docker compose down -v
```

---

## 📖 Dokümantasyon

* **Mimari:** `docs/architecture/`
* **Kurulum Rehberi:** `docs/proje-calisma-kilavuzu.md`
* **DB Özeti:** `docs/docker-databases-summary.md`

---

## 🤝 Katkı

Bu proje eğitim amaçlıdır. Öneri ve iyileştirmeler için issue/PR açabilirsin.

---

## 📝 Lisans

Eğitim amaçlı açık kaynak.

---

## 🙏 Teşekkürler

* ASP.NET Core — [https://dotnet.microsoft.com/](https://dotnet.microsoft.com/)
* Docker — [https://www.docker.com/](https://www.docker.com/)
* PostgreSQL — [https://www.postgresql.org/](https://www.postgresql.org/)
* Redis — [https://redis.io/](https://redis.io/)
* RabbitMQ — [https://www.rabbitmq.com/](https://www.rabbitmq.com/)
* gRPC — [https://grpc.io/](https://grpc.io/)
* YARP — [https://microsoft.github.io/reverse-proxy/](https://microsoft.github.io/reverse-proxy/)

⭐ Beğendiysen repo’ya yıldız bırakmayı unutma!

```

İstersen bir de README’yi “daha profesyonel” hale getirecek 3 küçük dokunuş ekleyebilirim (sen istemeden dosyayı uzatmadan):
- En üste **Quick Links** (Swagger, Health, UI’lar)
- “Architecture Decisions” veya “Roadmap / Next Steps” (örn: Observability, OpenTelemetry, retries, idempotency)
- Basit bir **Sequence Diagram** (Checkout akışı: Basket → RabbitMQ → Ordering)
```
