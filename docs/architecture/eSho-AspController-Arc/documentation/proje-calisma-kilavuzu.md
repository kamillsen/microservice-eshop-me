# Proje Çalıştırma Kılavuzu

> Bu doküman, e-ticaret microservice projesini nasıl çalıştıracağınızı adım adım açıklar.
> 
> **İçerik:**
> - Önkoşullar
> - Docker Container'ları Ayağa Kaldırma
> - Catalog API'yi Çalıştırma
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
cd /home/kSEN/Desktop/ Projects/microservice-practice-me
docker compose up -d
```

**Ne Yapar?**
- Tüm container'ları arka planda (`-d` flag) başlatır
- Bağımlılıkları (`depends_on`) otomatik çözer
- Container'lar hazır olana kadar bekler

**Başlatılan Container'lar:**
- `catalogdb` → PostgreSQL (port 5436)
- `orderingdb` → PostgreSQL (port 5435)
- `discountdb` → PostgreSQL (port 5434)
- `basketdb` → Redis (port 6379, RedisInsight UI: 8001)
- `messagebroker` → RabbitMQ (AMQP: 5673, Management UI: 15673)
- `pgadmin` → pgAdmin Web UI (port 5050)

**Kontrol:**
```bash
docker ps
# Tüm container'lar "Up" durumunda olmalı
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

#### 2.4. Basket Veritabanı (Redis)

**Ne İşe Yarar:**
- Basket Service'in cache veritabanı (henüz oluşturulmadı)
- Sepet bilgilerini geçici olarak saklar

**Komut:**
```bash
docker compose up -d basketdb
```

**Portlar:**
- Redis: `6379`
- RedisInsight UI: `8001` (http://localhost:8001)

**Kontrol:**
```bash
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

**Not:** pgAdmin başlatıldığında bağımlı olduğu veritabanı container'ları (`catalogdb`, `orderingdb`, `discountdb`) da otomatik başlatılır (`depends_on`).

**Port:** `5050` (http://localhost:5050)

**Giriş Bilgileri:**
- Email: `admin@admin.com`
- Password: `admin`

**Veritabanı Bağlantısı Ekleme:**
1. Tarayıcıda `http://localhost:5050` aç
2. Giriş yap (`admin@admin.com` / `admin`)
3. "Add New Server" tıkla
4. **General tab:**
   - Name: `CatalogDb`
5. **Connection tab:**
   - Host: `catalogdb` (Docker container adı)
   - Port: `5432` (container içindeki port, host port değil!)
   - Database: `CatalogDb`
   - Username: `postgres`
   - Password: `postgres`
   - "Save password" işaretle
6. "Save" tıkla

**Önemli:** Host olarak `catalogdb` kullanın (localhost değil). pgAdmin Docker container içinde çalıştığı için container network'ünde `catalogdb` adını çözümler.

---

### Container Durumlarını Kontrol Etme

**Tüm Container'ları Listele:**
```bash
docker ps
# veya
docker compose ps
```

**Belirli Bir Container'ı Kontrol Et:**
```bash
docker ps | grep catalogdb
```

**Container Loglarını Görüntüleme:**
```bash
# Tüm container logları
docker compose logs

# Belirli bir container logları
docker compose logs catalogdb

# Canlı log takibi (follow)
docker compose logs -f catalogdb
```

**Container Health Check:**
```bash
docker ps --format "table {{.Names}}\t{{.Status}}"
# "healthy" veya "Up" durumunda olmalı
```

---

## 🚀 Catalog API'yi Çalıştırma

Docker container'ları hazır olduktan sonra Catalog API'yi çalıştırabilirsiniz.

### Adım 1: Proje Dizinine Git

```bash
cd "/home/kSEN/Desktop/ Projects/microservice-practice-me/src/Services/Catalog/Catalog.API"
```

### Adım 2: API'yi Çalıştır

**Komut:**
```bash
dotnet run --urls "http://localhost:5001"
```

**Ne Yapar?**
- Catalog API'yi `http://localhost:5001` adresinde başlatır
- Migration'ları otomatik uygular (`MigrateAsync()`)
- Seed data'yı ekler (yoksa)
- API hazır olana kadar bekler

**Başarılı Başlatma Çıktısı:**
```
Now listening on: http://localhost:5001
Application started. Press Ctrl+C to shut down.
```

**Alternatif (Proje Root'tan):**
```bash
cd "/home/kSEN/Desktop/ Projects/microservice-practice-me"
dotnet run --project src/Services/Catalog/Catalog.API/Catalog.API.csproj --urls "http://localhost:5001"
```

---

### API Durumunu Kontrol Etme

**1. Health Check:**
```bash
curl http://localhost:5001/health
# Beklenen: Healthy
```

**2. API Endpoint'leri:**
```bash
# Kategorileri listele
curl http://localhost:5001/api/categories

# Ürünleri listele
curl http://localhost:5001/api/products

# Health check
curl http://localhost:5001/health
```

**3. Swagger UI:**
Tarayıcıda aç: `http://localhost:5001/`

- Tüm API endpoint'lerini görüntüleyebilirsiniz
- "Try it out" butonu ile API'leri test edebilirsiniz
- Request/Response örnekleri otomatik gösterilir

---

## 🧪 Test Etme

### 1. Health Check Testi

```bash
curl http://localhost:5001/health
```

**Beklenen Çıktı:**
```
Healthy
```

**Hata Durumu:**
- Container durmuşsa → `Unhealthy` veya hata
- Veritabanı bağlantısı yoksa → `Unhealthy`

---

### 2. API Endpoint Testleri

**Kategorileri Listele:**
```bash
curl http://localhost:5001/api/categories
```

**Beklenen Çıktı:**
```json
[
  {"id":"...","name":"Giyim"},
  {"id":"...","name":"Elektronik"},
  {"id":"...","name":"Ev & Yaşam"}
]
```

**Ürünleri Listele:**
```bash
curl http://localhost:5001/api/products
```

**Belirli Bir Ürünü Getir:**
```bash
# Önce bir ürün ID'si al
PRODUCT_ID=$(curl -s http://localhost:5001/api/products | jq -r '.[0].id')
curl http://localhost:5001/api/products/$PRODUCT_ID
```

---

### 3. Swagger UI'dan Test

1. Tarayıcıda `http://localhost:5001/` aç
2. Endpoint'i seç (örn: `GET /api/categories`)
3. "Try it out" butonuna tıkla
4. "Execute" butonuna tıkla
5. Response'u görüntüle

---

## 🛑 Durdurma ve Temizlik

### API'yi Durdurma

#### Yöntem 1: Terminal'de Durdurma (Önerilen)

**API çalıştığı terminal'de:**
- `Ctrl+C` tuşlarına bas
- API durur ve terminal kontrolü size geri döner

**Ne Yapar:**
- API process'ini güvenli şekilde sonlandırır
- Açık bağlantıları kapatır
- Veritabanı bağlantılarını temizler

---

#### Yöntem 2: Process'i Bulup Durdurma

**API başka bir terminal'de çalışıyorsa veya arka planda çalışıyorsa:**

```bash
# 1. Process'i bul
ps aux | grep dotnet | grep Catalog

# Çıktı örneği:
# kSEN  12345  0.5  2.1  ...  dotnet run --urls "http://localhost:5001"

# 2. Process ID'sini al (ikinci sütun: 12345)
# 3. Process'i durdur
kill 12345

# Veya tek komutta:
ps aux | grep "[d]otnet.*Catalog" | awk '{print $2}' | xargs kill
```

**Zorla Durdurma (gerekirse):**
```bash
# Process ID'sini bul
PID=$(ps aux | grep "[d]otnet.*Catalog" | awk '{print $2}')

# Zorla durdur
kill -9 $PID
```

**Kontrol:**
```bash
# Process durdu mu kontrol et
ps aux | grep dotnet | grep Catalog
# Hiçbir çıktı olmamalı
```

---

### Docker Container'ları Durdurma

#### Yöntem 1: Tüm Container'ları Durdur (Önerilen)

**Komut:**
```bash
cd "/home/kSEN/Desktop/ Projects/microservice-practice-me"
docker compose down
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

**Catalog Veritabanını Durdur:**
```bash
docker stop catalogdb
# veya
docker compose stop catalogdb
```

**Diğer Container'lar:**
```bash
# Ordering veritabanı
docker stop orderingdb

# Discount veritabanı
docker stop discountdb

# Basket (Redis)
docker stop basketdb

# RabbitMQ
docker stop messagebroker

# pgAdmin
docker stop pgadmin
```

**Kontrol:**
```bash
docker ps -a | grep catalogdb
# Status: Exited olmalı
```

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

### Tüm Sistemi Durdurma (API + Container'lar)

**Adım 1: API'yi Durdur**
```bash
# Terminal'de Ctrl+C
# veya
ps aux | grep "[d]otnet.*Catalog" | awk '{print $2}' | xargs kill
```

**Adım 2: Container'ları Durdur**
```bash
cd "/home/kSEN/Desktop/ Projects/microservice-practice-me"
docker compose down
```

**Kontrol:**
```bash
# API durdu mu?
ps aux | grep dotnet | grep Catalog
# Çıktı olmamalı

# Container'lar durdu mu?
docker ps
# Catalog ile ilgili container görünmemeli
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

**API'yi Durdur:**
```bash
# Terminal'de Ctrl+C
# veya
pkill -f "dotnet.*Catalog"
```

**Container'ları Durdur:**
```bash
docker compose down
```

**Tümünü Durdur:**
```bash
# API
pkill -f "dotnet.*Catalog"

# Container'lar
docker compose down
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

### Sorun 3: API Veritabanına Bağlanamıyor

**Hata:**
```
Npgsql.NpgsqlException: Connection refused
```

**Kontrol Adımları:**

1. **Container çalışıyor mu?**
   ```bash
   docker ps | grep catalogdb
   ```

2. **Veritabanı hazır mı?**
   ```bash
   docker exec catalogdb pg_isready -U postgres
   ```

3. **Connection string doğru mu?**
   - `appsettings.json` dosyasını kontrol et
   - Port: `5436` (host port)
   - Host: `localhost`

4. **Port erişilebilir mi?**
   ```bash
   netstat -tuln | grep 5436
   # veya
   ss -tuln | grep 5436
   ```

---

### Sorun 4: Migration Hatası

**Hata:**
```
Failed to apply migration
```

**Çözüm:**
```bash
# Migration'ları manuel uygula
cd src/Services/Catalog/Catalog.API
export DOTNET_ROOT=/usr/lib64/dotnet
dotnet ef database update --startup-project . --context CatalogDbContext
```

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

| Container | Servis | Host Port | Container Port | URL |
|-----------|--------|-----------|----------------|-----|
| `catalogdb` | PostgreSQL | 5436 | 5432 | - |
| `orderingdb` | PostgreSQL | 5435 | 5432 | - |
| `discountdb` | PostgreSQL | 5434 | 5432 | - |
| `basketdb` | Redis | 6379 | 6379 | - |
| `basketdb` | RedisInsight UI | 8001 | 8001 | http://localhost:8001 |
| `messagebroker` | RabbitMQ AMQP | 5673 | 5672 | - |
| `messagebroker` | RabbitMQ Management | 15673 | 15672 | http://localhost:15673 |
| `pgadmin` | pgAdmin Web UI | 5050 | 80 | http://localhost:5050 |

---

## 🎯 Hızlı Başlangıç (Özet)

**Tüm Sistemi Ayağa Kaldırma:**
```bash
# 1. Container'ları başlat
cd "/home/kSEN/Desktop/ Projects/microservice-practice-me"
docker compose up -d

# 2. Container'ların hazır olmasını bekle (5-10 saniye)
sleep 10

# 3. Catalog API'yi başlat
cd src/Services/Catalog/Catalog.API
dotnet run --urls "http://localhost:5001"
```

**Test:**
```bash
# Health check
curl http://localhost:5001/health

# API test
curl http://localhost:5001/api/categories

# Swagger UI
# Tarayıcıda: http://localhost:5001/
```

**Durdurma:**
```bash
# API: Terminal'de Ctrl+C
# Container'lar:
docker compose down
```

---

## 📝 Notlar

### Port Çakışması

- Sistemdeki PostgreSQL port 5432'de çalışıyorsa, Docker container'lar farklı portlarda çalışır
- `catalogdb`: Port 5436 (sistem PostgreSQL ile çakışmayı önlemek için)
- Connection string'de host port kullanılır: `Port=5436`

### Veritabanı Bağlantısı

- **Localhost'tan bağlanırken:** `Host=localhost;Port=5436` (host port)
- **Container network içinden:** `Host=catalogdb;Port=5432` (container port)

### Migration ve Seed Data

- Catalog API başlatıldığında migration'lar otomatik uygulanır
- Seed data otomatik eklenir (veri yoksa)
- Manuel migration gerekmez

---

**Son Güncelleme:** Aralık 2024

