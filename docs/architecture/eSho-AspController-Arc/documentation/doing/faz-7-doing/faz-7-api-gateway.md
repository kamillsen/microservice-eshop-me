# Faz 7 - API Gateway (YARP)

## Servis Hakkında

**Ne İşe Yarar?**
- Tüm servislere **tek giriş noktası** sağlar
- Kullanıcılar farklı servislerin portlarını bilmek zorunda kalmaz
- Request routing (hangi istek hangi servise gidecek)
- Load balancing (ileride birden fazla instance olursa)
- CORS yönetimi
- Rate limiting (ileride)

**Örnek Kullanım Senaryosu:**
```
1. Kullanıcı: GET http://localhost:5000/catalog-service/api/products
   → Gateway (YARP): Route'u kontrol et → "/catalog-service/**" → Catalog.API'ye yönlendir
   → Path'i dönüştür → "/catalog-service" prefix'ini kaldır
   → Request'i Catalog.API'ye gönder → http://localhost:5001/api/products
   → Catalog.API: Response döner
   → Gateway: Response'u kullanıcıya iletir

2. Kullanıcı: GET http://localhost:5000/basket-service/api/baskets/user1
   → Gateway: Basket.API'ye yönlendir → http://localhost:5278/api/baskets/user1
   → Basket.API: Sepeti döner

3. Kullanıcı: GET http://localhost:5000/ordering-service/api/orders
   → Gateway: Ordering.API'ye yönlendir → http://localhost:5103/api/orders
   → Ordering.API: Siparişleri döner
```

**Neden şimdi?** 
- ✅ Catalog hazır (Port 5001)
- ✅ Basket hazır (Port 5278)
- ✅ Ordering hazır (Port 5103)
- ✅ Tüm servisler çalışıyor
- ✅ Artık Gateway ile birleştirilebilir

**Neden YARP?**
- Microsoft destekli, performanslı reverse proxy
- Kolay konfigürasyon (appsettings.json)
- Load balancing desteği
- Health check entegrasyonu
- .NET native (performanslı)

**Neden API Gateway?**
- Tek giriş noktası (kullanıcı tek URL bilir)
- Servisler internal port'ta çalışır (güvenlik)
- İleride authentication/authorization eklenebilir
- Rate limiting, logging merkezi yapılabilir
- CORS yönetimi merkezi

---

## 7.1 Gateway.API Projesi Oluştur

**Hedef:** YARP reverse proxy projesi

### Görevler:

#### ApiGateway klasör yapısını oluştur
**Ne işe yarar:** Gateway servisi için klasör oluşturur.

```bash
cd src
mkdir ApiGateway
cd ApiGateway
```

**Açıklama:**
- `src/ApiGateway/` klasörü oluşturulur
- Diğer servislerden farklı klasörde (Gateway özel bir servis)

#### Web API projesi oluştur
**Ne işe yarar:** Gateway Service için REST API projesi oluşturur.

```bash
cd src/ApiGateway
dotnet new webapi -n Gateway.API
```

**Açıklama:**
- `webapi` template'i ile proje oluşturulur
- Otomatik olarak `Program.cs`, `appsettings.json` oluşturulur
- Swagger konfigürasyonu hazır gelir (Gateway'de Swagger gerekmez ama template'te var)

#### Projeyi solution'a ekle
**Ne işe yarar:** Projeyi solution'a ekler.

```bash
cd ../..
dotnet sln add src/ApiGateway/Gateway.API/Gateway.API.csproj
```

#### NuGet paketlerini ekle
**Ne işe yarar:** YARP ve Health Checks paketlerini ekler.

```bash
cd src/ApiGateway/Gateway.API
dotnet add package Yarp.ReverseProxy
dotnet add package AspNetCore.HealthChecks.Uris
```

**Paketler:**
- `Yarp.ReverseProxy` → YARP reverse proxy için
- `AspNetCore.HealthChecks.Uris` → Downstream servislerin health check'i için

#### appsettings.json'a YARP konfigürasyonu ekle
**Ne işe yarar:** Route ve cluster konfigürasyonlarını ekler.

**appsettings.json:**

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
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**Açıklama:**
- `Routes` → Hangi path hangi cluster'a gidecek
- `Clusters` → Downstream servislerin adresleri
- `PathRemovePrefix` → Path'ten prefix'i kaldırır (örn: `/catalog-service/api/products` → `/api/products`)

**Route Yapısı:**
- `/catalog-service/**` → `http://localhost:5001` (Catalog.API)
- `/basket-service/**` → `http://localhost:5278` (Basket.API)
- `/ordering-service/**` → `http://localhost:5103` (Ordering.API)

#### Program.cs'de YARP konfigürasyonu
**Ne işe yarar:** YARP middleware'ini ekler.

**Program.cs:**

```csharp
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// YARP (Reverse Proxy)
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// YARP Middleware
app.MapReverseProxy();

app.Run();
```

**Açıklama:**
- `AddReverseProxy()` → YARP servislerini DI container'a ekler
- `LoadFromConfig()` → appsettings.json'dan konfigürasyonu yükler
- `MapReverseProxy()` → Reverse proxy middleware'ini ekler

### Kontrol:
- Proje build oluyor mu? (`dotnet build`)
- Solution'da görünüyor mu? (`dotnet sln list`)
- Tüm paketler eklendi mi? (`cat Gateway.API.csproj`)

---

## 7.2 YARP Routing Konfigürasyonu

**Hedef:** Servisleri route'la ve test et

### Görevler:

#### Route'ları test et
**Ne işe yarar:** Her route'un doğru çalıştığını doğrular.

**Test Senaryoları:**

1. **Catalog Route:**
   ```bash
   # Gateway üzerinden
   curl http://localhost:5000/catalog-service/api/products
   
   # Direkt Catalog.API
   curl http://localhost:5001/api/products
   
   # Sonuç: Aynı response dönmeli
   ```

2. **Basket Route:**
   ```bash
   # Gateway üzerinden
   curl http://localhost:5000/basket-service/api/baskets/user1
   
   # Direkt Basket.API
   curl http://localhost:5278/api/baskets/user1
   
   # Sonuç: Aynı response dönmeli
   ```

3. **Ordering Route:**
   ```bash
   # Gateway üzerinden
   curl http://localhost:5000/ordering-service/api/orders
   
   # Direkt Ordering.API
   curl http://localhost:5103/api/orders
   
   # Sonuç: Aynı response dönmeli
   ```

#### Path Transform kontrolü
**Ne işe yarar:** Path prefix'inin doğru kaldırıldığını doğrular.

**Test:**
- `GET http://localhost:5000/catalog-service/api/products`
  - Gateway: `/catalog-service/api/products` alır
  - Path transform: `/catalog-service` kaldırılır
  - Catalog.API'ye gönderilir: `/api/products`
  - Sonuç: Doğru endpoint'e yönlendirilir

### Test:
- Gateway çalışıyor mu? (http://localhost:5000)
- Route'lar çalışıyor mu?
  - `GET http://localhost:5000/catalog-service/api/products`
  - `GET http://localhost:5000/basket-service/api/baskets/user1`
  - `GET http://localhost:5000/ordering-service/api/orders`
- Path transform doğru mu? (Prefix kaldırılıyor mu?)

---

## 7.3 Gateway Health Checks

**Hedef:** Downstream servislerin sağlığını kontrol et

### Görevler:

#### Health Checks ekle
**Ne işe yarar:** Catalog, Basket, Ordering servislerinin sağlığını kontrol eder.

**Program.cs güncellemesi:**

```csharp
using AspNetCore.HealthChecks.Uris;

var builder = WebApplication.CreateBuilder(args);

// YARP (Reverse Proxy)
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Health Checks (Downstream Services)
builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri("http://localhost:5001/health"), name: "catalog-api")
    .AddUrlGroup(new Uri("http://localhost:5278/health"), name: "basket-api")
    .AddUrlGroup(new Uri("http://localhost:5103/health"), name: "ordering-api");

var app = builder.Build();

// Health Check Endpoint
app.MapHealthChecks("/health");

// YARP Middleware
app.MapReverseProxy();

app.Run();
```

**Açıklama:**
- `AddUrlGroup()` → Her downstream servis için health check ekler
- `/health` endpoint'i → Tüm servislerin sağlık durumunu döner

#### Health Check Response Formatı
**Ne işe yarar:** Health check response'unun formatını gösterir.

**Response Örneği:**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "catalog-api": {
      "status": "Healthy",
      "duration": "00:00:00.0123456",
      "data": {}
    },
    "basket-api": {
      "status": "Healthy",
      "duration": "00:00:00.0234567",
      "data": {}
    },
    "ordering-api": {
      "status": "Healthy",
      "duration": "00:00:00.0345678",
      "data": {}
    }
  }
}
```

### Test:
- Health check çalışıyor mu? (http://localhost:5000/health)
- Tüm servisler healthy mi?
- Bir servis down olursa health check ne gösterir?

---

## 7.4 Gateway Swagger Konfigürasyonu (Opsiyonel)

**Hedef:** Swagger'ı kaldır veya devre dışı bırak

### Görevler:

#### Swagger'ı kaldır (Opsiyonel)
**Ne işe yarar:** Gateway'de Swagger gerekmez (sadece routing yapar).

**Program.cs güncellemesi:**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Swagger'ı kaldır (Gateway'de gerekmez)
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger middleware'lerini kaldır
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

app.MapHealthChecks("/health");
app.MapReverseProxy();

app.Run();
```

**Açıklama:**
- Gateway sadece routing yapar, Swagger gerekmez
- İsterseniz bırakabilirsiniz (opsiyonel)

---

## 7.5 Gateway Properties/launchSettings.json

**Hedef:** Gateway port'unu yapılandır

### Görevler:

#### launchSettings.json güncelle
**Ne işe yarar:** Gateway'in port'unu ayarlar.

**Properties/launchSettings.json:**

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "https://localhost:7191;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

**Açıklama:**
- Port: `5000` (external - kullanıcılar buraya bağlanır)
- HTTPS: `7191` (opsiyonel)

---

## Özet: Faz 7 adımlar sırası

1. ApiGateway klasörünü oluştur
2. Gateway.API Web API projesi oluştur (`dotnet new webapi`)
3. Projeyi solution'a ekle
4. NuGet paketlerini ekle (Yarp.ReverseProxy, AspNetCore.HealthChecks.Uris)
5. appsettings.json'a YARP konfigürasyonu ekle (Routes, Clusters)
6. Program.cs'de YARP middleware ekle (`AddReverseProxy`, `MapReverseProxy`)
7. Health Checks ekle (Catalog, Basket, Ordering)
8. launchSettings.json'da port ayarla (5000)
9. Gateway'i test et (her route'u test et)
10. Health check'i test et

**Bu adımlar tamamlandıktan sonra Faz 8'e (Docker Entegrasyonu) geçilebilir.**

**Not:** Gateway, tüm servislerin çalışır durumda olması gerektiği için en son yapılır. Gateway olmadan da servislere direkt erişilebilir, ama Gateway ile tek noktadan erişim sağlanır.

