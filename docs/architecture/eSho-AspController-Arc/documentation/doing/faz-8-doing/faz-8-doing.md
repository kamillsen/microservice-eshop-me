# Faz 8 - Docker Entegrasyonu

## Faz Hakkında

**Ne İşe Yarar?**
- Tüm servisleri Docker container'larında çalıştırma
- Tüm sistemi tek komutla başlatma/durdurma (`docker-compose up -d`)
- Production-like ortamda test etme
- Servislerin izolasyonu (her servis kendi container'ında)
- Bağımlılık yönetimi (servisler doğru sırayla başlatılır)

**Örnek Kullanım Senaryosu:**
```
1. Developer: Projeyi clone eder
   → git clone https://github.com/xxx/microservice-practice-me.git

2. Developer: Tek komutla tüm sistemi başlatır
   → docker-compose up -d
   → Tüm veritabanları, Redis, RabbitMQ, tüm servisler başlatılır

3. Developer: Sistemin çalıştığını kontrol eder
   → docker-compose ps
   → Tüm container'lar healthy mi?
   → http://localhost:5000 (Gateway) üzerinden test eder

4. Developer: Sistemi durdurur
   → docker-compose down
   → Tüm container'lar durdurulur
```

**Neden şimdi?** 
- ✅ Tüm servisler hazır (Catalog, Basket, Ordering, Discount, Gateway)
- ✅ Infrastructure hazır (PostgreSQL, Redis, RabbitMQ - Faz 1'de hazırlandı)
- ✅ Artık servisleri containerize edebiliriz
- ✅ End-to-end test için Docker gerekli

**Docker Avantajları:**
- Tek komutla tüm sistem (`docker-compose up -d`)
- Ortam tutarlılığı (development, staging, production aynı)
- Kolay deployment (CI/CD pipeline'a entegre edilebilir)
- İzolasyon (her servis kendi container'ında)
- Ölçeklenebilirlik (her servis ayrı ayrı scale edilebilir)

**Multi-Stage Build Stratejisi:**
- **Build Stage:** SDK ile derleme (büyük image)
- **Final Stage:** Sadece runtime (küçük image, sadece gerekli dosyalar)
- Avantaj: Production image'ları küçük ve güvenli

---

## 8.1 Dockerfile'lar Oluştur

**Hedef:** Her servis için multi-stage Dockerfile oluştur

### Görevler:

#### Catalog.API Dockerfile Oluştur
**Ne işe yarar:** Catalog.API servisini containerize eder.

**src/Services/Catalog/Catalog.API/Dockerfile:**

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file (shared projelere erişim için)
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["global.json", "./"]

# Copy project files
COPY ["src/Services/Catalog/Catalog.API/Catalog.API.csproj", "src/Services/Catalog/Catalog.API/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj", "src/BuildingBlocks/BuildingBlocks.Exceptions/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Behaviors/BuildingBlocks.Behaviors.csproj", "src/BuildingBlocks/BuildingBlocks.Behaviors/"]

# Restore dependencies
RUN dotnet restore "src/Services/Catalog/Catalog.API/Catalog.API.csproj"

# Copy all source files
COPY . .

# Build and publish
WORKDIR "/src/src/Services/Catalog/Catalog.API"
RUN dotnet build "Catalog.API.csproj" -c Release -o /app/build
RUN dotnet publish "Catalog.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set environment variable
ENV ASPNETCORE_URLS=http://+:8080

# Entry point
ENTRYPOINT ["dotnet", "Catalog.API.dll"]
```

**Açıklama:**
- **Build Stage:** SDK ile projeyi derler ve publish eder
- **Final Stage:** Sadece runtime ve publish edilmiş dosyaları içerir (küçük image)
- **Build Context:** Solution root (shared projelere erişim için)
- **Port:** 8080 (internal container port)
- **ENTRYPOINT:** Uygulamayı başlatır

**Build Komutu:**
```bash
cd /home/kSEN/Desktop/\ Projects/microservice-practice-me
docker build -f src/Services/Catalog/Catalog.API/Dockerfile -t catalogapi .
```

#### Basket.API Dockerfile Oluştur
**Ne işe yarar:** Basket.API servisini containerize eder.

**src/Services/Basket/Basket.API/Dockerfile:**

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["global.json", "./"]

# Copy project files
COPY ["src/Services/Basket/Basket.API/Basket.API.csproj", "src/Services/Basket/Basket.API/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj", "src/BuildingBlocks/BuildingBlocks.Exceptions/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Behaviors/BuildingBlocks.Behaviors.csproj", "src/BuildingBlocks/BuildingBlocks.Behaviors/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj", "src/BuildingBlocks/BuildingBlocks.Messaging/"]

# Restore dependencies
RUN dotnet restore "src/Services/Basket/Basket.API/Basket.API.csproj"

# Copy all source files
COPY . .

# Build and publish
WORKDIR "/src/src/Services/Basket/Basket.API"
RUN dotnet build "Basket.API.csproj" -c Release -o /app/build
RUN dotnet publish "Basket.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set environment variable
ENV ASPNETCORE_URLS=http://+:8080

# Entry point
ENTRYPOINT ["dotnet", "Basket.API.dll"]
```

**Açıklama:**
- Basket.API için BuildingBlocks.Messaging de gerekli (RabbitMQ + MassTransit)
- Diğer adımlar Catalog.API ile aynı

**Build Komutu:**
```bash
docker build -f src/Services/Basket/Basket.API/Dockerfile -t basketapi .
```

#### Ordering.API Dockerfile Oluştur
**Ne işe yarar:** Ordering.API servisini containerize eder.

**src/Services/Ordering/Ordering.API/Dockerfile:**

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["global.json", "./"]

# Copy project files
COPY ["src/Services/Ordering/Ordering.API/Ordering.API.csproj", "src/Services/Ordering/Ordering.API/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj", "src/BuildingBlocks/BuildingBlocks.Exceptions/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Behaviors/BuildingBlocks.Behaviors.csproj", "src/BuildingBlocks/BuildingBlocks.Behaviors/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj", "src/BuildingBlocks/BuildingBlocks.Messaging/"]

# Restore dependencies
RUN dotnet restore "src/Services/Ordering/Ordering.API/Ordering.API.csproj"

# Copy all source files
COPY . .

# Build and publish
WORKDIR "/src/src/Services/Ordering/Ordering.API"
RUN dotnet build "Ordering.API.csproj" -c Release -o /app/build
RUN dotnet publish "Ordering.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set environment variable
ENV ASPNETCORE_URLS=http://+:8080

# Entry point
ENTRYPOINT ["dotnet", "Ordering.API.dll"]
```

**Build Komutu:**
```bash
docker build -f src/Services/Ordering/Ordering.API/Dockerfile -t orderingapi .
```

#### Discount.Grpc Dockerfile Oluştur
**Ne işe yarar:** Discount.Grpc servisini containerize eder.

**src/Services/Discount/Discount.Grpc/Dockerfile:**

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["global.json", "./"]

# Copy project files
COPY ["src/Services/Discount/Discount.Grpc/Discount.Grpc.csproj", "src/Services/Discount/Discount.Grpc/"]

# Restore dependencies
RUN dotnet restore "src/Services/Discount/Discount.Grpc/Discount.Grpc.csproj"

# Copy all source files
COPY . .

# Build and publish
WORKDIR "/src/src/Services/Discount/Discount.Grpc"
RUN dotnet build "Discount.Grpc.csproj" -c Release -o /app/build
RUN dotnet publish "Discount.Grpc.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Container portları
# 8080: gRPC (HTTP/2 only)
# 8081: Health check (HTTP/1.1 only)
EXPOSE 8080 8081

# Set environment variable
ENV ASPNETCORE_URLS=http://+:8080

# Entry point
ENTRYPOINT ["dotnet", "Discount.Grpc.dll"]
```

**Açıklama:**
- Discount.Grpc için BuildingBlocks gerekmez (bağımsız gRPC servisi)
- **HTTP/2 Cleartext (h2c) Desteği:** Port 8080 sadece HTTP/2 (gRPC için), Port 8081 sadece HTTP/1.1 (health check için)
- **Prior Knowledge Mode:** HTTP/2 cleartext için Kestrel sadece Http2 protokolünü kullanmalı

**Build Komutu:**
```bash
docker build -f src/Services/Discount/Discount.Grpc/Dockerfile -t discountgrpc .
```

#### Gateway.API Dockerfile Oluştur
**Ne işe yarar:** Gateway.API servisini containerize eder.

**src/ApiGateway/Gateway.API/Dockerfile:**

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["global.json", "./"]

# Copy project files
COPY ["src/ApiGateway/Gateway.API/Gateway.API.csproj", "src/ApiGateway/Gateway.API/"]

# Restore dependencies
RUN dotnet restore "src/ApiGateway/Gateway.API/Gateway.API.csproj"

# Copy all source files
COPY . .

# Build and publish
WORKDIR "/src/src/ApiGateway/Gateway.API"
RUN dotnet build "Gateway.API.csproj" -c Release -o /app/build
RUN dotnet publish "Gateway.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set environment variable
ENV ASPNETCORE_URLS=http://+:8080

# Entry point
ENTRYPOINT ["dotnet", "Gateway.API.dll"]
```

**Açıklama:**
- Gateway.API için BuildingBlocks gerekmez (sadece YARP kullanır)

**Build Komutu:**
```bash
docker build -f src/ApiGateway/Gateway.API/Dockerfile -t gatewayapi .
```

### Test:
- Her Dockerfile build oluyor mu? (`docker build` komutu çalışıyor mu?)
- Image oluştu mu? (`docker images` → catalogapi, basketapi, orderingapi, discountgrpc, gatewayapi görünüyor mu?)
- Her image çalışıyor mu? (`docker run -p 8080:8080 catalogapi` → Servis çalışıyor mu?)

**Not:** Build context her zaman solution root olmalı (shared projelere erişim için).

---

## 8.2 Docker Compose - Servisler

**Hedef:** Tüm servisleri Docker Compose'a ekle ve yapılandır

### Görevler:

#### Catalog.API Service Ekle
**Ne işe yarar:** Catalog.API servisini docker-compose.yml'e ekler.

**docker-compose.yml güncellemesi:**

```yaml
services:
  # ==================== INFRASTRUCTURE ====================
  # ... (mevcut infrastructure servisleri)
  
  # ==================== SERVICES ====================
  
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

**Açıklama:**
- **build.context:** Solution root (shared projelere erişim için)
- **build.dockerfile:** Dockerfile yolu
- **environment:** Connection string (container network içinde: `catalogdb:5432`)
- **depends_on:** catalogdb hazır olana kadar bekle (healthy condition)
- **ports:** Host:5001 → Container:8080
- **healthcheck:** Servis sağlığını kontrol et

#### Basket.API Service Ekle
**Ne işe yarar:** Basket.API servisini docker-compose.yml'e ekler.

**docker-compose.yml güncellemesi:**

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

**Açıklama:**
- **ConnectionStrings__Redis:** Container network içinde `basketdb:6379`
- **ConnectionStrings__Database:** Container network içinde `basketpostgres:5432`
- **GrpcSettings__DiscountUrl:** Container network içinde `discount.grpc:8080`
- **MessageBroker__Host:** Container network içinde `messagebroker:5672` (container port)
- **depends_on:** Tüm bağımlılıklar hazır olana kadar bekle

#### Ordering.API Service Ekle
**Ne işe yarar:** Ordering.API servisini docker-compose.yml'e ekler.

**docker-compose.yml güncellemesi:**

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

#### Discount.Grpc Service Ekle
**Ne işe yarar:** Discount.Grpc servisini docker-compose.yml'e ekler.

**docker-compose.yml güncellemesi:**

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
      - "5004:8080"  # gRPC port (HTTP/2 only)
      - "5005:8081"  # Health check port (HTTP/1.1 only)
    healthcheck:
      test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8081/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

#### Gateway.API Service Ekle
**Ne işe yarar:** Gateway.API servisini docker-compose.yml'e ekler.

**docker-compose.yml güncellemesi:**

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

**Açıklama:**
- Gateway tüm servislerin hazır olmasını bekler (depends_on)
- Gateway'in appsettings.json'unda container network içindeki adresler olmalı:
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

**Not:** Container network içinde servisler birbirlerine container adıyla erişir (`catalog.api:8080`).

### Gateway appsettings.json Güncellemesi
**Ne işe yarar:** Gateway'in Docker container network içinde doğru adreslere bağlanmasını sağlar.

**src/ApiGateway/Gateway.API/appsettings.json:**

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
- Container network içinde: `catalog.api:8080`, `basket.api:8080`, `ordering.api:8080`
- Localhost yerine container adları kullanılır (Docker Compose network içinde)

### Test:
- Tüm servisler build oluyor mu? (`docker-compose build`)
- Tüm servisler çalışıyor mu? (`docker-compose up -d`)
- Tüm container'lar healthy mi? (`docker-compose ps`)
- Loglar temiz mi? (`docker-compose logs -f`)

**Docker Compose Komutları:**
```bash
# Tüm servisleri build et
docker-compose build

# Tüm servisleri başlat (arka planda)
docker-compose up -d

# Tüm servisleri durdur
docker-compose down

# Logları izle
docker-compose logs -f

# Belirli servisin logunu izle
docker-compose logs -f catalog.api

# Container'ları listele
docker-compose ps

# Tüm servisleri durdur + volume'ları sil
docker-compose down -v

# Yeniden build et ve başlat
docker-compose up -d --build
```

---

## 8.3 End-to-End Test

**Hedef:** Tüm sistemin çalıştığını doğrula

### Görevler:

#### Test 1: Gateway Üzerinden Catalog'a Erişim
**Ne işe yarar:** Gateway routing'inin çalıştığını doğrular.

**Test Adımları:**

1. **Gateway üzerinden ürün listesi al:**
   ```bash
   curl http://localhost:5000/catalog-service/api/products
   ```

2. **Beklenen Sonuç:**
   - Status Code: 200 OK
   - Response: Ürün listesi JSON formatında
   - Gateway, isteği Catalog.API'ye yönlendirmeli

3. **Gateway üzerinden ürün detayı al:**
   ```bash
   curl http://localhost:5000/catalog-service/api/products/{productId}
   ```

4. **Beklenen Sonuç:**
   - Status Code: 200 OK
   - Response: Ürün detayı JSON formatında

**Kontrol:**
- Gateway routing çalışıyor mu?
- Path transform çalışıyor mu? (`/catalog-service/api/products` → `/api/products`)
- Catalog.API response dönüyor mu?

#### Test 2: Gateway Üzerinden Basket'a Erişim
**Ne işe yarar:** Basket servisinin Gateway üzerinden erişilebildiğini doğrular.

**Test Adımları:**

1. **Sepet oluştur:**
   ```bash
   curl -X POST http://localhost:5000/basket-service/api/baskets \
     -H "Content-Type: application/json" \
     -d '{
       "userName": "testuser",
       "items": [
         {
           "productId": "guid",
           "productName": "iPhone 15",
           "quantity": 2,
           "price": 50000
         }
       ]
     }'
   ```

2. **Sepeti getir:**
   ```bash
   curl http://localhost:5000/basket-service/api/baskets/testuser
   ```

3. **Beklenen Sonuç:**
   - Status Code: 200 OK
   - Response: Sepet bilgileri (items, totalPrice, discount dahil)
   - Discount Service'ten indirim bilgisi alınmış olmalı (gRPC)

**Kontrol:**
- Basket.API çalışıyor mu?
- Redis'e yazılıyor mu?
- gRPC ile Discount Service'e bağlanıyor mu?

#### Test 3: Sepet → Checkout → Sipariş Oluştu mu?
**Ne işe yarar:** End-to-end akışın (Basket → RabbitMQ → Ordering) çalıştığını doğrular.

**Test Adımları:**

1. **Sepet oluştur:**
   ```bash
   curl -X POST http://localhost:5000/basket-service/api/baskets \
     -H "Content-Type: application/json" \
     -d '{
       "userName": "testuser",
       "items": [
         {
           "productId": "guid",
           "productName": "iPhone 15",
           "quantity": 2,
           "price": 50000
         }
       ]
     }'
   ```

2. **Checkout yap:**
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

3. **Beklenen Sonuç:**
   - Status Code: 200 OK
   - Basket silinmiş olmalı (Redis ve PostgreSQL'den)

4. **Sipariş oluştu mu kontrol et:**
   ```bash
   curl http://localhost:5000/ordering-service/api/orders/user/testuser
   ```

5. **Beklenen Sonuç:**
   - Status Code: 200 OK
   - Response: Kullanıcının siparişleri (yeni oluşturulan sipariş dahil)
   - Sipariş status: "Pending"

**Kontrol:**
- Basket Checkout event RabbitMQ'ya gönderildi mi?
- Ordering Service event'i aldı mı?
- Sipariş veritabanına kaydedildi mi?
- RabbitMQ Management UI'da queue görünüyor mu? (http://localhost:15673)

#### Test 4: gRPC Çalışıyor mu? (Basket → Discount)
**Ne işe yarar:** gRPC iletişiminin çalıştığını doğrular.

**Test Adımları:**

1. **Sepet oluştur (indirimli ürünle):**
   ```bash
   curl -X POST http://localhost:5000/basket-service/api/baskets \
     -H "Content-Type: application/json" \
     -d '{
       "userName": "testuser",
       "items": [
         {
           "productId": "guid",
           "productName": "iPhone 15",
           "quantity": 2,
           "price": 50000
         }
       ]
     }'
   ```

2. **Sepeti getir (indirim uygulanmış mı kontrol et):**
   ```bash
   curl http://localhost:5000/basket-service/api/baskets/testuser
   ```

3. **Beklenen Sonuç:**
   - Response'da `discount` field'ı olmalı
   - Eğer Discount Service'te "iPhone 15" için kupon varsa, indirim uygulanmış olmalı

**Kontrol:**
- Basket.API, Discount.Grpc'ye gRPC ile bağlanıyor mu?
- İndirim bilgisi alınıyor mu?
- İndirim sepete uygulanıyor mu?

**Not:** Discount Service'te test için kupon oluşturulmuş olmalı (Seed data veya manuel oluştur).

#### Test 5: RabbitMQ Event Akışı Çalışıyor mu?
**Ne işe yarar:** RabbitMQ event publish/consume akışının çalıştığını doğrular.

**Test Adımları:**

1. **RabbitMQ Management UI'yı aç:**
   - URL: http://localhost:15673
   - Username: `guest`
   - Password: `guest`

2. **Queues sekmesine git:**
   - `BasketCheckoutEvent` queue'su görünmeli

3. **Checkout yap (Test 3'teki gibi):**
   - Queue'da mesaj görünmeli
   - Mesaj consume edilmeli (Ordering Service tarafından)
   - Queue tekrar boş olmalı

4. **Ordering Service loglarını kontrol et:**
   ```bash
   docker-compose logs ordering.api
   ```
   - "BasketCheckoutEvent consumed" mesajı görünmeli
   - "Order created" mesajı görünmeli

**Kontrol:**
- RabbitMQ queue oluştu mu?
- Event publish edildi mi?
- Event consume edildi mi?
- Sipariş oluştu mu?

### Tüm Sistem Sağlık Kontrolü

**Health Check Endpoint'leri:**

```bash
# Gateway Health Check
curl http://localhost:5000/health

# Catalog Health Check
curl http://localhost:5001/health

# Basket Health Check
curl http://localhost:5002/health

# Ordering Health Check
curl http://localhost:5003/health

# Discount Health Check (HTTP/1.1 port - 8081)
curl http://localhost:5005/health
# NOT: Discount.Grpc iki port kullanır:
# - 5004:8080 (gRPC - HTTP/2 only)
# - 5005:8081 (Health check - HTTP/1.1 only)
```

**Beklenen Sonuç:**
- Tüm health check'ler "Healthy" dönmeli
- PostgreSQL, Redis, RabbitMQ bağlantıları çalışmalı

### Docker Compose Loglarını İzleme

```bash
# Tüm servislerin loglarını izle
docker-compose logs -f

# Belirli servisin logunu izle
docker-compose logs -f catalog.api
docker-compose logs -f basket.api
docker-compose logs -f ordering.api
docker-compose logs -f gateway.api

# Son 100 satır
docker-compose logs --tail=100 gateway.api
```

### Test Sonuçları

**Başarılı Test Kriterleri:**
- ✅ Gateway üzerinden tüm servislere erişilebiliyor
- ✅ Catalog API çalışıyor (ürün listesi, detay)
- ✅ Basket API çalışıyor (sepet oluşturma, getirme)
- ✅ Checkout → Sipariş oluşuyor (RabbitMQ event akışı)
- ✅ gRPC çalışıyor (Basket → Discount)
- ✅ Tüm health check'ler "Healthy"
- ✅ Tüm container'lar "healthy" durumda

**Sonuç:** ✅ Tüm sistem çalışıyor!

---

## Özet: Faz 8 adımlar sırası

1. Catalog.API Dockerfile oluştur (`src/Services/Catalog/Catalog.API/Dockerfile`)
2. Basket.API Dockerfile oluştur (`src/Services/Basket/Basket.API/Dockerfile`)
3. Ordering.API Dockerfile oluştur (`src/Services/Ordering/Ordering.API/Dockerfile`)
4. Discount.Grpc Dockerfile oluştur (`src/Services/Discount/Discount.Grpc/Dockerfile`)
5. Gateway.API Dockerfile oluştur (`src/ApiGateway/Gateway.API/Dockerfile`)
6. Her Dockerfile'ı test et (`docker build`)
7. docker-compose.yml'e Catalog.API service ekle
8. docker-compose.yml'e Basket.API service ekle
9. docker-compose.yml'e Ordering.API service ekle
10. docker-compose.yml'e Discount.Grpc service ekle
11. docker-compose.yml'e Gateway.API service ekle
12. Gateway appsettings.json'u güncelle (container network adresleri)
13. Tüm servisleri build et (`docker-compose build`)
14. Tüm servisleri başlat (`docker-compose up -d`)
15. Container'ları kontrol et (`docker-compose ps`)
16. Test 1: Gateway üzerinden Catalog'a erişim
17. Test 2: Gateway üzerinden Basket'a erişim
18. Test 3: Sepet → Checkout → Sipariş oluştu mu?
19. Test 4: gRPC çalışıyor mu? (Basket → Discount)
20. Test 5: RabbitMQ event akışı çalışıyor mu?
21. Tüm health check'leri test et
22. Logları kontrol et

**Bu adımlar tamamlandıktan sonra sistem production-ready durumda!**

---

## Troubleshooting

### Sorun: Dockerfile build hatası
**Hata:** `COPY failed: file not found`
**Çözüm:** Build context'in solution root olduğundan emin ol (`docker build -f ... .`)

### Sorun: Container başlamıyor
**Hata:** `Container exited with code 1`
**Çözüm:** 
- Logları kontrol et: `docker-compose logs catalog.api`
- Connection string'leri kontrol et (container network adresleri)
- Port çakışması var mı? (`netstat -tuln | grep 5001`)

### Sorun: Servisler birbirine bağlanamıyor
**Hata:** `Connection refused`
**Çözüm:**
- Container network adreslerini kontrol et (`catalog.api:8080` vs `localhost:5001`)
- `depends_on` doğru mu? (healthy condition)
- Health check'ler çalışıyor mu?

### Sorun: Gateway routing çalışmıyor
**Hata:** `404 Not Found`
**Çözüm:**
- Gateway appsettings.json'da container network adresleri var mı?
- Path transform doğru mu? (`PathRemovePrefix`)
- Downstream servisler çalışıyor mu?

### Sorun: Gateway container başlamıyor / hemen kapanıyor
**Hata:** 
```
gateway.api Exited (0) About a minute ago
fail: Microsoft.Extensions.Diagnostics.HealthChecks.DefaultHealthCheckService[103]
      Health check basket-api with status Unhealthy completed after 10000ms
```

**Neden:**
- Gateway'in `/health` endpoint'i tüm downstream servislerin health check'lerini kontrol ediyor
- Basket.api yavaş başladığında Gateway'in health check'i başarısız oluyor
- Docker Compose, Gateway'i unhealthy olarak işaretleyip container'ı kapatıyor

**Çözüm:**
Gateway'in `/health` endpoint'ini sadece Gateway'in kendisini kontrol edecek şekilde değiştirin:

**Gateway.API/Program.cs:**
```csharp
// Gateway'in kendi health check'i (Docker Compose için)
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    // Sadece Gateway'in kendisini kontrol et (downstream servisler kritik değil)
    Predicate = _ => false
});

// Downstream servislerin health check'leri (opsiyonel, monitoring için)
app.MapHealthChecks("/health/downstream", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Name.Contains("-api")
});
```

**Açıklama:**
- `/health` → Sadece Gateway'in durumunu kontrol eder (Docker Compose için)
- `/health/downstream` → Downstream servislerin durumunu gösterir (monitoring için)
- Gateway artık downstream servislerin durumundan bağımsız olarak çalışır

**Detaylı bilgi için:** `docs/architecture/eSho-AspController-Arc/documentation/done/faz-7-done/faz-7-1-gateway-api-projesi-olustur-note.md` dosyasındaki "4. Docker Compose Health Check Sorunu ve Çözümü" bölümüne bakın.

### Sorun: RabbitMQ event akışı çalışmıyor
**Hata:** Event consume edilmiyor
**Çözüm:**
- RabbitMQ Management UI'da queue görünüyor mu?
- Consumer kayıtlı mı? (`docker-compose logs ordering.api`)
- Connection string doğru mu? (`amqp://guest:guest@messagebroker:5672`)

---

## Sonuç

✅ **Faz 8 Tamamlandı!**

Tüm sistem Docker container'larında çalışıyor:
- ✅ Tüm servisler containerize edildi
- ✅ Docker Compose ile tek komutla başlatılabiliyor
- ✅ End-to-end testler başarılı
- ✅ Production-ready durumda

**Sonraki Adımlar (Opsiyonel):**
- Faz 9: Frontend Uygulaması
- CI/CD Pipeline entegrasyonu
- Kubernetes deployment (production için)

