# Docker ve Container Haberleşmesi - Kapsamlı Rehber

> Bu dokümantasyon, Docker, container'lar, port mapping, Docker network ve pgAdmin bağlantıları hakkında öğrenilen bilgileri içerir.

---

## 1. Docker ve Container'lar - Temel Kavramlar

### Docker Nedir? Container Nedir?

**Docker**, uygulamaları ve bağımlılıklarını izole bir ortamda çalıştırmayı sağlar.

- **Container**: Bir uygulamanın çalıştığı izole ortam (küçük bir sanal makine gibi)
- **Image**: Container'ı oluşturan şablon (kalıp)
- **Docker Compose**: Birden fazla container'ı birlikte yönetmek için

### Örnek:

```
Image (Kalıp) → Container (Çalışan Uygulama)
postgres:16-alpine → catalogdb (çalışan PostgreSQL)
```

---

## 2. docker-compose.yml Dosyası

Bu dosya, tüm container'ların yapılandırmasını tek yerde toplar.

### Temel Yapı:

```yaml
services:           # Çalışacak container'lar
  catalogdb:        # Container adı
    image: postgres:16-alpine  # Hangi image kullanılacak
    container_name: catalogdb   # Container'ın adı
    ports:          # Port mapping (PC ↔ Container)
    volumes:        # Veri kalıcılığı
    environment:    # Ortam değişkenleri
```

---

## 3. Port Mapping (Port Eşleştirme) - PC ↔ Container

### Port Mapping Nedir?

Container içindeki bir portu, PC'nin bir portuna bağlar. Böylece PC'den container'a erişebilirsin.

```yaml
ports:
  - "5432:5432"  # PC Port : Container Port
```

### Nasıl Çalışır?

```
┌─────────────────────────────────────┐
│  PC (Senin Bilgisayarın)            │
│                                      │
│  Tarayıcı: localhost:5050           │
│         │                            │
│         │ Port Mapping               │
│         │ "5050:80"                  │
│         ▼                            │
│  ┌──────────────────┐               │
│  │ pgadmin          │               │
│  │ Container        │               │
│  │ Port: 80         │               │
│  └──────────────────┘               │
└─────────────────────────────────────┘
```

**Açıklama:**
- PC'de `localhost:5050` yazarsın
- Docker bunu container'ın `80` portuna yönlendirir
- pgAdmin container'ı `80` portunda çalışıyor

### Başka Örnek:

```yaml
catalogdb:
  ports:
    - "5432:5432"  # PC:5432 → Container:5432
```

```
PC'den: localhost:5432 → catalogdb container'ının 5432 portuna
```

### Detaylı Şema:

```
┌─────────────────────────────────────────────┐
│  PC (Host Machine)                          │
│                                              │
│  localhost:5432 ────────────────────┐       │
│                                      │       │
└──────────────────────────────────────┼───────┘
                                       │
                    Port Mapping       │
                                       │
┌──────────────────────────────────────┼───┐
│  Container (catalogdb)                │   │
│                                      │   │
│  ┌──────────────────────────────┐   │   │
│  │ PostgreSQL                   │   │   │
│  │ Port: 5432 (internal)        │◄──┘   │
│  └──────────────────────────────┘       │
└──────────────────────────────────────────┘
```

---

## 4. Container'lar Arası Haberleşme (Docker Network)

### Docker Network Nedir?

Docker Compose, tüm container'ları otomatik olarak aynı network'e koyar. Container'lar birbirlerini container adıyla bulabilir.

### YML'den Örnek:

```yaml
catalogdb:
  container_name: catalogdb  # Bu isim önemli!
  ports:
    - "5432:5432"

pgadmin:
  container_name: pgadmin
  depends_on:
    - catalogdb  # catalogdb'yi bekler
```

### Nasıl Çalışıyor?

```
┌─────────────────────────────────────────────┐
│  Docker Network (Otomatik Oluşur)           │
│                                              │
│  ┌──────────────┐      ┌──────────────┐    │
│  │  catalogdb   │      │   pgadmin    │    │
│  │  Container   │      │   Container  │    │
│  │              │      │              │    │
│  │ PostgreSQL   │◄─────┤  pgAdmin     │    │
│  │ Port: 5432   │      │  Port: 80    │    │
│  └──────────────┘      └──────────────┘    │
│         ▲                    │              │
│         │                    │              │
│         └────────────────────┘              │
│    "catalogdb:5432" ile bağlanır           │
└─────────────────────────────────────────────┘
```

### pgAdmin'de Ne Yazmalısın?

```
❌ YANLIŞ: localhost:5432
   → pgAdmin container'ının kendi localhost'u
   → PostgreSQL yok orada!

✅ DOĞRU: catalogdb:5432
   → Docker Network "catalogdb" ismini bulur
   → PostgreSQL container'ına bağlanır
```

### Docker Network - DNS Çözümleme

Docker'un kendi DNS sistemi:

```
┌─────────────────────────────────────────────────────────────┐
│           Docker Network (Default Bridge Network)            │
│                                                               │
│  Docker'un kendi DNS sistemi:                                │
│  "catalogdb" → 172.18.0.2 (IP adresi)                       │
│  "pgadmin"   → 172.18.0.5 (IP adresi)                       │
│  "orderingdb"→ 172.18.0.3 (IP adresi)                       │
│                                                               │
│  ┌──────────────────┐         ┌──────────────────┐         │
│  │   catalogdb      │         │    pgadmin        │         │
│  │   Container      │         │    Container      │         │
│  │                  │         │                   │         │
│  │ IP: 172.18.0.2   │◄────────┤ IP: 172.18.0.5    │         │
│  │ Hostname:        │         │ Hostname:         │         │
│  │ catalogdb        │         │ pgadmin           │         │
│  │                  │         │                   │         │
│  │ PostgreSQL       │         │ pgAdmin Web App   │         │
│  │ Port: 5432       │         │ Port: 80          │         │
│  └──────────────────┘         └──────────────────┘         │
│         ▲                              │                      │
│         │                              │                      │
│         └──────────────────────────────┘                      │
│              Docker Network üzerinden                          │
│              "catalogdb:5432" ile bağlanır                     │
└─────────────────────────────────────────────────────────────┘
```

---

## 5. pgAdmin'in PostgreSQL'e Bağlanması - Detaylı Senaryo

### Adım 1: PC'den pgAdmin'e Git

```yaml
pgadmin:
  ports:
    - "5050:80"
```

```
Sen (PC)                    pgAdmin Container
localhost:5050 ────────────► Port 80
(Tarayıcıda açıyorsun)      (Web arayüzü açılıyor)
```

### Adım 2: pgAdmin'de Server Kaydı Oluştur

pgAdmin'de form dolduruyorsun:

```
Name: CatalogDb
Host: catalogdb    ← Container adı!
Port: 5432         ← Container içindeki port
Database: CatalogDb
Username: postgres
Password: postgres
```

### Adım 3: pgAdmin Container → PostgreSQL Container

```
┌─────────────────────────────────────────────┐
│  pgAdmin Container                          │
│  "catalogdb:5432" yazıyor                   │
│         │                                    │
│         │ Docker Network                     │
│         │ (Otomatik DNS)                     │
│         ▼                                    │
│  ┌──────────────────────────────┐          │
│  │  catalogdb Container         │          │
│  │  PostgreSQL Port: 5432       │          │
│  │  ✅ Bağlantı başarılı!       │          │
│  └──────────────────────────────┘          │
└─────────────────────────────────────────────┘
```

### Tam Senaryo - Adım Adım:

```
┌─────────────────────────────────────────────────────────────────┐
│  ADIM 1: PC'den pgAdmin'e Erişim (Port Mapping)                 │
│                                                                  │
│  PC (Tarayıcı)                                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Kullanıcı: http://localhost:5050 yazıyor                │  │
│  └───────────────────────┬──────────────────────────────────┘  │
│                          │                                      │
│                          │ Port Mapping: 5050 → 80              │
│                          ▼                                      │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  pgadmin Container                                        │  │
│  │  Port: 80 (internal)                                     │  │
│  │  pgAdmin Web UI çalışıyor                                │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  ADIM 2: pgAdmin'de Server Kaydı Oluşturma                      │
│                                                                  │
│  pgAdmin Web UI (Container içinde)                              │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Kullanıcı form dolduruyor:                               │  │
│  │  - Name: CatalogDb                                        │  │
│  │  - Host: catalogdb  ← Container adı!                    │  │
│  │  - Port: 5432        ← Container içindeki port           │  │
│  │  - Database: CatalogDb                                    │  │
│  │  - Username: postgres                                     │  │
│  │  - Password: postgres                                     │  │
│  └───────────────────────┬──────────────────────────────────┘  │
│                          │                                      │
│                          │ "Save" butonuna tıklıyor            │
└──────────────────────────┼──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│  ADIM 3: Docker Network DNS Çözümleme                          │
│                                                                  │
│  pgAdmin Container                                              │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  pgAdmin uygulaması:                                       │  │
│  │  "catalogdb:5432" adresine bağlanmaya çalışıyor          │  │
│  │                                                            │  │
│  │  Docker DNS: "catalogdb" nedir?                          │  │
│  │  → 172.18.0.2 (catalogdb container'ının IP'si)          │  │
│  └───────────────────────┬──────────────────────────────────┘  │
│                          │                                      │
│                          │ Docker Network üzerinden             │
│                          │ TCP bağlantısı: 172.18.0.2:5432      │
└──────────────────────────┼──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│  ADIM 4: PostgreSQL Container'a Bağlanma                        │
│                                                                  │
│  catalogdb Container                                            │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  PostgreSQL Server                                        │  │
│  │  Port: 5432 (dinliyor)                                   │  │
│  │                                                            │  │
│  │  Bağlantı geldi: 172.18.0.5:xxxxx → 172.18.0.2:5432     │  │
│  │  (pgadmin'dan catalogdb'ye)                              │  │
│  │                                                            │  │
│  │  Kimlik doğrulama:                                        │  │
│  │  - Username: postgres ✓                                    │  │
│  │  - Password: postgres ✓                                    │  │
│  │  - Database: CatalogDb ✓                                   │  │
│  │                                                            │  │
│  │  ✅ Bağlantı başarılı!                                     │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 6. Neden localhost:5432 Çalışmaz?

### localhost'un Anlamı

- **PC'de**: `localhost` = PC'nin kendisi (127.0.0.1)
- **Container'da**: `localhost` = Container'ın kendisi

```
┌─────────────────────────────────────────────────────────────┐
│  pgAdmin Container içinden                                  │
│                                                              │
│  localhost:5432 yazarsan:                                   │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  "localhost" = pgAdmin container'ının kendisi         │  │
│  │  → pgAdmin container'ında PostgreSQL yok!            │  │
│  │  → ❌ Bağlantı hatası                                │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
│  catalogdb:5432 yazarsan:                                   │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Docker DNS: "catalogdb" → 172.18.0.2               │  │
│  │  → PostgreSQL container'ına bağlanır               │  │
│  │  → ✅ Bağlantı başarılı                             │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## 7. PC'den Container'lara Erişim

### Senaryo 1: Tarayıcıdan Erişim

```
PC (Tarayıcı)
    │
    │ http://localhost:5050
    ▼
Port Mapping (5050:80)
    │
    ▼
pgadmin Container (Port 80)
```

### Senaryo 2: Terminal'den Erişim

```bash
# PC'den container'a komut çalıştırma
docker exec -it catalogdb psql -U postgres -d CatalogDb

# Bu komut:
# 1. catalogdb container'ına gir
# 2. psql komutunu çalıştır
```

---

## 8. Volume'lar (Veri Kalıcılığı)

### Volume Nedir?

Container silinse bile verilerin kalmasını sağlar. Veriler PC'de (Docker volume'larında) saklanır.

```yaml
volumes:
  - catalogdb_data:/var/lib/postgresql/data
```

```
┌─────────────────────────────────────────┐
│  PC (Docker Volume)                     │
│  catalogdb_data (veriler burada)       │
│         ▲                               │
│         │ Mount (bağla)                 │
│         │                                │
│  ┌──────┴──────────┐                    │
│  │ catalogdb       │                    │
│  │ Container       │                    │
│  │ /var/lib/...    │                    │
│  └─────────────────┘                    │
└─────────────────────────────────────────┘
```

Container silinse bile, volume'daki veriler kalır.

---

## 9. depends_on (Bağımlılık Yönetimi)

### depends_on Nedir?

Bir container'ın başlaması için önce başka container'ların hazır olmasını sağlar.

```yaml
pgadmin:
  depends_on:
    - catalogdb
    - orderingdb
    - discountdb
```

Bu, şu anlama gelir:
1. Önce `catalogdb`, `orderingdb`, `discountdb` başlar
2. Sonra `pgadmin` başlar

---

## 10. Port Mapping vs Docker Network - Fark

### Port Mapping (PC → Container)

```yaml
pgadmin:
  ports:
    - "5050:80"
```

**Kullanım:** PC'den container'a erişim
- PC'de: `localhost:5050`
- Container'da: `80` portu

```
PC ──Port Mapping──► Container
```

### Docker Network (Container → Container)

```yaml
catalogdb:
  container_name: catalogdb

pgadmin:
  depends_on:
    - catalogdb
```

**Kullanım:** Container'dan container'a erişim
- pgAdmin'de: `catalogdb:5432` (container adı)
- `localhost` kullanılmaz

```
Container ──Docker Network──► Container
```

### Özet Tablo

| Durum | YML Örneği | Nasıl Kullanılır | Açıklama |
|-------|-----------|------------------|----------|
| **PC → Container** | `ports: "5050:80"` | `localhost:5050` | PC'den container'a erişim |
| **Container → Container** | `container_name: catalogdb` | `catalogdb:5432` | Container'dan container'a erişim |

---

## 11. Tüm Sistem Mimarisi - Detaylı Şema

```
╔═══════════════════════════════════════════════════════════════════════╗
║                    PC (Host Machine - Fedora Linux)                    ║
╠═══════════════════════════════════════════════════════════════════════╣
║                                                                        ║
║  ┌────────────────────────────────────────────────────────────────┐  ║
║  │  Kullanıcı Erişim Noktaları                                    │  ║
║  │                                                                 │  ║
║  │  🌐 Tarayıcı:                                                  │  ║
║  │     http://localhost:5050  → pgAdmin                           │  ║
║  │     http://localhost:8001  → RedisInsight                      │  ║
║  │     http://localhost:15673 → RabbitMQ Management               │  ║
║  │                                                                 │  ║
║  │  💻 Terminal:                                                  │  ║
║  │     docker exec -it catalogdb psql ...                        │  ║
║  │     docker exec -it basketdb redis-cli ...                    │  ║
║  └───────────────────────┬──────────────────────────────────────┘  ║
║                          │                                           ║
║                          │ Port Mapping (PC ↔ Container)            ║
║                          │                                           ║
╚══════════════════════════╪═══════════════════════════════════════════╝
                           │
                           │ Docker Engine
                           │
╔══════════════════════════╪═══════════════════════════════════════════╗
║                          │                                           ║
║  ┌───────────────────────┴───────────────────────────────────────┐  ║
║  │         Docker Network (Default Bridge Network)                │  ║
║  │                                                                 │  ║
║  │  DNS Çözümleme:                                                │  ║
║  │  "catalogdb"  → 172.18.0.2                                     │  ║
║  │  "pgadmin"    → 172.18.0.5                                     │  ║
║  │  "orderingdb" → 172.18.0.3                                     │  ║
║  │  "basketdb"   → 172.18.0.4                                     │  ║
║  │                                                                 │  ║
║  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐        ║
║  │  │  catalogdb   │  │   pgadmin    │  │  orderingdb  │        ║
║  │  │              │  │              │  │              │        ║
║  │  │ PostgreSQL   │  │  pgAdmin     │  │  PostgreSQL  │        ║
║  │  │ Port: 5432   │  │  Port: 80    │  │  Port: 5432   │        ║
║  │  │              │  │              │  │              │        ║
║  │  │ PC: 5432     │  │  PC: 5050    │  │  PC: 5435     │        ║
║  │  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘        ║
║  │         │                 │                 │                 ║
║  │         │  Docker Network │                 │                 ║
║  │         │  (container adıyla)              │                 ║
║  │         └─────────────────┴─────────────────┘                 ║
║  │                                                                 │  ║
║  │  ┌──────────────┐  ┌──────────────┐                           ║
║  │  │  basketdb    │  │ messagebroker│                           ║
║  │  │              │  │              │                           ║
║  │  │  Redis       │  │  RabbitMQ    │                           ║
║  │  │  Port: 6379  │  │  Port: 5672  │                           ║
║  │  │              │  │              │                           ║
║  │  │  PC: 6379    │  │  PC: 5673    │                           ║
║  │  └──────────────┘  └──────────────┘                           ║
║  └──────────────────────────────────────────────────────────────┘  ║
║                                                                     ║
║  ┌──────────────────────────────────────────────────────────────┐  ║
║  │  Docker Volumes (PC'de saklanan veriler)                    │  ║
║  │                                                               │  ║
║  │  catalogdb_data    → /var/lib/docker/volumes/...            │  ║
║  │  orderingdb_data   → /var/lib/docker/volumes/...            │  ║
║  │  discountdb_data   → /var/lib/docker/volumes/...            │  ║
║  │  basketdb_data     → /var/lib/docker/volumes/...            │  ║
║  │  rabbitmq_data     → /var/lib/docker/volumes/...            │  ║
║  │  pgadmin_data      → /var/lib/docker/volumes/...            │  ║
║  └──────────────────────────────────────────────────────────────┘  ║
╚═══════════════════════════════════════════════════════════════════╝
```

### Basit Şema:

```
┌─────────────────────────────────────────────────┐
│  PC (Senin Bilgisayarın)                       │
│                                                 │
│  Tarayıcı: localhost:5050 ────┐                │
│                                │                │
│                                │ Port Mapping   │
│                                │ 5050 → 80      │
└────────────────────────────────┼────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────┐
│  Docker Network                                 │
│                                                 │
│  ┌──────────────┐         ┌──────────────┐   │
│  │  catalogdb   │         │   pgadmin    │   │
│  │              │         │              │   │
│  │ PostgreSQL   │◄────────┤  pgAdmin     │   │
│  │ Port: 5432   │         │  Port: 80    │   │
│  │              │         │              │   │
│  │ PC: 5432     │         │  PC: 5050    │   │
│  └──────────────┘         └──────────────┘   │
│         ▲                      │              │
│         │                      │              │
│         └──────────────────────┘              │
│    Docker Network (container adıyla)          │
└─────────────────────────────────────────────────┘
```

---

## 12. İletişim Yolları Özeti

### Yol 1: PC → Container (Port Mapping)

```
PC (localhost:5050) 
    │
    │ Port Mapping: 5050 → 80
    ▼
pgadmin Container (Port 80)
```

**Kullanım:** Tarayıcıdan `http://localhost:5050` açılır.

---

### Yol 2: Container → Container (Docker Network)

```
pgadmin Container
    │
    │ Docker Network DNS: "catalogdb" → 172.18.0.2
    │ Bağlantı: catalogdb:5432
    ▼
catalogdb Container (Port 5432)
```

**Kullanım:** pgAdmin'de Host = `catalogdb`, Port = `5432`.

---

### Yol 3: PC → Container (Terminal)

```
PC Terminal
    │
    │ docker exec komutu
    ▼
catalogdb Container
    │
    │ psql komutu çalıştırılır
    ▼
PostgreSQL (Container içinde)
```

**Kullanım:** `docker exec -it catalogdb psql -U postgres -d CatalogDb`

---

## 13. Pratik Örnekler

### Örnek 1: pgAdmin'den CatalogDb'ye Bağlanma

```yaml
# docker-compose.yml
catalogdb:
  container_name: catalogdb
  ports:
    - "5432:5432"

pgadmin:
  container_name: pgadmin
  ports:
    - "5050:80"
```

**Adımlar:**
1. Tarayıcıda: `http://localhost:5050` (Port mapping)
2. pgAdmin'de:
   - Host: `catalogdb` (Container adı, Docker Network)
   - Port: `5432` (Container içindeki port)

### Örnek 2: Terminal'den PostgreSQL'e Bağlanma

```bash
docker exec -it catalogdb psql -U postgres -d CatalogDb
```

Bu komut:
- `catalogdb` container'ına gir
- `psql` komutunu çalıştır
- PostgreSQL'e bağlan

### Örnek 3: pgAdmin'de Tüm Veritabanlarına Bağlanma

**CatalogDb:**
- General Tab → Name: `CatalogDb`
- Connection Tab:
  - Host name/address: `catalogdb` (container adı)
  - Port: `5432` (container içindeki port)
  - Maintenance database: `CatalogDb`
  - Username: `postgres`
  - Password: `postgres`
  - Save password?: ON

**OrderingDb:**
- General Tab → Name: `OrderingDb`
- Connection Tab:
  - Host name/address: `orderingdb` (container adı)
  - Port: `5432` (container içindeki port)
  - Maintenance database: `OrderingDb`
  - Username: `postgres`
  - Password: `postgres`
  - Save password?: ON

**DiscountDb:**
- General Tab → Name: `DiscountDb`
- Connection Tab:
  - Host name/address: `discountdb` (container adı)
  - Port: `5432` (container içindeki port)
  - Maintenance database: `DiscountDb`
  - Username: `postgres`
  - Password: `postgres`
  - Save password?: ON

**Önemli Notlar:**
- pgAdmin sadece PostgreSQL için çalışır. Redis için RedisInsight UI kullanılır.
- Host olarak container adlarını (`catalogdb`, `orderingdb`, `discountdb`) kullanın, `localhost` değil.
- Port her zaman `5432` (container içindeki port). Host'taki portlar (5432, 5434, 5435) değil.

---

## 14. Önemli Noktalar

### 1. Port Mapping vs Docker Network

| Durum | Kullanım | Örnek |
|-------|----------|-------|
| PC → Container | Port mapping | `localhost:5050` → `pgadmin:80` |
| Container → Container | Docker network | `catalogdb:5432` (container adı) |

### 2. localhost'un Anlamı

- **PC'de**: `localhost` = PC'nin kendisi
- **Container'da**: `localhost` = Container'ın kendisi
- **Container'dan başka container'a**: Container adı kullanılır (`catalogdb`)

### 3. DNS Çözümleme

Docker, container adlarını otomatik olarak IP adreslerine çevirir:
- `catalogdb` → `172.18.0.2`
- `pgadmin` → `172.18.0.5`

### 4. depends_on'un Rolü

```yaml
pgadmin:
  depends_on:
    - catalogdb
```

Bu, şu anlama gelir:
- `catalogdb` başlamadan `pgadmin` başlamaz
- `catalogdb` hazır olduğunda `pgadmin` başlar
- Ancak `catalogdb`'nin tamamen hazır olmasını beklemez (healthcheck ile kontrol edilebilir)

---

## 15. Sonuç ve Özet

### Temel Kurallar:

1. **PC'den container'a**: Port mapping (`localhost:5050`)
2. **Container'dan container'a**: Docker network (`catalogdb:5432`)
3. **DNS**: Docker otomatik olarak container adlarını IP'ye çevirir
4. **Volume**: Veriler PC'de kalıcı olarak saklanır

### Özet Tablo:

| Durum | YML Örneği | Nasıl Kullanılır | Açıklama |
|-------|-----------|------------------|----------|
| **PC → Container** | `ports: "5050:80"` | `localhost:5050` | PC'den container'a erişim |
| **Container → Container** | `container_name: catalogdb` | `catalogdb:5432` | Container'dan container'a erişim |

### Önemli Hatırlatmalar:

- ❌ Container içinden `localhost:5432` → Container'ın kendi localhost'u
- ✅ Container içinden `catalogdb:5432` → Docker Network üzerinden bağlanır
- ✅ PC'den `localhost:5050` → Port mapping ile container'a erişim

---

**Son Güncelleme:** Aralık 2024

