# Proto Dosyasından Generate Edilen C# Kodları

> **Tarih:** Aralık 2024  
> **Faz:** Faz 4 - Discount Service  
> **Konu:** Proto dosyasından otomatik oluşturulan C# kodları

---

## Soru: obj/Protos/ Klasöründeki Dosyalar Nasıl Oluşuyor?

`obj/Debug/net9.0/Protos/` klasöründeki dosyalar, proto dosyasından otomatik oluşturulan C# kodlarıdır.

---

## Nasıl Oluşuyorlar?

### 1. .csproj Dosyasındaki Tanım

**Dosya:** `Discount.Grpc.csproj`

```xml
<ItemGroup>
  <Protobuf Include="Protos\discount.proto" GrpcServices="Server" />
</ItemGroup>
```

**Bu satır ne yapıyor?**
- `Protos\discount.proto` dosyasını bulur
- `GrpcServices="Server"` → Server tarafı kodları generate eder
- Build sırasında otomatik C# kodları oluşturur

---

### 2. Build Süreci

```
1. dotnet build çalıştırıldığında
   ↓
2. MSBuild, .csproj'daki <Protobuf> elementini görür
   ↓
3. Grpc.Tools paketi devreye girer (Grpc.AspNetCore paketi ile gelir)
   ↓
4. Proto dosyasını okur (Protos/discount.proto)
   ↓
5. C# kodları generate eder:
   - Discount.cs → Message tipleri (CouponModel, GetDiscountRequest, vb.)
   - DiscountGrpc.cs → Service base class (DiscountProtoService.DiscountProtoServiceBase)
   ↓
6. obj/Debug/net9.0/Protos/ klasörüne kaydeder
```

---

## Oluşturulan Dosyalar

### Discount.cs → Message Tipleri

**Ne içeriyor?**
Proto dosyasındaki tüm `message` tiplerinin C# class karşılıkları.

**Proto dosyasındaki message'lar:**
```protobuf
message GetDiscountRequest {
  string productName = 1;
}

message CouponModel {
  int32 id = 1;
  string productName = 2;
  string description = 3;
  int32 amount = 4;
}

message CreateDiscountRequest {
  CouponModel coupon = 1;
}

message UpdateDiscountRequest {
  CouponModel coupon = 1;
}

message DeleteDiscountRequest {
  string productName = 1;
}

message DeleteDiscountResponse {
  bool success = 1;
}
```

**Generate edilen C# class'ları:**
1. **`GetDiscountRequest`** → GetDiscount RPC metodunun request tipi
   - `ProductName` (string) property'si
   - Parser, Builder pattern, vb.

2. **`CouponModel`** → Kupon bilgilerini taşıyan model
   - `Id` (int32) property'si
   - `ProductName` (string) property'si
   - `Description` (string) property'si
   - `Amount` (int32) property'si

3. **`CreateDiscountRequest`** → CreateDiscount RPC metodunun request tipi
   - `Coupon` (CouponModel) property'si

4. **`UpdateDiscountRequest`** → UpdateDiscount RPC metodunun request tipi
   - `Coupon` (CouponModel) property'si

5. **`DeleteDiscountRequest`** → DeleteDiscount RPC metodunun request tipi
   - `ProductName` (string) property'si

6. **`DeleteDiscountResponse`** → DeleteDiscount RPC metodunun response tipi
   - `Success` (bool) property'si

**Her class içinde:**
- Property'ler (proto field'larından)
- `Parser` (deserialization için)
- `Builder` pattern (object oluşturma için)
- `CalculateSize()` (serialization için)
- `Clone()` (deep copy için)

---

### DiscountGrpc.cs → Service Base Class

**Ne içeriyor?**
Proto dosyasındaki `service` tanımından oluşturulan base class.

**Proto dosyasındaki service:**
```protobuf
service DiscountProtoService {
  rpc GetDiscount (GetDiscountRequest) returns (CouponModel);
  rpc CreateDiscount (CreateDiscountRequest) returns (CouponModel);
  rpc UpdateDiscount (UpdateDiscountRequest) returns (CouponModel);
  rpc DeleteDiscount (DeleteDiscountRequest) returns (DeleteDiscountResponse);
}
```

**Generate edilen C# class'ları:**
1. **`DiscountProtoService`** (static partial class)
   - Service metadata
   - Method descriptor'lar
   - Marshaller'lar (serialization/deserialization)

2. **`DiscountProtoServiceBase`** (abstract class)
   - Bizim `DiscountService` class'ının inherit ettiği base class
   - Tüm RPC metodları için virtual metodlar:
     ```csharp
     public virtual Task<CouponModel> GetDiscount(
         GetDiscountRequest request, 
         ServerCallContext context)
     {
         throw new RpcException(new Status(
             StatusCode.Unimplemented, 
             ""));
     }
     ```
   - Bu metodları override ederek implement ediyoruz

---

### Greet.cs ve GreetGrpc.cs → Eski Dosyalar

**Ne içeriyor?**
- `greet.proto` dosyasından oluşturulan dosyalar
- Artık kullanılmıyor (greet.proto silindi)
- Build sırasında temizlenebilir

**Not:** Bu dosyalar artık gereksiz, ama build'i etkilemiyor.

---

## Neden obj/ Klasöründe?

**obj/ klasörü:**
- Build artifacts (geçici dosyalar)
- Her build'de yeniden oluşturulur
- Git'e commit edilmez (genellikle .gitignore'da)
- Source code değil, **generated code**

**Neden source code değil?**
- Proto dosyasından otomatik oluşturulur
- Manuel düzenlenmemeli
- Proto dosyasını düzenleyip yeniden build etmek gerekir

---

## Nasıl Kullanılıyor?

### DiscountService.cs İçinde:

```csharp
using Discount.Grpc.Protos;  // ← Generated kodlar bu namespace'de

public class DiscountService : DiscountProtoService.DiscountProtoServiceBase
//                              ↑ Bu base class generated kod'dan geliyor
{
    public override async Task<CouponModel> GetDiscount(
        GetDiscountRequest request, 
        ServerCallContext context)
    //  ↑ CouponModel ve GetDiscountRequest generated kod'dan geliyor
    {
        // ...
    }
}
```

### Kullanım Örnekleri:

**1. Request/Response Tipleri:**
```csharp
// GetDiscountRequest kullanımı
var request = new GetDiscountRequest 
{ 
    ProductName = "iPhone 15" 
};

// CouponModel kullanımı
return new CouponModel
{
    Id = coupon.Id,
    ProductName = coupon.ProductName,
    Description = coupon.Description ?? string.Empty,
    Amount = coupon.Amount
};
```

**2. Base Class:**
```csharp
// DiscountProtoServiceBase'den inherit ediyoruz
public class DiscountService : DiscountProtoService.DiscountProtoServiceBase
{
    // Base class'taki virtual metodları override ediyoruz
    public override async Task<CouponModel> GetDiscount(...) { }
}
```

---

## Özet Tablosu

| Dosya | Nasıl Oluşuyor? | Ne İçeriyor? | Kullanım |
|-------|----------------|--------------|----------|
| **Discount.cs** | Proto compiler tarafından | Message tipleri (6 adet) | Request/Response tipleri |
| **DiscountGrpc.cs** | Proto compiler tarafından | Service base class | DiscountService inherit ediyor |
| **Greet.cs** | Eski proto'dan kalan | Artık kullanılmıyor | - |
| **GreetGrpc.cs** | Eski proto'dan kalan | Artık kullanılmıyor | - |

---

## Message Tipleri Detay Listesi

### 1. GetDiscountRequest
- **Kullanım:** GetDiscount RPC metodunun request parametresi
- **Property'ler:**
  - `ProductName` (string) → Ürün adı

### 2. CouponModel
- **Kullanım:** Kupon bilgilerini taşıyan model (hem request hem response'da kullanılır)
- **Property'ler:**
  - `Id` (int32) → Kupon ID'si
  - `ProductName` (string) → Ürün adı
  - `Description` (string) → İndirim açıklaması
  - `Amount` (int32) → İndirim miktarı (TL cinsinden)

### 3. CreateDiscountRequest
- **Kullanım:** CreateDiscount RPC metodunun request parametresi
- **Property'ler:**
  - `Coupon` (CouponModel) → Oluşturulacak kupon bilgileri

### 4. UpdateDiscountRequest
- **Kullanım:** UpdateDiscount RPC metodunun request parametresi
- **Property'ler:**
  - `Coupon` (CouponModel) → Güncellenecek kupon bilgileri

### 5. DeleteDiscountRequest
- **Kullanım:** DeleteDiscount RPC metodunun request parametresi
- **Property'ler:**
  - `ProductName` (string) → Silinecek kuponun ürün adı

### 6. DeleteDiscountResponse
- **Kullanım:** DeleteDiscount RPC metodunun response tipi
- **Property'ler:**
  - `Success` (bool) → Silme işleminin başarılı olup olmadığı

---

## Önemli Notlar

1. **Manuel Düzenleme Yapma:**
   - Bu dosyalar otomatik generate edilir
   - Manuel düzenlenirse bir sonraki build'de üzerine yazılır
   - Değişiklik yapmak için proto dosyasını düzenle

2. **Git'e Commit Etme:**
   - Bu dosyalar genellikle `.gitignore`'da olur
   - Her build'de yeniden oluşturulur
   - Commit etmeye gerek yok

3. **Build Bağımlılığı:**
   - Proto dosyası değiştiğinde bu dosyalar otomatik yeniden oluşturulur
   - `dotnet build` çalıştırıldığında güncellenir

4. **Namespace:**
   - Tüm generated kodlar `Discount.Grpc.Protos` namespace'inde
   - Proto dosyasındaki `option csharp_namespace = "Discount.Grpc.Protos";` satırından gelir

---

## Proto Dosyası Değiştiğinde Ne Olur?

**Senaryo:** Proto dosyasında yeni bir message eklendi

```
1. Protos/discount.proto dosyası düzenlendi
   ↓
2. dotnet build çalıştırıldı
   ↓
3. Proto compiler yeni message'ı görür
   ↓
4. Yeni C# class'ı generate edilir
   ↓
5. Discount.cs dosyası güncellenir
   ↓
6. Kod içinde yeni class kullanılabilir
```

---

**Son Güncelleme:** Aralık 2024

