# AutoMapper Update İşlemi Detaylı Açıklama

> Bu dosya, AutoMapper'ın `Map<TSource, TDestination>(source, destination)` overload'unun UPDATE işlemlerinde nasıl çalıştığını ve EF Core Change Tracker ile etkileşimini detaylı olarak açıklar.
> 
> **İçerik:**
> - UpdateProductHandler'daki güncelleme akışı
> - AutoMapper.Map() metodunun çalışma prensibi
> - EF Core Change Tracker ile entegrasyon
> - CreateMap konfigürasyonunun önemi
> - Adım adım süreç açıklaması

---

## Update İşlemi Genel Bakış

### UpdateProductHandler Kodu

```csharp
public async Task<Unit> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
{
    // 1. Ürünü bul
    var product = await _context.Products.FindAsync(request.Id, cancellationToken);
    
    if (product == null)
        throw new NotFoundException(nameof(Product), request.Id);

    // 2. Command'den Entity'yi güncelle (AutoMapper ile)
    _mapper.Map(request, product);

    // 3. Değişiklikleri kaydet
    await _context.SaveChangesAsync(cancellationToken);

    return Unit.Value;
}
```

### Kritik Nokta: Map() Overload'u

```csharp
_mapper.Map(request, product);
```

Bu satır **iki parametreli overload** kullanır:
- **İlk parametre (source)**: `UpdateProductCommand` - Güncellenecek yeni değerler
- **İkinci parametre (destination)**: `Product` - Mevcut entity (veritabanından çekilmiş)

**Önemli:** Bu overload, **yeni nesne oluşturmaz**, mevcut `product` nesnesini **günceller**.

---

## Adım Adım Güncelleme Süreci

### 1. Adım: Entity'yi Veritabanından Çekme

```csharp
var product = await _context.Products.FindAsync(request.Id, cancellationToken);
```

**Ne Oluyor?**

1. **Entity Çekilir**: Veritabanından `Product` entity'si çekilir
2. **Change Tracking Başlar**: EF Core, entity'yi **Change Tracker**'a ekler
3. **Snapshot Oluşturulur**: EF Core, entity'nin bir **snapshot** (anlık görüntü) oluşturur
4. **State: Unchanged**: Entity'nin durumu `Unchanged` olarak işaretlenir

**Örnek Durum:**

```csharp
// Veritabanından çekilen product:
product = {
    Id = Guid("123e4567-e89b-12d3-a456-426614174000"),
    Name = "Eski Ürün Adı",
    Description = "Eski açıklama",
    Price = 1000.00m,
    ImageUrl = "eski-resim.jpg",
    CategoryId = Guid("cat-1")
}

// EF Core Change Tracker'da snapshot:
snapshot = {
    Id = Guid("123e4567-e89b-12d3-a456-426614174000"),
    Name = "Eski Ürün Adı",           // ← Bu değerler snapshot'ta saklanıyor
    Description = "Eski açıklama",
    Price = 1000.00m,
    ImageUrl = "eski-resim.jpg",
    CategoryId = Guid("cat-1")
}

// Entity State:
State = EntityState.Unchanged
```

**⭐ Snapshot Ne Zaman Alınır?**

Snapshot, **entity veritabanından çekildiği anda** (tracking başladığında) oluşturulur:

```csharp
// ⬇️ Bu satır çalıştığında snapshot alınır:
var product = await _context.Products.FindAsync(request.Id, cancellationToken);
// ↑ SNAPSHOT BURADA ALINIR! ✅
//   - Entity veritabanından çekildi
//   - Snapshot oluşturuldu (tüm property'lerin kopyası)
//   - Change Tracker'a eklendi
```

**Zaman Çizelgesi:**

```
FindAsync() Çağrılır
    ↓
Veritabanından Entity Çekilir
    ↓
Entity Memory'ye Yüklenir
    ↓
⭐ SNAPSHOT ALINIR (BURADA!) ⭐
    ↓
Change Tracker'a Eklenir
    ↓
FindAsync() Tamamlanır
```

**Hangi Metodlar Snapshot Alır?**

- ✅ **Snapshot alan metodlar** (tracking açık):
  ```csharp
  FindAsync(id)
  FirstOrDefaultAsync(...)
  ToListAsync()
  SingleAsync(...)
  ```

- ❌ **Snapshot almayan metodlar**:
  ```csharp
  AsNoTracking().FirstOrDefaultAsync(...)  // Tracking kapalı
  ```

**Önemli:** `AsNoTracking()` kullanılırsa snapshot oluşturulmaz ve değişiklikler algılanmaz!

---

### 2. Adım: AutoMapper ile Güncelleme

```csharp
_mapper.Map(request, product);
```

**Ne Oluyor?**

1. **Mapping Konfigürasyonu Kontrol Edilir**: `CreateMap<UpdateProductCommand, Product>()` aranır
2. **Property Mapping**: `UpdateProductCommand`'daki property'ler, `Product` entity'sindeki aynı isimli property'lere kopyalanır
3. **Entity Güncellenir**: Mevcut `product` nesnesinin property'leri değiştirilir (yeni nesne oluşturulmaz)

**Mapping Konfigürasyonu (MappingProfile.cs):**

```csharp
CreateMap<UpdateProductCommand, Product>();
```

**⚠️ ÖNEMLİ:** Bu konfigürasyon **olmadan** `_mapper.Map(request, product)` **çalışmaz** ve hata fırlatır!

**AutoMapper'ın İçsel İşlemi:**

```csharp
// AutoMapper içsel olarak şunu yapar:
product.Name = request.Name;              // "YENİ Ürün Adı"
product.Description = request.Description; // "YENİ açıklama"
product.Price = request.Price;            // 2000.00m
product.ImageUrl = request.ImageUrl;       // "yeni-resim.jpg"
product.CategoryId = request.CategoryId;   // cat-2
// Id property'si genelde ignore edilir (çünkü zaten aynı)
```

**Örnek Durum:**

```csharp
// Gelen request (UpdateProductCommand):
request = {
    Id = Guid("123e4567-e89b-12d3-a456-426614174000"),
    Name = "YENİ Ürün Adı",              // ← Değişti
    Description = "YENİ açıklama",       // ← Değişti
    Price = 2000.00m,                    // ← Değişti
    ImageUrl = "yeni-resim.jpg",         // ← Değişti
    CategoryId = Guid("cat-2")           // ← Değişti
}

// AutoMapper.Map(request, product) çalıştıktan SONRA:
product = {
    Id = Guid("123e4567-e89b-12d3-a456-426614174000"), // ← Aynı (değişmedi)
    Name = "YENİ Ürün Adı",              // ← GÜNCELLENDİ ✅
    Description = "YENİ açıklama",       // ← GÜNCELLENDİ ✅
    Price = 2000.00m,                    // ← GÜNCELLENDİ ✅
    ImageUrl = "yeni-resim.jpg",         // ← GÜNCELLENDİ ✅
    CategoryId = Guid("cat-2")           // ← GÜNCELLENDİ ✅
}

// Snapshot hala eski değerlerde:
snapshot = {
    Name = "Eski Ürün Adı",              // ← Hala eski değer
    Description = "Eski açıklama",       // ← Hala eski değer
    Price = 1000.00m,                    // ← Hala eski değer
    ImageUrl = "eski-resim.jpg",         // ← Hala eski değer
    CategoryId = Guid("cat-1")           // ← Hala eski değer
}
```

---

### 3. Adım: EF Core Change Tracker Değişiklikleri Algılar

**Ne Oluyor?**

1. **Property Değişiklikleri**: Entity'nin property'leri değişti
2. **Snapshot Karşılaştırması**: EF Core, mevcut değerleri **snapshot** ile karşılaştırır
3. **Modified İşaretleme**: Değişen property'leri algılar ve entity'yi `Modified` olarak işaretler

**EF Core Change Tracker Durumu:**

```csharp
// EF Core Change Tracker durumu:
ChangeTracker.Entries<Product>()[0] = {
    Entity: product,                      // Güncellenmiş entity
    State: EntityState.Modified,         // ← Modified olarak işaretlendi ✅
    OriginalValues: snapshot,             // Eski değerler (snapshot)
    CurrentValues: product               // Yeni değerler (güncel entity)
}

// Değişen property'ler:
ModifiedProperties = [
    "Name",        // Eski: "Eski Ürün Adı" → Yeni: "YENİ Ürün Adı"
    "Description", // Eski: "Eski açıklama" → Yeni: "YENİ açıklama"
    "Price",       // Eski: 1000.00m → Yeni: 2000.00m
    "ImageUrl",    // Eski: "eski-resim.jpg" → Yeni: "yeni-resim.jpg"
    "CategoryId"   // Eski: cat-1 → Yeni: cat-2
]
```

**Change Tracker Karşılaştırma Mantığı:**

```csharp
// EF Core içsel olarak şunu yapar:
foreach (var property in product.GetType().GetProperties())
{
    var currentValue = property.GetValue(product);
    var originalValue = property.GetValue(snapshot);
    
    if (!Equals(currentValue, originalValue))
    {
        // Değişiklik algılandı!
        ChangeTracker.MarkPropertyAsModified(product, property.Name);
    }
}
```

---

### 4. Adım: Değişiklikleri Veritabanına Kaydetme

```csharp
await _context.SaveChangesAsync(cancellationToken);
```

**Ne Oluyor?**

1. **Change Tracker Kontrolü**: EF Core, Change Tracker'daki tüm değişiklikleri kontrol eder
2. **SQL Üretimi**: `Modified` olarak işaretlenen entity'ler için **SQL UPDATE** komutu üretir
3. **Veritabanı İşlemi**: SQL komutu veritabanına gönderilir ve çalıştırılır
4. **Snapshot Güncelleme**: Başarılı olursa, snapshot yeni değerlerle güncellenir

**Üretilen SQL:**

```sql
UPDATE "Products"
SET 
    "Name" = @p0,              -- "YENİ Ürün Adı"
    "Description" = @p1,       -- "YENİ açıklama"
    "Price" = @p2,             -- 2000.00
    "ImageUrl" = @p3,          -- "yeni-resim.jpg"
    "CategoryId" = @p4         -- cat-2 (Guid)
WHERE "Id" = @p5;             -- 123e4567-e89b-12d3-a456-426614174000

-- Parametreler:
-- @p0 = "YENİ Ürün Adı"
-- @p1 = "YENİ açıklama"
-- @p2 = 2000.00
-- @p3 = "yeni-resim.jpg"
-- @p4 = cat-2 (Guid)
-- @p5 = 123e4567-e89b-12d3-a456-426614174000 (Guid)
```

**Önemli:** EF Core, **sadece değişen property'ler** için UPDATE üretir. Bu, performans açısından çok önemlidir!

**SaveChangesAsync Sonrası:**

```csharp
// Veritabanı güncellendi ✅
// Snapshot yeni değerlerle güncellendi:
snapshot = {
    Name = "YENİ Ürün Adı",              // ← Yeni snapshot
    Description = "YENİ açıklama",
    Price = 2000.00m,
    ImageUrl = "yeni-resim.jpg",
    CategoryId = Guid("cat-2")
}

// Entity State:
State = EntityState.Unchanged  // ← Artık değişiklik yok
```

---

## Görsel Akış Diyagramı

```
┌─────────────────────────────────────────────────────────────┐
│ 1. FindAsync - Entity Çekme                                │
├─────────────────────────────────────────────────────────────┤
│ var product = await _context.Products.FindAsync(id);        │
│                                                             │
│ product = { Name: "Eski", Price: 1000 }                    │
│ snapshot = { Name: "Eski", Price: 1000 }  ← EF Core oluşturdu│
│ State: Unchanged                                            │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. AutoMapper.Map - Değişiklik Uygulama                    │
├─────────────────────────────────────────────────────────────┤
│ _mapper.Map(request, product);                             │
│                                                             │
│ request = { Name: "YENİ", Price: 2000 }                   │
│                                                             │
│ product = { Name: "YENİ", Price: 2000 }  ← GÜNCELLENDİ ✅  │
│ snapshot = { Name: "Eski", Price: 1000 }  ← Hala eski      │
│ State: Modified  ← EF Core algıladı                        │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 3. Change Tracker Karşılaştırma                             │
├─────────────────────────────────────────────────────────────┤
│ EF Core: snapshot vs product karşılaştırır                  │
│                                                             │
│ Name: "Eski" ≠ "YENİ"  → Modified ✅                        │
│ Price: 1000 ≠ 2000     → Modified ✅                        │
│                                                             │
│ ModifiedProperties = ["Name", "Price"]                     │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 4. SaveChangesAsync - Veritabanına Yazma                    │
├─────────────────────────────────────────────────────────────┤
│ await _context.SaveChangesAsync();                          │
│                                                             │
│ SQL: UPDATE Products                                        │
│      SET Name = "YENİ", Price = 2000                       │
│      WHERE Id = ...                                         │
│                                                             │
│ Veritabanı güncellendi ✅                                   │
│ snapshot = { Name: "YENİ", Price: 2000 }  ← Yeni snapshot │
│ State: Unchanged  ← Artık değişiklik yok                   │
└─────────────────────────────────────────────────────────────┘
```

---

## CreateMap Konfigürasyonunun Önemi

### ❌ CreateMap Olmadan:

```csharp
// MappingProfile.cs'de bu satır YOK:
// CreateMap<UpdateProductCommand, Product>();

// UpdateProductHandler.cs'de:
_mapper.Map(request, product);  // ❌ HATA FIRLATIR!
```

**Hata Mesajı:**
```
AutoMapper.AutoMapperMappingException: 
Missing type map configuration or unsupported mapping.

Mapping types:
UpdateProductCommand -> Product
```

### ✅ CreateMap ile:

```csharp
// MappingProfile.cs'de:
CreateMap<UpdateProductCommand, Product>();  // ✅ Gerekli!

// UpdateProductHandler.cs'de:
_mapper.Map(request, product);  // ✅ Çalışır!
```

**Sonuç:** `CreateMap<UpdateProductCommand, Product>()` **olmadan** `_mapper.Map(request, product)` **çalışmaz**!

---

## AutoMapper Map() Overload'ları

### 1. Yeni Nesne Oluşturma (Create İşlemi)

```csharp
// Tek parametreli overload - YENİ nesne oluşturur
var newProduct = _mapper.Map<Product>(createCommand);

// Sonuç: Yeni bir Product nesnesi oluşturulur
// newProduct = new Product { ... }
```

**Kullanım:**
```csharp
// CreateProductHandler.cs
var product = _mapper.Map<Product>(request);  // Yeni nesne
_context.Products.Add(product);
```

### 2. Mevcut Nesneyi Güncelleme (Update İşlemi)

```csharp
// İki parametreli overload - Mevcut nesneyi günceller
_mapper.Map(updateCommand, existingProduct);

// Sonuç: existingProduct nesnesi güncellenir (yeni nesne oluşturulmaz)
```

**Kullanım:**
```csharp
// UpdateProductHandler.cs
var product = await _context.Products.FindAsync(id);  // Mevcut nesne
_mapper.Map(request, product);  // Mevcut nesneyi güncelle
```

---

## Önemli Noktalar

### ✅ Doğru Kullanım:

1. **Entity Tracking Açık Olmalı:**
   ```csharp
   // ✅ Doğru: FindAsync tracking açık çeker
   var product = await _context.Products.FindAsync(id);
   _mapper.Map(request, product);
   await _context.SaveChangesAsync();
   ```

2. **CreateMap Konfigürasyonu Gerekli:**
   ```csharp
   // ✅ Doğru: MappingProfile'da tanımlı
   CreateMap<UpdateProductCommand, Product>();
   ```

3. **İki Parametreli Overload:**
   ```csharp
   // ✅ Doğru: Mevcut entity'yi güncelle
   _mapper.Map(request, product);
   ```

### ❌ Yanlış Kullanım:

1. **AsNoTracking ile:**
   ```csharp
   // ❌ Yanlış: Tracking kapalı, değişiklik algılanmaz
   var product = await _context.Products
       .AsNoTracking()
       .FirstOrDefaultAsync(p => p.Id == id);
   _mapper.Map(request, product);
   await _context.SaveChangesAsync();  // ❌ Hiçbir şey olmaz!
   ```

2. **CreateMap Olmadan:**
   ```csharp
   // ❌ Yanlış: Mapping konfigürasyonu yok
   _mapper.Map(request, product);  // ❌ Hata fırlatır!
   ```

3. **Yanlış Overload:**
   ```csharp
   // ❌ Yanlış: Yeni nesne oluşturur, mevcut entity'yi güncellemez
   var newProduct = _mapper.Map<Product>(request);
   // existingProduct hala eski değerlerde!
   ```

---

## Performans Notları

### EF Core'un Akıllı UPDATE Üretimi:

EF Core, **sadece değişen property'ler** için UPDATE üretir:

```sql
-- Eğer sadece Name değiştiyse:
UPDATE "Products"
SET "Name" = @p0
WHERE "Id" = @p1;

-- Eğer Name ve Price değiştiyse:
UPDATE "Products"
SET "Name" = @p0, "Price" = @p1
WHERE "Id" = @p2;
```

**Avantaj:**
- ✅ Daha az veri transferi
- ✅ Daha hızlı SQL çalıştırma
- ✅ Daha az veritabanı yükü

---

## Özet

### Güncelleme İşlemi Akışı:

1. **Entity Çekilir**: `FindAsync()` ile veritabanından çekilir ve tracking başlar
2. **AutoMapper Günceller**: `Map(request, product)` ile mevcut entity güncellenir
3. **Change Tracker Algılar**: EF Core, snapshot ile karşılaştırarak değişiklikleri algılar
4. **Veritabanına Yazılır**: `SaveChangesAsync()` ile sadece değişen property'ler UPDATE edilir

### Kritik Gereksinimler:

- ✅ `CreateMap<UpdateProductCommand, Product>()` konfigürasyonu **gerekli**
- ✅ Entity **tracking açık** olmalı (AsNoTracking kullanılmamalı)
- ✅ **İki parametreli overload** kullanılmalı: `Map(source, destination)`

### Avantajlar:

- ✅ Sadece değişen kolonlar için UPDATE üretilir (performanslı)
- ✅ Kod tekrarı yok (AutoMapper property mapping'i otomatik yapar)
- ✅ Type-safe (compile-time kontrol)
- ✅ EF Core Change Tracker ile mükemmel entegrasyon

---

**Tarih:** Aralık 2024  
**Kaynak:** Catalog.API - UpdateProductHandler  
**Durum:** ✅ Dokümante edildi
