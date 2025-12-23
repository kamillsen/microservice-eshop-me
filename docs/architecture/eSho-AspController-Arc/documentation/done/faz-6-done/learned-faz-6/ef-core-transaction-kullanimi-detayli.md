# EF Core Transaction Kullanımı Detaylı Açıklama

> Bu dosya, Basket API'deki `BasketRepository.SaveBasket` metodunda kullanılan `await using var transaction` pattern'ini detaylı olarak açıklar.
> 
> **İçerik:**
> - `await using var transaction` syntax açıklaması
> - Transaction lifecycle
> - Race condition önleme
> - Otomatik dispose mekanizması
> - Best practices

---

## `await using var transaction = await _context.Database.BeginTransactionAsync();`

Bu satır, EF Core'da birden fazla veritabanı işlemini atomik (all-or-nothing) hale getirmek için kullanılır.

### Satırın Parçaları

#### 1. `await using` - Async Dispose Pattern

```csharp
await using var transaction = ...
```

**Ne İşe Yarar:**
- `using`: `IDisposable` kaynakların otomatik dispose edilmesi için (C# 1.0+)
- `await using`: `IAsyncDisposable` kaynakların otomatik dispose edilmesi için (C# 8.0+)
- `IDatabaseTransaction` (EF Core) `IAsyncDisposable` interface'ini implement eder

**Nasıl Çalışır:**
- Metod bitince (normal veya exception) transaction otomatik olarak dispose edilir
- Manuel `transaction.DisposeAsync()` çağrısına gerek yok
- `finally` bloğu gibi çalışır, ama daha temiz syntax

**Eşdeğer Kod (Manuel):**
```csharp
IDatabaseTransaction transaction = null;
try
{
    transaction = await _context.Database.BeginTransactionAsync();
    // ... kod ...
    await transaction.CommitAsync();
}
catch
{
    if (transaction != null)
        await transaction.RollbackAsync();
    throw;
}
finally
{
    if (transaction != null)
        await transaction.DisposeAsync();
}
```

**`await using` ile:**
```csharp
await using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // ... kod ...
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
// finally bloğu gerekmez - otomatik dispose edilir
```

---

#### 2. `var transaction` - Transaction Değişkeni

```csharp
var transaction = ...
```

**Transaction Tipi:**
- `IDatabaseTransaction` (EF Core'dan)
- Bu transaction ile şu işlemler yapılabilir:
  - `CommitAsync()` → Değişiklikleri kalıcı hale getirir
  - `RollbackAsync()` → Değişiklikleri geri alır
  - `DisposeAsync()` → Transaction'ı kapatır (otomatik)

---

#### 3. `await _context.Database.BeginTransactionAsync()` - Transaction Başlatma

```csharp
await _context.Database.BeginTransactionAsync()
```

**Ne İşe Yarar:**
- PostgreSQL'de yeni bir transaction başlatır
- Bu transaction içindeki tüm SQL işlemleri atomik olur
- **ACID özellikleri:**
  - **Atomicity:** Ya hepsi başarılı, ya hiçbiri (all-or-nothing)
  - **Consistency:** Veri tutarlılığı garantisi
  - **Isolation:** Diğer transaction'lar ara durumu görmez
  - **Durability:** Commit sonrası değişiklikler kalıcı

---

## Neden Transaction Kullanıyoruz?

### Senaryo: Sepet Güncelleme (BasketRepository.SaveBasket)

**Transaction Olmadan:**
```csharp
// 1. Eski item'ları sil
await _context.ShoppingCartItems.ExecuteDeleteAsync();  // DELETE - HEMEN DB'YE GİDİYOR

// 2. Yeni item'ları ekle
_context.ShoppingCartItems.AddRange(newItems);         // INSERT - HENÜZ DB'YE GİTMEDİ

// 3. Değişiklikleri kaydet
await _context.SaveChangesAsync();                     // INSERT commit
```

**Sorun: Race Condition**
```
Zaman  | İstek 1 (SaveBasket)          | İstek 2 (GetBasket)
-------|--------------------------------|----------------------
T0     | ExecuteDeleteAsync çalıştı     |
T1     |                                | DB'den oku → BOŞ SEPET
T2     |                                | Redis'e yaz → BOŞ SEPET cache'lendi ❌
T3     | AddRange + SaveChanges         |
T4     | DB'de dolu sepet var           | Cache'de boş sepet var ❌
```

**Sonuç:** Veri tutarsızlığı! Cache'de boş sepet, DB'de dolu sepet.

**Transaction ile:**
```csharp
await using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // 1. Eski item'ları sil
    await _context.ShoppingCartItems.ExecuteDeleteAsync();  // DELETE (transaction içinde)
    
    // 2. Yeni item'ları ekle
    _context.ShoppingCartItems.AddRange(newItems);          // INSERT (transaction içinde)
    
    // 3. Değişiklikleri kaydet
    await _context.SaveChangesAsync();                      // INSERT commit (transaction içinde)
    
    // 4. Transaction commit et
    await transaction.CommitAsync();                        // TÜM DEĞİŞİKLİKLER ATOMIK
}
catch
{
    // Hata olursa transaction rollback et
    await transaction.RollbackAsync();  // Tüm değişiklikler geri alınır
    throw;
}
```

**Sonuç:** DELETE + INSERT + SaveChanges atomik olarak çalışır. Başka istekler ara durumu görmez.

---

## Transaction Lifecycle

### Başarılı Senaryo:

```
1. BeginTransactionAsync()  → Transaction başlar
   ↓
2. ExecuteDeleteAsync()     → DELETE (transaction içinde, henüz commit olmadı)
   ↓
3. AddRange()                → INSERT hazırlığı (transaction içinde)
   ↓
4. SaveChangesAsync()        → INSERT commit (transaction içinde, henüz DB'ye yazılmadı)
   ↓
5. CommitAsync()             → Tüm değişiklikler kalıcı olur (DB'ye yazılır)
   ↓
6. DisposeAsync()            → Transaction kapatılır (otomatik - await using sayesinde)
```

### Hata Senaryosu:

```
1. BeginTransactionAsync()  → Transaction başlar
   ↓
2. ExecuteDeleteAsync()     → DELETE (transaction içinde)
   ↓
3. HATA OLUŞUR              → Exception fırlatılır
   ↓
4. RollbackAsync()          → Tüm değişiklikler geri alınır (DELETE iptal olur)
   ↓
5. DisposeAsync()           → Transaction kapatılır (otomatik - await using sayesinde)
```

---

## Örnek: BasketRepository.SaveBasket

### Tam Kod:

```csharp
public async Task<ShoppingCart> SaveBasket(ShoppingCart basket)
{
    // 1. Mevcut sepeti kontrol et (sadece Id'ye ihtiyaç var)
    var existing = await _context.ShoppingCarts
        .AsNoTracking()
        .Where(x => x.UserName == basket.UserName)
        .Select(x => new { x.Id })
        .FirstOrDefaultAsync();

    // 2. Transaction başlat (ExecuteDelete + Insert + SaveChanges atomik olsun)
    await using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        if (existing == null)
        {
            // YENİ SEPET: ID'ler atanıp PostgreSQL'e eklenir
            basket.Id = Guid.NewGuid();
            basket.Items.ForEach(item => item.Id = Guid.NewGuid());
            _context.ShoppingCarts.Add(basket);
        }
        else
        {
            // MEVCUT SEPET: Eski item'lar silinir, yeni item'lar eklenir
            // Transaction içinde olduğu için atomik: Delete + Insert + SaveChanges birlikte commit olur
            await _context.ShoppingCartItems
                .Where(x => x.ShoppingCartId == existing.Id)
                .ExecuteDeleteAsync();  // DELETE (transaction içinde)
            
            var newItems = basket.Items.Select(item =>
            {
                var entity = _mapper.Map<ShoppingCartItem>(item);
                entity.Id = Guid.NewGuid();
                entity.ShoppingCartId = existing.Id;
                return entity;
            }).ToList();
            
            _context.ShoppingCartItems.AddRange(newItems);  // INSERT (transaction içinde)
        }

        // 3. Tüm değişiklikleri PostgreSQL'e kaydet
        await _context.SaveChangesAsync();  // INSERT commit (transaction içinde)
        
        // 4. Transaction commit et (tüm değişiklikler atomik olarak kaydedildi)
        await transaction.CommitAsync();
    }
    catch
    {
        // Hata olursa transaction rollback et
        await transaction.RollbackAsync();
        throw;
    }

    // 5. Transaction otomatik dispose edilir (await using sayesinde)
    // ... devam eden kod ...
}
```

### Adım Adım Açıklama:

1. **Transaction Başlatma:**
   ```csharp
   await using var transaction = await _context.Database.BeginTransactionAsync();
   ```
   - PostgreSQL'de yeni transaction başlatılır
   - `await using` sayesinde metod bitince otomatik dispose edilir

2. **DELETE İşlemi:**
   ```csharp
   await _context.ShoppingCartItems.ExecuteDeleteAsync();
   ```
   - Eski item'lar silinir
   - **Ama henüz commit olmadı** - transaction içinde
   - Başka istekler bu değişikliği görmez

3. **INSERT Hazırlığı:**
   ```csharp
   _context.ShoppingCartItems.AddRange(newItems);
   ```
   - Yeni item'lar EF Core'a eklenir
   - **Henüz DB'ye gitmedi** - transaction içinde

4. **SaveChanges:**
   ```csharp
   await _context.SaveChangesAsync();
   ```
   - INSERT işlemi commit edilir
   - **Ama transaction henüz commit olmadı** - hala transaction içinde

5. **Transaction Commit:**
   ```csharp
   await transaction.CommitAsync();
   ```
   - **Tüm değişiklikler (DELETE + INSERT) atomik olarak DB'ye yazılır**
   - Artık başka istekler yeni durumu görebilir

6. **Otomatik Dispose:**
   - Metod bitince `await using` sayesinde `transaction.DisposeAsync()` otomatik çağrılır
   - Transaction kapatılır

---

## Best Practices

### ✅ Doğru Kullanım:

```csharp
await using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // Tüm DB işlemleri
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
// Otomatik dispose
```

### ❌ Yanlış Kullanım 1: Commit Unutmak

```csharp
await using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    await _context.SaveChangesAsync();
    // ❌ Commit unutuldu!
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
// Transaction dispose olur ama değişiklikler commit olmadı - ROLLBACK olur!
```

### ❌ Yanlış Kullanım 2: Rollback Unutmak

```csharp
await using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    // ❌ Rollback unutuldu!
    throw;
}
// Hata durumunda transaction otomatik rollback olur ama açıkça belirtmek daha iyi
```

### ✅ Önerilen: Explicit Rollback

```csharp
await using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();  // ✅ Açıkça rollback
    throw;
}
```

---

## Transaction vs SaveChanges

### SaveChanges Tek Başına:

```csharp
_context.Orders.Add(order);
await _context.SaveChangesAsync();  // Tek bir işlem - transaction otomatik
```

- **Ne zaman yeterli:** Tek bir entity üzerinde işlem yapıyorsan
- **Transaction gerekmez:** EF Core otomatik transaction kullanır

### Transaction Gerekli:

```csharp
// Birden fazla işlem - transaction gerekli
await _context.OrderItems.ExecuteDeleteAsync();  // DELETE
_context.OrderItems.AddRange(newItems);          // INSERT
await _context.SaveChangesAsync();               // INSERT commit
```

- **Ne zaman gerekli:** Birden fazla işlem (DELETE + INSERT) atomik olmalı
- **Race condition riski:** Ara durumda başka istekler tutarsız veri görebilir

---

## Özet Tablo

| Kısım | Ne İşe Yarar | Önem |
|-------|--------------|------|
| `await using` | Transaction'ı otomatik dispose eder | ⭐⭐⭐ Kritik |
| `var transaction` | Transaction referansını tutar | ⭐⭐ Önemli |
| `BeginTransactionAsync()` | PostgreSQL'de yeni transaction başlatır | ⭐⭐⭐ Kritik |
| `CommitAsync()` | Tüm değişiklikleri kalıcı hale getirir | ⭐⭐⭐ Kritik |
| `RollbackAsync()` | Hata durumunda tüm değişiklikleri geri alır | ⭐⭐⭐ Kritik |

---

## Neden Önemli?

### 1. Veri Tutarlılığı
- DELETE + INSERT atomik olur
- Ara durumda tutarsız veri oluşmaz

### 2. Race Condition Önleme
- Başka istekler ara durumu görmez
- Cache ve DB tutarlı kalır

### 3. Hata Yönetimi
- Hata olursa otomatik rollback
- Veri kaybı olmaz

### 4. Kod Temizliği
- Manuel dispose gerekmez
- `await using` ile otomatik yönetim

---

## Referanslar

- [EF Core Transactions - Microsoft Docs](https://learn.microsoft.com/en-us/ef/core/saving/transactions)
- [IAsyncDisposable Interface - Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/api/system.iasyncdisposable)
- [C# 8.0 await using - Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/async-using)

---

**Tarih:** Aralık 2024  
**Kaynak:** Basket API - BasketRepository.SaveBasket optimizasyonu  
**Durum:** ✅ Dokümante edildi

