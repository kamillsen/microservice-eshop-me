# 🐳 Docker Port Mapping ve Container Network Açıklaması

> Bu doküman, Docker Compose'da port mapping (`5002:8080`) ve container network'ün nasıl çalıştığını, neden tüm servisler aynı port'u (8080) kullanabildiğini açıklar.

---

## 📋 İçindekiler

- [Port Mapping Nedir?](#-port-mapping-nedir)
- [Neden Tüm Servisler 8080 Kullanıyor?](#-neden-tüm-servisler-8080-kullanıyor)
- [Container Network vs Host Network](#-container-network-vs-host-network)
- [Pratik Örnekler](#-pratik-örnekler)
- [Özet](#-özet)

---

## 🔌 Port Mapping Nedir?

### Format: `HOST_PORT:CONTAINER_PORT`

```yaml
ports:
  - "5002:8080"
    │    │
    │    └── Container Port (container içindeki gerçek port)
    └── Host Port (localhost'tan erişim için)
```

**Anlamı:**
- Host'tan (senin bilgisayarından) `localhost:5002` ile erişirsin
- Docker bu isteği container içindeki `8080` portuna yönlendirir
- Container içinde servis `8080` portunda çalışır

### Docker Compose'daki Örnekler

```yaml
# Basket.API
basket.api:
  ports:
    - "5002:8080"  # Host:5002 → Container:8080

# Catalog.API
catalog.api:
  ports:
    - "5001:8080"  # Host:5001 → Container:8080

# Ordering.API
ordering.api:
  ports:
    - "5003:8080"  # Host:5003 → Container:8080

# Gateway.API
gateway.api:
  ports:
    - "5000:8080"  # Host:5000 → Container:8080
```

**Görüldüğü gibi:**
- ✅ Her servis container içinde `8080` portunda çalışıyor
- ✅ Ama host'tan farklı portlardan erişiliyor (5000, 5001, 5002, 5003)
- ✅ Bu sayede port çakışması olmuyor!

---

## 🤔 Neden Tüm Servisler 8080 Kullanıyor?

### Cevap: Container Network İzolasyonu

Her container kendi izole network ortamında çalışır. Container'lar birbirine **container adı** ile erişir, port numarası değil!

### Mimari Diyagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        HOST (Senin Bilgisayarın)                    │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  Port Mapping (Host → Container)                            │   │
│  │                                                              │   │
│  │  localhost:5000  ──────┐                                    │   │
│  │  localhost:5001  ──────┼──┐                                 │   │
│  │  localhost:5002  ──────┼──┼──┐                              │   │
│  │  localhost:5003  ──────┼──┼──┼──┐                           │   │
│  │                        │  │  │  │                           │   │
│  └────────────────────────┼──┼──┼──┼───────────────────────────┘   │
│                           │  │  │  │                              │
└───────────────────────────┼──┼──┼──┼──────────────────────────────┘
                            │  │  │  │
                            │  │  │  │ Port Mapping
                            │  │  │  │
┌───────────────────────────┼──┼──┼──┼──────────────────────────────┐
│      DOCKER NETWORK       │  │  │  │                              │
│   (Container Network)     │  │  │  │                              │
│                           ▼  ▼  ▼  ▼                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │  Gateway.API     │  │  Catalog.API    │  │  Basket.API    │ │
│  │                  │  │                  │  │                 │ │
│  │ Port: 8080      │  │ Port: 8080      │  │ Port: 8080      │ │
│  │ Container:      │  │ Container:      │  │ Container:      │ │
│  │ gateway.api     │  │ catalog.api     │  │ basket.api      │ │
│  │                 │  │                 │  │                 │ │
│  │                 │  │                 │  │                 │ │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘ │
│           │                    │                     │          │
│           │                    │                     │          │
│           │ Container Network (Birbirlerine         │          │
│           │  container adı ile erişirler)           │          │
│           │                                          │          │
│           │  gateway.api → catalog.api:8080         │          │
│           │  gateway.api → basket.api:8080          │          │
│           │  basket.api → discount.grpc:8080        │          │
│           │                                          │          │
│  ┌────────┴────────┐  ┌─────────────────┐         │          │
│  │  Ordering.API   │  │  Discount.Grpc   │         │          │
│  │                 │  │                 │         │          │
│  │ Port: 8080      │  │ Port: 8080       │         │          │
│  │ Container:      │  │ Container:       │         │          │
│  │ ordering.api    │  │ discount.grpc    │         │          │
│  └─────────────────┘  └─────────────────┘         │          │
│                                                      │          │
└──────────────────────────────────────────────────────┴──────────┘
```

### Neden Karışmıyor?

**1. Her Container Kendi İzole Ortamında Çalışır**

```
Container 1 (Gateway.API):
  - Kendi network namespace'i
  - Port 8080 → Sadece bu container'a özel
  - Container adı: gateway.api

Container 2 (Catalog.API):
  - Kendi network namespace'i
  - Port 8080 → Sadece bu container'a özel
  - Container adı: catalog.api

Container 3 (Basket.API):
  - Kendi network namespace'i
  - Port 8080 → Sadece bu container'a özel
  - Container adı: basket.api
```

**2. Container Network DNS Çözümlemesi**

Docker Compose, tüm container'ları aynı network'e koyar ve her container'a bir DNS adı verir:

```yaml
services:
  gateway.api:
    container_name: gateway.api  # ← DNS adı: gateway.api
    ports:
      - "5000:8080"
  
  catalog.api:
    container_name: catalog.api  # ← DNS adı: catalog.api
    ports:
      - "5001:8080"
  
  basket.api:
    container_name: basket.api  # ← DNS adı: basket.api
    ports:
      - "5002:8080"
```

**Container'lar birbirine şöyle erişir:**
- ✅ `http://catalog.api:8080` → Catalog container'ının 8080 portu
- ✅ `http://basket.api:8080` → Basket container'ının 8080 portu
- ✅ `http://gateway.api:8080` → Gateway container'ının 8080 portu

**Karışmaz çünkü:**
- Her container'ın kendi IP adresi var
- Container adı (DNS) farklı
- Network namespace izolasyonu var

---

## 🌐 Container Network vs Host Network

### İki Farklı Erişim Yolu

#### 1. Host'tan Erişim (Senin Bilgisayarından)

```
┌─────────────────────────────────────────┐
│  HOST (Senin Bilgisayarın)              │
│                                          │
│  Tarayıcı / Postman / curl              │
│                                          │
│  http://localhost:5000  ──────────┐  │
│  http://localhost:5001  ────────────┼─┐│
│  http://localhost:5002  ────────────┼─┼┐│
│  http://localhost:5003  ────────────┼─┼┼┐│
│                                      │ ││││
└──────────────────────────────────────┼─┼┼┼┘
                                       │ │││
                                       │ │││ Port Mapping
                                       │ │││
┌──────────────────────────────────────┼─┼┼┼┐
│  DOCKER NETWORK                      │ ││││
│                                      ▼ ▼▼▼│
│  ┌──────────┐  ┌──────────┐  ┌──────────┐│
│  │Gateway   │  │Catalog   │  │Basket    ││
│  │:8080     │  │:8080     │  │:8080     ││
│  └──────────┘  └──────────┘  └──────────┘│
└──────────────────────────────────────────┘
```

**Kullanım:**
- Tarayıcıdan: `http://localhost:5000`
- Postman'den: `http://localhost:5000`
- Terminal'den: `curl http://localhost:5000`

**Port Mapping:**
- `localhost:5000` → Gateway container'ının `8080` portuna
- `localhost:5001` → Catalog container'ının `8080` portuna
- `localhost:5002` → Basket container'ının `8080` portuna

#### 2. Container Network İçinden Erişim

```
┌─────────────────────────────────────────┐
│  DOCKER NETWORK (Container Network)     │
│                                          │
│  ┌──────────────┐                       │
│  │ Gateway.API   │                       │
│  │ Container    │                       │
│  │              │                       │
│  │ İstek atıyor:│                       │
│  │              │                       │
│  │ http://      │───────────────────┐   │
│  │ catalog.api: │                   │   │
│  │ 8080         │                   │   │
│  └──────────────┘                   │   │
│                                     │   │
│                                     ▼   │
│  ┌──────────────┐                   │   │
│  │ Catalog.API  │◄──────────────────┘   │
│  │ Container    │                       │
│  │ Port: 8080   │                       │
│  └──────────────┘                       │
│                                          │
│  Container adı (DNS): catalog.api       │
│  Container port: 8080                   │
└──────────────────────────────────────────┘
```

**Kullanım:**
- Gateway → Catalog: `http://catalog.api:8080`
- Gateway → Basket: `http://basket.api:8080`
- Basket → Discount: `http://discount.grpc:8080`

**Neden container adı?**
- Docker Compose, container adlarını DNS olarak kaydeder
- `catalog.api` → Container'ın IP adresine çözümlenir
- Port numarası container içindeki gerçek port (8080)

---

## 📊 Detaylı Mimari Diyagram

### Tam Akış: Host → Gateway → Catalog

```
┌──────────────────────────────────────────────────────────────────┐
│  ADIM 1: Host'tan İstek                                          │
│                                                                    │
│  Kullanıcı (Sen):                                                 │
│  curl http://localhost:5000/catalog-service/api/products          │
│                                                                    │
└────────────────────────────────┬─────────────────────────────────┘
                                 │
                                 │ Host Port: 5000
                                 │
                                 ▼
┌──────────────────────────────────────────────────────────────────┐
│  ADIM 2: Port Mapping                                            │
│                                                                    │
│  Docker Port Mapping:                                             │
│  localhost:5000  →  gateway.api container:8080                   │
│                                                                    │
└────────────────────────────────┬─────────────────────────────────┘
                                 │
                                 │ Container Network
                                 │
                                 ▼
┌──────────────────────────────────────────────────────────────────┐
│  ADIM 3: Gateway Container İçinde                                │
│                                                                    │
│  Gateway.API Container:                                          │
│  - Port: 8080 (container içinde)                                 │
│  - YARP middleware isteği yakalar                               │
│  - Route matching: "/catalog-service/**"                         │
│  - Transform: "/catalog-service" prefix'i kaldır                 │
│  - Yeni path: "/api/products"                                    │
│                                                                    │
│  Gateway şimdi Catalog'a istek atacak:                           │
│  http://catalog.api:8080/api/products                           │
│                                                                    │
└────────────────────────────────┬─────────────────────────────────┘
                                 │
                                 │ Container Network
                                 │ DNS: catalog.api → Container IP
                                 │
                                 ▼
┌──────────────────────────────────────────────────────────────────┐
│  ADIM 4: Catalog Container İçinde                                │
│                                                                    │
│  Catalog.API Container:                                           │
│  - Port: 8080 (container içinde)                                 │
│  - Request: GET /api/products                                    │
│  - Handler çalışır, veritabanından ürünleri getirir             │
│  - Response döner                                                │
│                                                                    │
└────────────────────────────────┬─────────────────────────────────┘
                                 │
                                 │ Response
                                 │
                                 ▼
┌──────────────────────────────────────────────────────────────────┐
│  ADIM 5: Gateway'e Geri Dönüş                                   │
│                                                                    │
│  Gateway.API Container:                                          │
│  - Catalog'dan response alır                                     │
│  - Response'u client'a iletir                                   │
│                                                                    │
└────────────────────────────────┬─────────────────────────────────┘
                                 │
                                 │ Host Port: 5000
                                 │
                                 ▼
┌──────────────────────────────────────────────────────────────────┐
│  ADIM 6: Host'a Response                                         │
│                                                                    │
│  Kullanıcı (Sen):                                                 │
│  Response alır: [{ "id": "...", "name": "..." }]                │
│                                                                    │
└──────────────────────────────────────────────────────────────────┘
```

### Port Kullanımı Özeti

| Servis | Host Port | Container Port | Container Adı | Container Network Adresi |
|--------|-----------|----------------|---------------|-------------------------|
| **Gateway** | 5000 | 8080 | `gateway.api` | `http://gateway.api:8080` |
| **Catalog** | 5001 | 8080 | `catalog.api` | `http://catalog.api:8080` |
| **Basket** | 5002 | 8080 | `basket.api` | `http://basket.api:8080` |
| **Ordering** | 5003 | 8080 | `ordering.api` | `http://ordering.api:8080` |
| **Discount** | 5004/5005 | 8080/8081 | `discount.grpc` | `http://discount.grpc:8080` |

---

## 💡 Pratik Örnekler

### Örnek 1: Host'tan Erişim

```bash
# Sen bilgisayarından (host'tan) istek atıyorsun
curl http://localhost:5000/catalog-service/api/products

# Docker bu isteği şöyle işler:
# 1. localhost:5000 → Port mapping → gateway.api:8080
# 2. Gateway container içinde YARP çalışır
# 3. YARP route matching yapar: "/catalog-service/**"
# 4. Transform: "/catalog-service" kaldır → "/api/products"
# 5. Gateway, catalog.api:8080'a istek atar (container network)
# 6. Catalog response döner
# 7. Gateway response'u sana iletir
```

### Örnek 2: Container Network İçinden Erişim

```json
// Gateway.API/appsettings.json
{
  "ReverseProxy": {
    "Clusters": {
      "catalog-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://catalog.api:8080"  // ← Container network adresi
          }
        }
      }
    }
  }
}
```

**Neden `catalog.api:8080`?**
- Gateway container içinde çalışıyor
- Container network kullanıyor
- `catalog.api` → Docker DNS tarafından çözümlenir
- `:8080` → Container içindeki gerçek port

**Neden `localhost:5001` değil?**
- `localhost` container içinde Gateway'in kendisini ifade eder
- `localhost:5001` container içinde çalışmaz (port mapping host'ta)
- Container network'te container adı kullanılmalı

### Örnek 3: Basket → Discount gRPC İletişimi

```csharp
// Basket.API/Program.cs
builder.Services.AddGrpcClient<DiscountProtoService.DiscountProtoServiceClient>(options =>
{
    options.Address = new Uri("http://discount.grpc:8080");  // ← Container network
});
```

**Neden `discount.grpc:8080`?**
- Basket container içinde çalışıyor
- Container network kullanıyor
- `discount.grpc` → Docker DNS tarafından çözümlenir
- `:8080` → Discount container'ının gRPC portu

---

## 🔍 Docker Network Detayları

### Network Oluşturma

Docker Compose çalıştığında otomatik olarak bir network oluşturur:

```bash
# Network'ü görmek için
docker network ls

# Çıktı:
# NETWORK ID     NAME                              DRIVER    SCOPE
# abc123def456   microservice-practice-me_default   bridge    local
```

### Container'ların Network'e Bağlanması

```bash
# Network detaylarını görmek için
docker network inspect microservice-practice-me_default

# Çıktı:
# {
#   "Containers": {
#     "gateway-api-container-id": {
#       "Name": "gateway.api",
#       "IPv4Address": "172.20.0.2/16"
#     },
#     "catalog-api-container-id": {
#       "Name": "catalog.api",
#       "IPv4Address": "172.20.0.3/16"
#     },
#     "basket-api-container-id": {
#       "Name": "basket.api",
#       "IPv4Address": "172.20.0.4/16"
#     }
#   }
# }
```

**Görüldüğü gibi:**
- Her container'ın kendi IP adresi var
- Container adı (Name) DNS olarak çalışır
- `catalog.api` → `172.20.0.3` IP'sine çözümlenir

### DNS Çözümlemesi

Container network içinde DNS çözümlemesi şöyle çalışır:

```
Gateway container içinden:
  catalog.api → 172.20.0.3 (Catalog container'ın IP'si)
  basket.api → 172.20.0.4 (Basket container'ın IP'si)
  ordering.api → 172.20.0.5 (Ordering container'ın IP'si)
```

---

## 📝 Özet

### Port Mapping (`5002:8080`)

- **Sol taraf (5002):** Host port → Senin bilgisayarından erişim için
- **Sağ taraf (8080):** Container port → Container içindeki gerçek port
- **Anlamı:** `localhost:5002` → Container içindeki `8080` portuna yönlendirilir

### Neden Tüm Servisler 8080 Kullanıyor?

1. **Her container kendi izole ortamında çalışır**
   - Network namespace izolasyonu
   - Her container'ın kendi IP adresi var

2. **Container network DNS çözümlemesi**
   - Container adı (örn: `catalog.api`) DNS olarak çalışır
   - `catalog.api:8080` → Catalog container'ının IP'sine çözümlenir
   - `basket.api:8080` → Basket container'ının IP'sine çözümlenir

3. **Port çakışması olmaz**
   - Host'ta farklı portlar kullanılır (5000, 5001, 5002, 5003)
   - Container network'te container adı farklı olduğu için karışmaz

### Erişim Yolları

| Erişim Yeri | Kullanılan Adres | Örnek |
|-------------|------------------|-------|
| **Host'tan** | `localhost:HOST_PORT` | `http://localhost:5002` |
| **Container Network** | `CONTAINER_NAME:CONTAINER_PORT` | `http://basket.api:8080` |
| **Container İçi** | `localhost:CONTAINER_PORT` | `http://localhost:8080` (health check) |

### Sonuç

- ✅ Tüm servisler container içinde `8080` portunda çalışabilir (izolasyon sayesinde)
- ✅ Host'tan farklı portlardan erişilir (5000, 5001, 5002, 5003)
- ✅ Container'lar birbirine container adı ile erişir (`catalog.api:8080`)
- ✅ Port çakışması olmaz çünkü her container'ın kendi network namespace'i var

---

**Son Güncelleme:** Aralık 2024

