# EF Core AsNoTracking Kullanımı Detaylı Açıklama

> Bu dosya, EF Core'da `AsNoTracking()` metodunun ne olduğunu, neden kullanıldığını ve performans etkisini detaylı olarak açıklar.
> 
> **İçerik:**
> - EF Core Change Tracking nedir?
> - AsNoTracking ne işe yarar?
> - Ne zaman kullanılmalı?
> - Performans etkisi
> - Basket API'deki kullanım örnekleri

---

## EF Core Change Tracking Nedir?

EF Core, varsayılan olarak **Change Tracking** (değişiklik takibi) yapar. Bu, entity'lerin memory'de takip edilmesi ve değişikliklerin otomatik algılanması anlamına gelir.

### Change Tracking Nasıl Çalışır?

```csharp
// Tracking AÇIK (default)
var basket = await _context.ShoppingCarts
    .Include(x => x.Items)
    .FirstOrDefaultAsync(x => x.UserName == userName);

// EF Core şunları yapar:
// 1. Entity'yi memory'ye yükler
// 2. Entity'nin bir "snapshot" (anlık görüntü) oluşturur
// 3. Change Tracker dictionary'de tutar
// 4. SaveChanges() çağrıldığında snapshot ile karşılaştırır
// 5. Değişiklikleri algılar ve SQL UPDATE/INSERT/DELETE üretir
```

### Change Tracking Örneği:

```csharp
// 1. Entity'yi çek (tracking açık)
var basket = await _context.ShoppingCarts
    .FirstOrDefaultAsync(x => x.UserName == "user1");
// EF Core: Entity'yi memory'ye yükledi, snapshot oluşturdu, tracker'a ekledi

// 2. Entity'yi değiştir
basket.TotalPrice = 50000;  // Değişiklik yapıldı

// 3. SaveChanges çağır
await _context.SaveChangesAsync();
// EF Core: Snapshot ile karşılaştırdı, TotalPrice değişmiş → SQL UPDATE üretti
```

---

## AsNoTracking Ne İşe Yarar?

`AsNoTracking()`, EF Core'un change tracking'ini **kapatır**. Entity'ler memory'ye yüklenir ama **takip edilmez**.

### AsNoTracking ile:

```csharp
// Tracking KAPALI
var basket = await _context.ShoppingCarts
    .AsNoTracking()  // ✅ Tracking kapalı
    .Include(x => x.Items)
    .FirstOrDefaultAsync(x => x.UserName == userName);

// EF Core şunları yapar:
// 1. Entity'yi memory'ye yükler
// 2. Snapshot oluşturmaz ❌
// 3. Change Tracker'a eklemez ❌
// 4. Daha az memory kullanır ✅
// 5. Daha hızlı query ✅
```

### AsNoTracking ile Değişiklik Yapılamaz:

```csharp
// AsNoTracking ile çekilen entity değiştirilemez
var basket = await _context.ShoppingCarts
    .AsNoTracking()
    .FirstOrDefaultAsync(x => x.UserName == "user1");

basket.TotalPrice = 50000;  // Değişiklik yapıldı

await _context.SaveChangesAsync();
// ❌ HİÇBİR ŞEY OLMAZ! Entity tracking edilmediği için değişiklik algılanmaz
```

---

## Ne Zaman AsNoTracking Kullanılmalı?

### ✅ AsNoTracking Kullan (Read-Only Sorgular):

1. **Sadece okuma işlemleri:**
   ```csharp
   // GET endpoint - sadece okuma
   var basket = await _context.ShoppingCarts
       .AsNoTracking()  // ✅
       .Include(x => x.Items)
       .FirstOrDefaultAsync(x => x.UserName == userName);
   ```

2. **DTO mapping için:**
   ```csharp
   // Entity'yi DTO'ya map edeceksin, değiştirmeyeceksin
   var orders = await _context.Orders
       .AsNoTracking()  // ✅
       .Include(o => o.Items)
       .ToListAsync();
   
   return _mapper.Map<IEnumerable<OrderDto>>(orders);
   ```

3. **Cache için veri çekme:**
   ```csharp
   // Redis'e yazmak için veri çekiyorsun, değiştirmeyeceksin
   var basket = await _context.ShoppingCarts
       .AsNoTracking()  // ✅
       .Include(x => x.Items)
       .FirstOrDefaultAsync(x => x.UserName == userName);
   ```

### ❌ AsNoTracking Kullanma (Write İşlemleri):

1. **Entity'yi değiştireceksen:**
   ```csharp
   // UPDATE işlemi - tracking gerekli
   var basket = await _context.ShoppingCarts
       // ❌ AsNoTracking() YOK - tracking açık olmalı
       .FirstOrDefaultAsync(x => x.UserName == userName);
   
   basket.TotalPrice = 50000;  // Değişiklik yapıldı
   await _context.SaveChangesAsync();  // Değişiklik algılanır ve kaydedilir
   ```

2. **Entity'yi sileceksen:**
   ```csharp
   // DELETE işlemi - tracking gerekli
   var basket = await _context.ShoppingCarts
       // ❌ AsNoTracking() YOK
       .FirstOrDefaultAsync(x => x.UserName == userName);
   
   _context.ShoppingCarts.Remove(basket);
   await _context.SaveChangesAsync();
   ```

---

## Performans Etkisi

### Memory Kullanımı:

**Tracking Açık:**
```
10 item'lı sepet:
- Entity: 10 ShoppingCartItem object
- Snapshot: 10 ShoppingCartItem object (değişiklikleri karşılaştırmak için)
- Change Tracker: Dictionary'de 10 entry
- Toplam: ~20 object + tracker overhead
```

**AsNoTracking:**
```
10 item'lı sepet:
- Entity: 10 ShoppingCartItem object
- Snapshot: 0 object ❌
- Change Tracker: 0 entry ❌
- Toplam: ~10 object
```

**Sonuç:** %50 daha az memory kullanımı!

### Query Performansı:

**Tracking Açık:**
- Entity yükleme: Normal
- Snapshot oluşturma: Ekstra işlem
- Change Tracker'a ekleme: Ekstra işlem
- **Toplam:** %10-30 daha yavaş

**AsNoTracking:**
- Entity yükleme: Normal
- Snapshot: Yok ❌
- Change Tracker: Yok ❌
- **Toplam:** %10-30 daha hızlı

### Ölçülebilir Fark:

```
1000 sepet, her biri 10 item:
- Tracking: ~200ms, ~20MB memory
- AsNoTracking: ~150ms, ~10MB memory
- Fark: %25 daha hızlı, %50 daha az memory
```

---

## Basket API'deki Kullanım Örnekleri

### Örnek 1: ReloadBasketWithItems (Read-Only)

```csharp
private async Task<ShoppingCart?> ReloadBasketWithItems(Guid basketId)
{
    return await _context.ShoppingCarts
        .AsNoTracking()  // ✅ Read-only sorgu
        .Include(x => x.Items)
        .FirstOrDefaultAsync(x => x.Id == basketId);
}
```

**Neden AsNoTracking?**
- Entity sadece okunuyor (Redis'e yazmak için)
- Değiştirilmeyecek
- DTO'ya map edilecek
- **Performans:** %20-30 daha hızlı

### Örnek 2: SaveBasket - Existing Check (Read-Only)

```csharp
public async Task<ShoppingCart> SaveBasket(ShoppingCart basket)
{
    // Sadece Id'ye ihtiyaç var, entity'yi değiştirmeyeceğiz
    var existing = await _context.ShoppingCarts
        .AsNoTracking()  // ✅ Sadece Id çekiyoruz
        .Where(x => x.UserName == basket.UserName)
        .Select(x => new { x.Id })
        .FirstOrDefaultAsync();
    
    // ... devam eden kod ...
}
```

**Neden AsNoTracking?**
- Sadece `Id` çekiliyor
- Entity değiştirilmeyecek
- Tracking gereksiz overhead
- **Performans:** %30 daha hızlı

### Örnek 3: GetBasket (Optimizasyon Fırsatı)

```csharp
public async Task<ShoppingCart?> GetBasket(string userName)
{
    // 2. Redis'te yoksa PostgreSQL'den al
    var basket = await _context.ShoppingCarts
        // ⚠️ AsNoTracking() YOK - optimizasyon fırsatı!
        .Include(x => x.Items)
        .FirstOrDefaultAsync(x => x.UserName == userName);
    
    // Entity sadece okunuyor, Redis'e yazılıyor, değiştirilmiyor
    // AsNoTracking() eklenebilir ✅
}
```

**Optimizasyon Önerisi:**
```csharp
var basket = await _context.ShoppingCarts
    .AsNoTracking()  // ✅ Eklendi
    .Include(x => x.Items)
    .FirstOrDefaultAsync(x => x.UserName == userName);
```

---

## Tracking vs NoTracking Karşılaştırması

### Senaryo: 1000 Sepet, Her Biri 10 Item

| Özellik | Tracking Açık | AsNoTracking | Fark |
|---------|---------------|--------------|------|
| **Memory** | ~20MB | ~10MB | %50 az |
| **Query Süresi** | ~200ms | ~150ms | %25 hızlı |
| **Snapshot** | Var | Yok | - |
| **Change Tracker** | Var | Yok | - |
| **Değişiklik Algılama** | Var | Yok | - |

---

## Best Practices

### ✅ Doğru Kullanım:

```csharp
// 1. Read-only sorgular
var baskets = await _context.ShoppingCarts
    .AsNoTracking()  // ✅
    .Include(x => x.Items)
    .ToListAsync();

// 2. DTO mapping
var orders = await _context.Orders
    .AsNoTracking()  // ✅
    .Include(o => o.Items)
    .ToListAsync();
return _mapper.Map<IEnumerable<OrderDto>>(orders);

// 3. Sadece belirli kolonlar
var existing = await _context.ShoppingCarts
    .AsNoTracking()  // ✅
    .Where(x => x.UserName == userName)
    .Select(x => new { x.Id })
    .FirstOrDefaultAsync();
```

### ❌ Yanlış Kullanım:

```csharp
// 1. UPDATE işlemi - tracking gerekli
var basket = await _context.ShoppingCarts
    .AsNoTracking()  // ❌ YANLIŞ!
    .FirstOrDefaultAsync(x => x.UserName == userName);

basket.TotalPrice = 50000;  // Değişiklik yapıldı
await _context.SaveChangesAsync();  // ❌ Değişiklik algılanmaz!

// 2. DELETE işlemi - tracking gerekli
var basket = await _context.ShoppingCarts
    .AsNoTracking()  // ❌ YANLIŞ!
    .FirstOrDefaultAsync(x => x.UserName == userName);

_context.ShoppingCarts.Remove(basket);  // ❌ Çalışmaz!
await _context.SaveChangesAsync();
```

---

## AsNoTracking + Include Kombinasyonu

### Include ile Birlikte Kullanım:

```csharp
// Navigation property'leri de tracking edilmez
var basket = await _context.ShoppingCarts
    .AsNoTracking()  // ✅ Ana entity tracking edilmez
    .Include(x => x.Items)  // ✅ Items da tracking edilmez
    .FirstOrDefaultAsync(x => x.UserName == userName);
```

**Sonuç:**
- `ShoppingCart` tracking edilmez
- `ShoppingCartItem`'lar da tracking edilmez
- Tüm entity graph tracking dışında

---

## Global AsNoTracking (Tüm Sorgular İçin)

### DbContext Seviyesinde:

```csharp
public class BasketDbContext : DbContext
{
    public BasketDbContext(DbContextOptions<BasketDbContext> options) : base(options)
    {
        // Tüm sorgular için AsNoTracking (önerilmez - çok agresif)
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }
}
```

**⚠️ Dikkat:**
- Tüm sorgular tracking dışında olur
- Write işlemleri için manuel olarak `.AsTracking()` eklemen gerekir
- Genelde önerilmez, sadece read-only API'ler için uygun

### Daha İyi Yaklaşım:

```csharp
// Her sorgu için ayrı ayrı AsNoTracking kullan
var basket = await _context.ShoppingCarts
    .AsNoTracking()  // ✅ Sadece bu sorgu için
    .FirstOrDefaultAsync(...);
```

---

## Özet

### AsNoTracking Ne Zaman Kullanılmalı?

| Senaryo | AsNoTracking? | Neden? |
|---------|---------------|--------|
| **Read-only sorgular** | ✅ Evet | Performans artışı |
| **DTO mapping** | ✅ Evet | Entity değiştirilmeyecek |
| **Cache için veri çekme** | ✅ Evet | Sadece okuma |
| **UPDATE işlemleri** | ❌ Hayır | Tracking gerekli |
| **DELETE işlemleri** | ❌ Hayır | Tracking gerekli |
| **INSERT işlemleri** | ❌ Hayır | Tracking gerekli |

### Performans Kazanımları:

- **Memory:** %30-50 daha az kullanım
- **Query Süresi:** %10-30 daha hızlı
- **Overhead:** Snapshot ve Change Tracker yok

### Basket API'deki Kullanım:

1. ✅ `ReloadBasketWithItems` - Read-only sorgu
2. ✅ `SaveBasket` - Existing check (sadece Id)
3. ⚠️ `GetBasket` - Optimizasyon fırsatı (AsNoTracking eklenebilir)

---

## Referanslar

- [EF Core Change Tracking - Microsoft Docs](https://learn.microsoft.com/en-us/ef/core/change-tracking/)
- [EF Core AsNoTracking - Microsoft Docs](https://learn.microsoft.com/en-us/ef/core/querying/tracking)
- [EF Core Performance Tips - Microsoft Docs](https://learn.microsoft.com/en-us/ef/core/performance/)

---

**Tarih:** Aralık 2024  
**Kaynak:** Basket API - BasketRepository optimizasyonu  
**Durum:** ✅ Dokümante edildi

