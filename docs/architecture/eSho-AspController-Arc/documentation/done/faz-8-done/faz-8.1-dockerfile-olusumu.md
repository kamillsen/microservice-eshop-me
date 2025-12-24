# Faz 8.1 - Dockerfile'ların Oluşturulması

**Tarih:** 24 Aralık 2024  
**Durum:** ✅ Tamamlandı  
**Süre:** ~6 dakika (tüm servisler için build)

---

## 📋 Yapılan İşlemler

### 1. Dockerfile'ların Oluşturulması

Her servis için multi-stage build kullanan Dockerfile'lar oluşturuldu:

1. **Catalog.API** → `src/Services/Catalog/Catalog.API/Dockerfile`
2. **Basket.API** → `src/Services/Basket/Basket.API/Dockerfile`
3. **Ordering.API** → `src/Services/Ordering/Ordering.API/Dockerfile`
4. **Discount.Grpc** → `src/Services/Discount/Discount.Grpc/Dockerfile`
5. **Gateway.API** → `src/ApiGateway/Gateway.API/Dockerfile`

### 2. Dockerfile Yapısı

Her Dockerfile iki stage içerir:

#### Build Stage
- **Base Image:** `mcr.microsoft.com/dotnet/sdk:9.0` (derleme için)
- **İşlemler:**
  1. Solution seviyesi dosyaları kopyala (Directory.Build.props, Directory.Packages.props, global.json)
  2. Proje dosyalarını kopyala (.csproj)
  3. BuildingBlocks bağımlılıklarını kopyala (gerekli servisler için)
  4. `dotnet restore` (NuGet paketlerini indir - layer caching için)
  5. Tüm kaynak kodları kopyala
  6. `dotnet build` (Release modunda derle)
  7. `dotnet publish` (çalıştırılabilir dosyaları hazırla)

#### Final Stage
- **Base Image:** `mcr.microsoft.com/dotnet/aspnet:9.0` (runtime - küçük)
- **İşlemler:**
  1. Build stage'den publish edilmiş dosyaları kopyala
  2. Port 8080 expose et
  3. Environment variable ayarla (`ASPNETCORE_URLS=http://+:8080`)
  4. Entrypoint ayarla (`dotnet {ServiceName}.dll`)

---

## 🔧 Kullanılan Komutlar

### Build Komutları

#### Catalog.API
```bash
docker build -f src/Services/Catalog/Catalog.API/Dockerfile -t catalogapi .
```
**Süre:** ~182 saniye (ilk build - SDK image indirme dahil)

#### Basket.API
```bash
docker build -f src/Services/Basket/Basket.API/Dockerfile -t basketapi .
```
**Süre:** ~18 saniye (ikinci build - layer caching sayesinde hızlı)

#### Ordering.API
```bash
docker build -f src/Services/Ordering/Ordering.API/Dockerfile -t orderingapi .
```
**Süre:** ~71 saniye

#### Discount.Grpc
```bash
docker build -f src/Services/Discount/Discount.Grpc/Dockerfile -t discountgrpc .
```
**Süre:** ~58 saniye

#### Gateway.API
```bash
docker build -f src/ApiGateway/Gateway.API/Dockerfile -t gatewayapi .
```
**Süre:** ~25 saniye

---

## ✅ Test Komutları

### Tüm Image'ları Listele
```bash
docker images | grep -E '(basketapi|orderingapi|discountgrpc|gatewayapi|catalogapi)'
```

### Formatlı Liste
```bash
docker images --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}" | grep -E "REPOSITORY|basketapi|orderingapi|discountgrpc|gatewayapi|catalogapi"
```

### Image Detaylarını Kontrol Et
```bash
# Entrypoint kontrolü
docker inspect catalogapi --format '{{.Config.Entrypoint}}'

# Port kontrolü
docker inspect catalogapi --format '{{.Config.ExposedPorts}}'

# Environment variables
docker inspect catalogapi --format '{{range .Config.Env}}{{println .}}{{end}}' | grep ASPNETCORE_URLS
```

### DLL'leri Kontrol Et
```bash
# Catalog.API
docker run --rm --entrypoint ls catalogapi -1 /app | grep -E "(Catalog|BuildingBlocks)"

# Basket.API
docker run --rm --entrypoint ls basketapi -1 /app | grep -E "(Basket|BuildingBlocks)"

# Ordering.API
docker run --rm --entrypoint ls orderingapi -1 /app | grep -E "(Ordering|BuildingBlocks)"

# Discount.Grpc
docker run --rm --entrypoint ls discountgrpc -1 /app | grep -i discount

# Gateway.API
docker run --rm --entrypoint ls gatewayapi -1 /app | grep -i gateway
```

### Runtime Kontrolü
```bash
docker run --rm --entrypoint sh catalogapi -c "dotnet --list-runtimes 2>&1 | head -3"
```

---

## 📊 Sonuçlar

### Oluşturulan Image'lar

| Image | Tag | Boyut | Image ID |
|-------|-----|-------|----------|
| **catalogapi** | latest | 346MB | 1bf9e5e362fe |
| **basketapi** | latest | 358MB | 93bb783f0dd1 |
| **orderingapi** | latest | 355MB | 8eaa5c734099 |
| **discountgrpc** | latest | 340MB | 1a23977d51df |
| **gatewayapi** | latest | 330MB | 346645db07ea |

### Kontrol Sonuçları

#### ✅ DLL Kontrolleri

**Catalog.API:**
- ✅ Catalog.API.dll mevcut
- ✅ BuildingBlocks.Behaviors.dll mevcut
- ✅ BuildingBlocks.Exceptions.dll mevcut

**Basket.API:**
- ✅ Basket.API.dll mevcut
- ✅ BuildingBlocks.Behaviors.dll mevcut
- ✅ BuildingBlocks.Exceptions.dll mevcut
- ✅ BuildingBlocks.Messaging.dll mevcut

**Ordering.API:**
- ✅ Ordering.API.dll mevcut
- ✅ BuildingBlocks.Behaviors.dll mevcut
- ✅ BuildingBlocks.Exceptions.dll mevcut
- ✅ BuildingBlocks.Messaging.dll mevcut

**Discount.Grpc:**
- ✅ Discount.Grpc.dll mevcut
- ✅ BuildingBlocks yok (beklendiği gibi - bağımsız servis)

**Gateway.API:**
- ✅ Gateway.API.dll mevcut
- ✅ BuildingBlocks yok (beklendiği gibi - sadece YARP kullanıyor)

#### ✅ Entrypoint Kontrolleri

Tüm servisler için entrypoint doğru ayarlandı:
- `catalogapi`: `[dotnet Catalog.API.dll]`
- `basketapi`: `[dotnet Basket.API.dll]`
- `orderingapi`: `[dotnet Ordering.API.dll]`
- `discountgrpc`: `[dotnet Discount.Grpc.dll]`
- `gatewayapi`: `[dotnet Gateway.API.dll]`

#### ✅ Port ve Environment Kontrolleri

Tüm servisler için:
- **Port:** `8080/tcp` exposed
- **Environment:** `ASPNETCORE_URLS=http://+:8080`

#### ✅ Runtime Kontrolleri

Tüm image'larda:
- ✅ Microsoft.AspNetCore.App 9.0.11 yüklü
- ✅ Microsoft.NETCore.App 9.0.11 yüklü
- ✅ SDK yok (final stage'de olmaması gerekiyor - doğru)

---

## 🔍 Önemli Gözlemler

### 1. Layer Caching Etkisi

**İlk build (Catalog.API):** ~182 saniye
- SDK image indirme: ~100 saniye
- Runtime image indirme: ~33 saniye
- Restore: ~57 saniye
- Build + Publish: ~13 saniye

**İkinci build (Basket.API):** ~18 saniye
- Tüm base image'lar cache'den kullanıldı
- Sadece farklı dosyalar kopyalandı ve build edildi
- **%90 hız artışı!** 🚀

### 2. Multi-Stage Build Avantajları

- ✅ **Küçük final image:** Sadece runtime (SDK yok)
- ✅ **Güvenlik:** Build araçları production image'da yok
- ✅ **Hız:** Layer caching sayesinde hızlı rebuild
- ✅ **Temizlik:** Build artifact'ları final image'da yok

### 3. Image Boyutları

Image boyutları beklenen aralıkta:
- **En küçük:** gatewayapi (330MB) - Sadece YARP
- **En büyük:** basketapi (358MB) - Redis + PostgreSQL + gRPC + RabbitMQ bağımlılıkları

### 4. BuildingBlocks Bağımlılıkları

Bağımlılık yapısı doğru:
- **Catalog.API:** Exceptions + Behaviors
- **Basket.API:** Exceptions + Behaviors + Messaging
- **Ordering.API:** Exceptions + Behaviors + Messaging
- **Discount.Grpc:** Yok (bağımsız gRPC servisi)
- **Gateway.API:** Yok (sadece YARP)

---

## 📝 Dockerfile Örnekleri

### Catalog.API Dockerfile (Örnek)

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution files
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["global.json", "./"]

# Copy project files
COPY ["src/Services/Catalog/Catalog.API/Catalog.API.csproj", "src/Services/Catalog/Catalog.API/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj", "src/BuildingBlocks/BuildingBlocks.Exceptions/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Behaviors/BuildingBlocks.Behaviors.csproj", "src/BuildingBlocks/BuildingBlocks.Behaviors/"]

# Restore dependencies (layer caching için önce restore)
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
# Container portları
# 8080: gRPC (HTTP/2 only)
# 8081: Health check (HTTP/1.1 only)
EXPOSE 8080 8081

# Set environment variable
ENV ASPNETCORE_URLS=http://+:8080

# Entry point
ENTRYPOINT ["dotnet", "Catalog.API.dll"]
```

---

## 🎯 Öğrenilen Önemli Noktalar

### 1. Layer Caching Stratejisi

**Doğru sıralama:**
```
1. COPY .csproj files
2. RUN dotnet restore  ← Bu layer cache'lenir (sık değişmez)
3. COPY . (source code) ← Bu layer sık değişir
4. RUN dotnet build/publish
```

**Yanlış sıralama:**
```
1. COPY . (tüm kodlar)
2. RUN dotnet restore ← Her kod değişikliğinde restore tekrar çalışır!
```

### 2. Build Context

- **Build context:** Solution root (`.`) olmalı
- **Neden?** BuildingBlocks projelerine erişim için
- **Komut formatı:** `docker build -f {DockerfilePath} -t {ImageName} .`

### 3. Multi-Stage Build

**Build Stage:**
- SDK image (büyük, ~500MB+)
- Derleme araçları
- Kaynak kodlar

**Final Stage:**
- Runtime image (küçük, ~100MB+)
- Sadece publish edilmiş dosyalar
- Çalışma için yeterli

**Avantaj:** Final image %70 daha küçük!

### 4. Port Yönetimi

- **Container içi port:** 8080 (tüm servisler aynı)
- **Host port:** Docker Compose'da map edilecek (5000, 5001, 5002, ...)
- **Environment:** `ASPNETCORE_URLS=http://+:8080` (tüm network interface'lerinde dinle)

---

## ✅ Kontrol Listesi

Faz 8.1 için tamamlanan görevler:

- [x] Catalog.API Dockerfile oluşturuldu
- [x] Basket.API Dockerfile oluşturuldu
- [x] Ordering.API Dockerfile oluşturuldu
- [x] Discount.Grpc Dockerfile oluşturuldu
- [x] Gateway.API Dockerfile oluşturuldu
- [x] Tüm Dockerfile'lar build edildi
- [x] Image'lar oluşturuldu
- [x] DLL'ler kontrol edildi
- [x] Entrypoint'ler kontrol edildi
- [x] Port ve environment variables kontrol edildi
- [x] Runtime kontrol edildi
- [x] BuildingBlocks bağımlılıkları kontrol edildi

---

## 🚀 Sonraki Adım

**Faz 8.2:** Docker Compose - Servisler
- Tüm servisleri docker-compose.yml'e ekleme
- Environment variables yapılandırması
- depends_on ve healthcheck ayarları
- Gateway appsettings.json güncellemesi

---

## 📚 Kaynaklar

- [Docker Multi-Stage Builds](https://docs.docker.com/build/building/multi-stage/)
- [.NET Docker Images](https://hub.docker.com/_/microsoft-dotnet-aspnet)
- [Layer Caching Best Practices](https://docs.docker.com/build/cache/)

---

**Not:** Bu dokümantasyon, Faz 8.1 sırasında yapılan tüm işlemleri ve öğrenilen noktaları içerir. Sonraki fazlar için referans olarak kullanılabilir.

