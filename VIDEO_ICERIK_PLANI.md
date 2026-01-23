# 🎬 5 DAKİKALIK VİDEO SENARYOSU
## E-Shop Microservice Mimarisi - Proje Tanıtımı ve Canlı Demo

---

## ⏱️ ZAMAN ÖZETİ

| Süre | Bölüm | İçerik |
|------|-------|--------|
| 0:30 | Giriş | Docker, container sayısı |
| 0:45 | Mimari | Diyagram, gRPC/RabbitMQ |
| 0:15 | BuildingBlocks | Ortak kodlar |
| 0:30 | Demo: Ürün | Catalog Service, CQRS |
| 0:45 | Demo: Sepet | Redis, gRPC, indirim |
| 0:45 | Demo: Checkout | RabbitMQ, event-driven |
| 0:30 | Demo: Sipariş | Consumer, MassTransit |
| 0:30 | Teknik | Swagger, Health Check |
| 0:30 | Kapanış | Teknoloji özeti |
| **5:00** | **TOPLAM** | |

---

## 📝 DETAYLI SENARYO

### 0:00 - 0:30 | GİRİŞ

**Ekran:** Terminal + `docker compose ps`

**Söylenecekler:**
> "Merhaba, bugün eğitim amaçlı geliştirdiğim bir e-ticaret mikroservis projesini tanıtacağım.
>
> Gördüğünüz gibi `docker compose up` komutuyla 14 container tek seferde ayağa kalkıyor: 5 .NET servisi, 4 PostgreSQL, Redis, RabbitMQ ve Blazor UI."

---

### 0:30 - 1:15 | MİMARİ GENEL BAKIŞ

**Ekran:** README'deki mimari diyagram

**Söylenecekler:**
> "Sistemin mimarisine bakalım:
>
> **3 katman var:**
> - **Blazor WebAssembly** frontend
> - **API Gateway** (YARP) - tek giriş noktası
> - **4 Mikroservis**: Catalog, Basket, Ordering, Discount
>
> **Servisler arası iletişim:**
> - **gRPC**: Basket → Discount (senkron, hızlı indirim sorgulama)
> - **RabbitMQ**: Basket → Ordering (asenkron, checkout event)
>
> **Her servisin kendi veritabanı var** - Database per Service pattern. Basket servisi ayrıca **Redis cache** kullanıyor."

---

### 1:15 - 1:30 | BUILDINGBLOCKS

**Ekran:** BuildingBlocks klasör yapısı

**Söylenecekler:**
> "**BuildingBlocks** klasöründe tüm servislerin ortak kullandığı kodlar var:
> - **Behaviors**: MediatR pipeline - otomatik logging ve validation
> - **Exceptions**: Merkezi hata yönetimi - GlobalExceptionHandler
> - **Messaging**: Servisler arası event tanımları - BasketCheckoutEvent"

---

### 1:30 - 2:00 | DEMO: ÜRÜN LİSTELEME

**Ekran:** Web UI ana sayfa (http://localhost:3000)

**Söylenecekler:**
> "Web arayüzüne geçelim. Ana sayfada ürünler listeleniyor.
>
> Bu veriler şu akışla geliyor:
> **Blazor UI → API Gateway → Catalog Service → PostgreSQL**
>
> Catalog Service'te **CQRS pattern** kullanıyorum - okuma ve yazma işlemleri ayrı. **MediatR** ile controller'dan handler'a istek yönlendiriliyor."

**Yapılacaklar:**
- Ana sayfayı göster
- Bir ürüne tıkla, detay sayfasını göster

---

### 2:00 - 2:45 | DEMO: SEPETE EKLEME + İNDİRİM

**Ekran:** Sepete ekle → Sepet sayfası

**Söylenecekler:**
> "Sepete ürün ekliyorum. Arka planda şunlar oluyor:
>
> 1. **gRPC ile Discount Service'e** bağlanıp indirim sorguluyor
> 2. **PostgreSQL'e** kaydediyor - source of truth
> 3. **Redis'e** cache'liyor - bir sonraki okuma çok hızlı
>
> Bu **Cache-aside Pattern**: önce cache'e bak, yoksa DB'den al ve cache'e yaz.
>
> Sepet sayfasında görüyorsunuz - indirim otomatik uygulandı. gRPC binary protokol kullandığı için JSON'dan 10 kat hızlı."

**Yapılacaklar:**
- Sepete ürün ekle
- Header'daki sepet sayacının güncellendiğini göster
- Sepet sayfasına git
- İndirimin uygulandığını vurgula

---

### 2:45 - 3:30 | DEMO: CHECKOUT + EVENT-DRIVEN

**Ekran:** Checkout sayfası → RabbitMQ Management UI (opsiyonel)

**Söylenecekler:**
> "Siparişi tamamlayalım. Bu kısım **Event-Driven Architecture** örneği:
>
> 1. Basket Service **BasketCheckoutEvent** oluşturuyor
> 2. **RabbitMQ'ya** publish ediyor - asenkron
> 3. **Ordering Service** bu event'i consumer ile dinliyor
> 4. Otomatik sipariş oluşturuyor
>
> **Asenkron iletişimin avantajı**: Basket Service hemen cevap veriyor, Ordering Service'i beklemiyor. Ordering down olsa bile event kuyrukta bekler."

**Yapılacaklar:**
- Checkout sayfasına git
- Formu doldur (örnek veriler):
  - İsim: Test User
  - Email: test@example.com
  - Adres: Test Address
  - Kart: 1234 5678 9012 3456
- Siparişi tamamla
- (Opsiyonel) RabbitMQ UI'da event'i göster

---

### 3:30 - 4:00 | DEMO: SİPARİŞ LİSTELEME

**Ekran:** Siparişlerim sayfası

**Söylenecekler:**
> "Siparişlerim sayfasında az önce oluşturduğumuz sipariş görünüyor.
>
> **Ordering Service**'te MassTransit Consumer, RabbitMQ'dan gelen event'i alıp MediatR ile CreateOrderHandler'ı çağırdı. İndirim bilgisi de siparişe kaydedildi."

**Yapılacaklar:**
- Siparişlerim sayfasına git
- Sipariş listesini göster
- Sipariş detayını göster

---

### 4:00 - 4:30 | TEKNİK ÖZELLİKLER

**Ekran:** Swagger UI + Health Check endpoint

**Söylenecekler:**
> "Her servisin **Swagger UI**'si var - API'leri test edebilirsiniz.
>
> **Health Check** endpoint'leri ile servis durumlarını izleyebilirsiniz. Gateway'de `/health/downstream` tüm servislerin durumunu toplu gösteriyor.
>
> **API Gateway** YARP kullanıyor - path'e göre doğru servise yönlendiriyor. CORS, authentication merkezi olarak yönetilebilir."

**Yapılacaklar:**
- Swagger UI'ı göster (http://localhost:5001/swagger)
- Health check endpoint'ini göster (http://localhost:5000/health)

---

### 4:30 - 5:00 | KAPANIŞ

**Ekran:** Teknoloji listesi veya terminal

**Söylenecekler:**
> "Özetleyecek olursam, bu projede kullandığım teknolojiler:
>
> **Backend:** .NET 9, ASP.NET Core, CQRS + MediatR, EF Core, PostgreSQL
>
> **İletişim:** gRPC (senkron), RabbitMQ + MassTransit (asenkron), YARP (API Gateway)
>
> **Cache:** Redis - Cache-aside pattern
>
> **Frontend:** Blazor WebAssembly
>
> **Infrastructure:** Docker Compose, Health Checks
>
> Tüm sistem tek `docker compose up` komutuyla ayağa kalkıyor. İzlediğiniz için teşekkürler."

---

## 🏗️ PROJE YAPISI VE BİLEŞEN AÇIKLAMALARI

### 📁 BuildingBlocks (Ortak Kütüphaneler)

Tüm mikroservislerin ortak kullandığı kodlar. Kod tekrarını önler.

| Kütüphane | Dosyalar | Açıklama |
|-----------|----------|----------|
| **BuildingBlocks.Behaviors** | `LoggingBehavior.cs`, `ValidationBehavior.cs` | MediatR pipeline - her request'te otomatik logging ve FluentValidation |
| **BuildingBlocks.Exceptions** | `GlobalExceptionHandler.cs`, `NotFoundException.cs` | Merkezi hata yönetimi, RFC 7807 ProblemDetails formatı |
| **BuildingBlocks.Messaging** | `BasketCheckoutEvent.cs`, `IntegrationEvent.cs` | Servisler arası event tanımları |

---

### 📁 API Gateway (YARP)

**Port:** 5000

Tek giriş noktası. Microsoft YARP (Yet Another Reverse Proxy) kullanıyor.

| Route | Hedef |
|-------|-------|
| `/catalog-service/*` | Catalog.API (5001) |
| `/basket-service/*` | Basket.API (5002) |
| `/ordering-service/*` | Ordering.API (5003) |

**Özellikler:**
- CORS yönetimi
- Health Check aggregation (`/health/downstream`)
- İleride: authentication, rate limiting

---

### 📁 Catalog Service

**Port:** 5001 | **DB:** PostgreSQL

Ürün ve kategori yönetimi.

| Pattern/Teknoloji | Kullanım |
|-------------------|----------|
| **CQRS** | Command/Query ayrımı |
| **MediatR** | Handler dispatch |
| **FluentValidation** | Request validation |
| **AutoMapper** | DTO ↔ Entity mapping |
| **EF Core** | PostgreSQL ORM |

---

### 📁 Basket Service ⭐

**Port:** 5002 | **DB:** PostgreSQL + Redis

En karmaşık servis. 4 farklı teknoloji ile iletişim kuruyor.

| Özellik | Teknoloji | Açıklama |
|---------|-----------|----------|
| **Cache** | Redis | Cache-aside pattern |
| **Source of Truth** | PostgreSQL | Kalıcı veri |
| **İndirim Sorgulama** | gRPC → Discount | Senkron, binary protokol |
| **Checkout Event** | RabbitMQ | Asenkron, event-driven |

**Cache-aside Pattern:**
```
GET Basket:
1. Redis'te var mı? → Evet: Redis'ten döner
2. Redis'te yok → PostgreSQL'den al → Redis'e yaz → Döner

SET Basket:
1. PostgreSQL'e yaz (source of truth)
2. Redis'e yaz (cache)
```

---

### 📁 Ordering Service

**Port:** 5003 | **DB:** PostgreSQL

RabbitMQ'dan event dinleyerek sipariş oluşturur.

| Bileşen | Açıklama |
|---------|----------|
| **BasketCheckoutConsumer** | MassTransit consumer, RabbitMQ'dan event alır |
| **CreateOrderHandler** | MediatR handler, sipariş oluşturur |

**Event Akışı:**
```
RabbitMQ → Consumer → MediatR → Handler → PostgreSQL
```

---

### 📁 Discount Service (gRPC)

**Port:** 5004 (gRPC), 5005 (Health Check) | **DB:** PostgreSQL

Ürün bazlı indirim kuponu yönetimi.

| RPC Metodu | Açıklama |
|------------|----------|
| `GetDiscount` | Ürün adına göre indirim sorgular |
| `CreateDiscount` | Yeni kupon oluşturur |
| `UpdateDiscount` | Kuponu günceller |
| `DeleteDiscount` | Kuponu siler |

**Proto Dosyası:** `discount.proto`
```protobuf
service DiscountProtoService {
  rpc GetDiscount (GetDiscountRequest) returns (CouponModel);
}
```

**Port Ayrımı:**
- 8080: HTTP/2 only (gRPC)
- 8081: HTTP/1.1 only (Health Check)

---

### 📁 Web.UI (Blazor WebAssembly)

**Port:** 3000 (Docker), 5006 (Dev)

Client-side SPA. API Gateway üzerinden backend'e bağlanır.

| Sayfa | Açıklama |
|-------|----------|
| `Index.razor` | Ürün listesi |
| `ProductDetail.razor` | Ürün detayı |
| `Basket.razor` | Sepet sayfası |
| `Checkout.razor` | Ödeme formu |
| `Orders.razor` | Sipariş geçmişi |

| Servis | Açıklama |
|--------|----------|
| `CatalogService` | Ürün API iletişimi |
| `BasketService` | Sepet API iletişimi |
| `OrderingService` | Sipariş API iletişimi |
| `BasketStateService` | Sepet sayacı state management |

---

## ✅ ÇEKİM ÖNCESİ KONTROL LİSTESİ

### Komutlar
```bash
# Tüm servisleri başlat
docker compose up -d

# Container durumunu kontrol et
docker compose ps

# Health check
curl http://localhost:5000/health
curl http://localhost:5001/health
curl http://localhost:5002/health
curl http://localhost:5003/health
curl http://localhost:5005/health
```

### Kontrol Listesi
- [ ] Tüm Docker container'lar çalışıyor
- [ ] Web UI erişilebilir (http://localhost:3000)
- [ ] API Gateway çalışıyor (http://localhost:5000)
- [ ] Örnek ürünler yüklü
- [ ] Örnek indirimler yüklü
- [ ] RabbitMQ UI erişilebilir (http://localhost:15673)
- [ ] Ekran kayıt yazılımı hazır
- [ ] Mikrofon test edildi

---

## 🔗 URL'LER

| Servis | URL |
|--------|-----|
| Web UI | http://localhost:3000 |
| API Gateway | http://localhost:5000 |
| Catalog Swagger | http://localhost:5001/swagger |
| Basket Swagger | http://localhost:5002/swagger |
| Ordering Swagger | http://localhost:5003/swagger |
| RabbitMQ Management | http://localhost:15673 (guest/guest) |
| RedisInsight | http://localhost:8001 |
| pgAdmin | http://localhost:5050 (admin@admin.com/admin) |

---

## 🎯 VURGULANACAK MİMARİ ÖZELLİKLER

1. ✅ **Microservice Architecture** - Her servis bağımsız
2. ✅ **API Gateway Pattern** - YARP ile tek giriş noktası
3. ✅ **Database per Service** - Her servis kendi DB'si
4. ✅ **CQRS Pattern** - Command/Query ayrımı
5. ✅ **Event-Driven Architecture** - RabbitMQ ile asenkron
6. ✅ **gRPC** - Yüksek performanslı senkron iletişim
7. ✅ **Cache-aside Pattern** - Redis + PostgreSQL

---

## 🛠️ KULLANILAN TEKNOLOJİLER

### Backend
- .NET 9.0, ASP.NET Core
- CQRS + MediatR
- FluentValidation
- AutoMapper
- Entity Framework Core
- PostgreSQL

### İletişim
- gRPC (senkron)
- RabbitMQ + MassTransit (asenkron)
- YARP (API Gateway)

### Cache
- Redis (Cache-aside pattern)

### Frontend
- Blazor WebAssembly

### Infrastructure
- Docker & Docker Compose
- Health Checks

---

**Başarılar! 🚀**
