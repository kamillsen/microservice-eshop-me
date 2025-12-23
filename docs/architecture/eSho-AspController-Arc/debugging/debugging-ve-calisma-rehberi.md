# Debugging & Çalışma Rehberi (E-Shop Microservice Practice)

Bu doküman, projede **neyin ne işe yaradığını**, **nasıl çalıştığını** ve özellikle **debug yaparken kontrolün sende olmasını** sağlayacak pratik adımları içerir.

> Odak: Şu an aktif servis **Catalog.API**. Diğer servisler (Discount/Basket/Ordering/Gateway) planlı durumda.

---

## 1) Projede şu an neler var?

### 1.1 Mikroservisler

- **Catalog.API** (`src/Services/Catalog/Catalog.API`)
  - **Ne yapar?** Ürün ve kategori yönetimi (REST API).
  - **Teknik**: Controller-based API + CQRS + MediatR + EF Core (PostgreSQL) + AutoMapper + FluentValidation.
  - **Debug için en önemli noktalar**:
    - Controller → MediatR → Handler → DbContext → PostgreSQL akışı.
    - Breakpoint’i handler’lara koyunca DB sorgusunun nasıl oluştuğunu görürsün.

> Not: `src/Services` altında şu an sadece `Catalog/Catalog.API` var. Discount/Basket/Ordering projeleri henüz eklenmemiş.

### 1.2 BuildingBlocks (Paylaşılan altyapı)

- **BuildingBlocks.Exceptions** (`src/BuildingBlocks/BuildingBlocks.Exceptions`)
  - **Ne yapar?** Global exception handling + ProblemDetails formatı.
  - **Debug’de ne sağlar?** Bir hata olduğunda “nerede patladı”yı tek bir formatta görürsün (stack trace vs. log).

- **BuildingBlocks.Behaviors** (`src/BuildingBlocks/BuildingBlocks.Behaviors`)
  - **Ne yapar?** MediatR pipeline behavior’ları:
    - `LoggingBehavior` → request/response loglar
    - `ValidationBehavior` → FluentValidation validator’larını çalıştırır
  - **Debug’de ne sağlar?** Controller’dan çıkıp handler’a gitmeden önce pipeline’da neler döndüğünü izleyebilirsin.

- **BuildingBlocks.Messaging** (`src/BuildingBlocks/BuildingBlocks.Messaging`)
  - **Ne yapar?** İleride RabbitMQ/MassTransit event’leri için temel event modelleri.
  - **Debug’de ne sağlar?** Şimdilik Catalog’da doğrudan kullanılmıyor; Basket/Ordering gelince devreye girecek.

---

## 2) Altyapı / DB / Docker tarafı (neyi ne çalıştırıyor?)

### 2.1 docker-compose.yml (infrastructure)

`docker-compose.yml` içinde planlanan altyapılar var:
- **PostgreSQL**: `catalogdb`, `orderingdb`, `discountdb`
- **Redis**: `basketdb`
- **RabbitMQ**: `messagebroker`
- (Opsiyonel) **pgAdmin**: `pgadmin`

> Senin bilgisayarda Docker daemon çalışmıyorsa “container listesi” göremezsin; ama compose tanımı yine doğru rehberdir.

### 2.2 Catalog DB bilgisi (kısa)

- **DB adı**: `CatalogDb`
- **Container adı**: `catalogdb`
- **Host port**: `5436` (host) → `5432` (container)

**Catalog.API connection string (local)** genelde şu formatta:
`Host=localhost;Port=5436;Database=CatalogDb;Username=postgres;Password=postgres`

---

## 3) Catalog.API nasıl çalışıyor? (akış)

### 3.1 HTTP → Controller → MediatR → Handler → DB

Örnek akış: `GET /api/products`

```
Client (Swagger/Postman)
        |
        v
ProductsController.GetProducts()
        |
        v
IMediator.Send(GetProductsQuery)
        |
        v
Pipeline:
  LoggingBehavior -> ValidationBehavior -> Handler
        |
        v
GetProductsHandler (EF Core query)
        |
        v
PostgreSQL (CatalogDb)
```

### 3.2 Swagger nereden geliyor?

Catalog.API `Program.cs` içinde:
- `AddSwaggerGen()` → swagger dokümanını üretir
- `UseSwagger()` + `UseSwaggerUI()` → UI’ı açar

Bu projede `RoutePrefix = string.Empty` olduğu için Swagger UI **root’ta** açılır:
- `http://localhost:5001/`

Swagger JSON:
- `http://localhost:5001/swagger/v1/swagger.json`

---

## 4) Debugging (Breakpoint ile) – Kontrol sende

Bu bölüm “debug sırasında istediğini elde et, kontrol sende olsun” mantığıyla yazıldı.

### 4.1 En pratik breakpoint noktaları (nerede durdurmalıyım?)

#### A) Controller (HTTP giriş noktası)
- `src/Services/Catalog/Catalog.API/Controllers/ProductsController.cs`
  - `GetProducts()` içinde `_mediator.Send(query)` satırına breakpoint.

**Ne sağlar?**
- Request’in gerçekten buraya geliyor mu görürsün.
- Query parametreleri bind olmuş mu (PageNumber/PageSize/CategoryId) görürsün.

#### B) Handler (DB sorgusunun kurulduğu yer)
- `src/Services/Catalog/Catalog.API/Features/Products/Queries/GetProducts/GetProductsHandler.cs`
  - `var query = _context.Products...` satırına breakpoint.

**Ne sağlar?**
- EF Core query nasıl oluşuyor, filtreler uygulanıyor mu, sayfalama doğru mu görürsün.
- `products` listesinde sonuç geliyor mu görürsün.

#### C) Pipeline behavior (log/validation nerede devreye giriyor?)
- `src/BuildingBlocks/BuildingBlocks.Behaviors/Behaviors/LoggingBehavior.cs`
- `src/BuildingBlocks/BuildingBlocks.Behaviors/Behaviors/ValidationBehavior.cs`

**Ne sağlar?**
- Handler’a gitmeden önce “validation patlıyor mu?” veya “log atılıyor mu?” netleşir.

#### D) DB migration/seed (uygulama açılırken patlıyor mu?)
- `src/Services/Catalog/Catalog.API/Program.cs`
  - `context.Database.MigrateAsync()` ve `SeedData.InitializeAsync(context)` satırlarına breakpoint.

**Ne sağlar?**
- Swagger açılmıyor sanırsın ama aslında uygulama DB’ye bağlanırken çöküyor olabilir; bunu yakalarsın.

---

## 5) VS Code ile Debug (breakpoint çalıştırma)

Projede VS Code için debug dosyaları hazır:
- `.vscode/launch.json`
- `.vscode/tasks.json`

### 5.1 Breakpoint koyma

- Satır numarasının soluna tıkla (kırmızı nokta)
- veya **F9**

### 5.2 Debug başlatma

- **Run and Debug** paneli (Ctrl+Shift+D)
- Konfig: **“.NET Core Launch (Catalog.API)”**
- **F5**

### 5.3 Step kontrol tuşları (kontrol sende)

- **F5**: Continue (sonraki breakpoint’e kadar devam)
- **F10**: Step Over (satır satır ilerle, fonksiyona girme)
- **F11**: Step Into (fonksiyon içine gir)
- **Shift+F11**: Step Out (fonksiyondan çık)
- **Shift+F5**: Stop

### 5.4 İzleme (Watch/Locals)

Breakpoint’te durduğunda:
- **Locals**: o anki değişkenler
- **Watch**: özellikle görmek istediğin değerleri ekle
  - örn: `request.PageNumber`, `request.CategoryId`, `products.Count`

---

## 6) Swagger açılmıyor: en sık sebepler ve hızlı kontrol listesi

### 6.1 Doğru adresi açıyor musun?

Bu projede Swagger **root’ta**:
- `http://localhost:5001/`

Swagger JSON:
- `http://localhost:5001/swagger/v1/swagger.json`

> `.../swagger` değil, `.../` olabilir (RoutePrefix boş).

### 6.2 Uygulama gerçekten ayakta mı?

Debug Console / Terminal çıktısında şu satırları aramalısın:

```
Now listening on: http://localhost:5001
```

Eğer bunu görmüyorsan:
- uygulama daha start olmadan hata alıyor olabilir (çoğunlukla DB bağlantısı/migration).

### 6.3 Development modunda mı?

Swagger sadece şurada açılıyor:
`if (app.Environment.IsDevelopment()) { ... }`

Bu yüzden `ASPNETCORE_ENVIRONMENT=Development` olmalı.

### 6.4 DB migration/seed sırasında patlıyor olabilir

`Program.cs` içinde uygulama açılır açılmaz şunlar çalışıyor:
- `MigrateAsync()`
- `SeedData.InitializeAsync()`

DB yoksa / port yanlışsa / docker kapalıysa uygulama start olmaz → Swagger da açılmaz.

**Çözüm yaklaşımı (debug ile):**
- `Program.cs` migration satırına breakpoint koy
- exception nerede çıkıyor netleşsin

---

## 7) “DB’ye istek gidiyor mu?” nasıl anlarsın?

### 7.1 Debugger ile

Handler’da breakpoint:
- `ToListAsync()` satırında dur
- `query` üzerinde filtreler doğru mu kontrol et
- `products` doluyor mu kontrol et

### 7.2 Log ile

`LoggingBehavior` devredeyse request/response loglarını görürsün.
DB query loglamak istiyorsan EF Core SQL logging ayrıca eklenebilir (istersen birlikte ekleriz).

### 7.3 DB tarafı ile

pgAdmin veya `psql` ile:
- `SELECT * FROM "Products";`
- `SELECT * FROM "Categories";`

> SeedData çalıştıysa tablolar dolu olmalı.

---

## 8) “Kontrol bende olsun” için küçük pratikler

### 8.1 Conditional Breakpoint

Breakpoint’e sağ tık → **Edit Breakpoint** → Condition:
- Örn: `request.PageNumber == 2`

### 8.2 Logpoint (kodu değiştirmeden log)

Breakpoint’e sağ tık → **Add Logpoint**
- Örn mesaj: `PageNumber={request.PageNumber} CategoryId={request.CategoryId}`

### 8.3 Exception Breakpoints (hata anında otomatik dur)

VS Code’da C# tarafında exception ayarları sınırlı olabilir; ama pratikte:
- `Program.cs` migration/seed bloğuna breakpoint koyarak “start-up crash” yakalanır.
- `GlobalExceptionHandler` içine breakpoint koyarak runtime exception’ları yakalarsın.

---

## 9) Bu klasördeki dosyalar ne işe yarıyor?

`docs/architecture/eSho-AspController-Arc/` içinde sık kullanılanlar:

- `plan/implementation-plan.md`
  - Projenin faz faz yol haritası (hangi servis ne zaman).

- `plan/eshop-microservice-architecture.md`
  - Mimari kararlar: CQRS+MediatR, gRPC, RabbitMQ, YARP, DB’ler, portlar.

- `documentation/doing/`
  - Yapım aşamasındaki dokümanlar (ör: `faz-4-doing` discount planı).

- `documentation/done/`
  - Tamamlanmış faz dokümantasyonları + “learned” notları.

- `update-product-images.sql`
  - Catalog DB’de `Products.ImageUrl` güncellemek için SQL script.

- `CurrentStatus.txt`
  - “Şu an neredeyiz?” gibi kısa durum özeti (varsa güncel tutulur).

---

## 10) Hızlı başlangıç (debug senaryosu)

Hedef: `GET /api/products` çağrısında DB’den veri çekildiğini breakpoint ile görmek.

1. Breakpoint koy:
   - `ProductsController.GetProducts()` içinde `_mediator.Send(query)` satırı
   - `GetProductsHandler.Handle()` içinde `ToListAsync()` satırı

2. VS Code: F5 ile debug başlat

3. Tarayıcı:
   - `http://localhost:5001/` (Swagger UI)

4. Swagger’dan:
   - `GET /api/products` → Execute

5. Debugger breakpoint’lerde duracak:
   - `query` parametreleri doğru mu?
   - `products.Count` kaç?
   - `CategoryId` filtresi çalışıyor mu?

---

## 11) İstersen bir sonraki adım

Eğer istersen, debug sürecini daha “kontrollü” yapmak için şu iki iyileştirmeden birini ekleyebiliriz (sen onay verirsen):

- **(A)** EF Core SQL loglarını açmak (hangi SQL çalıştı görürsün)
- **(B)** Development ortamında Swagger’ı her zaman açacak şekilde yapılandırmak (env takılmasın)


