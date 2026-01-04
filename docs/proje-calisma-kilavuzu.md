# Proje Çalıştırma Kılavuzu

> Bu doküman, e-ticaret microservice projesini nasıl çalıştıracağınızı adım adım açıklar.
> 
> **İçerik:**
> - Önkoşullar
> - Docker Container'ları Ayağa Kaldırma
> - Tüm Servisleri Çalıştırma (Docker Compose)
> - API Gateway Üzerinden Erişim
> - Test Etme
> - Durdurma ve Temizlik
> - Sorun Giderme

---

## 📋 Önkoşullar

Projeyi çalıştırmadan önce aşağıdaki araçların yüklü olduğundan emin olun:

### Gerekli Araçlar

1. **.NET 9.0 SDK**
   ```bash
   dotnet --version
   # Beklenen: 9.0.x veya üzeri
   ```

2. **Docker ve Docker Compose**
   ```bash
   docker --version
   docker compose version
   ```

3. **Git** (projeyi klonlamak için)

### Docker Durumu

Docker'ın çalıştığından emin olun:
```bash
docker ps
# Hata vermemeli, container listesi göstermeli
```

**Docker Durumunu Kontrol Etme:**
```bash
# Docker daemon çalışıyor mu?
docker info

# Docker Compose versiyonu
docker compose version
# Beklenen: Docker Compose version v2.x.x
```

**Docker Çalışmıyorsa:**
```bash
# Linux (systemd kullanan sistemlerde)
sudo systemctl start docker
sudo systemctl enable docker  # Otomatik başlatma için

# Docker servis durumunu kontrol et
sudo systemctl status docker
```

---

## 🐳 Docker Container'ları Ayağa Kaldırma

Proje, veritabanları ve altyapı servisleri için Docker container'ları kullanır.

### Seçenek 1: Tüm Container'ları Tek Seferde Başlatma (Önerilen)

**Avantajlar:**
- ✅ Tek komutla tüm servisler hazır
- ✅ Hızlı ve kolay
- ✅ Bağımlılıklar otomatik çözülür

**Komut:**
```bash
cd "/home/kSEN/Desktop/ Projects/microservice-practice-me"
docker compose up -d
```

**Ne Yapar?**
- Tüm container'ları arka planda (`-d` flag) başlatır
- Bağımlılıkları (`depends_on`) otomatik çözer
- Container'lar hazır olana kadar bekler
- Volume'ları oluşturur (veritabanı verileri için)
- Network'leri oluşturur (container'lar arası iletişim için)

**Başlatılan Container'lar:**

**Altyapı Servisleri:**
- `catalogdb` → PostgreSQL (port 5436)
- `orderingdb` → PostgreSQL (port 5435)
- `discountdb` → PostgreSQL (port 5434)
- `basketpostgres` → PostgreSQL (port 5437) - Basket Service için
- `basketdb` → Redis (port 6379, RedisInsight UI: 8001) - Basket Service cache için
- `messagebroker` → RabbitMQ (AMQP: 5673, Management UI: 15673)
- `pgadmin` → pgAdmin Web UI (port 5050)

**Uygulama Servisleri:**
- `catalog.api` → Catalog API (port 5001)
- `basket.api` → Basket API (port 5002)
- `ordering.api` → Ordering API (port 5003)
- `discount.grpc` → Discount gRPC Service (port 5004/5005)
- `gateway.api` → API Gateway (port 5000)
- `web.ui` → Web UI (Blazor) (port 3000)

**Başlatma Adımları:**
```bash
# 1. Proje dizinine git
cd "/home/kSEN/Desktop/ Projects/microservice-practice-me"

# 2. Container'ları başlat
docker compose up -d

# 3. Container'ların durumunu kontrol et
docker compose ps

# 4. Logları izle (opsiyonel)
docker compose logs -f
```

**Kontrol:**
```bash
# Tüm container'ları listele
docker ps
# Tüm container'lar "Up" durumunda olmalı

# Docker Compose ile durum kontrolü
docker compose ps
# Status: running (healthy) veya Up olmalı

# Belirli bir container'ın durumunu kontrol et
docker ps --filter "name=catalogdb"
```

**Başarılı Başlatma Çıktısı:**
```
[+] Running 7/7]
 ✔ Container discountdb      Started
 ✔ Container orderingdb      Started
 ✔ Container catalogdb       Started
 ✔ Container basketpostgres  Started
 ✔ Container basketdb        Started
 ✔ Container messagebroker   Started
 ✔ Container pgadmin         Started
```

**Container'ların Hazır Olmasını Bekleme:**
```bash
# Container'ların health check'lerini bekle
docker compose ps --format json | jq '.[] | select(.Health != "healthy")'

# Tüm container'lar healthy olana kadar bekle
while [ $(docker compose ps --format json | jq -r '.[] | select(.Health != "healthy" and .State == "running") | .Name' | wc -l) -gt 0 ]; do
  echo "Container'lar hazır oluyor..."
  sleep 2
done
echo "Tüm container'lar hazır!"
```

---

### Seçenek 2: Container'ları Tek Tek Başlatma

Bazı durumlarda sadece belirli container'ları başlatmak isteyebilirsiniz.

#### 2.1. Catalog Veritabanı (CatalogDb)

**Ne İşe Yarar:**
- Catalog API'nin veritabanı
- Ürün ve kategori bilgilerini saklar

**Komut:**
```bash
docker compose up -d catalogdb
```

**Kontrol:**
```bash
docker ps | grep catalogdb
# Status: Up (healthy) olmalı

# Veritabanı hazır mı kontrol et
docker exec catalogdb pg_isready -U postgres
# Beklenen: /var/run/postgresql:5432 - accepting connections
```

**Port:** `5436` (host) → `5432` (container)

---

#### 2.2. Ordering Veritabanı (OrderingDb)

**Ne İşe Yarar:**
- Ordering Service'in veritabanı (henüz oluşturulmadı)
- Sipariş bilgilerini saklar

**Komut:**
```bash
docker compose up -d orderingdb
```

**Port:** `5435` (host) → `5432` (container)

---

#### 2.3. Discount Veritabanı (DiscountDb)

**Ne İşe Yarar:**
- Discount Service'in veritabanı (henüz oluşturulmadı)
- Kupon ve indirim bilgilerini saklar

**Komut:**
```bash
docker compose up -d discountdb
```

**Port:** `5434` (host) → `5432` (container)

---

#### 2.4. Basket Veritabanları (PostgreSQL + Redis)

**Ne İşe Yarar:**
- **PostgreSQL (basketpostgres):** Basket Service'in source of truth veritabanı
- **Redis (basketdb):** Basket Service'in cache veritabanı (Cache-aside pattern)

**Komutlar:**
```bash
# PostgreSQL
docker compose up -d basketpostgres

# Redis
docker compose up -d basketdb
```

**Portlar:**
- PostgreSQL: `5437` (host) → `5432` (container)
- Redis: `6379`
- RedisInsight UI: `8001` (http://localhost:8001)

**Kontrol:**
```bash
# PostgreSQL hazır mı?
docker exec basketpostgres pg_isready -U postgres
# Beklenen: /var/run/postgresql:5432 - accepting connections

# Redis çalışıyor mu?
docker exec basketdb redis-cli ping
# Beklenen: PONG
```

---

#### 2.5. Message Broker (RabbitMQ)

**Ne İşe Yarar:**
- Servisler arası mesajlaşma (henüz kullanılmıyor)
- Event-driven communication için

**Komut:**
```bash
docker compose up -d messagebroker
```

**Portlar:**
- AMQP: `5673` (host) → `5672` (container)
- Management UI: `15673` (http://localhost:15673)

**Giriş Bilgileri:**
- Username: `guest`
- Password: `guest`

---

#### 2.6. pgAdmin (Veritabanı Yönetim Arayüzü)

**Ne İşe Yarar:**
- PostgreSQL veritabanlarını görsel olarak yönetmek
- SQL sorguları çalıştırmak
- Tabloları görüntülemek

**Komut:**
```bash
docker compose up -d pgadmin
```

**Not:** pgAdmin başlatıldığında bağımlı olduğu veritabanı container'ları (`catalogdb`, `orderingdb`, `discountdb`, `basketpostgres`) da otomatik başlatılır (`depends_on`).

**Port:** `5050` (http://localhost:5050)

**Giriş Bilgileri:**
- Email: `admin@admin.com`
- Password: `admin`

**Veritabanı Bağlantısı Ekleme:**
1. Tarayıcıda `http://localhost:5050` aç
2. Giriş yap (`admin@admin.com` / `admin`)
3. Her veritabanı için ayrı server kaydı oluştur:

   **CatalogDb:**
   - "Add New Server" tıkla
   - **General tab:** Name: `CatalogDb`
   - **Connection tab:**
     - Host: `catalogdb` (Docker container adı)
     - Port: `5432` (container içindeki port, host port değil!)
     - Database: `CatalogDb`
     - Username: `postgres`
     - Password: `postgres`
     - "Save password" işaretle
   - "Save" tıkla

   **BasketDb:**
   - "Add New Server" tıkla
   - **General tab:** Name: `BasketDb`
   - **Connection tab:**
     - Host: `basketpostgres` (Docker container adı)
     - Port: `5432` (container içindeki port, host port değil!)
     - Database: `BasketDb`
     - Username: `postgres`
     - Password: `postgres`
     - "Save password" işaretle
   - "Save" tıkla

**Önemli:** Host olarak container adlarını kullanın (localhost değil). pgAdmin Docker container içinde çalıştığı için container network'ünde container adlarını çözümler.

---

### Container Durumlarını Kontrol Etme

Docker container'larının durumunu kontrol etmek için çeşitli yöntemler kullanabilirsiniz.

#### Tüm Container'ları Listele

```bash
# Kısa liste (sadece çalışan container'lar)
docker ps

# Tüm container'lar (durdurulmuş dahil)
docker ps -a

# Docker Compose ile durum kontrolü (önerilen)
docker compose ps

# Detaylı format ile
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

**Beklenen Çıktı:**
```
NAME            STATUS                    PORTS
catalogdb       Up 5 minutes (healthy)   0.0.0.0:5436->5432/tcp
orderingdb      Up 5 minutes (healthy)   0.0.0.0:5435->5432/tcp
discountdb      Up 5 minutes (healthy)   0.0.0.0:5434->5432/tcp
basketpostgres  Up 5 minutes (healthy)   0.0.0.0:5437->5432/tcp
basketdb        Up 5 minutes (healthy)   0.0.0.0:6379->6379/tcp, 0.0.0.0:8001->8001/tcp
messagebroker   Up 5 minutes (healthy)   4369/tcp, 5672/tcp, 15672/tcp, 0.0.0.0:5673->5672/tcp, 0.0.0.0:15673->15672/tcp
pgadmin         Up 5 minutes             0.0.0.0:5050->80/tcp
```

#### Belirli Bir Container'ı Kontrol Et

```bash
# Container adı ile
docker ps | grep catalogdb

# Filter ile
docker ps --filter "name=catalogdb"

# Detaylı bilgi
docker inspect catalogdb

# Sadece durum
docker inspect catalogdb --format='{{.State.Status}}'
# Beklenen: running
```

#### Container Health Check

Tüm container'ların health check durumunu kontrol edin:

```bash
# Health check durumunu göster
docker ps --format "table {{.Names}}\t{{.Status}}"

# Sadece healthy container'ları göster
docker ps --filter "health=healthy"

# Sadece unhealthy container'ları göster
docker ps --filter "health=unhealthy"

# Health check sonuçlarını detaylı göster
docker inspect catalogdb --format='{{json .State.Health}}' | jq
```

**Health Check Durumları:**
- `healthy` → Container sağlıklı, hazır
- `unhealthy` → Container çalışıyor ama health check başarısız
- `starting` → Health check henüz başlamadı
- `none` → Health check tanımlı değil

#### Container Loglarını Görüntüleme

```bash
# Tüm container logları
docker compose logs

# Belirli bir container logları
docker compose logs catalogdb

# Son N satırı göster
docker compose logs --tail=50 catalogdb

# Canlı log takibi (follow)
docker compose logs -f catalogdb

# Birden fazla container loglarını izle
docker compose logs -f catalogdb orderingdb

# Timestamp ile log göster
docker compose logs -f --timestamps catalogdb
```

#### Veritabanı Container'larının Özel Kontrolleri

**PostgreSQL Container'ları:**
```bash
# CatalogDb hazır mı?
docker exec catalogdb pg_isready -U postgres
# Beklenen: /var/run/postgresql:5432 - accepting connections

# Veritabanına bağlanma testi
docker exec catalogdb psql -U postgres -d CatalogDb -c "SELECT version();"

# Tablo sayısını kontrol et
docker exec catalogdb psql -U postgres -d CatalogDb -c "\dt"
```

**Redis Container:**
```bash
# Redis çalışıyor mu?
docker exec basketdb redis-cli ping
# Beklenen: PONG

# Redis bilgilerini göster
docker exec basketdb redis-cli info server
```

**RabbitMQ Container:**
```bash
# RabbitMQ çalışıyor mu?
docker exec messagebroker rabbitmq-diagnostics ping
# Beklenen: Pong

# RabbitMQ durumunu göster
docker exec messagebroker rabbitmqctl status
```

#### Container İstatistikleri

```bash
# Container kaynak kullanımını göster
docker stats

# Belirli container'ların istatistiklerini göster
docker stats catalogdb basketdb

# Sadece CPU ve Memory
docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}"
```

#### Network Kontrolleri

```bash
# Docker network'lerini listele
docker network ls

# Projenin network'ünü kontrol et
docker network inspect microservice-practice-me_default

# Container'ların network bağlantılarını göster
docker inspect catalogdb --format='{{json .NetworkSettings.Networks}}' | jq
```

#### Volume Kontrolleri

```bash
# Volume'ları listele
docker volume ls

# Proje volume'larını filtrele
docker volume ls | grep -E "(catalog|basket|ordering|discount|rabbitmq|pgadmin)"

# Volume detaylarını görüntüle
docker volume inspect catalogdb_data

# Volume boyutunu kontrol et (Linux)
docker exec catalogdb du -sh /var/lib/postgresql/data
```

#### Port Kontrolleri

```bash
# Port'ları dinleyen container'ları göster
docker ps --format "table {{.Names}}\t{{.Ports}}"

# Belirli bir port'u kullanan container'ı bul
docker ps --filter "publish=5436"

# Port erişilebilirliğini test et
nc -zv localhost 5436  # Catalog PostgreSQL
nc -zv localhost 5435  # Ordering PostgreSQL
nc -zv localhost 5434  # Discount PostgreSQL
nc -zv localhost 5437  # Basket PostgreSQL
nc -zv localhost 6379  # Redis
nc -zv localhost 5673  # RabbitMQ AMQP
nc -zv localhost 15673 # RabbitMQ Management
nc -zv localhost 5050  # pgAdmin
nc -zv localhost 8001  # RedisInsight
```

#### Tüm Container'ların Durum Özeti

```bash
# Tek komutla tüm bilgileri göster
echo "=== Container Durumları ==="
docker compose ps

echo -e "\n=== Health Check Durumları ==="
docker ps --format "table {{.Names}}\t{{.Status}}" --filter "name=catalogdb|orderingdb|discountdb|basketpostgres|basketdb|messagebroker|pgadmin"

echo -e "\n=== Port Durumları ==="
docker ps --format "table {{.Names}}\t{{.Ports}}" --filter "name=catalogdb|orderingdb|discountdb|basketdb|messagebroker|pgadmin"
```

#### Container'ların Hazır Olup Olmadığını Kontrol Etme

**Otomatik Kontrol Script'i:**
```bash
#!/bin/bash
# container-check.sh

check_container() {
    local container_name=$1
    local health_check=$2
    
    echo "Kontrol ediliyor: $container_name"
    
    # Container çalışıyor mu?
    if ! docker ps --format "{{.Names}}" | grep -q "^${container_name}$"; then
        echo "❌ $container_name çalışmıyor!"
        return 1
    fi
    
    # Health check var mı ve healthy mi?
    if [ -n "$health_check" ]; then
        health=$(docker inspect $container_name --format='{{.State.Health.Status}}')
        if [ "$health" != "healthy" ]; then
            echo "⏳ $container_name başlatılıyor... (Status: $health)"
            return 1
        fi
    fi
    
    echo "✅ $container_name hazır!"
    return 0
}

# Tüm container'ları kontrol et
check_container "catalogdb" "pg_isready"
check_container "orderingdb" "pg_isready"
check_container "discountdb" "pg_isready"
check_container "basketpostgres" "pg_isready"
check_container "basketdb" "redis-cli ping"
check_container "messagebroker" "rabbitmq-diagnostics ping"
check_container "pgadmin"

echo -e "\n✅ Tüm container'lar hazır!"
```

**Kullanım:**
```bash
chmod +x container-check.sh
./container-check.sh
```

#### Sorun Giderme için Hızlı Kontrol

```bash
# 1. Tüm container'lar çalışıyor mu?
docker compose ps

# 2. Hangi container'lar unhealthy?
docker ps --filter "health=unhealthy"

# 3. Container loglarında hata var mı?
docker compose logs --tail=50 | grep -i error

# 4. Port çakışması var mı?
netstat -tuln | grep -E "(5436|5435|5434|5437|6379|5673|15673|5050|8001)"

# 5. Disk alanı yeterli mi?
df -h

# 6. Docker kaynak kullanımı
docker system df
```

---

## 🚀 Tüm Servisleri Çalıştırma

Tüm servisler Docker container'ları içinde çalışır. Tek komutla tüm sistemi başlatabilirsiniz.

### Adım 1: Tüm Container'ları Başlat

```bash
cd "/home/kSEN/Desktop/ Projects/microservice-practice-me"
docker compose up -d
```

**Ne Yapar?**
- Tüm altyapı servislerini başlatır (PostgreSQL, Redis, RabbitMQ, pgAdmin)
- Tüm uygulama servislerini build eder ve başlatır
- Migration'ları otomatik uygular
- Health check'leri kontrol eder
- Bağımlılıkları sırayla başlatır

**Başarılı Başlatma Çıktısı:**
```
[+] Running 13/13
 ✔ Container discountdb      Started
 ✔ Container orderingdb      Started
 ✔ Container catalogdb      Started
 ✔ Container basketpostgres  Started
 ✔ Container basketdb        Started
 ✔ Container messagebroker   Started
 ✔ Container pgadmin         Started
 ✔ Container discount.grpc   Started
 ✔ Container catalog.api     Started
 ✔ Container basket.api      Started
 ✔ Container ordering.api     Started
 ✔ Container gateway.api     Started
 ✔ Container web.ui          Started
```

### Adım 2: Container Durumlarını Kontrol Et

```bash
# Tüm container'ların durumunu kontrol et
docker compose ps

# Health check durumlarını kontrol et
docker ps --format "table {{.Names}}\t{{.Status}}"
```

**Beklenen Durum:**
- Tüm container'lar `Up` veya `Up (healthy)` durumunda olmalı
- Gateway container'ı diğer servisler hazır olduktan sonra başlamalı

### Adım 3: Logları İzle (Opsiyonel)

```bash
# Tüm servislerin loglarını izle
docker compose logs -f

# Belirli bir servisin loglarını izle
docker compose logs -f gateway.api
docker compose logs -f catalog.api
```

---

## 🌐 API Gateway Üzerinden Erişim

Tüm servislere API Gateway (port 5000) üzerinden erişilir.

### Gateway Endpoint'leri

| Servis | Gateway Route | Doğrudan Erişim |
|--------|---------------|----------------|
| **Catalog** | `http://localhost:5000/catalog-service/api/...` | `http://localhost:5001/api/...` |
| **Basket** | `http://localhost:5000/basket-service/api/...` | `http://localhost:5002/api/...` |
| **Ordering** | `http://localhost:5000/ordering-service/api/...` | `http://localhost:5003/api/...` |

### Health Check Endpoint'leri

```bash
# Gateway kendi health check'i
curl http://localhost:5000/health

# Gateway downstream servislerin health check'i
curl http://localhost:5000/health/downstream

# Catalog API health check (doğrudan)
curl http://localhost:5001/health

# Basket API health check (doğrudan)
curl http://localhost:5002/health

# Ordering API health check (doğrudan)
curl http://localhost:5003/health
```

### API Endpoint Örnekleri

**Catalog API (Gateway üzerinden):**
```bash
# Kategorileri listele
curl http://localhost:5000/catalog-service/api/categories

# Ürünleri listele
curl http://localhost:5000/catalog-service/api/products
```

**Basket API (Gateway üzerinden):**
```bash
# Sepeti getir
curl http://localhost:5000/basket-service/api/baskets/user1

# Sepete ürün ekle
curl -X POST http://localhost:5000/basket-service/api/baskets \
  -H "Content-Type: application/json" \
  -d '{"userId":"user1","items":[...]}'
```

**Ordering API (Gateway üzerinden):**
```bash
# Siparişleri listele
curl http://localhost:5000/ordering-service/api/orders
```

### Swagger UI

Her servisin kendi Swagger UI'ı vardır:

- **Catalog API:** http://localhost:5001/
- **Basket API:** http://localhost:5002/
- **Ordering API:** http://localhost:5003/

**Not:** Gateway üzerinden Swagger UI erişimi yoktur. Doğrudan servis portlarından erişilir.

### Web UI

Blazor WebAssembly kullanıcı arayüzü:

- **Web UI:** http://localhost:3000/

**Not:** Web UI, API Gateway üzerinden backend servislerine bağlanır.

---

## 🧪 Test Etme

### 1. Health Check Testleri

**Gateway Health Check:**
```bash
curl http://localhost:5000/health
# Beklenen: Healthy
```

**Downstream Services Health Check:**
```bash
curl http://localhost:5000/health/downstream
# Beklenen: Healthy (catalog-api, basket-api, ordering-api)
```

**Bireysel Servis Health Check'leri:**
```bash
# Catalog API
curl http://localhost:5001/health

# Basket API
curl http://localhost:5002/health

# Ordering API
curl http://localhost:5003/health
```

**Hata Durumu:**
- Container durmuşsa → `Unhealthy` veya hata
- Veritabanı bağlantısı yoksa → `Unhealthy`
- Downstream servis hazır değilse → Gateway `/health/downstream` hata döner

---

### 2. API Endpoint Testleri (Gateway Üzerinden)

**Catalog API:**
```bash
# Kategorileri listele
curl http://localhost:5000/catalog-service/api/categories

# Ürünleri listele
curl http://localhost:5000/catalog-service/api/products

# Belirli bir ürünü getir
PRODUCT_ID=$(curl -s http://localhost:5000/catalog-service/api/products | jq -r '.[0].id')
curl http://localhost:5000/catalog-service/api/products/$PRODUCT_ID
```

**Basket API:**
```bash
# Sepeti getir
curl http://localhost:5000/basket-service/api/baskets/user1

# Sepete ürün ekle
curl -X POST http://localhost:5000/basket-service/api/baskets \
  -H "Content-Type: application/json" \
  -d '{"userId":"user1","items":[{"productId":"...","quantity":2}]}'
```

**Ordering API:**
```bash
# Siparişleri listele
curl http://localhost:5000/ordering-service/api/orders
```

---

### 3. Swagger UI'dan Test

Her servisin kendi Swagger UI'ı vardır:

1. **Catalog API:** Tarayıcıda `http://localhost:5001/` aç
2. **Basket API:** Tarayıcıda `http://localhost:5002/` aç
3. **Ordering API:** Tarayıcıda `http://localhost:5003/` aç

**Test Adımları:**
1. Endpoint'i seç (örn: `GET /api/categories`)
2. "Try it out" butonuna tıkla
3. "Execute" butonuna tıkla
4. Response'u görüntüle

---

### 4. Docker Container Loglarını İzleme

```bash
# Tüm servislerin loglarını izle
docker compose logs -f

# Belirli bir servisin loglarını izle
docker compose logs -f catalog.api
docker compose logs -f basket.api
docker compose logs -f gateway.api

# Son 50 satırı göster
docker compose logs --tail=50 gateway.api
```

---

## 🛑 Durdurma ve Temizlik

### Tüm Servisleri Durdurma

Tüm servisler Docker container'ları içinde çalıştığı için tek komutla durdurulabilir.

---

### Docker Container'ları Durdurma

#### Yöntem 1: Tüm Container'ları Durdur (Önerilen)

**Komut:**
```bash
cd "/home/kSEN/Desktop/ Projects/microservice-practice-me"
docker compose down
```

**Container'ların Durumunu Kontrol Et:**
```bash
# Durdurma öncesi durum
docker compose ps

# Durdurma işlemi
docker compose down

# Durdurma sonrası kontrol
docker compose ps
# Hiçbir container görünmemeli
```

**Ne Yapar:**
- Tüm container'ları durdurur
- Container'ları kaldırır (remove)
- Network'leri temizler
- **Volume'ları korur** (veritabanı verileri kalır)

**Kontrol:**
```bash
docker ps
# Hiçbir container görünmemeli (veya sadece başka projelerin container'ları)
```

---

#### Yöntem 2: Container'ları Durdur ama Kaldırma (Restart için)

**Komut:**
```bash
docker compose stop
```

**Ne Yapar:**
- Container'ları durdurur ama kaldırmaz
- Daha hızlı yeniden başlatılabilir
- Volume'lar ve network'ler korunur

**Yeniden Başlatma:**
```bash
docker compose start
```

---

#### Yöntem 3: Belirli Bir Container'ı Durdur

Tek bir container'ı durdurmak istediğinizde bu yöntemi kullanın.

**Docker Compose ile Durdurma (Önerilen):**
```bash
# Catalog veritabanını durdur
docker compose stop catalogdb

# Ordering veritabanını durdur
docker compose stop orderingdb

# Discount veritabanını durdur
docker compose stop discountdb

# Basket PostgreSQL durdur
docker compose stop basketpostgres

# Basket (Redis) durdur
docker compose stop basketdb

# RabbitMQ durdur
docker compose stop messagebroker

# pgAdmin durdur
docker compose stop pgadmin
```

**Docker CLI ile Durdurma:**
```bash
# Catalog veritabanını durdur
docker stop catalogdb

# Ordering veritabanını durdur
docker stop orderingdb

# Discount veritabanını durdur
docker stop discountdb

# Basket PostgreSQL durdur
docker stop basketpostgres

# Basket (Redis) durdur
docker stop basketdb

# RabbitMQ durdur
docker stop messagebroker

# pgAdmin durdur
docker stop pgadmin
```

**Container Durumunu Kontrol Etme:**
```bash
# Belirli container'ın durumunu kontrol et
docker ps -a | grep catalogdb
# Status: Exited olmalı

# Docker Compose ile kontrol
docker compose ps catalogdb

# Detaylı durum bilgisi
docker inspect catalogdb --format='{{.State.Status}}'
# Beklenen: exited
```

**Birden Fazla Container'ı Durdurma:**
```bash
# Birden fazla container'ı tek komutla durdur
docker compose stop catalogdb orderingdb discountdb

# Tüm veritabanı container'larını durdur
docker compose stop catalogdb orderingdb discountdb basketpostgres

# Altyapı container'larını durdur (Redis, RabbitMQ)
docker compose stop basketdb messagebroker
```

**Container'ı Durdur ve Kaldır:**
```bash
# Container'ı durdur ve kaldır (ama volume korunur)
docker compose rm -s catalogdb
# -s: Durdurma işlemi yap (stop)

# Veya önce durdur, sonra kaldır
docker compose stop catalogdb
docker compose rm catalogdb
```

**Container'ı Zorla Durdurma:**
```bash
# Normal durdurma çalışmazsa zorla durdur
docker kill catalogdb

# Veya Docker Compose ile
docker compose kill catalogdb
```

**Örnek Senaryolar:**

**Senaryo 1: Sadece Catalog Veritabanını Yeniden Başlatma**
```bash
# 1. Durdur
docker compose stop catalogdb

# 2. Kaldır (volume korunur)
docker compose rm catalogdb

# 3. Yeniden başlat
docker compose up -d catalogdb

# 4. Durumu kontrol et
docker compose ps catalogdb
```

**Senaryo 2: Veritabanını Temiz Başlangıçla Yeniden Başlatma**
```bash
# 1. Durdur ve kaldır
docker compose stop catalogdb
docker compose rm catalogdb

# 2. Volume'u sil (DİKKAT: Veriler silinir!)
docker volume rm microservice-practice-me_catalogdb_data

# 3. Yeniden başlat
docker compose up -d catalogdb
```

**Senaryo 3: Sadece pgAdmin'i Durdurma (Veritabanları Çalışır Durumda)**
```bash
# pgAdmin'i durdur (veritabanı container'ları çalışmaya devam eder)
docker compose stop pgadmin

# Durumu kontrol et
docker compose ps
# catalogdb, orderingdb, discountdb çalışır durumda olmalı
# pgadmin durmuş olmalı
```

**Senaryo 4: Tüm Veritabanlarını Durdur, Diğerleri Çalışır Durumda Kalsın**
```bash
# Sadece PostgreSQL container'larını durdur
docker compose stop catalogdb orderingdb discountdb basketpostgres

# Redis ve RabbitMQ çalışmaya devam eder
docker compose ps
# basketdb ve messagebroker çalışır durumda olmalı
```

**Not:** Container'ı durdurmak verileri silmez. Volume'lar korunur. Verileri silmek için volume'u da silmeniz gerekir.

---

#### Yöntem 4: Container'ları Durdur ve Volume'ları Sil (⚠️ DİKKATLİ!)

**Komut:**
```bash
docker compose down -v
```

**Ne Yapar:**
- Tüm container'ları durdurur ve kaldırır
- **Tüm volume'ları siler** (veritabanı verileri silinir!)
- Network'leri temizler

**⚠️ UYARI:**
- Bu komut **tüm veritabanı verilerini siler**
- Migration'ları tekrar uygulamanız gerekir
- Seed data tekrar eklenecektir

**Ne Zaman Kullanılır:**
- Temiz bir başlangıç yapmak istediğinizde
- Test verilerini temizlemek için
- Veritabanı şemasını sıfırdan oluşturmak için

---

### Container'ları Yeniden Başlatma

#### Tüm Container'ları Yeniden Başlat

**Komut:**
```bash
docker compose restart
```

**Ne Yapar:**
- Tüm container'ları durdurur ve tekrar başlatır
- Volume'lar ve veriler korunur
- Hızlı yeniden başlatma

---

#### Belirli Bir Container'ı Yeniden Başlat

**Catalog Veritabanını Yeniden Başlat:**
```bash
docker restart catalogdb
# veya
docker compose restart catalogdb
```

**Kontrol:**
```bash
docker ps | grep catalogdb
# Status: Up (healthy) olmalı
```

---

### Tüm Sistemi Durdurma

**Komut:**
```bash
cd "/home/kSEN/Desktop/ Projects/microservice-practice-me"
docker compose down
```

**Kontrol:**
```bash
# Container'lar durdu mu?
docker compose ps
# Hiçbir container görünmemeli

# Veya
docker ps
# Proje container'ları görünmemeli
```

---

### Temizlik İşlemleri

#### Kullanılmayan Container'ları Temizle

```bash
# Durdurulmuş container'ları kaldır
docker container prune

# Onay ister, -f ile otomatik onay
docker container prune -f
```

#### Kullanılmayan Image'ları Temizle

```bash
# Kullanılmayan image'ları kaldır
docker image prune

# Tüm kullanılmayan image'ları kaldır (dikkatli!)
docker image prune -a
```

#### Kullanılmayan Volume'ları Temizle

```bash
# Kullanılmayan volume'ları kaldır
docker volume prune
```

#### Tüm Docker Kaynaklarını Temizle (⚠️ DİKKATLİ!)

```bash
# Tüm durdurulmuş container'lar, network'ler, image'lar, volume'lar
docker system prune -a --volumes

# ⚠️ Bu komut çok agresif, dikkatli kullanın!
```

---

### Durdurma Sonrası Kontroller

**1. API Durdu mu?**
```bash
curl http://localhost:5001/health
# Beklenen: Connection refused veya timeout
```

**2. Container'lar Durdu mu?**
```bash
docker ps
# Catalog ile ilgili container görünmemeli
```

**3. Port'lar Boş mu?**
```bash
netstat -tuln | grep 5001
# veya
ss -tuln | grep 5001
# Çıktı olmamalı
```

---

### Hızlı Durdurma (Özet)

**Tüm Servisleri Durdur:**
```bash
docker compose down
```

**Container'ları Durdur ama Kaldırma (Hızlı Restart için):**
```bash
docker compose stop
# Yeniden başlatmak için: docker compose start
```

---

## 🔧 Sorun Giderme

### Sorun 1: Port Zaten Kullanılıyor

**Hata:**
```
Error response from daemon: ports are not available: exposing port TCP 0.0.0.0:5436 -> 127.0.0.1:0: listen tcp 0.0.0.0:5436: bind: address already in use
```

**Çözüm:**
```bash
# Port'u kullanan process'i bul
sudo lsof -i :5436
# veya
sudo netstat -tuln | grep 5436

# Process'i durdur (PID'yi yukarıdaki komuttan al)
kill <PID>
```

---

### Sorun 2: Container Başlatılamıyor

**Hata:**
```
Container catalogdb  Exited (255)
```

**Çözüm:**
```bash
# Container loglarını kontrol et
docker logs catalogdb

# Container'ı yeniden başlat
docker start catalogdb

# Health check kontrol et
docker exec catalogdb pg_isready -U postgres
```

---

### Sorun 3: Servis Veritabanına Bağlanamıyor

**Hata:**
```
Npgsql.NpgsqlException: Connection refused
```

**Kontrol Adımları:**

1. **Container çalışıyor mu?**
   ```bash
   docker compose ps
   # Tüm container'lar Up (healthy) olmalı
   ```

2. **Veritabanı hazır mı?**
   ```bash
   docker exec catalogdb pg_isready -U postgres
   docker exec orderingdb pg_isready -U postgres
   docker exec discountdb pg_isready -U postgres
   docker exec basketpostgres pg_isready -U postgres
   ```

3. **Connection string doğru mu?**
   - Container network içinde: `Host=catalogdb;Port=5432` (container port)
   - Localhost'tan: `Host=localhost;Port=5436` (host port)
   - Docker Compose environment variables'ı kontrol et

4. **Port erişilebilir mi?**
   ```bash
   netstat -tuln | grep -E "(5436|5435|5434|5437)"
   # veya
   ss -tuln | grep -E "(5436|5435|5434|5437)"
   ```

5. **Container loglarını kontrol et:**
   ```bash
   docker compose logs catalog.api
   docker compose logs basket.api
   ```

---

### Sorun 4: Migration Hatası

**Hata:**
```
Failed to apply migration
```

**Çözüm:**
Migration'lar genellikle servis başlatıldığında otomatik uygulanır. Eğer hata varsa:

```bash
# Container loglarını kontrol et
docker compose logs catalog.api | grep -i migration

# Container içine girip manuel uygula (gerekirse)
docker exec -it catalog.api bash
# Container içinde dotnet ef komutları çalıştırılabilir
```

**Not:** Docker container'ları içinde EF Core tool'ları yüklü olmayabilir. Migration'lar genellikle `Program.cs` içinde `MigrateAsync()` ile otomatik uygulanır.

---

### Sorun 5: Docker Daemon Çalışmıyor

**Hata:**
```
Cannot connect to the Docker daemon
```

**Çözüm:**
```bash
# Docker'ı başlat
sudo systemctl start docker
# veya
sudo service docker start

# Docker'ın çalıştığını kontrol et
docker ps
```

---

## 📊 Container Port Özeti

### Altyapı Servisleri

| Container | Servis | Host Port | Container Port | URL |
|-----------|--------|-----------|----------------|-----|
| `catalogdb` | PostgreSQL | 5436 | 5432 | - |
| `orderingdb` | PostgreSQL | 5435 | 5432 | - |
| `discountdb` | PostgreSQL | 5434 | 5432 | - |
| `basketpostgres` | PostgreSQL | 5437 | 5432 | - |
| `basketdb` | Redis | 6379 | 6379 | - |
| `basketdb` | RedisInsight UI | 8001 | 8001 | http://localhost:8001 |
| `messagebroker` | RabbitMQ AMQP | 5673 | 5672 | - |
| `messagebroker` | RabbitMQ Management | 15673 | 15672 | http://localhost:15673 |
| `pgadmin` | pgAdmin Web UI | 5050 | 80 | http://localhost:5050 |

### Uygulama Servisleri

| Container | Servis | Host Port | Container Port | URL |
|-----------|--------|-----------|----------------|-----|
| `gateway.api` | API Gateway | 5000 | 8080 | http://localhost:5000 |
| `catalog.api` | Catalog API | 5001 | 8080 | http://localhost:5001 |
| `basket.api` | Basket API | 5002 | 8080 | http://localhost:5002 |
| `ordering.api` | Ordering API | 5003 | 8080 | http://localhost:5003 |
| `discount.grpc` | Discount gRPC | 5004 | 8080 | - |
| `discount.grpc` | Discount Health | 5005 | 8081 | http://localhost:5005/health |
| `web.ui` | Web UI (Blazor) | 3000 | 80 | http://localhost:3000 |

---

## 🎯 Hızlı Başlangıç (Özet)

**Tüm Sistemi Ayağa Kaldırma:**
```bash
# 1. Proje dizinine git
cd "/home/kSEN/Desktop/ Projects/microservice-practice-me"

# 2. Tüm container'ları başlat
docker compose up -d

# 3. Container'ların hazır olmasını bekle (30-60 saniye)
# Health check'ler tamamlanana kadar bekleyin
docker compose ps
```

**Test:**
```bash
# Gateway health check
curl http://localhost:5000/health

# Downstream services health check
curl http://localhost:5000/health/downstream

# Catalog API test (Gateway üzerinden)
curl http://localhost:5000/catalog-service/api/categories

# Swagger UI
# Catalog: http://localhost:5001/
# Basket: http://localhost:5002/
# Ordering: http://localhost:5003/

# Web UI
# http://localhost:3000/
```

**Durdurma:**
```bash
docker compose down
```

---

## 📝 Notlar

### Port Çakışması

- Sistemdeki PostgreSQL port 5432'de çalışıyorsa, Docker container'lar farklı portlarda çalışır
- `catalogdb`: Port 5436
- `orderingdb`: Port 5435
- `discountdb`: Port 5434
- `basketpostgres`: Port 5437
- Connection string'de container network içinde container port kullanılır: `Port=5432`
- Localhost'tan bağlanırken host port kullanılır: `Port=5436`

### Veritabanı Bağlantısı

- **Container network içinden:** `Host=catalogdb;Port=5432` (container port)
- **Localhost'tan bağlanırken:** `Host=localhost;Port=5436` (host port)

### Migration ve Seed Data

- Tüm servisler başlatıldığında migration'lar otomatik uygulanır
- Seed data otomatik eklenir (veri yoksa)
- Manuel migration gerekmez

### Gateway Routing

- Gateway tüm servislere tek giriş noktası sağlar
- Route prefix'leri: `/catalog-service`, `/basket-service`, `/ordering-service`
- Gateway route'ları `appsettings.json` içinde tanımlıdır

### Health Checks

- Gateway'in kendi health check'i: `/health` (sadece Gateway durumunu kontrol eder)
- Downstream servislerin health check'i: `/health/downstream` (Catalog, Basket, Ordering)
- Her servisin kendi health check endpoint'i vardır: `http://localhost:PORT/health`

---

**Son Güncelleme:** Aralık 2024

