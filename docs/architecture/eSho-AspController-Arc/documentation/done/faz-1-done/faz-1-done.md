# Faz 1 - Solution & Altyapı Notları

> Bu dosya, Faz 1'i adım adım yaparken öğrendiklerimi not aldığım dosyadır.

---

## Solution Dosyası (.sln) Nedir?

### `dotnet new sln -n EShop` Komutu Ne İşe Yarar?

Bu komut, proje adı `EShop` olan bir **.NET Solution dosyası** (`EShop.sln`) oluşturur.

### Solution Dosyası Nedir?

**Solution dosyası**, birden fazla projeyi tek bir çatı altında toplayan bir **kök dosya**dır. Microservice projelerinde çok sayıda proje olduğu için bu dosya gereklidir.

### Neden Gerekli?

1. **Birden Fazla Projeyi Yönetmek**
   - Catalog.API
   - Basket.API
   - Ordering.API
   - Discount.Grpc
   - Gateway.API
   - BuildingBlocks.Exceptions
   - BuildingBlocks.Behaviors
   - BuildingBlocks.Messaging
   - Test projeleri

2. **Projeler Arası Referansları Yönetmek**
   - Catalog.API → BuildingBlocks.Exceptions
   - Basket.API → BuildingBlocks.Messaging
   - vb.

3. **Tek Komutla Tüm Projeleri Build/Run Etmek**
   ```bash
   dotnet build          # Tüm projeleri build eder
   dotnet restore        # Tüm paketleri restore eder
   dotnet test           # Tüm testleri çalıştırır
   ```

4. **IDE Desteği**
   - Visual Studio, Rider, VS Code solution dosyasını açar
   - Tüm projeleri tek ekranda görürsünüz
   - Proje referansları otomatik yönetilir

### Ne Sağlıyor?

- ✅ **Organizasyon:** Tüm projeler tek yerde
- ✅ **Kolay Yönetim:** Tek komutla build/test
- ✅ **Referans Yönetimi:** Projeler arası bağımlılıklar
- ✅ **IDE Entegrasyonu:** IDE'ler solution'ı anlar

### Örnek Kullanım:

```bash
# Solution oluştur
dotnet new sln -n EShop

# Projeleri solution'a ekle (ileride yapacağız)
dotnet sln add src/Services/Catalog/Catalog.API/Catalog.API.csproj
dotnet sln add src/Services/Basket/Basket.API/Basket.API.csproj
# ... diğer projeler

# Solution'daki tüm projeleri listele
dotnet sln list

# Tüm projeleri build et
dotnet build
```

### Sonuç

Solution dosyası, microservice projelerinde çok sayıda projeyi organize etmek ve yönetmek için gereklidir. Olmadan her projeyi ayrı ayrı yönetmek gerekir; bu da zaman kaybettirir ve hata riskini artırır.

**Özet:** Solution dosyası, projelerinizi tek bir çatı altında toplayan bir "kök dosya"dır.

---

## 1.1 Solution ve Temel Proje Yapısı - Yapılanlar

### Adım 1: Solution Dosyası Oluştur

**Komut:**
```bash
dotnet new sln -n EShop
```

**Açıklamalar:**
- `-n` (veya `--name`) parametresi: Oluşturulacak dosyanın adını belirtir
- `-n EShop` → `EShop.sln` dosyası oluşturur
- Alternatif: `-n` olmadan kullanılırsa, klasör adı kullanılır

**Sonuç:** `EShop.sln` dosyası oluşturuldu

**Kontrol:**
```bash
dotnet sln list
# Çıktı: "No projects found in the solution." (Normal, henüz proje eklemedik)
```

---

### Adım 2: Klasör Yapısını Oluştur

**Komutlar:**
```bash
mkdir -p src/Services
mkdir -p src/ApiGateway
mkdir -p src/BuildingBlocks
mkdir -p tests
```

**Açıklamalar:**
- `-p` (veya `--parents`) parametresi: Üst klasörler yoksa onları da oluşturur
- Örnek: `src/` yoksa önce onu oluşturur, sonra `Services/` oluşturur
- Hata vermez, güvenli bir komut

**Klasör Açıklamaları:**
- `src/Services/` → Microservice'ler buraya gelecek (Catalog, Basket, Ordering, Discount)
- `src/ApiGateway/` → API Gateway (YARP) buraya gelecek
- `src/BuildingBlocks/` → Paylaşılan kodlar buraya gelecek (Exceptions, Behaviors, Messaging)
- `tests/` → Test projeleri buraya gelecek

**Sonuç:** Klasör yapısı oluşturuldu

**Kontrol:**
```bash
ls -la src/
tree src/  # Eğer tree komutu varsa
```

**Çıktı:**
```
src/
├── ApiGateway
├── BuildingBlocks
└── Services
```

---

### Adım 3: global.json Kontrol Et/Oluştur

**Durum:** Dosya zaten mevcut

**İçerik:**
```json
{
  "sdk": {
    "version": "9.0.112",
    "rollForward": "latestFeature"
  }
}
```

**Açıklamalar:**
- `version`: .NET SDK versiyonunu belirtir (9.0.112)
- `rollForward`: Eğer tam olarak `9.0.112` yoksa, en yakın feature versiyonunu kullanır
  - Örnek: `9.0.112` yoksa `9.0.113` kullanılabilir
  - Ama `9.1.x` veya `10.x` kullanılmaz (major/minor değişmez)

**Neden Gerekli?**
- Tüm geliştiriciler aynı SDK versiyonunu kullanır
- Build tutarlılığı sağlar
- CI/CD'de aynı versiyon kullanılır

---

### Adım 4: Directory.Build.props Oluştur

**Dosya İçeriği:**
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>
```

**Açıklamalar:**
- `TargetFramework>net9.0</TargetFramework>` → Tüm projeler .NET 9.0 kullanır
- `Nullable>enable</Nullable>` → Nullable reference types açık (null güvenliği)
- `ImplicitUsings>enable</ImplicitUsings>` → Otomatik using'ler (System, System.Linq vb.)
- `LangVersion>latest</LangVersion>` → En son C# özelliklerini kullan

**Neden Gerekli?**
- Tüm projeler için ortak MSBuild ayarları
- Her projede aynı ayarları tekrar yazmaya gerek yok
- Kod tutarlılığı sağlar
- Değişiklik tek yerden yapılır

**Nasıl Çalışır?**
- MSBuild, proje dosyalarını derlerken önce `Directory.Build.props` dosyasını okur
- Bu ayarları tüm projelere otomatik uygular

**Dosya Oluşturma Yöntemleri:**
1. IDE ile (VS Code, Visual Studio)
2. Terminal ile (nano, vim)
3. Echo ile (küçük dosyalar için)
4. AI asistan ile (bizim yaptığımız gibi)

---

### Adım 5: Directory.Packages.props Oluştur

**Dosya İçeriği (Başlangıç):**
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Paketler buraya eklenecek -->
  </ItemGroup>
</Project>
```

**Açıklamalar:**
- `ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` → Merkezi paket yönetimini aktif eder
- İleride paket eklerken versiyonları buraya yazacağız
- Proje dosyalarında sadece paket adı yazılır, versiyon buradan alınır

**Neden Gerekli?**
- Tüm projeler aynı paket versiyonlarını kullanır
- Versiyon tutarlılığı sağlar
- Paket güncellemeleri kolaylaşır
- Dependency hell sorunlarını azaltır

**Örnek (İleride):**
```xml
<ItemGroup>
  <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
  <PackageVersion Include="MediatR" Version="12.2.0" />
  <PackageVersion Include="FluentValidation" Version="11.9.0" />
</ItemGroup>
```

---

### 1.1 Bölümü - Tamamlanan Kontroller

✅ Solution oluşturuldu (`EShop.sln`)
✅ Klasör yapısı oluşturuldu (`src/`, `tests/`)
✅ `global.json` kontrol edildi (zaten vardı, doğru)
✅ `Directory.Build.props` oluşturuldu
✅ `Directory.Packages.props` oluşturuldu
✅ Solution açılıyor (`dotnet sln list` → "No projects found" - Normal)
✅ Klasörler doğru (`src/` altında: ApiGateway, BuildingBlocks, Services)

---

## 1.2 Docker Compose (Sadece Infrastructure) - Yapılanlar

### Adım 6: docker-compose.yml Oluştur

**Dosya:** Proje root dizininde `docker-compose.yml`

**Neden Docker Compose?**
1. **Tek komutla tüm altyapıyı başlatır**
   - `docker compose up -d` → Tüm container'lar başlar
2. **Yapılandırmayı kod olarak tutar**
   - Herkes aynı ortamı kullanır
   - Git'te saklanır
3. **Bağımlılıkları yönetir**
   - Container'ların sırayla başlamasını sağlar
4. **Volume'ları yönetir**
   - Verilerin kalıcı olmasını sağlar (container silinse bile veriler kalır)

**Dosya İçeriği:**

#### 1. PostgreSQL Container'ları (3 adet):
- `catalogdb` → Port: 5432, Database: CatalogDb
- `orderingdb` → Port: 5435, Database: OrderingDb (5433 kullanılıyordu, 5435'e değiştirildi)
- `discountdb` → Port: 5434, Database: DiscountDb
- Her biri için health check var (PostgreSQL hazır mı kontrol eder)
- Image: `postgres:16-alpine` (hafif Alpine Linux tabanlı)

#### 2. Redis Container:
- `basketdb` → Port: 6379 (Redis), Port: 8001 (RedisInsight UI)
- Image: `redis/redis-stack:latest` (Redis + RedisInsight UI birlikte)
- Health check: `redis-cli ping`

#### 3. RabbitMQ Container:
- `messagebroker` → Port: 5673 (AMQP - 5672 kullanılıyordu, 5673'e değiştirildi), Port: 15673 (Management UI - 15672 kullanılıyordu, 15673'e değiştirildi)
- Image: `rabbitmq:3-management-alpine`
- Kullanıcı: `guest`, Şifre: `guest`
- Hostname: `ecommerce-mq`

#### 4. pgAdmin Container:
- `pgadmin` → Port: 5050:80 (Web UI)
- Image: `dpage/pgadmin4:latest`
- Email: `admin@admin.com`
- Password: `admin`
- PostgreSQL veritabanlarına web üzerinden erişim sağlar
- `depends_on`: catalogdb, orderingdb, discountdb (PostgreSQL container'ları başladıktan sonra başlar)

#### 5. Volume'lar:
- `catalogdb_data` → PostgreSQL verileri
- `orderingdb_data` → PostgreSQL verileri
- `discountdb_data` → PostgreSQL verileri
- `basketdb_data` → Redis verileri
- `rabbitmq_data` → RabbitMQ verileri
- `pgadmin_data` → pgAdmin yapılandırma ve bağlantı bilgileri

**Volume'lar Neden Gerekli?**
- Container silinse bile veriler kalır
- Veri kalıcılığı sağlar
- Development ortamında veriler kaybolmaz

**Health Check Nedir?**
- Docker, container'ın sağlıklı olup olmadığını kontrol eder
- Belirli aralıklarla test komutu çalıştırır
- Eğer test başarısız olursa container'ı yeniden başlatır

---

### Adım 7: .env Dosyası Oluştur (Opsiyonel)

**Dosya:** Proje root dizininde `.env`

**Neden .env Dosyası?**
1. **Hassas bilgileri ayrı tutar**
   - Şifreler, kullanıcı adları kod içinde değil, `.env` dosyasında
2. **Ortam bazlı yapılandırma**
   - Development, Production için farklı değerler
3. **Git'te saklanmaz**
   - `.gitignore`'da olduğu için güvenli

**İçerik:**
```env
# PostgreSQL
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres

# RabbitMQ
RABBITMQ_DEFAULT_USER=guest
RABBITMQ_DEFAULT_PASS=guest
```

**Not:** `.env` dosyası `.gitignore`'da olduğu için Git'e commit edilmez. Güvenlik için önemlidir.

---

### Adım 8: Test Et

#### Docker Compose V2 Kullanımı

**Önemli:** Fedora'da Docker Compose V2 kullanılıyor. Komut `docker compose` (boşluklu) şeklinde.

**Kontrol:**
```bash
docker compose version
# Çıktı: Docker Compose version v2.40.3-desktop.1
```

#### Test Adımları:

**1. Container'ları Başlat:**
```bash
docker compose up -d
```

**Ne yapar?**
- `docker compose` → Docker Compose V2 komutu (boşluklu)
- `up` → Container'ları başlat
- `-d` → Detached mode (arka planda çalışır)

**Beklenen:**
- İlk seferde image'lar indirilir (PostgreSQL, Redis, RabbitMQ)
- Container'lar başlatılır
- Her container için "Creating..." ve "Started" mesajları görünür

**2. Container Durumlarını Kontrol Et:**
```bash
docker compose ps
```

**Beklenen:** Tüm container'lar `Up` ve `healthy` durumunda olmalı

**3. PostgreSQL Test (CatalogDb):**
```bash
docker exec -it catalogdb psql -U postgres -d CatalogDb -c "SELECT version();"
```

**Beklenen:** PostgreSQL versiyon bilgisi

**4. Redis Test:**
```bash
docker exec -it basketdb redis-cli ping
```

**Beklenen:** `PONG`

**5. RabbitMQ Test:**
- Tarayıcıda aç: http://localhost:15673 (15672 kullanılıyordu, 15673'e değiştirildi)
- Username: `guest`
- Password: `guest`

**Beklenen:** RabbitMQ Management UI açılmalı

**6. RedisInsight UI Test (Opsiyonel):**
- Tarayıcıda aç: http://localhost:8001

**Beklenen:** RedisInsight UI açılmalı

**7. pgAdmin Test:**
- Tarayıcıda aç: http://localhost:5050
- Email: `admin@admin.com`
- Password: `admin`

**Beklenen:** pgAdmin login ekranı açılmalı

**pgAdmin'de PostgreSQL Veritabanlarına Bağlanma:**

Her veritabanı için ayrı bir server kaydı oluşturulmalı:

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

### 1.2 Bölümü - Tamamlanan Kontroller

✅ `docker-compose.yml` oluşturuldu
✅ PostgreSQL container'ları tanımlandı (3 adet)
✅ Redis container tanımlandı
✅ RabbitMQ container tanımlandı
✅ pgAdmin container eklendi (PostgreSQL yönetimi için)
✅ Volume'lar tanımlandı (6 adet: catalogdb_data, orderingdb_data, discountdb_data, basketdb_data, rabbitmq_data, pgadmin_data)
✅ `.env` dosyası oluşturuldu (opsiyonel)
✅ Docker Compose V2 kontrol edildi
✅ Container'lar başlatıldı ve test edildi
✅ Port çakışmaları çözüldü (OrderingDb: 5435, RabbitMQ: 5673/15673)
✅ pgAdmin'de PostgreSQL veritabanlarına bağlantılar oluşturuldu

### Güncel Port Tablosu

| Servis | Port | Açıklama |
|--------|------|----------|
| **CatalogDb** | 5432 | PostgreSQL |
| **OrderingDb** | 5435 | PostgreSQL (5433 kullanılıyordu, 5435'e değiştirildi) |
| **DiscountDb** | 5434 | PostgreSQL |
| **Redis** | 6379 | Redis |
| **RedisInsight** | 8001 | Redis UI |
| **RabbitMQ AMQP** | 5673 | Message Broker (5672 kullanılıyordu, 5673'e değiştirildi) |
| **RabbitMQ Management** | 15673 | Management UI (15672 kullanılıyordu, 15673'e değiştirildi) |
| **pgAdmin** | 5050 | PostgreSQL Web UI |

**Not:** Port çakışmaları nedeniyle bazı portlar değiştirildi. Sistemde zaten kullanılan portlar:
- 5433 → OrderingDb için 5435 kullanılıyor
- 5672 → RabbitMQ AMQP için 5673 kullanılıyor
- 15672 → RabbitMQ Management için 15673 kullanılıyor

---

## Diğer Notlar

### [Tarih: ...]
- ...

---

