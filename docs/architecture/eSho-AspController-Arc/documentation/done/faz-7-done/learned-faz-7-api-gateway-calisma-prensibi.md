# API Gateway Çalışma Prensibi ve Akış

> **Tarih:** Aralık 2024  
> **Faz:** Faz 7 - API Gateway (YARP)  
> **Amaç:** API Gateway'in nasıl çalıştığını, mevcut akışı ve YARP'ın çalışma mantığını anlamak

---

## 📋 İçindekiler

1. [API Gateway Nedir?](#api-gateway-nedir)
2. [YARP (Yet Another Reverse Proxy) Nedir?](#yarp-yet-another-reverse-proxy-nedir)
3. [Mevcut Sistem Mimarisi](#mevcut-sistem-mimarisi)
4. [Request Akışı (Detaylı)](#request-akışı-detaylı)
5. [YARP Routing Mekanizması](#yarp-routing-mekanizması)
6. [Path Transform (Prefix Kaldırma)](#path-transform-prefix-kaldırma)
7. [Health Check Mekanizması](#health-check-mekanizması)
8. [Örnek Senaryolar](#örnek-senaryolar)
9. [Avantajlar ve Dezavantajlar](#avantajlar-ve-dezavantajlar)

---

## 🚪 API Gateway Nedir?

### Tanım

**API Gateway**, tüm microservice'lere **tek giriş noktası** sağlayan bir reverse proxy sunucusudur. İstemciler (frontend, mobil uygulama, vb.) farklı servislerin portlarını bilmek zorunda kalmaz, sadece Gateway'e bağlanır.

### Temel İşlevler

1. **Request Routing** → Hangi istek hangi servise gidecek?
2. **Load Balancing** → Birden fazla servis instance'ı varsa yükü dağıtır
3. **Health Checking** → Servislerin sağlık durumunu kontrol eder
4. **Path Transformation** → URL'leri dönüştürür (prefix kaldırma, vb.)
5. **Authentication/Authorization** → Merkezi güvenlik kontrolü (ileride)
6. **Rate Limiting** → İstek sınırlaması (ileride)
7. **Logging** → Merkezi loglama (ileride)

### Neden Gerekli?

**Sorun (Gateway Olmadan):**
```
Frontend → Catalog.API (localhost:5001)
Frontend → Basket.API (localhost:5278)
Frontend → Ordering.API (localhost:5103)
```

**Problemler:**
- ❌ Frontend 3 farklı port bilmek zorunda
- ❌ CORS sorunları (her servis için ayrı CORS ayarı)
- ❌ Güvenlik (servisler direkt erişilebilir)
- ❌ Load balancing yok
- ❌ Health check yok

**Çözüm (Gateway ile):**
```
Frontend → Gateway (localhost:5000) → Catalog.API
Frontend → Gateway (localhost:5000) → Basket.API
Frontend → Gateway (localhost:5000) → Ordering.API
```

**Avantajlar:**
- ✅ Frontend tek port bilir (5000)
- ✅ Merkezi CORS kontrolü
- ✅ Güvenlik (servisler internal port'ta çalışır)
- ✅ Load balancing hazır
- ✅ Health check merkezi

---

## 🔄 YARP (Yet Another Reverse Proxy) Nedir?

### Tanım

**YARP**, Microsoft'un geliştirdiği **yüksek performanslı reverse proxy** kütüphanesidir. .NET native olarak yazılmıştır ve async/await pattern'i kullanır.

### Özellikler

| Özellik | Açıklama | Örnek |
|---------|----------|-------|
| **Reverse Proxy** | İstemci ile backend arasında aracı | Gateway → Catalog.API |
| **Request Routing** | URL pattern'lerine göre yönlendirme | `/catalog-service/**` → Catalog.API |
| **Path Transform** | URL dönüşümü | `/catalog-service/api/products` → `/api/products` |
| **Load Balancing** | Yük dağıtımı | Birden fazla Catalog.API instance'ı |
| **Health Check** | Servis sağlık kontrolü | Downstream servislerin durumu |
| **Configuration** | JSON dosyasından konfigürasyon | `appsettings.json` |

### Neden YARP?

- ✅ **Microsoft destekli** → Güvenilir, aktif geliştirme
- ✅ **Yüksek performans** → Native .NET, async/await
- ✅ **Kolay konfigürasyon** → JSON dosyası
- ✅ **Load balancing hazır** → Birden fazla destination
- ✅ **Health check entegrasyonu** → Sağlıklı servislere yönlendirme
- ✅ **Request/Response transform** → URL ve header dönüşümü

---

## 🏗️ Mevcut Sistem Mimarisi

### Servisler ve Portlar

```
┌─────────────────────────────────────────────────────────────┐
│                    CLIENT (Frontend/Mobile)                  │
│                    localhost:5000 (Gateway)                  │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                    API GATEWAY (YARP)                        │
│                    Gateway.API                              │
│                    Port: 5000                                │
│                                                              │
│  Routes:                                                     │
│  - /catalog-service/** → Catalog.API                        │
│  - /basket-service/** → Basket.API                          │
│  - /ordering-service/** → Ordering.API                      │
└──────┬──────────────┬──────────────┬────────────────────────┘
       │              │              │
       ▼              ▼              ▼
┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│ Catalog.API │ │ Basket.API  │ │Ordering.API │
│ Port: 5001  │ │ Port: 5278   │ │ Port: 5103  │
└─────────────┘ └─────────────┘ └─────────────┘
```

### Konfigürasyon (appsettings.json)

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
            "Address": "http://localhost:5001"
          }
        }
      },
      "basket-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5278"
          }
        }
      },
      "ordering-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5103"
          }
        }
      }
    }
  }
}
```

### Program.cs Konfigürasyonu

```csharp
using Yarp.ReverseProxy.Configuration;
using AspNetCore.HealthChecks.Uris;

var builder = WebApplication.CreateBuilder(args);

// 1. YARP (Reverse Proxy) - Routing ve yönlendirme
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// 2. Health Checks - Downstream servislerin sağlık kontrolü
builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri("http://localhost:5001/health"), name: "catalog-api")
    .AddUrlGroup(new Uri("http://localhost:5278/health"), name: "basket-api")
    .AddUrlGroup(new Uri("http://localhost:5103/health"), name: "ordering-api");

var app = builder.Build();

// 3. Health Check Endpoint - Gateway'in kendi health check'i
app.MapHealthChecks("/health");

// 4. YARP Middleware - Reverse proxy işlemlerini başlatır
app.MapReverseProxy();

app.Run();
```

---

## 🔄 Request Akışı (Detaylı)

### Senaryo: Kullanıcı Ürün Listesini İstiyor

```
1. İstemci (Frontend)
   ↓
   GET http://localhost:5000/catalog-service/api/products
   ↓
2. Gateway (YARP) - Request Yakalama
   ↓
   MapReverseProxy() middleware request'i yakalar
   ↓
3. Route Matching (Yönlendirme Eşleştirme)
   ↓
   YARP, gelen path'i route pattern'leriyle karşılaştırır:
   - "/catalog-service/{**catch-all}" → ✅ EŞLEŞTİ!
   - "/basket-service/{**catch-all}" → ❌ Eşleşmedi
   - "/ordering-service/{**catch-all}" → ❌ Eşleşmedi
   ↓
4. Cluster Belirleme
   ↓
   Route'un ClusterId'si: "catalog-cluster"
   Cluster'dan destination adresi alınır: "http://localhost:5001"
   ↓
5. Path Transform (URL Dönüşümü)
   ↓
   PathRemovePrefix: "/catalog-service" uygulanır
   "/catalog-service/api/products" → "/api/products"
   ↓
6. Request Gönderimi
   ↓
   GET http://localhost:5001/api/products
   (HTTP headers, body, vb. korunur)
   ↓
7. Catalog.API İşleme
   ↓
   Catalog.API request'i alır, işler, response döner
   ↓
8. Response Dönüşü
   ↓
   Catalog.API → Gateway → İstemci
   Response (JSON, status code, headers) korunur
   ↓
9. İstemci Response Alır
   ↓
   Frontend, ürün listesini alır
```

### Adım Adım Detaylı Açıklama

#### Adım 1: İstemci Request Gönderir

**İstemci (Frontend):**
```javascript
fetch('http://localhost:5000/catalog-service/api/products')
  .then(response => response.json())
  .then(data => console.log(data));
```

**HTTP Request:**
```
GET /catalog-service/api/products HTTP/1.1
Host: localhost:5000
User-Agent: Mozilla/5.0...
Accept: application/json
```

#### Adım 2: Gateway Request'i Yakalar

**Program.cs:**
```csharp
app.MapReverseProxy(); // Tüm request'leri yakalar
```

**YARP Middleware:**
- HTTP request pipeline'ında çalışır
- Tüm gelen request'leri yakalar
- Route matching yapar
- Path transform uygular
- Downstream servise yönlendirir

#### Adım 3: Route Matching (Yönlendirme Eşleştirme)

**YARP Route Matching Algoritması:**

1. **Path Pattern Kontrolü:**
   ```
   Gelen Path: "/catalog-service/api/products"
   
   Route Pattern'leri:
   - "/catalog-service/{**catch-all}" → ✅ EŞLEŞTİ!
     {**catch-all} → "/api/products" kısmını yakalar
   - "/basket-service/{**catch-all}" → ❌ Eşleşmedi
   - "/ordering-service/{**catch-all}" → ❌ Eşleşmedi
   ```

2. **Route Bulundu:**
   ```json
   {
     "catalog-route": {
       "ClusterId": "catalog-cluster",
       "Match": {
         "Path": "/catalog-service/{**catch-all}"
       },
       "Transforms": [
         { "PathRemovePrefix": "/catalog-service" }
       ]
     }
   }
   ```

3. **ClusterId Alındı:**
   - `ClusterId: "catalog-cluster"`

#### Adım 4: Cluster'dan Destination Adresi Alınır

**Cluster Konfigürasyonu:**
```json
{
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
```

**YARP İşlemi:**
- `catalog-cluster` bulundu
- `destination1` seçildi (şu an tek destination var, ileride load balancing için birden fazla olabilir)
- `Address: "http://localhost:5001"` alındı

#### Adım 5: Path Transform (URL Dönüşümü)

**Transform Uygulanır:**
```
Önce: "/catalog-service/api/products"
      ↓ (PathRemovePrefix: "/catalog-service")
Sonra: "/api/products"
```

**Neden Gerekli?**
- Gateway URL'inde servis adı var: `/catalog-service/api/products`
- Catalog.API'de prefix yok: `/api/products`
- Transform ile uyumluluk sağlanır

**Transform Türleri:**
- `PathRemovePrefix` → Prefix kaldırma
- `PathSet` → Path'i değiştirme
- `PathPrefix` → Path'e prefix ekleme
- `RequestHeader` → Header ekleme/değiştirme

#### Adım 6: Request Gönderimi

**YARP İşlemi:**
1. HTTP client oluşturulur (HttpClientFactory kullanılır)
2. Request hazırlanır:
   - Method: GET (korunur)
   - Path: `/api/products` (transform edilmiş)
   - Headers: Korunur (User-Agent, Accept, vb.)
   - Body: Varsa korunur
3. Request gönderilir: `GET http://localhost:5001/api/products`

**HTTP Request (Catalog.API'ye giden):**
```
GET /api/products HTTP/1.1
Host: localhost:5001
User-Agent: Mozilla/5.0...
Accept: application/json
```

#### Adım 7: Catalog.API İşleme

**Catalog.API:**
- Request'i alır
- Controller'a yönlendirir (`ProductsController`)
- Handler çalıştırılır (CQRS pattern)
- Veritabanından ürünler çekilir
- Response oluşturulur

**Response:**
```json
[
  {
    "id": "a3e70032-d428-4a7b-87d2-b2c0a935de98",
    "name": "Spor Ayakkabı",
    "description": "Rahat koşu ayakkabısı",
    "price": 1200.00
  },
  ...
]
```

#### Adım 8: Response Dönüşü

**Catalog.API → Gateway:**
- Response alınır (JSON, status code, headers)
- Gateway'e iletilir

**Gateway → İstemci:**
- Response korunur (değiştirilmez)
- İstemciye iletilir

**HTTP Response:**
```
HTTP/1.1 200 OK
Content-Type: application/json
Content-Length: 1234

[{...}, {...}, ...]
```

#### Adım 9: İstemci Response Alır

**Frontend:**
```javascript
// Response alındı
[
  {
    "id": "a3e70032-d428-4a7b-87d2-b2c0a935de98",
    "name": "Spor Ayakkabı",
    ...
  },
  ...
]
```

---

## 🎯 YARP Routing Mekanizması

### Route Matching Algoritması

**YARP, route'ları şu sırayla kontrol eder:**

1. **Path Pattern Eşleştirme:**
   - Gelen path, route pattern'leriyle karşılaştırılır
   - İlk eşleşen route kullanılır
   - `{**catch-all}` → Her şeyi yakalar (wildcard)

2. **Route Seçimi:**
   - Eşleşen route'un `ClusterId`'si alınır
   - Cluster'dan destination adresi alınır

3. **Transform Uygulama:**
   - Route'daki transform'lar uygulanır
   - Path, headers, vb. dönüştürülür

### Route Pattern Örnekleri

| Pattern | Açıklama | Eşleşen URL'ler | Eşleşmeyen URL'ler |
|---------|----------|----------------|-------------------|
| `/catalog-service/{**catch-all}` | Catalog servisi için tüm path'ler | `/catalog-service/api/products`<br>`/catalog-service/api/products/123`<br>`/catalog-service/health` | `/basket-service/api/baskets`<br>`/ordering-service/api/orders` |
| `/basket-service/{**catch-all}` | Basket servisi için tüm path'ler | `/basket-service/api/baskets`<br>`/basket-service/api/baskets/user1` | `/catalog-service/api/products` |
| `/ordering-service/{**catch-all}` | Ordering servisi için tüm path'ler | `/ordering-service/api/orders`<br>`/ordering-service/api/orders/123` | `/catalog-service/api/products` |

### Cluster ve Destination

**Cluster:** Bir grup servis instance'ı (load balancing için)

**Destination:** Gerçek servis adresi

**Şu Anki Durum:**
- Her cluster'da 1 destination var
- İleride birden fazla destination eklenebilir (load balancing)

**Örnek (İleride):**
```json
{
  "catalog-cluster": {
    "Destinations": {
      "destination1": {
        "Address": "http://catalog-api-1:8080"
      },
      "destination2": {
        "Address": "http://catalog-api-2:8080"
      }
    },
    "LoadBalancingPolicy": "RoundRobin"
  }
}
```

---

## 🔀 Path Transform (Prefix Kaldırma)

### Sorun

**Gateway URL:**
```
http://localhost:5000/catalog-service/api/products
```

**Catalog.API Endpoint:**
```
http://localhost:5001/api/products
```

**Problem:**
- Gateway URL'inde `/catalog-service` prefix'i var
- Catalog.API'de prefix yok
- Direkt yönlendirme yapılırsa 404 hatası alınır

### Çözüm: PathRemovePrefix Transform

**Konfigürasyon:**
```json
{
  "Routes": {
    "catalog-route": {
      "Transforms": [
        { "PathRemovePrefix": "/catalog-service" }
      ]
    }
  }
}
```

**Transform Uygulanması:**
```
1. Gelen Path: "/catalog-service/api/products"
   ↓
2. PathRemovePrefix: "/catalog-service" uygulanır
   ↓
3. Yeni Path: "/api/products"
   ↓
4. Catalog.API'ye gönderilir: "http://localhost:5001/api/products"
```

### Transform Türleri

| Transform | Açıklama | Örnek |
|-----------|----------|-------|
| `PathRemovePrefix` | Path'ten prefix kaldırır | `/catalog-service/api/products` → `/api/products` |
| `PathSet` | Path'i tamamen değiştirir | `/old-path` → `/new-path` |
| `PathPrefix` | Path'e prefix ekler | `/api/products` → `/v1/api/products` |
| `RequestHeader` | Request header'ı ekler/değiştirir | `X-Forwarded-For: 192.168.1.1` |
| `ResponseHeader` | Response header'ı ekler/değiştirir | `X-Response-Time: 123ms` |

---

## 🏥 Health Check Mekanizması

### Nasıl Çalışır?

**Gateway Health Check:**
```csharp
builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri("http://localhost:5001/health"), name: "catalog-api")
    .AddUrlGroup(new Uri("http://localhost:5278/health"), name: "basket-api")
    .AddUrlGroup(new Uri("http://localhost:5103/health"), name: "ordering-api");

app.MapHealthChecks("/health");
```

### Health Check Akışı

```
1. İstemci: GET http://localhost:5000/health
   ↓
2. Gateway: Health check endpoint'i çağrılır
   ↓
3. Her Downstream Servise İstek Gönderilir:
   - GET http://localhost:5001/health (Catalog.API)
   - GET http://localhost:5278/health (Basket.API)
   - GET http://localhost:5103/health (Ordering.API)
   ↓
4. Response'lar Alınır:
   - Catalog.API → "Healthy" (200 OK) ✅
   - Basket.API → "Healthy" (200 OK) ✅
   - Ordering.API → "Healthy" (200 OK) ✅
   ↓
5. Sonuçlar Birleştirilir:
   - Tüm servisler healthy → Gateway "Healthy" döner
   - Bir servis unhealthy → Gateway "Unhealthy" döner
   ↓
6. İstemci Response Alır:
   - "Healthy" veya "Unhealthy"
```

### Health Check Response

**Tüm Servisler Healthy:**
```
Healthy
HTTP Status: 200 OK
```

**Bir Servis Unhealthy:**
```
Unhealthy
HTTP Status: 503 Service Unavailable
```

### Health Check Kullanım Senaryoları

1. **Docker Health Check:**
   ```yaml
   healthcheck:
     test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
     interval: 10s
     timeout: 5s
     retries: 3
   ```

2. **Kubernetes Liveness/Readiness Probe:**
   ```yaml
   livenessProbe:
     httpGet:
       path: /health
       port: 5000
     initialDelaySeconds: 30
     periodSeconds: 10
   ```

3. **Monitoring Tools:**
   - Prometheus, Grafana gibi monitoring araçları
   - Health check endpoint'ini düzenli olarak kontrol eder
   - Servis down olduğunda alert gönderir

---

## 📝 Örnek Senaryolar

### Senaryo 1: Ürün Listesi Getirme

**İstemci:**
```javascript
fetch('http://localhost:5000/catalog-service/api/products')
```

**Akış:**
```
1. GET http://localhost:5000/catalog-service/api/products
   ↓
2. Gateway: Route matching → "catalog-route" eşleşti
   ↓
3. Path Transform: "/catalog-service" prefix'i kaldırıldı
   "/catalog-service/api/products" → "/api/products"
   ↓
4. Cluster: "catalog-cluster" → "http://localhost:5001"
   ↓
5. Request: GET http://localhost:5001/api/products
   ↓
6. Catalog.API: Ürün listesini döner
   ↓
7. Gateway: Response'u istemciye iletir
```

**Sonuç:** ✅ Başarılı

---

### Senaryo 2: Sepet Getirme

**İstemci:**
```javascript
fetch('http://localhost:5000/basket-service/api/baskets/user1')
```

**Akış:**
```
1. GET http://localhost:5000/basket-service/api/baskets/user1
   ↓
2. Gateway: Route matching → "basket-route" eşleşti
   ↓
3. Path Transform: "/basket-service" prefix'i kaldırıldı
   "/basket-service/api/baskets/user1" → "/api/baskets/user1"
   ↓
4. Cluster: "basket-cluster" → "http://localhost:5278"
   ↓
5. Request: GET http://localhost:5278/api/baskets/user1
   ↓
6. Basket.API: Sepet bilgisini döner
   ↓
7. Gateway: Response'u istemciye iletir
```

**Sonuç:** ✅ Başarılı

---

### Senaryo 3: Sipariş Listesi Getirme

**İstemci:**
```javascript
fetch('http://localhost:5000/ordering-service/api/orders')
```

**Akış:**
```
1. GET http://localhost:5000/ordering-service/api/orders
   ↓
2. Gateway: Route matching → "ordering-route" eşleşti
   ↓
3. Path Transform: "/ordering-service" prefix'i kaldırıldı
   "/ordering-service/api/orders" → "/api/orders"
   ↓
4. Cluster: "ordering-cluster" → "http://localhost:5103"
   ↓
5. Request: GET http://localhost:5103/api/orders
   ↓
6. Ordering.API: Sipariş listesini döner
   ↓
7. Gateway: Response'u istemciye iletir
```

**Sonuç:** ✅ Başarılı

---

### Senaryo 4: Health Check Kontrolü

**İstemci:**
```bash
curl http://localhost:5000/health
```

**Akış:**
```
1. GET http://localhost:5000/health
   ↓
2. Gateway: Health check endpoint'i çağrılır
   ↓
3. Her Downstream Servise İstek:
   - GET http://localhost:5001/health (Catalog.API) → "Healthy" ✅
   - GET http://localhost:5278/health (Basket.API) → "Healthy" ✅
   - GET http://localhost:5103/health (Ordering.API) → "Healthy" ✅
   ↓
4. Sonuç: Tüm servisler healthy → Gateway "Healthy" döner
```

**Sonuç:** ✅ Başarılı

---

### Senaryo 5: Servis Down Durumu

**Durum:** Ordering.API çalışmıyor

**İstemci:**
```bash
curl http://localhost:5000/health
```

**Akış:**
```
1. GET http://localhost:5000/health
   ↓
2. Gateway: Health check endpoint'i çağrılır
   ↓
3. Her Downstream Servise İstek:
   - GET http://localhost:5001/health (Catalog.API) → "Healthy" ✅
   - GET http://localhost:5278/health (Basket.API) → "Healthy" ✅
   - GET http://localhost:5103/health (Ordering.API) → Timeout/Error ❌
   ↓
4. Sonuç: Bir servis unhealthy → Gateway "Unhealthy" döner
```

**Response:**
```
Unhealthy
HTTP Status: 503 Service Unavailable
```

**Sonuç:** ⚠️ Unhealthy (Ordering.API down)

---

## ✅ Avantajlar ve Dezavantajlar

### Avantajlar

| Avantaj | Açıklama |
|---------|----------|
| **Tek Giriş Noktası** | Frontend tek port bilir (5000) |
| **Merkezi Yönetim** | Routing, health check, logging merkezi |
| **Güvenlik** | Servisler internal port'ta çalışır |
| **Load Balancing** | Birden fazla servis instance'ı destekler |
| **Health Check** | Servis durumunu merkezi kontrol eder |
| **Path Transform** | URL'leri dönüştürebilir |
| **Kolay Konfigürasyon** | JSON dosyasından yönetilir |

### Dezavantajlar

| Dezavantaj | Açıklama |
|------------|----------|
| **Single Point of Failure** | Gateway down olursa tüm sistem down |
| **Ekstra Latency** | Her request Gateway'den geçer (küçük gecikme) |
| **Komplekslik** | Ek bir servis yönetmek gerekir |
| **Bottleneck** | Yüksek trafikte Gateway bottleneck olabilir |

### Çözümler

**Single Point of Failure:**
- Gateway'i birden fazla instance'da çalıştır
- Load balancer (nginx, HAProxy) kullan

**Latency:**
- Gateway native .NET (yüksek performans)
- Async/await pattern (non-blocking)
- Gecikme genellikle < 1ms

**Komplekslik:**
- YARP kolay konfigürasyon (JSON)
- Microsoft destekli (güvenilir)

---

## 🎓 Öğrenilenler

### 1. Reverse Proxy Pattern

**Reverse Proxy:** İstemci ile backend servisler arasında aracı görevi gören sunucu.

**Forward Proxy vs Reverse Proxy:**
- **Forward Proxy:** İstemci tarafında (VPN, corporate proxy)
- **Reverse Proxy:** Sunucu tarafında (API Gateway, load balancer)

### 2. YARP Routing Mekanizması

**Akış:**
1. Request gelir
2. Route matching (pattern eşleştirme)
3. Cluster belirleme
4. Path transform
5. Request gönderimi
6. Response dönüşü

### 3. Path Transform Kullanımı

**Neden Gerekli?**
- Gateway URL'inde servis adı var (organizasyon)
- Backend servislerde prefix yok (temiz API)
- Transform ile uyumluluk sağlanır

### 4. Health Check Stratejisi

**Gateway Health Check:**
- Downstream servislerin durumunu kontrol eder
- Bir servis down olduğunda tespit eder
- Monitoring ve alerting için kullanılır

**Downstream Servis Health Check:**
- Kendi bağımlılıklarını kontrol eder (DB, Redis, RabbitMQ)
- Gateway'e durumunu bildirir

---

## 🔗 İlgili Dosyalar

- `src/ApiGateway/Gateway.API/Program.cs` → YARP konfigürasyonu
- `src/ApiGateway/Gateway.API/appsettings.json` → Routes ve Clusters
- `docs/architecture/eSho-AspController-Arc/documentation/done/faz-7-done/faz-7-1-gateway-api-projesi-olustur-note.md` → Detaylı dokümantasyon

---

## 📚 Kaynaklar

- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Reverse Proxy Pattern](https://en.wikipedia.org/wiki/Reverse_proxy)

---

**Son Güncelleme:** Aralık 2024

