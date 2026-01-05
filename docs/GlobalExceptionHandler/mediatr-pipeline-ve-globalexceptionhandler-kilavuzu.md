# 🎯 **MediatR Pipeline ve GlobalExceptionHandler - GENİŞLETİLMİŞ KILAVUZ**

## 📋 İçindekiler
1. [Temel Kavramlar](#temel-kavramlar)
2. [MediatR Pipeline Akışı](#mediatr-pipeline-akışı)
3. [GlobalExceptionHandler Nasıl Çalışır?](#globalexceptionhandler-nasıl-çalışır)
4. [Controller'da Try-Catch Durumu](#controllerda-try-catch-durumu)
5. [Exception Nasıl Fırlatılır ve Yakalanır?](#exception-nasıl-fırlatılır-ve-yakalanır)
6. [🔥 Async Metotlar Otomatik Exception Fırlatabilir](#-async-metotlar-otomatik-exception-fırlatabilir)
7. [Repository'de Exception Fırlatma Analizi](#repositoryde-exception-fırlatma-analizi)
8. [Exception Yakalama Şartları](#exception-yakalama-şartları)
9. [Senin Projende Özel Durumlar](#senin-projende-özel-durumlar)
10. [Debug ve Test Yöntemleri](#debug-ve-test-yöntemleri)
11. [En İyi Pratikler](#en-iyi-pratikler)

---

## 🎯 **TEMEL KAVRAMLAR**

### **MediatR Nedir?**
**Analoji:** Santral Operatörü - Gelen çağrıları doğru departmana bağlar
**Teknik:** Controller'lar ile handler'lar arasında aracılık yapan mediator pattern implementasyonu

### **GlobalExceptionHandler Nedir?**
**Analoji:** Hastane Acil Servisi - Tüm acil vakaları tek merkezde yönetir
**Teknik:** Yakalanmamış exception'ları yakalayıp HTTP response'una çeviren ASP.NET Core middleware'i

---

## 🔄 **MEDIATR PIPELINE AKIŞI**

### **Teknik Akış:**
```
HTTP Request → Controller → _mediator.Send(command)
    ↓
MediatR Pipeline:
    1. LoggingBehavior (Request logu)
    2. ValidationBehavior (FluentValidation)
    3. Handler (İş mantığı)
    ↓
Response → Controller → HTTP Response
```

### **ValidationBehavior Kod Analizi:**
```csharp
public async Task<TResponse> Handle(TRequest request, ...)
{
    // Validator'ları çalıştır
    var validationResults = await Task.WhenAll(
        _validators.Select(v => v.ValidateAsync(context)));
    
    if (failures.Any())
    {
        // ⚡ Exception fırlat
        throw new ValidationException(failures);
        // Bu exception GlobalExceptionHandler'a gidecek
    }
    
    return await next();
}
```

---

## 🚨 **GLOBALEXCEPTIONHANDLER NASIL ÇALIŞIR?**

### **⚠️ KRİTİK: Program.cs'de 3 Satır ZORUNLU!**

Exception'ın yakalanabilmesi için Program.cs'de **3 satırın** mutlaka olması gerekir:

```csharp
// Program.cs'de:

// 1. Handler'ı kaydet:
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
// ↑ DI container'a IExceptionHandler olarak kaydeder

// 2. ProblemDetails desteğini ekle (opsiyonel ama önerilir):
builder.Services.AddProblemDetails();
// ↑ ProblemDetails formatını destekler

// 3. Middleware'ı aktif et:
app.UseExceptionHandler();
// ↑ ASP.NET Core'un exception yakalama sistemini açar
```

**Bu 3 satırdan biri eksikse, GlobalExceptionHandler ÇALIŞMAZ!**

---

### **3 Aşamalı Sistem:**

#### **1. `app.UseExceptionHandler()` - Exception Yakalama Middleware'i**
```csharp
// Program.cs'de:
app.UseExceptionHandler();
```
**Analoji:** Güvenlik Kamerası - Tüm olayları kaydeder ve merkeze bildirir
**Teknik:** ASP.NET Core pipeline'ına exception yakalama middleware'i ekler. Pipeline'daki herhangi bir yerde fırlatılan ve yakalanmamış exception'ları yakalar.

**Arka Planda Ne Oluyor?**
```csharp
// ASP.NET Core'un içindeki ExceptionHandlerMiddleware:
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IExceptionHandler _handler; // ← DI'dan gelir
    
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context); // ← Controller'ı çalıştır
        }
        catch (Exception exception) // ← BURADA YAKALANIR!
        {
            // DI'dan IExceptionHandler al
            var handler = context.RequestServices
                .GetRequiredService<IExceptionHandler>();
            
            // TryHandleAsync'ı çağır
            await handler.TryHandleAsync(context, exception, cancellationToken);
        }
    }
}
```

**Akış:**
1. `UseExceptionHandler()` → `ExceptionHandlerMiddleware` pipeline'a eklenir
2. Controller çalışırken exception fırlatılırsa
3. `ExceptionHandlerMiddleware`'in `catch` bloğu yakalar
4. DI'dan `IExceptionHandler` alınır (senin `GlobalExceptionHandler`'ın)
5. `TryHandleAsync()` çağrılır

#### **2. `builder.Services.AddExceptionHandler<GlobalExceptionHandler>()` - DI Kaydı**
```csharp
// Program.cs'de:
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
```
**Analoji:** Doktor Randevu Sistemi - Hangi doktorun hangi vakayı alacağını belirler
**Teknik:** GlobalExceptionHandler'ı IExceptionHandler olarak DI container'a kaydeder. Böylece yakalanan exception'lar bu handler'a yönlendirilir.

**Ne Yapıyor?**
```csharp
// AddExceptionHandler() metodunun yaptığı:
builder.Services.AddSingleton<IExceptionHandler, GlobalExceptionHandler>();
// ↑ GlobalExceptionHandler'ı IExceptionHandler olarak kaydeder
// ExceptionHandlerMiddleware, DI'dan IExceptionHandler'ı alırken
// senin GlobalExceptionHandler'ını bulur
```

#### **3. `GlobalExceptionHandler` Sınıfı - Exception İşleyici**
```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception, // ← GELEN EXCEPTION
        CancellationToken cancellationToken)
    {
        // Exception tipine bak
        var problemDetails = CreateProblemDetails(exception, httpContext);
        
        // HTTP Response oluştur
        httpContext.Response.StatusCode = problemDetails.Status ?? 500;
        await httpContext.Response.WriteAsJsonAsync(problemDetails);
        
        return true; // "Exception'ı ben handle ettim"
    }
}
```
**Analoji:** Acil Servis Doktoru - Gelen hastayı muayene eder, teşhis koyar ve tedavi planı oluşturur
**Teknik:** Yakalanan exception'ı analiz eder, tipine göre uygun HTTP status code ve ProblemDetails oluşturur, kullanıcıya JSON formatında döner.

**Tam Akış:**
```
1. Controller'da exception fırlatılır
   ↓
2. ExceptionHandlerMiddleware'in catch bloğu yakalar
   ↓
3. DI'dan IExceptionHandler alınır (GlobalExceptionHandler)
   ↓
4. GlobalExceptionHandler.TryHandleAsync() çağrılır
   ↓
5. Exception tipine göre ProblemDetails oluşturulur
   ↓
6. HTTP Response olarak döner
```

---

### **4. `builder.Services.AddProblemDetails()` - ProblemDetails Desteği (Opsiyonel)**
```csharp
// Program.cs'de:
builder.Services.AddProblemDetails();
```
**Analoji:** Standart Form Doldurma - Tüm doktorlar aynı formu kullanır
**Teknik:** ProblemDetails formatını destekler. RFC 7807 standardına uygun hata response'ları oluşturur.

**Ne İşe Yarar?**
- Standart hata formatı sağlar
- Swagger/OpenAPI entegrasyonu için faydalıdır
- Opsiyoneldir ama önerilir

---

## ❓ **CONTROLLER'DA TRY-CATCH OLURSA NE OLUR?**

### **Analoji:** Postacı Örneği
- **Normalde:** Postacı hasta mektubu (exception) görünce hastaneye (GlobalExceptionHandler) gönderir
- **Try-Catch ile:** Postacı hastayı kendi evinde (Controller) tedavi etmeye çalışır

### **3 Senaryo:**

#### **1. Controller Exception'ı Yakalar ve Yeniden FIRLATMAZSA ❌**
```csharp
[HttpPost]
public async Task<IActionResult> StoreBasket([FromBody] ShoppingCartDto basket)
{
    try
    {
        var result = await _mediator.Send(new StoreBasketCommand(basket));
        return Ok(result);
    }
    catch (Exception ex)
    {
        // ❌ Sadece logluyor, yeniden fırlatmıyor!
        _logger.LogError(ex, "Hata oluştu");
        return StatusCode(500, "Internal Server Error");
    }
}
```
**SONUÇ:** GlobalExceptionHandler ÇALIŞMAZ! Exception Controller'da yakalanıp bitirildi.

#### **2. Controller Exception'ı Yakalar ve Yeniden FIRLATIRSA ✅**
```csharp
[HttpPost]
public async Task<IActionResult> StoreBasket([FromBody] ShoppingCartDto basket)
{
    try
    {
        var result = await _mediator.Send(new StoreBasketCommand(basket));
        return Ok(result);
    }
    catch (Exception ex)
    {
        // ✅ Logladı ve YENİDEN FIRLATTI
        _logger.LogError(ex, "Hata oluştu");
        throw; // ⭐ KRİTİK: GlobalExceptionHandler çalışır
    }
}
```
**SONUÇ:** GlobalExceptionHandler ÇALIŞIR! Exception yeniden fırlatıldı.

#### **3. Controller Exception'ı Yakalamazsa (SENİN YAPTIĞIN) ✅**
```csharp
[HttpPost]
public async Task<ActionResult<ShoppingCartDto>> StoreBasket([FromBody] ShoppingCartDto basket)
{
    // ✅ Exception yakalamıyor, direkt dışarı fırlatıyor
    var result = await _mediator.Send(new StoreBasketCommand(basket));
    return Ok(result);
}
```
**SONUÇ:** GlobalExceptionHandler ÇALIŞIR! Exception hiç yakalanmadı.

### **ÖZET:**
- ❌ **Controller yakalayıp fırlatmazsa** → GlobalExceptionHandler çalışmaz
- ✅ **Controller yakalayıp yeniden fırlatırsa** → GlobalExceptionHandler çalışır
- ✅ **Controller hiç yakalamazsa** → GlobalExceptionHandler çalışır

**SENİN PROJENDE:** Controller'lar exception yakalamıyor (✅ EN İYİ PRATİK)

---

## ⚡ **EXCEPTION NASIL FIRLATILIR VE YAKALANIR?**

### **1. Exception Nasıl Fırlatılır?**
```csharp
// Basit exception fırlatma:
throw new Exception("Hata mesajı");

// Custom exception fırlatma:
throw new NotFoundException("Ürün bulunamadı");

// İç exception ile fırlatma:
throw new InvalidOperationException("İşlem geçersiz", innerException);
```

### **2. GlobalExceptionHandler'ın Yakalaması İçin Gerekenler:**

#### **KRİTİK KURAL:** Exception **YAKALANMAMALI** veya **YENİDEN FIRLATILMALI**

```csharp
// ✅ Senaryo 1: Hiç yakalanmaz (GlobalExceptionHandler çalışır)
throw new Exception();

// ✅ Senaryo 2: Yakalanır ve yeniden fırlatılır (GlobalExceptionHandler çalışır)
try 
{ 
    throw new Exception(); 
} 
catch 
{ 
    throw; // ⭐ Yeniden fırlat!
}

// ❌ Senaryo 3: Yakalanır ve fırlatılmaz (GlobalExceptionHandler ÇALIŞMAZ)
try 
{ 
    throw new Exception(); 
} 
catch 
{ 
    _logger.LogError("Hata"); 
    // throw YOK!
}
```

---

## 🔥 **ASYNC METOTLAR OTOMATİK OLARAK EXCEPTION FIRLATABİLİR!**

### **Detaylı Açıklama:**

```csharp
// Bu satırda "throw" gözükmüyor ama...
var basket = await _context.ShoppingCarts
    .FirstOrDefaultAsync(x => x.UserName == userName);
// ⚡ ARKA PLANDA EXCEPTION FIRLATILABİLİR!
```

### **Neden Exception Fırlatıyor?**

#### **1. Database Bağlantı Hatası:**
```csharp
// Örnek senaryo:
// - Database down
// - Connection string yanlış
// - Network problemi

await _context.ShoppingCarts.FirstOrDefaultAsync(...);
// 👉 DbUpdateException fırlatır!
```

**Gerçek Hayat Senaryosu:**
```
1. await FirstOrDefaultAsync() çağrılır
2. EF Core database'e bağlanmaya çalışır
3. Connection başarısız olur (database server çöktü)
4. EF Core INTERNALLY exception fırlatır
5. Senin koduna exception olarak gelir
```

#### **2. SQL Sorgu Hatası:**
```csharp
// Örnek senaryo:
// - Tablo yok
// - Kolon yok
// - Yanlış SQL syntax

await _context.ShoppingCarts.FirstOrDefaultAsync(...);
// 👉 SqlException fırlatır!
```

#### **3. Timeout:**
```csharp
// Örnek senaryo:
// - Query çok uzun sürüyor
// - Database yavaş

await _context.ShoppingCarts.FirstOrDefaultAsync(...);
// 👉 TimeoutException fırlatır!
```

### **İşin Arkasındaki Teknik Detay:**

```csharp
// FirstOrDefaultAsync() metodunun basitleştirilmiş implementasyonu:
public async Task<TEntity> FirstOrDefaultAsync(...)
{
    try
    {
        // Database'e bağlan
        // SQL çalıştır
        // Sonucu dön
    }
    catch (Exception ex)
    {
        // ⭐ İŞTE BURASI KRİTİK!
        // Exception yakalanır VE YENİDEN FIRLATILIR!
        throw new DbUpdateException("Database error", ex);
    }
}
```

**Önemli Nokta:** EF Core, kendi içinde exception'ları yakalayıp, daha anlamlı exception tiplerine çevirerek yeniden fırlatır. Bu yüzden senin kodunda `throw` keyword'ü görünmese bile, async metot çağrısı exception fırlatabilir.

### **Kod Örneği ile Gösterim:**

```csharp
// SENİN KODUN:
public async Task<bool> DeleteBasket(string userName)
{
    // ⭐ BU SATIR EXCEPTION FIRLATABİLİR:
    var basket = await _context.ShoppingCarts
        .FirstOrDefaultAsync(x => x.UserName == userName);
    
    // Database down olduğunda bu satıra ASLA gelinmez!
    // Çünkü yukarıda exception fırlatılır ve method'dan çıkılır
    
    // Eğer buraya geldiyse, demek ki exception YOK
    if (basket != null)
    {
        _context.ShoppingCarts.Remove(basket);
        await _context.SaveChangesAsync();
        // ⚠️ Bu satır da exception fırlatabilir: DbUpdateException
    }
    
    return basket != null;
}
```

### **Hangi Exception'lar Fırlatılabilir?**

```csharp
// Entity Framework Core'dan gelebilecek exception'lar:
await _context.ShoppingCarts.FirstOrDefaultAsync(...);
// 👇 Potansiyel exception'lar:

// 1. DbUpdateException
//    - Database connection hatası
//    - Constraint violation
//    - Deadlock

// 2. SqlException (SQL Server için)
//    - Invalid SQL
//    - Permission denied
//    - Table not found

// 3. InvalidOperationException
//    - Context disposed
//    - Multiple async operations

// 4. TimeoutException
//    - Query timeout

// 5. ObjectDisposedException
//    - DbContext disposed
```

### **GlobalExceptionHandler'a Nasıl Ulaşır?**

```
1. FirstOrDefaultAsync() INTERNALLY exception fırlatır
2. Exception senin DeleteBasket() metoduna gelir
3. DeleteBasket() method'unda try-catch YOK
4. Exception DeleteBasket()'ten çıkar
5. MediatR Handler'a gelir
6. Handler'da try-catch YOK
7. Controller'a gelir
8. Controller'da try-catch YOK
9. app.UseExceptionHandler() yakalar
10. GlobalExceptionHandler işler
```

### **Kritik Fark: "throw" vs "async exception"**

```csharp
// SEN exception fırlatıyorsun:
throw new NotFoundException("Basket not found");
// 👉 Bu SENİN yazdığın kod

// EF Core exception fırlatıyor:
await _context.ShoppingCarts.FirstOrDefaultAsync(...);
// 👉 Bu EF Core'un INTERNALLY fırlattığı exception
//    Sen görmüyorsun ama oluyor!
```

**Özet:** Async metotlar (özellikle EF Core metotları), senin kodunda `throw` keyword'ü görünmese bile, arka planda exception fırlatabilir. Bu exception'lar, eğer yakalanmazsa, GlobalExceptionHandler'a kadar ulaşır.

---

## 🔍 **REPOSITORY'DE EXCEPTION FIRLATMA ANALİZİ**

### **Verilen Kod: BasketRepository.DeleteBasket()**
```csharp
public async Task<bool> DeleteBasket(string userName)
{
    // 1. PostgreSQL'den sil
    var basket = await _context.ShoppingCarts
        .FirstOrDefaultAsync(x => x.UserName == userName);
    // ⚠️ Bu satır exception fırlatabilir: DbUpdateException, SqlException, vb.
    // ⚡ ARKA PLANDA EXCEPTION FIRLATABİLİR!
    // Neden? EF Core, database bağlantısı başarısız olursa, SQL hatası olursa,
    // timeout olursa INTERNALLY exception fırlatır ve senin koduna iletir.
    // Senin kodunda "throw" gözükmese bile, async metot çağrısı exception üretebilir.

    if (basket != null)
    {
        _context.ShoppingCarts.Remove(basket);
        await _context.SaveChangesAsync(); 
        // ⚠️ Bu satır exception fırlatabilir: DbUpdateException
        // ⚡ ARKA PLANDA EXCEPTION FIRLATABİLİR!
        // Neden? SaveChangesAsync() database'e yazma işlemi yapar.
        // Constraint violation, foreign key hatası, deadlock gibi durumlarda
        // EF Core INTERNALLY exception fırlatır.
    }

    // 2. Redis'ten sil
    try
    {
        var deleted = await _redis.KeyDeleteAsync(GetRedisKey(userName));
        if (deleted || basket != null)
        {
            _logger.LogInformation("Basket deleted for {UserName}", userName);
            return true;
        }
    }
    catch (RedisConnectionException ex)
    {
        _logger.LogWarning(ex, "Redis unavailable...");
        // ❌ throw YOK! GlobalExceptionHandler çalışmaz
    }

    return basket != null;
}
```

### **Bu Kodda Exception Fırlatan Yerler:**

#### **1. `FirstOrDefaultAsync()` - EXCEPTION FIRLATABİLİR ✅**
```csharp
var basket = await _context.ShoppingCarts
    .FirstOrDefaultAsync(x => x.UserName == userName);
```
**Hangi Exception'lar fırlatılabilir?**
- `DbUpdateException` - Database connection hatası
- `SqlException` - SQL sorgu hatası
- `TimeoutException` - Timeout hatası

**Nasıl Exception Fırlatıyor?**
- EF Core, database'e bağlanmaya çalışırken hata olursa, SQL sorgusu çalıştırırken hata olursa veya timeout olursa, kendi içinde exception yakalayıp daha anlamlı exception tiplerine çevirerek yeniden fırlatır. Senin kodunda `throw` keyword'ü görünmese bile, bu async metot çağrısı exception üretebilir.

**GlobalExceptionHandler çalışır mı?** → **EVET** (Çünkü yakalanmıyor)

#### **2. `SaveChangesAsync()` - EXCEPTION FIRLATABİLİR ✅**
```csharp
await _context.SaveChangesAsync();
```
**Hangi Exception'lar fırlatılabilir?**
- `DbUpdateException` - Constraint violation, foreign key hatası
- `DbUpdateConcurrencyException` - Concurrency conflict

**Nasıl Exception Fırlatıyor?**
- EF Core, database'e yazma işlemi yaparken constraint violation, foreign key hatası, deadlock gibi durumlarla karşılaşırsa, kendi içinde exception yakalayıp `DbUpdateException` olarak yeniden fırlatır.

**GlobalExceptionHandler çalışır mı?** → **EVET** (Çünkü yakalanmıyor)

#### **3. `KeyDeleteAsync()` - EXCEPTION FIRLATABİLİR ama YAKALANIYOR ❌**
```csharp
try
{
    await _redis.KeyDeleteAsync(GetRedisKey(userName));
}
catch (RedisConnectionException ex)
{
    _logger.LogWarning(...);
    // ❌ throw YOK! GlobalExceptionHandler çalışmaz
}
```
**GlobalExceptionHandler çalışır mı?** → **HAYIR** (Çünkü catch var ve throw yok)

### **Eksik Olan: NotFoundException Fırlatma**
Şu anki kod:
```csharp
if (basket != null)
{
    // Silme işlemi
}
return basket != null; // false döner
```

**GlobalExceptionHandler'ın çalışması için:**
```csharp
public async Task DeleteBasket(string userName)
{
    var basket = await _context.ShoppingCarts
        .FirstOrDefaultAsync(x => x.UserName == userName);

    // ⭐ EXCEPTION FIRLAT!
    if (basket == null)
        throw new NotFoundException($"Basket for {userName} not found");

    _context.ShoppingCarts.Remove(basket);
    await _context.SaveChangesAsync();

    // Redis silme...
}
```

---

## ⚠️ **EXCEPTION YAKALAMA ŞARTLARI**

### **GlobalExceptionHandler'ın Çalışması İçin 5 Şart:**

#### **1. Program.cs'de 3 Satır ZORUNLU OLMALI** ⭐⭐⭐
```csharp
// Program.cs'de MUTLAKA olmalı:

// 1. Handler'ı DI'ya kaydet:
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
// ← BU OLMADAN GlobalExceptionHandler çalışmaz!

// 2. ProblemDetails desteği (opsiyonel ama önerilir):
builder.Services.AddProblemDetails();

// 3. Middleware'ı aktif et:
app.UseExceptionHandler();
// ← BU OLMADAN exception yakalanmaz!
```

**Bu 3 satırdan biri eksikse, GlobalExceptionHandler ÇALIŞMAZ!**

#### **2. THROW OLMALI** ⭐
```csharp
// ✅ Çalışır: throw var
throw new NotFoundException("Ürün yok");

// ✅ Çalışır: Async metot exception fırlatır (throw görünmez ama olur)
await _context.ShoppingCarts.FirstOrDefaultAsync(...);

// ❌ Çalışmaz: throw yok
_logger.LogError("Hata");
return false;
```

#### **3. YAKALANMAMALI veya YENİDEN FIRLATILMALI**
```csharp
// ✅ Senaryo 1: Hiç yakalanmaz
throw new Exception(); // GlobalExceptionHandler ÇALIŞIR

// ✅ Senaryo 2: Yakalanır ve yeniden fırlatılır
try { throw; } catch { throw; } // GlobalExceptionHandler ÇALIŞIR

// ❌ Senaryo 3: Yakalanır ve fırlatılmaz
try { throw; } catch { log; } // GlobalExceptionHandler ÇALIŞMAZ
```

#### **4. `app.UseExceptionHandler()` AKTİF OLMALI**
```csharp
// Program.cs'de:
var app = builder.Build();
app.UseExceptionHandler(); // ← BU SATIR OLMALI!
app.MapControllers();
```

**Not:** Bu şart, aslında 1. şartın bir parçası ama önemli olduğu için ayrı belirtildi.

#### **5. `AddExceptionHandler()` KAYDI OLMALI**
```csharp
// Program.cs'de:
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
// ← BU KAYIT OLMALI!
```

**Not:** Bu şart, aslında 1. şartın bir parçası ama önemli olduğu için ayrı belirtildi.

---

## 🔍 **SENİN PROJENDE ÖZEL DURUMLAR**

### **1. SaveBasket - PostgreSQL Hatası (ÇALIŞIR) ✅**
```csharp
try
{
    await _context.SaveChangesAsync(); // DbUpdateException
}
catch
{
    await transaction.RollbackAsync();
    throw; // ← KRİTİK! GlobalExceptionHandler ÇALIŞIR
}
```

### **2. SaveBasket - Redis Hatası (ÇALIŞMAZ) ❌**
```csharp
try
{
    await _redis.StringSetAsync(...); // RedisConnectionException
}
catch (RedisConnectionException ex)
{
    _logger.LogWarning(...);
    // ❌ throw YOK! GlobalExceptionHandler ÇALIŞMAZ
}
```

### **3. ValidationBehavior (ÇALIŞIR ama 500 DÖNER) ⚠️**
```csharp
throw new ValidationException(failures); 
// GlobalExceptionHandler çalışır ama ValidationException case'i yok → 500 döner
```

### **4. DeleteBasket - Basket Yoksa (ÇALIŞMAZ) ❌**
```csharp
var basket = await _context.ShoppingCarts
    .FirstOrDefaultAsync(x => x.UserName == userName);

if (basket != null)
{
    // Silme işlemi
}
return basket != null; // Exception FIRLATMIYOR, sadece false dönüyor
```

---

## 🧪 **DEBUG VE TEST YÖNTEMLERİ**

### **1. GlobalExceptionHandler'ı Debug Etmek:**
```csharp
// GlobalExceptionHandler.cs'ye ekle:
public async ValueTask<bool> TryHandleAsync(...)
{
    // Debug için:
    Console.WriteLine($"[DEBUG] Exception Type: {exception.GetType().Name}");
    Console.WriteLine($"[DEBUG] Exception Message: {exception.Message}");
    Console.WriteLine($"[DEBUG] Stack Trace: {exception.StackTrace}");
    
    // Kalan kod...
}
```

### **2. Postman Test Senaryoları:**
```http
### Senaryo 1: Controller'da Try-Catch (Yeniden Fırlatma)
POST http://localhost:5000/api/test/exception

### Senaryo 2: ValidationException Testi
POST http://localhost:5000/api/baskets
Content-Type: application/json

{
    "userName": "",  # Boş bırak
    "items": []
}

### Senaryo 3: DeleteBasket - NotFound Testi
DELETE http://localhost:5000/api/baskets/nonexistent-user
```

---

## ✅ **EN İYİ PRATİKLER**

### **1. GlobalExceptionHandler Güncellemesi:**
```csharp
// GlobalExceptionHandler.cs'de:
using FluentValidation;

return exception switch
{
    // Mevcutlar:
    NotFoundException => 404,
    BadRequestException => 400,
    InternalServerException => 500,
    
    // EKLENMESİ GEREKEN:
    ValidationException validationException => new ProblemDetails
    {
        Status = 400,
        Title = "Validation Error",
        Extensions = { ["errors"] = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()) }
    },
    
    // Diğer exception'lar:
    DbUpdateException => 500,
    RedisConnectionException => 500,
    
    // Default (exception mesajını göster):
    _ => new ProblemDetails
    {
        Status = 500,
        Title = "Internal Server Error",
        Detail = exception.Message // ⭐ Mesajı göster!
    }
};
```

### **2. Repository Pattern Düzeltmeleri:**
```csharp
// Redis işlemlerinde:
catch (RedisConnectionException ex)
{
    _logger.LogWarning(ex, "Redis hatası");
    throw; // ⭐ BU SATIRI EKLE!
}

// DeleteBasket'te:
public async Task DeleteBasket(string userName)
{
    var basket = await _context.ShoppingCarts
        .FirstOrDefaultAsync(x => x.UserName == userName);
    
    // ⭐ Exception fırlat:
    if (basket == null)
        throw new NotFoundException($"Basket for {userName} not found");
    
    _context.ShoppingCarts.Remove(basket);
    await _context.SaveChangesAsync();
    
    // Redis silme...
}
```

### **3. Handler'larda Exception Standardizasyonu:**
```csharp
public async Task<Response> Handle()
{
    try
    {
        // İş mantığı
        if (product == null)
            throw new NotFoundException("Ürün bulunamadı"); // 404
        
        if (user.Balance < product.Price)
            throw new BadRequestException("Yetersiz bakiye"); // 400
    }
    catch (Exception ex) when (ex is not NotFoundException and not BadRequestException)
    {
        throw new InternalServerException("Sistem hatası", ex); // 500
    }
}
```

---

## 🎯 **ÖZET TABLOLAR**

### **GlobalExceptionHandler Çalışma Matrisi:**
| **Durum** | **throw** | **catch** | **GlobalExceptionHandler** |
|-----------|-----------|-----------|---------------------------|
| Direkt throw | ✅ VAR | ❌ YOK | ✅ ÇALIŞIR |
| Controller'da try-catch → throw; | ✅ VAR | ✅ VAR | ✅ ÇALIŞIR |
| Controller'da try-catch → return | ❌ YOK | ✅ VAR | ❌ ÇALIŞMAZ |
| Repository'de catch → throw; | ✅ VAR | ✅ VAR | ✅ ÇALIŞIR |
| Repository'de catch → log | ❌ YOK | ✅ VAR | ❌ ÇALIŞMAZ |
| Async metot exception (EF Core) | ✅ VAR (görünmez) | ❌ YOK | ✅ ÇALIŞIR |

### **Senin Projende Exception Akışları:**
| **Kaynak** | **Exception Tipi** | **throw?** | **GlobalExceptionHandler** |
|------------|-------------------|------------|---------------------------|
| FirstOrDefaultAsync() | DbUpdateException | ✅ (EF Core INTERNALLY) | ✅ ÇALIŞIR |
| SaveChangesAsync() | DbUpdateException | ✅ (EF Core INTERNALLY) | ✅ ÇALIŞIR |
| KeyDeleteAsync() | RedisConnectionException | ❌ (catch var) | ❌ ÇALIŞMAZ |
| Handler direkt | NotFoundException | ✅ (Sen yazdın) | ✅ ÇALIŞIR |
| ValidationBehavior | ValidationException | ✅ (Sen yazdın) | ✅ ÇALIŞIR (500 döner) |
| DeleteBasket (basket null) | (Exception yok) | ❌ | ❌ ÇALIŞMAZ |

---

## 🔧 **ACİL YAPILMASI GEREKENLER**

### **0. Program.cs'de 3 Satır Kontrolü (EN ÖNEMLİSİ!):**
```csharp
// Program.cs'de MUTLAKA olmalı:

// 1. Handler'ı DI'ya kaydet:
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// 2. ProblemDetails desteği (opsiyonel ama önerilir):
builder.Services.AddProblemDetails();

// 3. Middleware'ı aktif et:
app.UseExceptionHandler();
```

**Bu 3 satırdan biri eksikse, GlobalExceptionHandler ÇALIŞMAZ!**

### **1. GlobalExceptionHandler Güncelle:**
```csharp
// 1. using FluentValidation; ekle
// 2. ValidationException case'i ekle (400)
// 3. Default case'de exception.Message göster
```

### **2. Repository Catch Bloklarını Düzelt:**
```csharp
// Tüm Redis catch bloklarına:
catch (RedisConnectionException ex)
{
    _logger.LogWarning(ex, "Redis hatası");
    throw; // ← BU SATIRI EKLE
}
```

### **3. DeleteBasket'i Düzelt:**
```csharp
public async Task DeleteBasket(string userName)
{
    var basket = await _context.ShoppingCarts
        .FirstOrDefaultAsync(x => x.UserName == userName);
    
    if (basket == null)
        throw new NotFoundException($"Basket for {userName} not found");
    
    // Kalan kod...
}
```

---

## 🏆 **SONUÇ**

### **SENİN DEDİĞİN GİBİ ÖZETLE:**
- ✅ **Program.cs'de 3 satır ZORUNLU:**
  - `builder.Services.AddExceptionHandler<GlobalExceptionHandler>();` → Handler'ı DI'ya kaydet
  - `builder.Services.AddProblemDetails();` → ProblemDetails desteği (opsiyonel ama önerilir)
  - `app.UseExceptionHandler();` → Exception YAKALAMA motoru (ExceptionHandlerMiddleware)
- ✅ **`ExceptionHandlerMiddleware`** → Arka planda exception'ları yakalar, DI'dan `IExceptionHandler` alır, `TryHandleAsync()` çağırır
- ✅ **`GlobalExceptionHandler`** → Exception AYIRT ETME ve response oluşturma
- ✅ **Controller exception yakalamaz** → En iyi pratik
- ✅ **Async metotlar (EF Core) otomatik exception fırlatabilir** → `throw` görünmese bile olur

### **CEVAPLAR:**

#### **1. "Eğer Controller'da try-catch varsa UseExceptionHandler yine yakalar mı?"**
- **Controller catch'te `throw;` varsa** → ✅ EVET, GlobalExceptionHandler çalışır
- **Controller catch'te `throw;` yoksa** → ❌ HAYIR, GlobalExceptionHandler çalışmaz

#### **2. "Repository'de exception fırlatması için ne olması gerekir?"**
- **Program.cs'de 3 satır ZORUNLU:**
  - `builder.Services.AddExceptionHandler<GlobalExceptionHandler>();` → Handler'ı DI'ya kaydet
  - `builder.Services.AddProblemDetails();` → ProblemDetails desteği (opsiyonel ama önerilir)
  - `app.UseExceptionHandler();` → ExceptionHandlerMiddleware'i aktif et
- `throw` keyword'ü kullanılmalı VEYA
- Async metot çağrısı yapılmalı (EF Core gibi - INTERNALLY exception fırlatır)
- Exception yakalanmamalı veya yeniden fırlatılmalı (`throw;`)

#### **3. "DeleteBasket'te hata olursa exception fırlatan yer nerede?"**
- `FirstOrDefaultAsync()` ve `SaveChangesAsync()` exception fırlatabilir (EF Core INTERNALLY)
- Ama **basket null olduğunda exception fırlatmıyor**, sadece `false` dönüyor
- **Eksik olan:** `if (basket == null) throw new NotFoundException(...);`

**Bu düzenlemeleri yaparsan, exception handling sistemin TAMAMEN SAĞLAM olacak!** 🚀

