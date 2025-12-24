GitHub README’leri için “genel kabul görmüş” yazım kuralları özetle şunlara dayanıyor: README, projeni **neden var**, **ne işe yarar** ve **nasıl kullanılır** sorularına hızlı cevap vermeli. ([GitHub Docs][1])
İçerik **önem sırasına göre** düzenlenmeli, **uzun paragraflardan kaçınılmalı**, madde işaretleri/başlıklar ile okunabilirlik artırılmalı. ([GitHub Docs][2])
GitHub, **GitHub Flavored Markdown** ile başlıklar, tablolar, kod blokları vb. standart Markdown yapısını önerir. ([GitHub Docs][3])
ASCII diyagramlar yerine GitHub’ın desteklediği **Mermaid diyagramları** kullanılırsa görüntü bozulma ihtimali çok azalır. ([GitHub Docs][4])
Badge tarafında da en stabil yöntem, HTML yerine düz Markdown formatıdır; Shields örnekleri bu biçimi temel alır. ([shields.io][5])

Aşağıdaki README; **HTML’siz**, **badge’leri düzgün çalışan**, **Mermaid ile diyagramlı**, GitHub’da daha “temiz” görünen yeniden yazılmış sürüm:

````md
# 🏪 E-Shop Microservice Practice Project

> **Eğitim amaçlı microservice mimarisi e-ticaret projesi**  
> Microservice, Redis, RabbitMQ, gRPC, Docker, CQRS ve API Gateway (YARP) pratik etmek için tasarlanmıştır.

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Docker Compose](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat-square&logo=docker&logoColor=white)](https://www.docker.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=flat-square&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-Stack-DC382D?style=flat-square&logo=redis&logoColor=white)](https://redis.io/)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3-FF6600?style=flat-square&logo=rabbitmq&logoColor=white)](https://www.rabbitmq.com/)

---

## ✨ Öne Çıkanlar

- 🧩 **Microservice Architecture** (Database-per-service)
- 🔄 **CQRS + MediatR**
- 💾 **Redis Cache-aside** (Basket)
- 📨 **RabbitMQ + MassTransit** (event-driven)
- ⚡ **gRPC (h2c)** (Basket → Discount)
- 🚪 **API Gateway (YARP)** (tek giriş noktası)
- 🏥 **Health Checks** (servis izleme)

---

## 📌 İçindekiler

- [Mimari](#-mimari)
- [Servisler](#-servisler)
- [Teknoloji Stack](#-teknoloji-stack)
- [Hızlı Başlangıç](#-hızlı-başlangıç)
- [Portlar ve Yönetim Panelleri](#-portlar-ve-yönetim-panelleri)
- [Kullanım](#-kullanım)
- [API Endpoints](#-api-endpoints)
- [Test Senaryosu (E2E)](#-test-senaryosu-e2e)
- [Proje Yapısı](#-proje-yapısı)
- [Dokümantasyon](#-dokümantasyon)
- [Durdurma](#-durdurma)

---

## 🏗️ Mimari

### Genel Akış (Gateway → Servisler)

```mermaid
flowchart TB
  GW[API Gateway (YARP)\n:5000]

  C[Catalog.API\n:5001]
  B[Basket.API\n:5002]
  O[Ordering.API\n:5003]
  D[Discount.Grpc\n:5004]

  CDB[(PostgreSQL\nCatalogDb)]
  BDB[(PostgreSQL\nBasketDb)]
  ODB[(PostgreSQL\nOrderingDb)]
  DDB[(PostgreSQL\nDiscountDb)]
  R[(Redis\nCache)]
  MQ[(RabbitMQ)]

  GW --> C
  GW --> B
  GW --> O

  C --> CDB
  B --> BDB
  B --> R
  O --> ODB
  D --> DDB

  B -->|gRPC sync| D
  B -->|Checkout event async| MQ --> O
````

### İletişim Tipleri

* **Synchronous (gRPC):** `Basket.API → Discount.Grpc` (indirim sorgulama)
* **Asynchronous (RabbitMQ):** `Basket.API → Ordering.API` (checkout event)

### Cache Stratejisi (Basket - Cache-aside)

* **Get Basket**

  1. Redis’te varsa Redis’ten dön
  2. Yoksa PostgreSQL’den al → Redis’e yaz → dön
* **Upsert Basket**

  1. PostgreSQL’e yaz (source of truth)
  2. Redis’e yaz (cache)

---

## 🔧 Servisler

| Servis          | Port | DB                            | Not                               |
| --------------- | ---- | ----------------------------- | --------------------------------- |
| Gateway.API     | 5000 | -                             | YARP reverse proxy                |
| Catalog.API     | 5001 | PostgreSQL (CatalogDb)        | Ürün/Kategori CRUD                |
| Basket.API      | 5002 | PostgreSQL (BasketDb) + Redis | Sepet + cache + gRPC + event      |
| Ordering.API    | 5003 | PostgreSQL (OrderingDb)       | Event consumer + sipariş yönetimi |
| Discount.Grpc   | 5004 | PostgreSQL (DiscountDb)       | gRPC kupon/indirim                |
| Discount Health | 5005 | -                             | Health endpoint                   |

---

## 🛠️ Teknoloji Stack

### Backend

* ASP.NET Core 9.0, C# 13
* CQRS + MediatR
* EF Core, AutoMapper, FluentValidation

### Data / Cache

* PostgreSQL 16
* Redis Stack (cache-aside)

### Messaging / Communication

* RabbitMQ 3 + MassTransit
* gRPC (h2c)
* YARP (API Gateway)

### Infrastructure

* Docker, Docker Compose
* Health Checks

---

## 🚀 Hızlı Başlangıç

### Önkoşullar

* .NET 9.0 SDK+
* Docker + Docker Compose (v2)
* Git

### Çalıştırma

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

### Management UI

| Araç                | URL                                              | Kullanıcı/Şifre                                   |
| ------------------- | ------------------------------------------------ | ------------------------------------------------- |
| RabbitMQ Management | [http://localhost:15673](http://localhost:15673) | guest / guest                                     |
| RedisInsight        | [http://localhost:8001](http://localhost:8001)   | -                                                 |
| pgAdmin             | [http://localhost:5050](http://localhost:5050)   | [admin@admin.com](mailto:admin@admin.com) / admin |

### Swagger

* Catalog: [http://localhost:5001/swagger](http://localhost:5001/swagger)
* Basket: [http://localhost:5002/swagger](http://localhost:5002/swagger)
* Ordering: [http://localhost:5003/swagger](http://localhost:5003/swagger)

---

## 💻 Kullanım

> Tüm API’lere Gateway üzerinden erişilir.

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

### Catalog

```text
GET    /api/products
GET    /api/products/{id}
POST   /api/products
PUT    /api/products/{id}
DELETE /api/products/{id}

GET    /api/categories
GET    /api/categories/{id}
```

### Basket

```text
GET    /api/baskets/{userName}
POST   /api/baskets
DELETE /api/baskets/{userName}
POST   /api/baskets/checkout
```

### Ordering

```text
GET    /api/orders
GET    /api/orders/{id}
GET    /api/orders/user/{userName}
```

### Discount (gRPC)

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

## 📁 Proje Yapısı

```text
microservice-practice-me/
├── src/
│   ├── Services/
│   │   ├── Catalog/Catalog.API/
│   │   ├── Basket/Basket.API/
│   │   ├── Ordering/Ordering.API/
│   │   └── Discount/Discount.Grpc/
│   ├── ApiGateway/Gateway.API/
│   └── BuildingBlocks/
├── docs/
├── docker-compose.yml
├── global.json
├── Directory.Build.props
├── Directory.Packages.props
└── README.md
```

---

## 📖 Dokümantasyon

* Mimari: `docs/architecture/`
* Kurulum: `docs/proje-calisma-kilavuzu.md`
* DB Özeti: `docs/docker-databases-summary.md`

---

## 🛑 Durdurma

```bash
docker compose down
```

> DB verilerini de silmek için:

```bash
docker compose down -v
```

---

⭐ Repo işine yaradıysa yıldız bırakmayı unutma!

```

İstersen bir sonraki adım olarak README’ye “çok şık görünen” şu iki şeyi de ekleyebilirim (bozmadan):
- Gateway route’larının **tek tabloda** listelendiği bir “Routing” bölümü
- “Troubleshooting” (port çakışması, container health, DB connection vb.) mini rehber
::contentReference[oaicite:5]{index=5}
```

[1]: https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-readmes?utm_source=chatgpt.com "About the repository README file"
[2]: https://docs.github.com/en/contributing/writing-for-github-docs/best-practices-for-github-docs?utm_source=chatgpt.com "Best practices for GitHub Docs"
[3]: https://docs.github.com/github/writing-on-github/getting-started-with-writing-and-formatting-on-github/basic-writing-and-formatting-syntax?utm_source=chatgpt.com "Basic writing and formatting syntax"
[4]: https://docs.github.com/en/get-started/writing-on-github/working-with-advanced-formatting/creating-diagrams?utm_source=chatgpt.com "Creating Mermaid diagrams"
[5]: https://shields.io/badges?utm_source=chatgpt.com "Static Badge"
