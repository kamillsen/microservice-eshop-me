# Faz 2 - BuildingBlocks (Paylaşılan Kod)

## 2.1 BuildingBlocks.Exceptions

**Hedef:** Tüm servislerde kullanılacak exception yapısı

### Görevler:

#### Class library projesi oluştur
**Ne işe yarar:** Paylaşılan exception yapısı için class library projesi oluşturur.

```bash
cd src/BuildingBlocks
dotnet new classlib -n BuildingBlocks.Exceptions
```

#### Projeyi solution'a ekle
**Ne işe yarar:** Projeyi solution'a ekler, böylece diğer projeler referans verebilir.

```bash
cd ../..
dotnet sln add src/BuildingBlocks/BuildingBlocks.Exceptions/BuildingBlocks.Exceptions.csproj
```

#### NuGet paketlerini ekle
**Ne işe yarar:** ProblemDetails formatı için gerekli paketi ekler (RFC 7807 standardı).

```bash
cd src/BuildingBlocks/BuildingBlocks.Exceptions
dotnet add package Microsoft.AspNetCore.Diagnostics
```

**Paketler:**
- `Microsoft.AspNetCore.Diagnostics` (ProblemDetails için)

#### Klasör yapısını oluştur
**Ne işe yarar:** Exception class'ları ve handler için klasör yapısını oluşturur.

```bash
mkdir Exceptions
mkdir Handler
```

**Açıklama:**
- `Exceptions/` → Custom exception class'ları
- `Handler/` → Global exception handler middleware

#### Exception class'larını oluştur
**Ne işe yarar:** Tüm servislerde kullanılacak özel exception tiplerini oluşturur (404, 400, 500 hataları için).

- `Exceptions/NotFoundException.cs` (Exception'dan inherit) → 404 Not Found hatası
- `Exceptions/BadRequestException.cs` (Exception'dan inherit) → 400 Bad Request hatası
- `Exceptions/InternalServerException.cs` (Exception'dan inherit) → 500 Internal Server hatası

#### Global Exception Handler oluştur
**Ne işe yarar:** Tüm exception'ları yakalayıp standart ProblemDetails formatında response döner (tüm servislerde aynı hata formatı).

- `Handler/GlobalExceptionHandler.cs` oluştur
- `IExceptionHandler` interface'ini implement et
- `ProblemDetails` response formatı kullan (RFC 7807 standardı)

### Kontrol:
- Proje build oluyor mu? (`dotnet build`)
- Solution'da görünüyor mu? (`dotnet sln list`)

**Sonuç:** ✅ Exception handling hazır

---

## 2.2 BuildingBlocks.Behaviors

**Hedef:** MediatR Pipeline Behaviors (Validation, Logging)

### Görevler:

#### Class library projesi oluştur
**Ne işe yarar:** MediatR pipeline behaviors için class library projesi oluşturur (validation ve logging).

```bash
cd src/BuildingBlocks
dotnet new classlib -n BuildingBlocks.Behaviors
```

#### Projeyi solution'a ekle
**Ne işe yarar:** Projeyi solution'a ekler, böylece diğer projeler referans verebilir.

```bash
cd ../..
dotnet sln add src/BuildingBlocks/BuildingBlocks.Behaviors/BuildingBlocks.Behaviors.csproj
```

#### NuGet paketlerini ekle
**Ne işe yarar:** MediatR pipeline, validation ve logging için gerekli paketleri ekler.

```bash
cd src/BuildingBlocks/BuildingBlocks.Behaviors
dotnet add package MediatR
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
dotnet add package Serilog.AspNetCore
```

**Paketler:**
- `MediatR` (latest) → Pipeline behavior'lar için
- `FluentValidation` (latest) → Validation için
- `FluentValidation.DependencyInjectionExtensions` (latest) → DI entegrasyonu için
- `Serilog.AspNetCore` (latest) → Logging için

#### Validation Behavior oluştur
**Ne işe yarar:** MediatR pipeline'ında tüm request'leri otomatik olarak FluentValidation ile doğrular, hata varsa exception fırlatır.

- `ValidationBehavior.cs` oluştur
- `IPipelineBehavior<TRequest, TResponse>` interface'ini implement et
- FluentValidation kullanarak request'leri doğrula
- Validation hatalarında `ValidationException` fırlat

#### Logging Behavior oluştur
**Ne işe yarar:** MediatR pipeline'ında tüm request/response'ları otomatik olarak loglar (handler'dan önce ve sonra).

- `LoggingBehavior.cs` oluştur
- `IPipelineBehavior<TRequest, TResponse>` interface'ini implement et
- Serilog kullanarak request/response logla
- Handler'dan önce ve sonra log kaydı

### Kontrol:
- Proje build oluyor mu? (`dotnet build`)
- Solution'da görünüyor mu? (`dotnet sln list`)

**Sonuç:** ✅ Pipeline behaviors hazır

---

## 2.3 BuildingBlocks.Messaging

**Hedef:** RabbitMQ + MassTransit için event'ler

### Görevler:

#### Class library projesi oluştur
**Ne işe yarar:** RabbitMQ event'leri için class library projesi oluşturur (servisler arası async iletişim).

```bash
cd src/BuildingBlocks
dotnet new classlib -n BuildingBlocks.Messaging
```

#### Projeyi solution'a ekle
**Ne işe yarar:** Projeyi solution'a ekler, böylece diğer projeler referans verebilir.

```bash
cd ../..
dotnet sln add src/BuildingBlocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj
```

#### NuGet paketlerini ekle
**Ne işe yarar:** RabbitMQ ile async mesajlaşma için MassTransit paketlerini ekler.

```bash
cd src/BuildingBlocks/BuildingBlocks.Messaging
dotnet add package MassTransit
dotnet add package MassTransit.RabbitMQ
```

**Paketler:**
- `MassTransit` (latest) → Message broker abstraction
- `MassTransit.RabbitMQ` (latest) → RabbitMQ implementasyonu

#### Klasör yapısını oluştur
**Ne işe yarar:** Integration event'ler için klasör oluşturur.

```bash
mkdir Events
```

**Açıklama:**
- `Events/` → Integration event'ler (servisler arası mesajlaşma için)

#### IntegrationEvent base class oluştur
**Ne işe yarar:** Tüm event'lerin ortak özelliklerini (Id, CreatedAt) içeren base class oluşturur.

- `Events/IntegrationEvent.cs` oluştur
- `record` olarak tanımla (immutable, value equality)
- `Id` (Guid) property → Event'in unique ID'si
- `CreatedAt` (DateTime) property → Event'in oluşturulma zamanı

#### BasketCheckoutEvent oluştur
**Ne işe yarar:** Basket Service'ten Ordering Service'e gönderilecek checkout event'ini oluşturur (sepet ödeme bilgileri).

- `Events/BasketCheckoutEvent.cs` oluştur
- `IntegrationEvent`'ten inherit
- Tüm checkout bilgilerini içer:
  - UserName → Kullanıcı adı
  - TotalPrice → Toplam fiyat
  - Shipping Address (FirstName, LastName, EmailAddress, AddressLine, Country, State, ZipCode) → Teslimat adresi
  - Payment Info (CardName, CardNumber, Expiration, CVV, PaymentMethod) → Ödeme bilgileri

### Kontrol:
- Proje build oluyor mu? (`dotnet build`)
- Solution'da görünüyor mu? (`dotnet sln list`)

**Sonuç:** ✅ Messaging yapısı hazır

---

## Özet: Faz 2 adımlar sırası

1. BuildingBlocks.Exceptions projesi oluştur
2. BuildingBlocks.Exceptions'ı solution'a ekle
3. Microsoft.AspNetCore.Diagnostics paketini ekle
4. Exception class'larını oluştur (NotFoundException, BadRequestException, InternalServerException)
5. GlobalExceptionHandler oluştur
6. BuildingBlocks.Behaviors projesi oluştur
7. BuildingBlocks.Behaviors'ı solution'a ekle
8. MediatR, FluentValidation, Serilog paketlerini ekle
9. ValidationBehavior oluştur
10. LoggingBehavior oluştur
11. BuildingBlocks.Messaging projesi oluştur
12. BuildingBlocks.Messaging'i solution'a ekle
13. MassTransit paketlerini ekle
14. IntegrationEvent base class oluştur
15. BasketCheckoutEvent oluştur
16. Tüm projeleri build et ve test et

**Bu adımlar tamamlandıktan sonra Faz 3'e (Catalog Service) geçilebilir.**

