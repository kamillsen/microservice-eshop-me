# Class Library Projesi - Öğrenme Notları

> Class Library = Paylaşılan Kod Kütüphanesi

---

## Class Library Nedir?

**Class Library**, çalıştırılabilir bir uygulama değildir (exe/dll olarak çalışmaz). Diğer projeler tarafından kullanılmak üzere kod içeren bir kütüphanedir.

**Ne demek?**
- Ortak kodları tek bir yerde toplar
- Diğer projeler bunu referans eder
- Tek başına çalıştırılamaz, başka projeler tarafından kullanılır

---

## Örnek: BuildingBlocks.Exceptions

```
┌─────────────────────────────────────────┐
│  BuildingBlocks.Exceptions              │
│  (Class Library)                        │
│                                         │
│  - NotFoundException.cs                 │
│  - BadRequestException.cs               │
│  - GlobalExceptionHandler.cs            │
└──────────────┬──────────────────────────┘
               │
               │ Referans verir
               │
    ┌──────────┼──────────┐
    │          │          │
    ▼          ▼          ▼
┌────────┐ ┌────────┐ ┌────────┐
│Catalog │ │ Basket │ │Ordering│
│  API   │ │  API   │ │  API   │
└────────┘ └────────┘ └────────┘
```

**Açıklama:**
- `BuildingBlocks.Exceptions` → Class Library (kütüphane)
- Exception class'ları içerir
- Catalog.API, Basket.API, Ordering.API bunu kullanır
- Tek başına çalışmaz, başka projeler tarafından kullanılır

---

## Fark: Web API vs Class Library

| Proje Tipi | Ne İşe Yarar | Çalıştırılabilir mi? |
|------------|--------------|----------------------|
| **Web API** | REST API servisi | ✅ Evet (çalıştırılır) |
| **Class Library** | Paylaşılan kod | ❌ Hayır (sadece referans edilir) |

---

## Bizim Durumumuzda

### BuildingBlocks.Exceptions → Class Library

**İçerir:**
- Exception class'ları (NotFoundException, BadRequestException, vb.)
- GlobalExceptionHandler

**Kullanır:**
- Catalog.API
- Basket.API
- Ordering.API
- Discount.Grpc

**Nasıl Kullanılır?**
1. Diğer projeler bu projeyi referans eder
2. Exception class'larını kullanır
3. GlobalExceptionHandler'ı kullanır

---

## Komut Açıklaması

```bash
dotnet new classlib -n BuildingBlocks.Exceptions
```

**Açıklama:**
- `dotnet new classlib` → Class library projesi oluştur
- `-n BuildingBlocks.Exceptions` → Proje adı

**Ne yapar?**
- İçinde exception class'ları olacak bir kütüphane projesi oluşturur
- Çalıştırılabilir değil, sadece kod içerir
- Diğer projeler tarafından referans edilebilir

---

## Pratik Örnek

### Senaryo: Catalog.API, BuildingBlocks.Exceptions'i Kullanıyor

```
1. Catalog.API projesi oluşturulur
2. BuildingBlocks.Exceptions'e referans verilir
3. Catalog.API içinde:
   - throw new NotFoundException("Product not found");
   - GlobalExceptionHandler otomatik yakalar
```

**Kod Örneği:**

```csharp
// Catalog.API/Features/Products/Queries/GetProductById/GetProductByIdHandler.cs
public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FindAsync(request.Id);
        
        if (product == null)
        {
            // BuildingBlocks.Exceptions'den gelen class
            throw new NotFoundException($"Product with id {request.Id} not found");
        }
        
        return _mapper.Map<ProductDto>(product);
    }
}
```

---

## Özet

**Class Library:**
- ✅ Paylaşılan kod kütüphanesi
- ✅ Çalıştırılamaz, sadece referans edilir
- ✅ Kod tekrarını önler
- ✅ Tutarlılık sağlar

**Bizim Projede:**
- BuildingBlocks.Exceptions → Exception handling için
- BuildingBlocks.Behaviors → MediatR pipeline için
- BuildingBlocks.Messaging → RabbitMQ event'ler için

---

**Son Güncelleme:** Aralık 2024

