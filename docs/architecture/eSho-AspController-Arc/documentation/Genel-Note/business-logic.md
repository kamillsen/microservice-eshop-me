# Genel Notlar — Business Logic / Business Rule

Bu not, projedeki “business logic” kavramını netleştirmek ve kod okurken/debug yaparken **neyin iş mantığı olduğunu** hızlı ayırt edebilmek için yazıldı.

---

## Business rule vs Business logic (kısa ve net)

- **Business rule (iş kuralı)**: *“Ne olmalı?”*  
  Kuralın kendisi; çoğu zaman cümleyle ifade edilebilir.

- **Business logic (iş mantığı)**: *“O kuralı nasıl uygularız?”*  
  Rule’ların kodda hayat bulmuş hâli; akış + karar + hesaplama + hata üretimi.

**İlişki:** Bir feature genelde **birden çok rule** içerir; business logic bu rule’ları bir araya getirip süreci işletir.

---

## Bir şeyin business logic olup olmadığını nasıl anlarsın?

Kodu okurken şu soruları sor:

1. **Domain’e özel bir karar mı veriyor?**  
   (E‑ticaret kuralı, fiyat/indirim/stock/checkout/permission gibi)

2. **UI/HTTP/DB değişse bile kural aynı kalır mı?**  
   REST yerine gRPC olsa, PostgreSQL yerine başka storage olsa “kural” yine geçerli mi?

3. **Karar/hüküm var mı?**  
   `if/else`, limit, hesaplama, durum geçişi, “yapılabilir/yapılamaz” kontrolü…

4. **Yanlış olursa işsel hata mı olur, yoksa teknik hata mı?**
   - “İndirim yanlış hesaplandı” → business logic sorunu
   - “Port dolu / Swagger açılmıyor / DB’ye bağlanamadı” → teknik/infrastructure sorunu

Bu sorulara “evet” yaklaştıkça business logic’e yaklaşırsın.

---

## Bu projede business logic nerede durur?

Şu an aktif servis: **Catalog.API**.

### Business logic’in ana adresi

- `src/Services/Catalog/Catalog.API/Features/**/**Handler.cs`
  - Örn: `GetProductsHandler`, `GetProductByIdHandler`, `CreateProductHandler`, `UpdateProductHandler`, `DeleteProductHandler`

Burada “ürün listeleme”, “ürün bulma”, “yoksa notfound”, “sayfalama/filtreleme” gibi davranışlar şekillenir.

### Business logic olmayan (ama projede gerekli) parçalar

- **Controller**: routing, HTTP response (200/201/404), `IMediator.Send(...)`
  - *Neden değil?* Çünkü bunlar “işin kuralı” değil, **dış dünyaya sunum/taşıma (presentation/transport)** detayları.

- **AutoMapper mapping**: `Product` → `ProductDto`
  - *Neden değil?* Çünkü genelde “veriyi dışarıya hangi formatta göstereceğiz?” sorusunu çözer (API contract).  
    **Kural koymaz**, çoğunlukla alan taşır/flatten eder.

- **FluentValidation**: request doğrulama
  - Genelde input validation’dır (format/boş geçme/limit).  
  - Bazı doğrulamalar iş kuralına yaklaşabilir ama çoğu “giriş doğrulama”dır.

- **LoggingBehavior / ValidationBehavior**: cross‑cutting concerns
  - *Neden değil?* Çünkü domain kararı vermez; “logla / doğrula” gibi ortak altyapı işini yapar.

- **DbContext / EF Core**: veri erişimi
  - *Neden değil?* Kural değil, **veriye nasıl ulaştığımızın** tekniğidir.

- **GlobalExceptionHandler**: exception → HTTP ProblemDetails
  - *Neden değil?* İş kuralını değil, hatayı HTTP’ye “çevirme” işini yapar.

---

## Catalog’dan mini örnek (rule → logic)

### Örnek Rule
“Ürün yoksa bulunamadı sayılır.”

### Örnek Logic
Handler’da DB’den ürünü bul → yoksa `NotFoundException` fırlat → global handler bunu HTTP 404’e çevirir.

**Kısa akış:**

```
GET /api/products/{id}
  -> Controller
  -> MediatR (Pipeline)
  -> Handler (rule uygulanır: yoksa NotFound)
  -> Exception Handler (HTTP 404)
```

Burada:
- “yoksa bulunamadı” fikri = **business rule**
- bunu kodda uygulamak = **business logic**
- 404 dönmek için HTTP’ye çevirmek = **presentation/transport**

---

## ASCII “Business Logic Diagram” örneği (projeden)

Bu projede “business logic diagram” isteği mantıklı; ama en faydalı kullanım için diagramı ikiye ayırmak iyi olur:

- **Business Flow (rule/decision odaklı)**: “İş nasıl çalışıyor?” (domain kararları)
- **Technical Flow (katman akışı)**: “Kod nereden nereye gidiyor?” (debug için)

### Örnek 1 — Catalog: Ürün Listeleme (Business Flow)

> Domain davranışı: filtre + sayfalama + sonuç döndürme

```
GetProducts
  |
  +-- CategoryId var mı?
  |      |-- Evet  -> sadece o kategori ürünleri
  |      '-- Hayır -> tüm ürünler
  |
  +-- Sayfalama uygula (PageNumber, PageSize)
  |
  '-- Ürünleri döndür
```

### Örnek 2 — Catalog: Ürün Listeleme (Technical Flow)

> Debug/akış görünürlüğü: Controller → MediatR → Pipeline → Handler → DB → DTO

```
HTTP GET /api/products
      |
      v
ProductsController
      |
      v
IMediator.Send(GetProductsQuery)
      |
      v
Pipeline:
  LoggingBehavior
  ValidationBehavior
      |
      v
GetProductsHandler
      |
      v
EF Core Query -> PostgreSQL (catalogdb)
      |
      v
AutoMapper: Product -> ProductDto
      |
      v
HTTP 200 OK (JSON)
```

---

## Debug yaparken pratik öneri

“İş mantığı nerde çalışıyor?” sorusunun en hızlı cevabı:

- Breakpoint’i **Handler** içine koy (`Features/**/Handler.cs`)
- Sonra sırayla:
  - request parametreleri doğru mu?
  - hangi branch’e girdi? (if/else)
  - hangi hesap çıktı?
  - hangi exception fırladı?

Bu şekilde kontrol tamamen sende olur.

---

**Son Güncelleme:** Aralık 2025


