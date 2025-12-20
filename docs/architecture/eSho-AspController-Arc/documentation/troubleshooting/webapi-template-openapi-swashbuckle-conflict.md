# Web API Template - OpenAPI vs Swashbuckle Çakışması

## Sorun

.NET 9 Web API template'i ile yeni bir proje oluşturulduğunda, `Program.cs` dosyasında `AddOpenApi()` ve `MapOpenApi()` metodları kullanılır. Ancak projede `Swashbuckle.AspNetCore` paketi kullanılıyorsa, bu metodlar bulunamaz ve build hatası oluşur.

### Hata Mesajı

```
error CS1061: 'IServiceCollection' does not contain a definition for 'AddOpenApi' 
and no accessible extension method 'AddOpenApi' accepting a first argument of type 
'IServiceCollection' could be found

error CS1061: 'WebApplication' does not contain a definition for 'MapOpenApi' 
and no accessible extension method 'MapOpenApi' accepting a first argument of 
type 'WebApplication' could be found
```

### Neden Oluşur?

- **.NET 9 Web API Template**: Varsayılan olarak `Microsoft.AspNetCore.OpenApi` paketini kullanır ve `AddOpenApi()`/`MapOpenApi()` metodlarını içerir.
- **Swashbuckle.AspNetCore**: Farklı bir OpenAPI implementasyonu kullanır ve `AddSwaggerGen()`/`UseSwagger()` metodlarını içerir.
- **İki paket birlikte kullanılamaz**: Template'den gelen kod `Microsoft.AspNetCore.OpenApi` paketini bekler, ancak projede `Swashbuckle.AspNetCore` kullanılıyorsa metodlar bulunamaz.

## Çözüm

### Adım 1: Program.cs'i Swashbuckle Kullanacak Şekilde Güncelle

Template'den gelen `AddOpenApi()` ve `MapOpenApi()` kodlarını kaldırıp, `Swashbuckle.AspNetCore` kullanacak şekilde güncelleyin.

**Önce (Template'den gelen kod):**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();  // ❌ Hata verir

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();  // ❌ Hata verir
}
```

**Sonra (Swashbuckle ile):**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Basket API",
        Version = "v1",
        Description = "E-ticaret Basket Service API - Sepet yönetimi için REST API"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Swagger UI (OpenAPI spesifikasyonunu görselleştirir)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Basket API v1");
        c.RoutePrefix = "swagger"; // Swagger UI'ı /swagger'da göster
    });
}

app.UseHttpsRedirection();

// Controllers
app.MapControllers();

app.Run();
```

### Adım 2: Paket Kontrolü

`.csproj` dosyasında `Swashbuckle.AspNetCore` paketinin eklendiğinden emin olun:

```xml
<ItemGroup>
  <PackageReference Include="Swashbuckle.AspNetCore" />
</ItemGroup>
```

**Not:** Central Package Management (CPM) kullanılıyorsa, `Directory.Packages.props` dosyasında versiyon tanımlı olmalı:

```xml
<PackageVersion Include="Swashbuckle.AspNetCore" Version="10.0.1" />
```

### Adım 3: Microsoft.AspNetCore.OpenApi Paketini Kaldır (Opsiyonel)

Eğer `Microsoft.AspNetCore.OpenApi` paketi ekliyse ve kullanılmayacaksa, `.csproj` dosyasından kaldırın:

```xml
<!-- Bu satırı kaldırın -->
<PackageReference Include="Microsoft.AspNetCore.OpenApi" />
```

## Örnek: Catalog.API vs Basket.API

### Catalog.API (Doğru Kullanım)
```csharp
// Program.cs
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Catalog API",
        Version = "v1",
        Description = "E-ticaret Catalog Service API"
    });
});

// ...

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API v1");
        c.RoutePrefix = string.Empty; // Root'ta göster
    });
}
```

### Basket.API (Düzeltilmiş)
Template'den gelen kodu kaldırıp, Catalog.API'deki gibi Swashbuckle kullanacak şekilde güncellendi.

## Farklar

| Özellik | Microsoft.AspNetCore.OpenApi | Swashbuckle.AspNetCore |
|---------|----------------------------|----------------------|
| **Metodlar** | `AddOpenApi()`, `MapOpenApi()` | `AddSwaggerGen()`, `UseSwagger()` |
| **UI** | Minimal (OpenAPI spec) | Swagger UI (zengin UI) |
| **Kullanım** | .NET 9 template varsayılanı | Daha yaygın, özelleştirilebilir |
| **Controller Desteği** | Minimal API odaklı | Controller-based API için ideal |

## Sonuç

- ✅ **Swashbuckle.AspNetCore** kullanıyorsanız: `AddSwaggerGen()` ve `UseSwagger()` kullanın
- ✅ **Microsoft.AspNetCore.OpenApi** kullanıyorsanız: `AddOpenApi()` ve `MapOpenApi()` kullanın
- ❌ **İkisini birlikte kullanmayın**

## İlgili Dosyalar

- `src/Services/Basket/Basket.API/Program.cs`
- `src/Services/Catalog/Catalog.API/Program.cs`
- `Directory.Packages.props`

## Tarih

- **Oluşturulma:** Aralık 2024
- **Sorun:** Basket.API projesi oluşturulurken yaşandı
- **Çözüm:** Program.cs Swashbuckle kullanacak şekilde güncellendi



