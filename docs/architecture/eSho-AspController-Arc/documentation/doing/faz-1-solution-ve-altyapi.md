# Projeye başlarken ilk yapılacak: Faz 1 - Solution & Altyapı

## 1.1 Solution ve Temel Proje Yapısı

**Hedef:** Solution oluştur, klasör yapısını kur

### Görevler:

#### Solution dosyası oluştur
```bash
dotnet new sln -n EShop
```

#### Klasör yapısını oluştur
```bash
mkdir -p src/Services
mkdir -p src/ApiGateway
mkdir -p src/BuildingBlocks
mkdir -p tests
```

**Açıklama:**
- `src/Services/` (Catalog, Basket, Ordering, Discount için)
- `src/ApiGateway/` (Gateway için)
- `src/BuildingBlocks/` (Paylaşılan kod için)
- `tests/` (Test projeleri için)

#### global.json kontrol et/oluştur
- .NET 9 SDK: `"version": "9.0.112"`

#### Directory.Build.props oluştur
- Ortak MSBuild ayarları: TargetFramework, Nullable, vb.

#### Directory.Packages.props oluştur
- Central Package Management: tüm paket versiyonları burada

### Kontrol:
- Solution açılıyor mu? (`dotnet sln list`)
- Klasörler doğru mu?

---

## 1.2 Docker Compose (Sadece Infrastructure)

**Hedef:** Veritabanları, Redis, RabbitMQ container'larını ayağa kaldır

### Görevler:

#### docker-compose.yml oluştur

#### PostgreSQL container'ları ekle
- CatalogDb (port: 5432)
- OrderingDb (port: 5435 - 5433 kullanılıyordu, 5435'e değiştirildi)
- DiscountDb (port: 5434)

#### Redis container ekle
- Redis Stack + RedisInsight UI
- Port: 6379 (Redis), 8001 (RedisInsight UI)

#### RabbitMQ container ekle
- Management UI ile
- Port: 5673 (AMQP - 5672 kullanılıyordu, 5673'e değiştirildi), 15673 (Management UI - 15672 kullanılıyordu, 15673'e değiştirildi)

#### pgAdmin container ekle (opsiyonel ama önerilir)
- PostgreSQL veritabanlarına web üzerinden erişim için
- Port: 5050:80 (Web UI)
- Email: admin@admin.com, Password: admin
- depends_on: catalogdb, orderingdb, discountdb

#### Volume'ları tanımla
- Veri kalıcılığı için (catalogdb_data, basketdb_data, orderingdb_data, discountdb_data, rabbitmq_data, pgadmin_data)

#### .env dosyası oluştur (opsiyonel)

### Test:
- `docker-compose up -d` → Tüm container'lar çalışıyor mu?
- PostgreSQL: `docker exec -it catalogdb psql -U postgres -d CatalogDb`
- Redis: `docker exec -it basketdb redis-cli ping`
- RabbitMQ: http://localhost:15673 (guest/guest - 15672 kullanılıyordu, 15673'e değiştirildi)
- pgAdmin: http://localhost:5050 (admin@admin.com / admin)
  - Her PostgreSQL veritabanı için ayrı server kaydı oluştur:
    - CatalogDb: host=catalogdb, port=5432
    - OrderingDb: host=orderingdb, port=5432
    - DiscountDb: host=discountdb, port=5432

**Sonuç:** ✅ Altyapı hazır, servisler için hazırız

---

## Özet: İlk adımlar sırası

1. Solution oluştur (`EShop.sln`)
2. Klasör yapısını oluştur (`src/`, `tests/`)
3. `global.json` ayarla (.NET 9 SDK)
4. `Directory.Build.props` oluştur (ortak build ayarları)
5. `Directory.Packages.props` oluştur (merkezi paket yönetimi)
6. `docker-compose.yml` oluştur (sadece infrastructure)
7. PostgreSQL container'ları ekle (3 adet)
8. Redis container ekle
9. RabbitMQ container ekle
10. pgAdmin container ekle (PostgreSQL yönetimi için)
11. Volume'ları tanımla
12. Test et (tüm container'lar çalışıyor mu?)

**Bu adımlar tamamlandıktan sonra Faz 2'ye (BuildingBlocks) geçilebilir.**
