# appsettings.json ve launchSettings.json Detaylı Açıklama

> Bu dokümantasyon, ASP.NET Core'da `appsettings.json`, `appsettings.{Environment}.json` ve `launchSettings.json` dosyalarının ne işe yaradığını, nasıl çalıştığını ve aralarındaki ilişkiyi detaylı olarak açıklar.
> 
> **İçerik:**
> - launchSettings.json nedir ve ne işe yarar?
> - appsettings.json nedir ve ne işe yarar?
> - appsettings.{Environment}.json nedir ve ne işe yarar?
> - Dosyalar arasındaki ilişki
> - Yükleme sırası ve öncelik
> - Pratik örnekler

---

## Genel Bakış

ASP.NET Core projelerinde üç önemli yapılandırma dosyası vardır:

1. **`Properties/launchSettings.json`** - Uygulamanın nasıl başlatılacağını belirler
2. **`appsettings.json`** - Tüm environment'lar için ortak ayarlar
3. **`appsettings.{Environment}.json`** - Belirli environment'a özel ayarlar

---

## 1. launchSettings.json - Başlatma Ayarları

### Konum
```
Catalog.API/
└── Properties/
    └── launchSettings.json
```

### Ne İşe Yarar?

`launchSettings.json` dosyası, uygulamanın **nasıl başlatılacağını** belirler:

- **Port ayarları**: Uygulama hangi portta çalışacak?
- **Environment değişkenleri**: Hangi environment modunda çalışacak?
- **Tarayıcı açılışı**: Uygulama başladığında tarayıcı açılsın mı?
- **Profiller**: Farklı başlatma senaryoları (Development, Production, Staging)

### Örnek Dosya İçeriği

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "https://localhost:7107;http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### Önemli Alanlar

#### `applicationUrl`
```json
"applicationUrl": "http://localhost:5001"
```
- Uygulamanın hangi URL'de çalışacağını belirler
- HTTP ve HTTPS portlarını belirtebilirsin

#### `environmentVariables`
```json
"environmentVariables": {
  "ASPNETCORE_ENVIRONMENT": "Development"
}
```
- **Kritik**: Bu değer, hangi `appsettings.{Environment}.json` dosyasının yükleneceğini belirler
- `"Development"` → `appsettings.Development.json` yüklenir
- `"Production"` → `appsettings.Production.json` yüklenir
- `"Staging"` → `appsettings.Staging.json` yüklenir

#### `launchBrowser`
```json
"launchBrowser": false
```
- `true`: Uygulama başladığında tarayıcı otomatik açılır
- `false`: Tarayıcı açılmaz

### Ne Zaman Kullanılır?

- **IDE'den Run/Debug yapıldığında**: Visual Studio, Rider, VS Code bu dosyayı otomatik okur
- **`dotnet run` komutu çalıştırıldığında**: Varsayılan profil kullanılır
- **`dotnet run --launch-profile {profil-adı}`**: Belirli profil seçilir

### Örnek: Farklı Profiller

```json
{
  "profiles": {
    "Development": {
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "Production": {
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Production"
      }
    },
    "Staging": {
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Staging"
      }
    }
  }
}
```

**Kullanım:**
```bash
# Development profili
dotnet run --launch-profile Development

# Production profili
dotnet run --launch-profile Production
```

---

## 2. appsettings.json - Temel Ayarlar

### Konum
```
Catalog.API/
└── appsettings.json
```

### Ne İşe Yarar?

`appsettings.json` dosyası, **tüm environment'lar için ortak ayarları** içerir:

- Veritabanı bağlantı dizeleri
- Logging yapılandırması
- Uygulama ayarları
- Güvenlik ayarları

### Örnek Dosya İçeriği

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5436;Database=CatalogDb;Username=postgres;Password=postgres"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Önemli Alanlar

#### `ConnectionStrings`
```json
"ConnectionStrings": {
  "Database": "Host=localhost;Port=5436;Database=CatalogDb;..."
}
```
- Veritabanı bağlantı bilgileri
- `Program.cs`'de `builder.Configuration.GetConnectionString("Database")` ile okunur

#### `Logging`
```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  }
}
```
- Log seviyelerini belirler
- `Default`: Tüm loglar için varsayılan seviye
- `Microsoft.AspNetCore`: ASP.NET Core logları için özel seviye

#### `AllowedHosts`
```json
"AllowedHosts": "*"
```
- Hangi host'ların uygulamaya erişebileceğini belirler
- `"*"`: Tüm host'lara izin ver
- `"example.com"`: Sadece belirli domain'e izin ver

### Ne Zaman Yüklenir?

**Her zaman yüklenir!** Uygulama başladığında ilk olarak bu dosya yüklenir.

---

## 3. appsettings.{Environment}.json - Environment'a Özel Ayarlar

### Konum
```
Catalog.API/
├── appsettings.json
├── appsettings.Development.json
├── appsettings.Production.json
└── appsettings.Staging.json
```

### Ne İşe Yarar?

`appsettings.{Environment}.json` dosyaları, **belirli environment'a özel ayarları** içerir:

- `appsettings.Development.json` → Sadece Development modunda yüklenir
- `appsettings.Production.json` → Sadece Production modunda yüklenir
- `appsettings.Staging.json` → Sadece Staging modunda yüklenir

### Örnek: appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Örnek: appsettings.Production.json

```json
{
  "ConnectionStrings": {
    "Database": "Host=prod-db-server;Port=5432;Database=CatalogDb;Username=prod_user;Password=secure_password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error"
    }
  },
  "AllowedHosts": "example.com"
}
```

### Ne Zaman Yüklenir?

**Sadece ilgili environment aktifken yüklenir:**

- `ASPNETCORE_ENVIRONMENT = "Development"` → `appsettings.Development.json` yüklenir
- `ASPNETCORE_ENVIRONMENT = "Production"` → `appsettings.Production.json` yüklenir
- `ASPNETCORE_ENVIRONMENT = "Staging"` → `appsettings.Staging.json` yüklenir

---

## Dosyalar Arasındaki İlişki

### Görsel Akış Diyagramı

```
┌─────────────────────────────────────────────────────────┐
│ 1. Uygulama Başlatılır                                 │
│    dotnet run veya IDE'den Run/Debug                    │
└─────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────┐
│ 2. launchSettings.json Okunur                            │
│    Properties/launchSettings.json                       │
│                                                          │
│    "ASPNETCORE_ENVIRONMENT": "Development"              │
│    ↑ Bu değer alınır                                     │
└─────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────┐
│ 3. appsettings.json Yüklenir                            │
│    (Temel ayarlar - Her zaman yüklenir)                 │
│                                                          │
│    ConnectionStrings, Logging, AllowedHosts             │
└─────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────┐
│ 4. appsettings.{Environment}.json Yüklenir              │
│    ASPNETCORE_ENVIRONMENT = "Development"               │
│    → appsettings.Development.json yüklenir              │
│                                                          │
│    appsettings.json'daki ayarları override eder          │
│    (Aynı key'ler üzerine yazar)                         │
└─────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────┐
│ 5. Environment Variables Yüklenir                       │
│    (En yüksek öncelik - Her şeyi override eder)         │
└─────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────┐
│ 6. Uygulama Başlar                                       │
│    (Birleştirilmiş ayarlarla)                           │
└─────────────────────────────────────────────────────────┘
```

---

## Yükleme Sırası ve Öncelik

ASP.NET Core, yapılandırma dosyalarını **şu sırayla** yükler (sonraki öncekini override eder):

### 1. appsettings.json (En Düşük Öncelik)
```json
{
  "Logging": {
    "Default": "Information"
  },
  "ConnectionStrings": {
    "Database": "localhost"
  }
}
```

### 2. appsettings.{Environment}.json
```json
{
  "Logging": {
    "Default": "Warning"  // ← appsettings.json'daki "Information" override edilir
  }
  // ConnectionStrings yok → appsettings.json'daki değer kalır
}
```

### 3. Environment Variables (En Yüksek Öncelik)
```bash
export ASPNETCORE_Logging__LogLevel__Default=Error
# Tüm JSON dosyalarındaki değerleri override eder
```

### Öncelik Sırası (Düşükten Yükseğe)

```
1. appsettings.json                    ← En düşük öncelik
2. appsettings.{Environment}.json      ← Orta öncelik
3. Environment Variables               ← En yüksek öncelik
```

---

## Pratik Örnekler

### Örnek 1: Logging Ayarları

#### appsettings.json (Temel)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

#### appsettings.Development.json (Development'a Özel)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",  // ← "Information" override edildi
      "Microsoft.AspNetCore": "Information"  // ← "Warning" override edildi
    }
  }
}
```

**Sonuç (Development modunda):**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",  // ← Development.json'dan
      "Microsoft.AspNetCore": "Information"  // ← Development.json'dan
    }
  }
}
```

**Sonuç (Production modunda):**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",  // ← appsettings.json'dan (Production.json yok)
      "Microsoft.AspNetCore": "Warning"  // ← appsettings.json'dan
    }
  }
}
```

### Örnek 2: ConnectionStrings

#### appsettings.json (Temel)
```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5436;Database=CatalogDb;..."
  }
}
```

#### appsettings.Production.json (Production'a Özel)
```json
{
  "ConnectionStrings": {
    "Database": "Host=prod-db-server;Port=5432;Database=CatalogDb;..."
  }
}
```

**Sonuç (Development modunda):**
```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5436;..."  // ← appsettings.json'dan
  }
}
```

**Sonuç (Production modunda):**
```json
{
  "ConnectionStrings": {
    "Database": "Host=prod-db-server;Port=5432;..."  // ← Production.json'dan
  }
}
```

### Örnek 3: Swagger Ayarları

#### Program.cs
```csharp
var builder = WebApplication.CreateBuilder(args);

// Swagger sadece Development'ta açık
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

**Nasıl Çalışır?**

1. `launchSettings.json` → `ASPNETCORE_ENVIRONMENT = "Development"`
2. `builder.Environment.IsDevelopment()` → `true` döner
3. Swagger açılır ✅

**Production'da:**
1. `launchSettings.json` → `ASPNETCORE_ENVIRONMENT = "Production"`
2. `builder.Environment.IsDevelopment()` → `false` döner
3. Swagger açılmaz ❌

---

## Farklı Environment'ları Kullanma

### Yöntem 1: launchSettings.json'da Profil Değiştir

```json
{
  "profiles": {
    "Production": {
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Production"  // ← Değiştir
      }
    }
  }
}
```

### Yöntem 2: Komut Satırından

```bash
# Development
dotnet run

# Production
dotnet run --environment Production

# Staging
dotnet run --environment Staging
```

### Yöntem 3: Environment Variable

```bash
# Linux/Mac
export ASPNETCORE_ENVIRONMENT=Production
dotnet run

# Windows (PowerShell)
$env:ASPNETCORE_ENVIRONMENT="Production"
dotnet run

# Windows (CMD)
set ASPNETCORE_ENVIRONMENT=Production
dotnet run
```

### Yöntem 4: IDE'den Profil Seç

**Visual Studio / Rider / VS Code:**
- Debug/Run butonunun yanındaki dropdown'dan profil seç
- Her profil farklı `ASPNETCORE_ENVIRONMENT` değeri kullanır

---

## Program.cs'de Environment Kontrolü

### Environment Kontrolü

```csharp
var builder = WebApplication.CreateBuilder(args);

// Environment kontrolü
if (builder.Environment.IsDevelopment())
{
    // Sadece Development'ta çalışır
    builder.Services.AddSwaggerGen();
}

if (builder.Environment.IsProduction())
{
    // Sadece Production'da çalışır
    builder.Services.AddHttpsRedirection();
}

if (builder.Environment.IsStaging())
{
    // Sadece Staging'de çalışır
    // Özel staging ayarları
}
```

### Environment'a Göre Farklı Ayarlar

```csharp
var builder = WebApplication.CreateBuilder(args);

// Environment'a göre farklı connection string
var connectionString = builder.Environment.IsDevelopment()
    ? builder.Configuration.GetConnectionString("Database")
    : builder.Configuration.GetConnectionString("ProductionDatabase");

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(connectionString));
```

---

## Özet Tablo

| Dosya | Ne İşe Yarar | Ne Zaman Yüklenir | Öncelik |
|-------|--------------|-------------------|---------|
| `launchSettings.json` | Uygulamanın nasıl başlatılacağını belirler (port, environment) | Uygulama başlatılırken | - |
| `appsettings.json` | Tüm environment'lar için ortak ayarlar | Her zaman | 1 (En düşük) |
| `appsettings.Development.json` | Sadece Development için özel ayarlar | `ASPNETCORE_ENVIRONMENT = "Development"` | 2 |
| `appsettings.Production.json` | Sadece Production için özel ayarlar | `ASPNETCORE_ENVIRONMENT = "Production"` | 2 |
| `appsettings.Staging.json` | Sadece Staging için özel ayarlar | `ASPNETCORE_ENVIRONMENT = "Staging"` | 2 |
| Environment Variables | Sistem/ortam değişkenleri | Her zaman | 3 (En yüksek) |

---

## Önemli Notlar

### ✅ Doğru Kullanım

1. **Ortak ayarları `appsettings.json`'a koy**
2. **Environment'a özel ayarları `appsettings.{Environment}.json`'a koy**
3. **Hassas bilgileri (şifreler, API key'ler) environment variable'lara koy**

### ❌ Yanlış Kullanım

1. **Tüm ayarları `appsettings.json`'a koymak** (Production ayarları Development'ta görünür)
2. **Şifreleri JSON dosyalarına yazmak** (Güvenlik riski)
3. **Environment kontrolü yapmadan tüm özellikleri açmak** (Swagger Production'da açık olmamalı)

---

## Best Practices

### 1. Güvenlik

```json
// ❌ YANLIŞ - Şifre JSON'da
{
  "ConnectionStrings": {
    "Database": "Password=mysecretpassword"
  }
}

// ✅ DOĞRU - Environment variable kullan
// export DATABASE_PASSWORD=mysecretpassword
{
  "ConnectionStrings": {
    "Database": "Password=${DATABASE_PASSWORD}"
  }
}
```

### 2. Environment'a Özel Ayarlar

```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"  // Development'ta detaylı log
    }
  }
}

// appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"  // Production'da sadece önemli loglar
    }
  }
}
```

### 3. Profil Yönetimi

```json
{
  "profiles": {
    "Development": {
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "Production": {
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Production"
      }
    }
  }
}
```

---

## Referanslar

- [ASP.NET Core Configuration - Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [ASP.NET Core Environments - Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments)
- [launchSettings.json Schema](https://json.schemastore.org/launchsettings.json)

---

**Tarih:** Aralık 2024  
**Kaynak:** Catalog.API - Configuration Dosyaları  
**Durum:** ✅ Dokümante edildi
