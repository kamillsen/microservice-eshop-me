# Custom Exception Class'ları - Öğrenme Notları

> NotFoundException, BadRequestException, InternalServerException - Kullanım ve Mantık

---

## `base` ve Constructor İlişkisi

### `: base(message)` Ne Demek?

```csharp
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    //                        ↑              ↑
    //                        │              └─ Aynı message değişkeni
    //                        └─ Parametre olarak gelen message
}
```

**Açıklama:**
- `NotFoundException` → `Exception` sınıfından türer (inheritance)
- `: base(message)` → Üst sınıfın (`Exception`) constructor'ını çağırır
- Parametre olarak alınan `message`, `Exception` sınıfına iletilir
- `Exception.Message` property'si bu şekilde doldurulur

**Akış:**
```
1. throw new NotFoundException("Product not found")
2. NotFoundException constructor çalışıyor → message = "Product not found"
3. base(message) → Exception constructor'ına gidiyor
4. Exception.Message = "Product not found" olarak kaydediliyor
```

---

## Exception Class'ları ve Kullanımları

### 1. NotFoundException

**Constructor'lar:**
```csharp
// Constructor 1: Sadece mesaj
public NotFoundException(string message) : base(message)

// Constructor 2: Entity adı + Key (standart format)
public NotFoundException(string name, object key)
    : base($"Entity \"{name}\" ({key}) was not found.")
```

**Kullanım:**
```csharp
throw new NotFoundException("Product", 123);
// Sonuç: "Entity "Product" (123) was not found."
```

---

### 2. BadRequestException

**Constructor:**
```csharp
public BadRequestException(string message) : base(message)
```

**Kullanım:**
```csharp
throw new BadRequestException("Product ID must be greater than 0");
```

---

### 3. InternalServerException

**Constructor'lar:**
```csharp
// Constructor 1: Sadece mesaj
public InternalServerException(string message) : base(message)

// Constructor 2: Mesaj + InnerException (orijinal hatayı sakla)
public InternalServerException(string message, Exception innerException)
    : base(message, innerException)
```

**Kullanım:**
```csharp
// Sadece mesaj
throw new InternalServerException("An error occurred");

// Mesaj + InnerException (önerilen - debug için)
catch (SqlException ex)
{
    throw new InternalServerException("Database error occurred", ex);
}
```

**InnerException Nedir?**
- Orijinal hatayı saklar (debug için)
- Kullanıcıya genel mesaj, log'a detaylı hata
- Hata zincirini korur

---

## Pratik Senaryo: ProductController

**Tek örnekte 3 exception'ın kullanımı:**

```csharp
// Catalog.API - ProductController.cs
public async Task<IActionResult> GetProduct(int id)
{
    // 1. Validasyon (BadRequestException)
    if (id <= 0)
    {
        throw new BadRequestException("Product ID must be greater than 0");
    }
    
    try
    {
        // 2. Veritabanı sorgusu
        var product = await _context.Products.FindAsync(id);
        
        // 3. Bulunamadı (NotFoundException)
        if (product == null)
        {
            throw new NotFoundException("Product", id);
            // Sonuç: "Entity "Product" (123) was not found."
        }
        
        return Ok(product);
    }
    catch (SqlException ex)
    {
        // 4. Veritabanı hatası (InternalServerException)
        throw new InternalServerException(
            "Database error occurred while fetching product", 
            ex  // ← Orijinal hata burada saklanıyor
        );
    }
}
```

**Akış:**
1. ✅ Validasyon geçerse → Veritabanı sorgusu
2. ✅ Product bulunursa → Döndür
3. ❌ Product bulunamazsa → `NotFoundException`
4. ❌ Veritabanı hatası → `InternalServerException` (innerException ile)

---

## Özet Tablo

| Exception | HTTP Status | Constructor Sayısı | Ne Zaman Kullan? |
|-----------|-------------|-------------------|------------------|
| **NotFoundException** | 404 | 2 | Kayıt bulunamadığında |
| **BadRequestException** | 400 | 1 | Geçersiz istek/validasyon hatası |
| **InternalServerException** | 500 | 2 | Beklenmeyen sunucu hatası |

---

## Constructor Özeti

### NotFoundException
- ✅ **2 constructor:** sadece mesaj, veya entity adı + key
- ✅ Kayıt bulunamadığında kullan

### BadRequestException
- ✅ **1 constructor:** mesaj
- ✅ Geçersiz istek/validasyon hatasında kullan

### InternalServerException
- ✅ **2 constructor:** sadece mesaj, veya mesaj + innerException
- ✅ Beklenmeyen sunucu hatasında kullan
- ✅ InnerException ile orijinal hatayı sakla

---

## Önemli Noktalar

1. **`base(message)`** → Üst sınıfın constructor'ını çağırır, mesajı iletir
2. **String Interpolation** → `$"Entity \"{name}\" ({key})"` formatında mesaj oluşturur
3. **InnerException** → Orijinal hatayı saklar, debug için önemli
4. **Standart Format** → `NotFoundException("Product", id)` tutarlı mesaj üretir

---

**Son Güncelleme:** Aralık 2024

