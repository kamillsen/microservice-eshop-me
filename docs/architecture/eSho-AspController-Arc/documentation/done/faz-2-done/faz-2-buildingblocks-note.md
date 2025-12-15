# Faz 2.1 - BuildingBlocks.Exceptions Notları

> Bu dosya, Faz 2.1 (BuildingBlocks.Exceptions) adım adım yaparken öğrendiklerimi not aldığım dosyadır.

---

## BuildingBlocks Nedir?

**BuildingBlocks**, tüm microservice'lerde ortak kullanılacak kod parçalarını içeren class library projeleridir. Bu sayede:

- ✅ Kod tekrarı önlenir
- ✅ Tutarlılık sağlanır
- ✅ Merkezi yönetim yapılır
- ✅ Bakım kolaylaşır

### Bu Bölümde:

**BuildingBlocks.Exceptions** → Exception handling (hata yönetimi)

> **Not:** 2.2 (BuildingBlocks.Behaviors) ve 2.3 (BuildingBlocks.Messaging) henüz yapılmadı. Yapıldığında ayrı dokümantasyon eklenecek.

---

## 2.1 BuildingBlocks.Exceptions - Yapılanlar

### Adım 1: Class Library Projesi Oluştur

**Komut:**
```bash
cd src/BuildingBlocks
dotnet new classlib -n BuildingBlocks.Exceptions
```

**Açıklamalar:**
- `cd src/BuildingBlocks` → BuildingBlocks klasörüne geç
- `dotnet new classlib` → Yeni class library projesi oluştur
- `-n BuildingBlocks.Exceptions` → Proje adı

**Ne işe yarar:**
- Paylaşılan exception yapısı için class library projesi oluşturur
- Bu proje, diğer servisler tarafından referans edilecek
- Class library = çalıştırılabilir değil, sadece kod içerir (kütüphane)

**Sonuç:** 
- `src/BuildingBlocks/BuildingBlocks.Exceptions/` klasörü oluşturuldu
- `BuildingBlocks.Exceptions.csproj` dosyası oluşturuldu
- Varsayılan `Class1.cs` dosyası oluşturuldu (sonra silinecek)

---

### Adım 2: Projeyi Solution'a Ekle

**Komut:**
```bash
cd ../..
dotnet sln add src/BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj
```

**Açıklamalar:**
- `cd ../..` → Proje root dizinine dön (2 seviye yukarı)
- `dotnet sln add` → Solution'a proje ekle
- `src/BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj` → Eklenecek proje dosyasının yolu

**Ne işe yarar:**
- Projeyi solution'a ekler
- `dotnet sln list` ile görülebilir
- Diğer projeler bu projeyi referans edebilir
- IDE'lerde (VS Code, Visual Studio) solution içinde görünür

**Kontrol:**
```bash
dotnet sln list
```

**Beklenen çıktı:**
```
Project(s)
----------
src/BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj
```

**Sonuç:** ✅ Proje solution'a eklendi

---

### Adım 3: NuGet Paketlerini Ekle

**Komut:**
```bash
cd src/BuildingBlocks/BuildingBlocks.Exceptions
dotnet add package Microsoft.AspNetCore.Diagnostics
```

**Açıklamalar:**
- `cd src/BuildingBlocks/BuildingBlocks.Exceptions` → Proje klasörüne geç
- `dotnet add package` → NuGet paketi ekle
- `Microsoft.AspNetCore.Diagnostics` → ProblemDetails formatı için gerekli paket

**Ne işe yarar:**
- `Microsoft.AspNetCore.Diagnostics` paketi, ProblemDetails formatını sağlar
- ProblemDetails = RFC 7807 standardı (API hata response formatı)
- Global exception handler'da kullanılacak

**Paket Detayları:**
- `Microsoft.AspNetCore.Diagnostics` → ProblemDetails için
- Bu paket, standart hata formatı sağlar:
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7807",
    "title": "Not Found",
    "status": 404,
    "detail": "Product with id 999 not found"
  }
  ```

**Sonuç:** ✅ Paket eklendi, `BuildingBlocks.Exceptions.csproj` dosyasına otomatik eklendi

---

### Adım 4: Klasör Yapısını Oluştur

**Komut:**
```bash
mkdir Exceptions
mkdir Handler
```

**Açıklamalar:**
- `mkdir Exceptions` → Exception class'ları için klasör
- `mkdir Handler` → Global exception handler için klasör

**Ne işe yarar:**
- Kod organizasyonu için klasör yapısı oluşturur
- `Exceptions/` → Custom exception class'ları (NotFoundException, BadRequestException, vb.)
- `Handler/` → Global exception handler middleware

**Klasör Yapısı:**
```
BuildingBlocks.Exceptions/
├── Exceptions/
│   ├── NotFoundException.cs
│   ├── BadRequestException.cs
│   └── InternalServerException.cs
├── Handler/
│   └── GlobalExceptionHandler.cs
└── BuildingBlocks.Exceptions.csproj
```

**Sonuç:** ✅ Klasör yapısı oluşturuldu

---

### Adım 5: Exception Class'larını Oluştur

**Ne yapacağız:** Tüm servislerde kullanılacak özel exception tiplerini oluşturacağız.

**Oluşturulacak Dosyalar:**

1. **`Exceptions/NotFoundException.cs`** → 404 Not Found hatası
2. **`Exceptions/BadRequestException.cs`** → 400 Bad Request hatası
3. **`Exceptions/InternalServerException.cs`** → 500 Internal Server hatası

**Ne işe yarar:**
- Her exception tipi, farklı HTTP status code'u temsil eder
- Tüm servislerde aynı exception tipleri kullanılır (tutarlılık)
- Global exception handler bu exception'ları yakalayıp ProblemDetails formatına çevirir

**Kod Yapısı (Örnek):**
```csharp
// NotFoundException.cs
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
```

**Sonuç:** ✅ Exception class'ları oluşturuldu

---

### Adım 6: Global Exception Handler Oluştur

**Ne yapacağız:** Tüm exception'ları yakalayıp standart ProblemDetails formatında response dönen middleware oluşturacağız.

**Oluşturulacak Dosya:**
- `Handler/GlobalExceptionHandler.cs`

**Ne işe yarar:**
- Tüm exception'ları yakalar (try-catch gibi, ama global)
- Exception tipine göre HTTP status code belirler
- ProblemDetails formatında response döner (RFC 7807 standardı)
- Tüm servislerde aynı hata formatı kullanılır

**Kod Yapısı (Örnek):**
```csharp
// GlobalExceptionHandler.cs
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Exception'ı ProblemDetails'e çevir
        // HTTP status code belirle
        // Response döndür
    }
}
```

**Sonuç:** ✅ Global exception handler oluşturuldu

---

### 2.1 Bölümü - Tamamlanan Kontroller

✅ BuildingBlocks.Exceptions projesi oluşturuldu
✅ Proje solution'a eklendi
✅ Microsoft.AspNetCore.Diagnostics paketi eklendi
✅ Klasör yapısı oluşturuldu (Exceptions/, Handler/)
✅ Exception class'ları oluşturuldu
✅ GlobalExceptionHandler oluşturuldu
✅ Proje build oluyor mu? (`dotnet build`)

---

## Öğrenilenler

### Class Library Projesi Nedir?

**Class Library** = Paylaşılan kod kütüphanesi

- Çalıştırılabilir bir uygulama değildir (exe/dll olarak çalışmaz)
- Diğer projeler tarafından kullanılmak üzere kod içeren bir kütüphanedir
- Ortak kodları tek bir yerde toplar, diğer projeler bunu referans eder

**Fark:**

| Proje Tipi | Ne İşe Yarar | Çalıştırılabilir mi? |
|------------|--------------|----------------------|
| **Web API** | REST API servisi | ✅ Evet (çalıştırılır) |
| **Class Library** | Paylaşılan kod | ❌ Hayır (sadece referans edilir) |

### Solution'a Proje Ekleme

**Neden gerekli?**
- Projeyi solution'a eklemek, projenin solution'ın bir parçası olduğunu belirtir
- IDE'lerde (VS Code, Visual Studio) görünür
- `dotnet sln list` ile kontrol edilebilir
- Diğer projeler bu projeyi referans edebilir

**Komut:**
```bash
dotnet sln add <proje-yolu>
```

### ProblemDetails Nedir?

**ProblemDetails** = RFC 7807 standardı (API hata response formatı)

**Örnek Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Not Found",
  "status": 404,
  "detail": "Product with id 999 not found",
  "instance": "/api/products/999"
}
```

**Avantajları:**
- Standart format (tüm API'lerde aynı)
- Kullanıcı dostu hata mesajları
- Güvenlik (stack trace göstermez)

---

## Diğer Notlar

### [Tarih: ...]
- ...

---

**Son Güncelleme:** Aralık 2024

