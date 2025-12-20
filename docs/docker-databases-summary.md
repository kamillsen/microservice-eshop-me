# Docker Ortamındaki Veritabanları

## 📊 Veritabanı Listesi

Docker Compose dosyasından tespit edilen veritabanları:

### PostgreSQL Veritabanları

| Container Adı | Veritabanı Adı | Host Port | Container Port | Kullanıcı | Şifre |
|--------------|----------------|-----------|----------------|-----------|-------|
| **catalogdb** | CatalogDb | 5436 | 5432 | postgres | postgres |
| **orderingdb** | OrderingDb | 5435 | 5432 | postgres | postgres |
| **discountdb** | DiscountDb | 5434 | 5432 | postgres | postgres |
| **basketpostgres** | BasketDb | 5437 | 5432 | postgres | postgres |

### Redis (Key-Value Store)

| Container Adı | Host Port | Container Port | UI Port | Açıklama |
|--------------|-----------|----------------|---------|----------|
| **basketdb** | 6379 | 6379 | 8001 (RedisInsight) | Basket Service için cache (Cache-aside pattern) |

### RabbitMQ (Message Broker)

| Container Adı | AMQP Port | Management UI Port | Kullanıcı | Şifre |
|--------------|-----------|-------------------|-----------|-------|
| **messagebroker** | 5673 | 15673 | guest | guest |

### Yönetim Araçları

| Container Adı | Port | Email | Şifre | Açıklama |
|--------------|------|-------|-------|----------|
| **pgadmin** | 5050 | admin@admin.com | admin | PostgreSQL yönetim arayüzü |

---

## 🔍 Container Durumunu Kontrol Etme

Docker daemon çalışıyorsa aşağıdaki komutları kullanabilirsiniz:

```bash
# Tüm container'ları listele
docker ps -a

# Çalışan container'ları listele
docker ps

# Docker Compose ile container durumunu kontrol et (V2 komutu)
docker compose ps

# Veritabanı container'larını filtrele
docker ps -a | grep -E "(catalog|basket|ordering|discount|postgres|redis|rabbitmq)"

# Belirli bir container'ın durumunu kontrol et
docker ps -a --filter "name=catalogdb"
```

---

## 🔌 Bağlantı Bilgileri

### CatalogDb (PostgreSQL)
```
Host: localhost (veya catalogdb - container network içinde)
Port: 5436 (localhost) veya 5432 (container network)
Database: CatalogDb
Username: postgres
Password: postgres
```

**Connection String Örnekleri:**
- Localhost'tan: `Host=localhost;Port=5436;Database=CatalogDb;Username=postgres;Password=postgres`
- Container network içinden: `Host=catalogdb;Port=5432;Database=CatalogDb;Username=postgres;Password=postgres`

**Localhost'tan bağlanma:**
```bash
psql -h localhost -p 5436 -U postgres -d CatalogDb
```

**Container içinden bağlanma:**
```bash
docker exec -it catalogdb psql -U postgres -d CatalogDb
```

### OrderingDb (PostgreSQL)
```
Host: localhost (veya orderingdb - container network içinde)
Port: 5435 (localhost) veya 5432 (container network)
Database: OrderingDb
Username: postgres
Password: postgres
```

**Connection String Örnekleri:**
- Localhost'tan: `Host=localhost;Port=5435;Database=OrderingDb;Username=postgres;Password=postgres`
- Container network içinden: `Host=orderingdb;Port=5432;Database=OrderingDb;Username=postgres;Password=postgres`

**Localhost'tan bağlanma:**
```bash
psql -h localhost -p 5435 -U postgres -d OrderingDb
```

**Container içinden bağlanma:**
```bash
docker exec -it orderingdb psql -U postgres -d OrderingDb
```

### DiscountDb (PostgreSQL)
```
Host: localhost (veya discountdb - container network içinde)
Port: 5434 (localhost) veya 5432 (container network)
Database: DiscountDb
Username: postgres
Password: postgres
```

**Connection String Örnekleri:**
- Localhost'tan: `Host=localhost;Port=5434;Database=DiscountDb;Username=postgres;Password=postgres`
- Container network içinden: `Host=discountdb;Port=5432;Database=DiscountDb;Username=postgres;Password=postgres`

**Localhost'tan bağlanma:**
```bash
psql -h localhost -p 5434 -U postgres -d DiscountDb
```

**Container içinden bağlanma:**
```bash
docker exec -it discountdb psql -U postgres -d DiscountDb
```

### BasketDb (PostgreSQL)
```
Host: localhost (veya basketpostgres - container network içinde)
Port: 5437 (localhost) veya 5432 (container network)
Database: BasketDb
Username: postgres
Password: postgres
```

**Connection String Örnekleri:**
- Localhost'tan: `Host=localhost;Port=5437;Database=BasketDb;Username=postgres;Password=postgres`
- Container network içinden: `Host=basketpostgres;Port=5432;Database=BasketDb;Username=postgres;Password=postgres`

**Localhost'tan bağlanma:**
```bash
psql -h localhost -p 5437 -U postgres -d BasketDb
```

**Container içinden bağlanma:**
```bash
docker exec -it basketpostgres psql -U postgres -d BasketDb
```

### BasketDb (Redis)
```
Host: localhost (veya basketdb - container network içinde)
Port: 6379
```

**Connection String Örnekleri:**
- Container network içinden: `basketdb:6379`
- Localhost'tan: `localhost:6379`

**Redis CLI ile bağlanma:**
```bash
# Localhost'tan
redis-cli -h localhost -p 6379

# Container içinden
docker exec -it basketdb redis-cli

# Test komutu
redis-cli ping
```

**RedisInsight UI:**
- URL: http://localhost:8001

### RabbitMQ
```
AMQP Host: localhost (veya messagebroker - container network içinde)
AMQP Port: 5673 (localhost) veya 5672 (container network)
Username: guest
Password: guest
```

**Connection String Örnekleri:**
- Container network içinden: `amqp://guest:guest@messagebroker:5672`
- Localhost'tan (MassTransit için): `amqp://guest:guest@localhost:5673`

**Management UI:**
- URL: http://localhost:15673
- Kullanıcı: guest
- Şifre: guest

### pgAdmin
```
URL: http://localhost:5050
Email: admin@admin.com
Password: admin
```

**pgAdmin'de PostgreSQL sunucularını ekleme:**
1. pgAdmin'i aç: http://localhost:5050
2. Servers → Register → Server
3. Her veritabanı için ayrı server kaydı oluştur:

   **CatalogDb:**
   - General → Name: CatalogDb
   - Connection → Host: catalogdb (veya localhost)
   - Connection → Port: 5432 (container port)
   - Connection → Maintenance database: CatalogDb
   - Connection → Username: postgres
   - Connection → Password: postgres

   **OrderingDb:**
   - General → Name: OrderingDb
   - Connection → Host: orderingdb (veya localhost)
   - Connection → Port: 5432
   - Connection → Maintenance database: OrderingDb
   - Connection → Username: postgres
   - Connection → Password: postgres

   **DiscountDb:**
   - General → Name: DiscountDb
   - Connection → Host: discountdb (veya localhost)
   - Connection → Port: 5432
   - Connection → Maintenance database: DiscountDb
   - Connection → Username: postgres
   - Connection → Password: postgres

   **BasketDb:**
   - General → Name: BasketDb
   - Connection → Host: basketpostgres (veya localhost)
   - Connection → Port: 5432
   - Connection → Maintenance database: BasketDb
   - Connection → Username: postgres
   - Connection → Password: postgres

---

## 🚀 Container'ları Başlatma

```bash
# Tüm container'ları başlat (Docker Compose V2 komutu)
docker compose up -d

# Sadece veritabanı container'larını başlat
docker compose up -d catalogdb orderingdb discountdb basketpostgres basketdb messagebroker

# Logları izle
docker compose logs -f

# Belirli bir container'ın loglarını izle
docker compose logs -f catalogdb

# Container'ların durumunu kontrol et
docker compose ps
```

---

## 🛑 Container'ları Durdurma

```bash
# Tüm container'ları durdur (Docker Compose V2 komutu)
docker compose down

# Container'ları durdur + volume'ları sil (VERİLER SİLİNİR!)
docker compose down -v

# Container'ları durdur ve image'ları da sil
docker compose down --rmi local
```

---

## 📦 Volume'lar (Kalıcı Veri)

Verilerin kalıcı olması için volume'lar tanımlanmış:

- `catalogdb_data` → CatalogDb verileri
- `orderingdb_data` → OrderingDb verileri
- `discountdb_data` → DiscountDb verileri
- `basketpostgres_data` → BasketDb verileri (PostgreSQL)
- `basketdb_data` → Redis verileri (Basket Service cache)
- `rabbitmq_data` → RabbitMQ verileri
- `pgadmin_data` → pgAdmin ayarları

**Volume'ları kontrol etme:**
```bash
docker volume ls | grep -E "(catalog|basket|ordering|discount|rabbitmq|pgadmin)"
```

**Volume detaylarını görme:**
```bash
docker volume inspect catalogdb_data
```

---

## ✅ Health Check

Tüm container'ların health check'leri tanımlanmış:

```bash
# Health check durumunu kontrol et
docker ps --format "table {{.Names}}\t{{.Status}}"

# Sağlıklı container'lar
docker ps --filter "health=healthy"
```

