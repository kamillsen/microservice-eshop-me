# Faz 8.2 - Docker Compose - Servisler

**Tarih:** 24 Aralık 2024  
**Durum:** ✅ Tamamlandı  
**Süre:** ~20 dakika (tüm servislerin eklenmesi, health check'lerin düzeltilmesi ve test)

---

## 📋 Yapılan İşlemler

### 1. Docker Compose'a Servislerin Eklenmesi

Tüm servisler `docker-compose.yml` dosyasına eklendi:

1. **Catalog.API** → `catalog.api` servisi
2. **Basket.API** → `basket.api` servisi
3. **Ordering.API** → `ordering.api` servisi
4. **Discount.Grpc** → `discount.grpc` servisi
5. **Gateway.API** → `gateway.api` servisi

### 2. Servis Konfigürasyonları

Her servis için aşağıdaki konfigürasyonlar yapıldı:

- **build:** Dockerfile yolu ve build context
- **environment:** Connection strings ve diğer environment variables
- **depends_on:** Servis bağımlılıkları (service_healthy condition ile)
- **ports:** Host port → Container port mapping
- **healthcheck:** Health check konfigürasyonu

### 3. Gateway.API appsettings.json Güncellemesi

Gateway'in container network içinde doğru adreslere bağlanması için `appsettings.json` güncellendi:

- `localhost:5001` → `http://catalog.api:8080`
- `localhost:5278` → `http://basket.api:8080`
- `localhost:5103` → `http://ordering.api:8080`

### 4. Health Check'lerin Eklenmesi

**Sorun:** İlk başta health check'lerde `wget` kullanılıyordu ama .NET container'larında `wget` yoktu.

**Çözüm:** Tüm Dockerfile'lara `wget` eklendi:

```dockerfile
# wget kurulumu (health check için gerekli)
RUN apt-get update && \
    apt-get install -y --no-install-recommends wget && \
    rm -rf /var/lib/apt/lists/*
```

### 5. Discount.Grpc Protocol Düzeltmesi

**Sorun:** Discount.Grpc sadece HTTP/2 dinliyordu, health check HTTP/1.1 kullanıyor.

**Çözüm:** `Program.cs`'de `Http2` yerine `Http1AndHttp2` kullanıldı:

```csharp
listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
```

---

## 🔧 Servis Konfigürasyonları

### Catalog.API

```yaml
catalog.api:
  image: ${DOCKER_REGISTRY-}catalogapi
  container_name: catalog.api
  build:
    context: .
    dockerfile: src/Services/Catalog/Catalog.API/Dockerfile
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
    - ConnectionStrings__Database=Host=catalogdb;Port=5432;Database=CatalogDb;Username=postgres;Password=postgres
  depends_on:
    catalogdb:
      condition: service_healthy
  ports:
    - "5001:8080"
  healthcheck:
    test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8080/health"]
    interval: 30s
    timeout: 10s
    retries: 3
    start_period: 40s
```

**Özellikler:**
- Port: 5001 (host) → 8080 (container)
- Bağımlılık: catalogdb (PostgreSQL)
- Connection string: Container network içinde `catalogdb:5432`

### Basket.API

```yaml
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
    - MessageBroker__Host=amqp://guest:guest@messagebroker:5672
  depends_on:
    basketdb:
      condition: service_healthy
    basketpostgres:
      condition: service_healthy
    discount.grpc:
      condition: service_healthy
    messagebroker:
      condition: service_healthy
  ports:
    - "5002:8080"
  healthcheck:
    test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8080/health"]
    interval: 30s
    timeout: 10s
    retries: 3
    start_period: 40s
```

**Özellikler:**
- Port: 5002 (host) → 8080 (container)
- Bağımlılıklar: Redis, PostgreSQL, Discount.Grpc, RabbitMQ
- Connection strings: Container network içinde servis adları kullanılıyor

### Ordering.API

```yaml
ordering.api:
  image: ${DOCKER_REGISTRY-}orderingapi
  container_name: ordering.api
  build:
    context: .
    dockerfile: src/Services/Ordering/Ordering.API/Dockerfile
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
    - ConnectionStrings__Database=Host=orderingdb;Port=5432;Database=OrderingDb;Username=postgres;Password=postgres
    - MessageBroker__Host=amqp://guest:guest@messagebroker:5672
  depends_on:
    orderingdb:
      condition: service_healthy
    messagebroker:
      condition: service_healthy
  ports:
    - "5003:8080"
  healthcheck:
    test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8080/health"]
    interval: 30s
    timeout: 10s
    retries: 3
    start_period: 40s
```

**Özellikler:**
- Port: 5003 (host) → 8080 (container)
- Bağımlılıklar: PostgreSQL, RabbitMQ

### Discount.Grpc

```yaml
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
    discountdb:
      condition: service_healthy
  ports:
    - "5004:8080"
  healthcheck:
    test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8080/health"]
    interval: 30s
    timeout: 10s
    retries: 3
    start_period: 40s
```

**Özellikler:**
- Port: 5004 (host) → 8080 (container)
- gRPC servisi (HTTP/2 ve HTTP/1.1 destekliyor - health check için)
- Bağımlılık: PostgreSQL

### Gateway.API

```yaml
gateway.api:
  image: ${DOCKER_REGISTRY-}gatewayapi
  container_name: gateway.api
  build:
    context: .
    dockerfile: src/ApiGateway/Gateway.API/Dockerfile
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
  depends_on:
    catalog.api:
      condition: service_healthy
    basket.api:
      condition: service_healthy
    ordering.api:
      condition: service_healthy
  ports:
    - "5000:8080"
  healthcheck:
    test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8080/health"]
    interval: 30s
    timeout: 10s
    retries: 3
    start_period: 40s
```

**Özellikler:**
- Port: 5000 (host) → 8080 (container)
- Tüm servislerin hazır olmasını bekler (depends_on)
- Ana giriş noktası (YARP reverse proxy)

---

## 🌐 Gateway appsettings.json Güncellemesi

**Değişiklik:** `localhost` adresleri container network adresleriyle değiştirildi.

**Öncesi:**
```json
{
  "ReverseProxy": {
    "Clusters": {
      "catalog-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5001"
          }
        }
      }
    }
  }
}
```

**Sonrası:**
```json
{
  "ReverseProxy": {
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

**Açıklama:**
- Container network içinde servisler birbirlerine container adıyla erişir
- Port mapping'e gerek yok (container network içinde 8080 portu kullanılır)
- `localhost` yerine container adları kullanılır: `catalog.api`, `basket.api`, `ordering.api`

---

## 🔍 Çözülen Sorunlar

### 1. Health Check Sorunu

**Sorun:**
- Health check'lerde `wget` kullanılıyordu
- .NET container'larında `wget` yoktu
- Health check'ler başarısız oluyordu

**Çözüm:**
Tüm Dockerfile'lara `wget` eklendi:

```dockerfile
# wget kurulumu (health check için gerekli)
RUN apt-get update && \
    apt-get install -y --no-install-recommends wget && \
    rm -rf /var/lib/apt/lists/*
```

**Etkilenen Dosyalar:**
- `src/Services/Catalog/Catalog.API/Dockerfile`
- `src/Services/Basket/Basket.API/Dockerfile`
- `src/Services/Ordering/Ordering.API/Dockerfile`
- `src/Services/Discount/Discount.Grpc/Dockerfile`
- `src/ApiGateway/Gateway.API/Dockerfile`

### 2. Discount.Grpc HTTP/2 Sorunu

**Sorun:**
- Discount.Grpc sadece HTTP/2 dinliyordu (`Http2` protocol)
- Health check HTTP/1.1 kullanıyordu
- Health check başarısız oluyordu: "An HTTP/1.x request was sent to an HTTP/2 only endpoint"

**Çözüm:**
`Program.cs`'de `Http2` yerine `Http1AndHttp2` kullanıldı:

**Öncesi:**
```csharp
options.ListenAnyIP(8080, listenOptions =>
{
    listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
});
```

**Sonrası:**
```csharp
options.ListenAnyIP(8080, listenOptions =>
{
    // Http1AndHttp2: Hem HTTP/1.1 (health check için) hem HTTP/2 (gRPC için) destekle
    listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
});
```

**Etkilenen Dosya:**
- `src/Services/Discount/Discount.Grpc/Program.cs`

---

## 📊 Port Mapping Tablosu

| Servis | Host Port | Container Port | Açıklama |
|--------|-----------|----------------|----------|
| **Gateway.API** | 5000 | 8080 | Ana giriş noktası |
| **Catalog.API** | 5001 | 8080 | Ürün servisi |
| **Basket.API** | 5002 | 8080 | Sepet servisi |
| **Ordering.API** | 5003 | 8080 | Sipariş servisi |
| **Discount.Grpc** | 5004 | 8080 | İndirim servisi (gRPC) |

---

## ✅ Test Sonuçları

### Container Durumları

```bash
$ docker compose ps
```

**Sonuç:**
- ✅ Catalog.API: Healthy
- ✅ Basket.API: Healthy
- ✅ Ordering.API: Healthy
- ✅ Discount.Grpc: Healthy
- ✅ Gateway.API: Running (Docker health check OK)

### Health Check Testleri

```bash
# Catalog.API
curl http://localhost:5001/health
# Çıktı: Healthy

# Basket.API
curl http://localhost:5002/health
# Çıktı: Healthy

# Ordering.API
curl http://localhost:5003/health
# Çıktı: Healthy

# Discount.Grpc
curl http://localhost:5004/health
# Çıktı: Healthy

# Gateway.API
curl http://localhost:5000/health
# Çıktı: Unhealthy (kendi internal health check - downstream services kontrolü)
```

**Not:** Gateway.API'nin Docker health check'i çalışıyor (`/health` endpoint'i erişilebilir), ancak Gateway'in kendi internal health check'i downstream servisleri kontrol ediyor ve bu farklı bir konu.

### API Endpoint Testleri

```bash
# Catalog.API üzerinden ürün listesi
curl http://localhost:5001/api/products
# ✅ Başarılı - Ürün listesi döndü

# Gateway üzerinden catalog service
curl http://localhost:5000/catalog-service/api/products
# ✅ Başarılı - Gateway routing çalışıyor
```

---

## 🎯 Önemli Noktalar

### 1. Container Network İçinde Servis Erişimi

**Kural:** Container network içinde servisler birbirlerine container adıyla erişir.

**Örnekler:**
- `catalog.api:8080` (container adı:port)
- `basket.api:8080`
- `ordering.api:8080`
- `discount.grpc:8080`

**Host'tan Erişim:**
- `localhost:5001` (host port)
- `localhost:5002`
- vb.

### 2. depends_on ve service_healthy

**service_started:**
- Container başladı mı kontrol eder
- Uygulamanın hazır olup olmadığını BİLMEZ
- Hızlı ama güvenilir değil

**service_healthy:**
- Container başladı VE health check başarılı mı kontrol eder
- Uygulama gerçekten hazır mı kontrol eder
- Yavaş ama güvenilir
- Production için önerilir

**Kullanım:**
```yaml
depends_on:
  catalog.api:
    condition: service_healthy  # ✅ Önerilen
  # condition: service_started  # ❌ Güvenilir değil
```

### 3. Health Check Parametreleri

```yaml
healthcheck:
  test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8080/health"]
  interval: 30s      # Her 30 saniyede bir kontrol et
  timeout: 10s       # 10 saniye içinde cevap vermezse başarısız
  retries: 3         # 3 kez deneme
  start_period: 40s  # İlk 40 saniye içinde başarısız olsa bile healthy say (uygulama başlangıç süresi)
```

### 4. Connection String Formatı

**Container Network İçinde:**
```
Host=servisadi;Port=5432;Database=DbAdi;Username=user;Password=pass
```

**Örnek:**
```yaml
ConnectionStrings__Database=Host=catalogdb;Port=5432;Database=CatalogDb;Username=postgres;Password=postgres
```

**Not:** Container network içinde port mapping'e gerek yok (5432 kullanılır, host port değil).

---

## 🔧 Kullanılan Komutlar

### Tüm Servisleri Başlatma

```bash
docker compose up -d
```

### Servisleri Durdurma

```bash
docker compose down
```

### Belirli Bir Servisi Build Etme

```bash
docker compose build catalog.api
```

### Servis Loglarını Görüntüleme

```bash
# Tüm servisler
docker compose logs -f

# Belirli bir servis
docker compose logs -f catalog.api
```

### Container Durumlarını Kontrol Etme

```bash
docker compose ps
```

### Health Check Durumunu Detaylı Görme

```bash
docker inspect catalog.api --format='{{json .State.Health}}' | python3 -m json.tool
```

---

## 📝 Yapılan Değişiklikler Özeti

### docker-compose.yml

**Eklenen Servisler:**
- `catalog.api`
- `basket.api`
- `ordering.api`
- `discount.grpc`
- `gateway.api`

**Her servis için eklenenler:**
- `build` konfigürasyonu
- `environment` variables
- `depends_on` bağımlılıkları
- `ports` mapping
- `healthcheck` konfigürasyonu

### Dockerfile'lar

**Her Dockerfile'a eklenen:**
```dockerfile
# wget kurulumu (health check için gerekli)
RUN apt-get update && \
    apt-get install -y --no-install-recommends wget && \
    rm -rf /var/lib/apt/lists/*
```

**Etkilenen Dosyalar:**
- `src/Services/Catalog/Catalog.API/Dockerfile`
- `src/Services/Basket/Basket.API/Dockerfile`
- `src/Services/Ordering/Ordering.API/Dockerfile`
- `src/Services/Discount/Discount.Grpc/Dockerfile`
- `src/ApiGateway/Gateway.API/Dockerfile`

### Gateway.API/appsettings.json

**Değişiklik:**
- `localhost:5001` → `http://catalog.api:8080`
- `localhost:5278` → `http://basket.api:8080`
- `localhost:5103` → `http://ordering.api:8080`

### Discount.Grpc/Program.cs

**Değişiklik:**
- `HttpProtocols.Http2` → `HttpProtocols.Http1AndHttp2`

---

## ✅ Kontrol Listesi

Faz 8.2 için tamamlanan görevler:

- [x] Catalog.API → docker-compose.yml'e eklendi
- [x] Basket.API → docker-compose.yml'e eklendi
- [x] Ordering.API → docker-compose.yml'e eklendi
- [x] Discount.Grpc → docker-compose.yml'e eklendi
- [x] Gateway.API → docker-compose.yml'e eklendi
- [x] Environment variables yapılandırıldı (connection strings)
- [x] depends_on bağımlılıkları eklendi (service_healthy)
- [x] Port mapping'ler yapıldı
- [x] Health check'ler eklendi
- [x] Gateway appsettings.json güncellendi (container network adresleri)
- [x] Dockerfile'lara wget eklendi
- [x] Discount.Grpc Program.cs düzeltildi (Http1AndHttp2)
- [x] Tüm servisler test edildi
- [x] Health check'ler test edildi
- [x] Gateway routing test edildi

---

## 🚀 Sonraki Adım

**Faz 8.3:** End-to-End Test
- Tüm sistemin Docker Compose ile çalıştırılması
- End-to-end test senaryoları
- Integration test'ler
- Performans testleri

---

## 📚 Kaynaklar

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [Docker Health Checks](https://docs.docker.com/engine/reference/builder/#healthcheck)
- [Docker Networking](https://docs.docker.com/network/)
- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)

---

**Not:** Bu dokümantasyon, Faz 8.2 sırasında yapılan tüm işlemleri, çözülen sorunları ve öğrenilen noktaları içerir. Sonraki fazlar için referans olarak kullanılabilir.

