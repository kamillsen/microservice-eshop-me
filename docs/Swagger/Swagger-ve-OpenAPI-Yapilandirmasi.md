# Swagger ve OpenAPI Yapılandırması

## Genel Bakış

Swagger/OpenAPI, REST API'lerin dokümantasyonunu ve test edilmesini sağlayan bir araçtır. Catalog.API projesinde Development ortamında Swagger UI kullanılmaktadır.

---

## Program.cs'deki Yapılandırma

### 1. Builder Aşaması (Servis Kayıtları)

```csharp
// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Catalog API",
        Version = "v1",
        Description = "E-ticaret Catalog Service API - Ürün ve kategori yönetimi için REST API"
    });
});
```

### 2. App Aşaması (Middleware ve UI)

```csharp
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Swagger UI (OpenAPI spesifikasyonunu görselleştirir)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API v1");
        c.RoutePrefix = string.Empty; // Swagger UI'ı root'ta göster (http://localhost:5001/)
    });
}
```

---

## 1. `builder.Services.AddEndpointsApiExplorer()`

### Ne Yapar?

API endpoint'lerini keşfetmek için gerekli servisleri ekler. Controller'lardaki endpoint'leri analiz eder ve Swagger dokümantasyonu için hazırlar.

### Nasıl Çalışır?

```csharp
// Arka planda yapılan (basitleştirilmiş)
// 1. Controller'ları bulur
// 2. [HttpGet], [HttpPost] vb. attribute'ları okur
// 3. Route'ları analiz eder
// 4. Parametreleri, request/response modellerini toplar
```

---

## 2. `builder.Services.AddSwaggerGen(...)`

### Ne Yapar?

Swagger dokümantasyonunu oluşturmak için gerekli servisleri kaydeder ve yapılandırmayı yapar.

### Yapılandırma

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Catalog API",           // API başlığı
        Version = "v1",                  // API versiyonu
        Description = "E-ticaret Catalog Service API - Ürün ve kategori yönetimi için REST API"
    });
});
```

### SwaggerDoc Parametreleri

- **"v1"**: Swagger dokümantasyonunun versiyonu (birden fazla versiyon olabilir)
- **Title**: API'nin başlığı (Swagger UI'da görünür)
- **Version**: API versiyonu
- **Description**: API açıklaması

### Ne Zaman Çalışır?

- **Uygulama başlangıcında** (startup)
- Controller'lar analiz edilir
- OpenAPI JSON dokümantasyonu oluşturulur
- Henüz endpoint'ler aktif değil

---

## 3. `if (app.Environment.IsDevelopment())`

### Ne Yapar?

Sadece Development ortamında Swagger'ı etkinleştirir.

### Neden Önemli?

1. **Güvenlik**: Production'da API dokümantasyonunu gizler
2. **Performans**: Gereksiz middleware'i kaldırır
3. **Best Practice**: Swagger sadece development'ta kullanılmalı

### Environment Kontrolü

```csharp
app.Environment.IsDevelopment()  // Development ortamında true
app.Environment.IsProduction()  // Production ortamında true
app.Environment.IsStaging()      // Staging ortamında true
```

### Örnek Kullanım

```csharp
// Development ortamında
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();        // ✅ Çalışır
    app.UseSwaggerUI(...);   // ✅ Çalışır
}

// Production ortamında
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();        // ❌ Çalışmaz
    app.UseSwaggerUI(...);   // ❌ Çalışmaz
}
```

---

## 4. `app.UseSwagger()`

### Ne Yapar?

OpenAPI JSON endpoint'ini HTTP pipeline'a ekler. Bu endpoint, API dokümantasyonunu JSON formatında döndürür.

### Endpoint

```
GET /swagger/v1/swagger.json
```

### Response Örneği

```json
{
  "openapi": "3.0.1",
  "info": {
    "title": "Catalog API",
    "version": "v1",
    "description": "E-ticaret Catalog Service API - Ürün ve kategori yönetimi için REST API"
  },
  "paths": {
    "/api/products": {
      "get": {
        "tags": ["Products"],
        "summary": "Get all products",
        "responses": {
          "200": {
            "description": "Success",
            "content": {
              "application/json": {
                "schema": {
                  "type": "array",
                  "items": {
                    "$ref": "#/components/schemas/ProductDto"
                  }
                }
              }
            }
          }
        }
      },
      "post": {
        "tags": ["Products"],
        "summary": "Create a new product",
        "requestBody": {
          "content": {
            "application/json": {
              "schema": {
                "$ref": "#/components/schemas/CreateProductCommand"
              }
            }
          }
        },
        "responses": {
          "201": {
            "description": "Created"
          }
        }
      }
    }
  },
  "components": {
    "schemas": {
      "ProductDto": {
        "type": "object",
        "properties": {
          "id": { "type": "string", "format": "uuid" },
          "name": { "type": "string" },
          "price": { "type": "number", "format": "decimal" }
        }
      }
    }
  }
}
```

### Ne Zaman Kullanılır?

1. **API Dokümantasyonu**: Programatik olarak API'yi okumak için
2. **Client Kod Üretimi**: OpenAPI generator ile client kod üretmek için
3. **Postman/Insomnia**: API'yi import etmek için
4. **API Gateway**: API gateway'lerin API'yi keşfetmesi için

---

## 5. `app.UseSwaggerUI(...)`

### Ne Yapar?

Swagger UI'ı HTTP pipeline'a ekler. Bu, API'yi tarayıcıda görselleştiren ve test etmeyi sağlayan bir web arayüzüdür.

### Yapılandırma

```csharp
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API v1");
    c.RoutePrefix = string.Empty; // Swagger UI'ı root'ta göster
});
```

### Parametreler

#### `c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API v1")`

- **İlk parametre**: Swagger JSON dosyasının yolu
- **İkinci parametre**: Swagger UI'da görünecek başlık

#### `c.RoutePrefix = string.Empty`

- **`string.Empty`**: Swagger UI'ı root URL'de gösterir
  - `http://localhost:5001/` → Swagger UI açılır
- **`"swagger"`**: Swagger UI'ı `/swagger` altında gösterir
  - `http://localhost:5001/swagger` → Swagger UI açılır

### Örnek Kullanım

```
http://localhost:5001/  → Swagger UI açılır
http://localhost:5001/swagger/v1/swagger.json  → OpenAPI JSON
```

---

## Görsel Akış

```
┌─────────────────────────────────────────────────────────┐
│ 1. Builder Aşaması                                      │
│    builder.Services.AddEndpointsApiExplorer()          │
│    builder.Services.AddSwaggerGen(...)                 │
│    ↓                                                     │
│    • Swagger servislerini kaydet                        │
│    • API dokümantasyonunu oluştur                        │
│    • Controller'lardan endpoint'leri topla              │
│    • Request/Response modellerini analiz et             │
└─────────────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ 2. builder.Build()                                      │
│    ↓                                                     │
│    • Service provider oluşturulur                      │
│    • Swagger servisleri hazır                            │
└─────────────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ 3. App Aşaması (Development ortamında)                  │
│    if (app.Environment.IsDevelopment())                │
│    {                                                     │
│        app.UseSwagger()                                 │
│        ↓                                                 │
│        • GET /swagger/v1/swagger.json endpoint'i ekle  │
│        • OpenAPI JSON formatında dokümantasyon döndür   │
│    }                                                     │
└─────────────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ 4. app.UseSwaggerUI(...)                                │
│    ↓                                                     │
│    • Swagger UI web arayüzünü ekle                      │
│    • Root URL'de göster (RoutePrefix = string.Empty)     │
│    • http://localhost:5001/ → Swagger UI açılır         │
└─────────────────────────────────────────────────────────┘
```

---

## Swagger UI Özellikleri

### 1. Endpoint Listesi

Swagger UI'da tüm endpoint'ler görüntülenir:

```
📋 Catalog API v1

Products
  GET    /api/products
  GET    /api/products/{id}
  GET    /api/products/category/{categoryId}
  POST   /api/products
  PUT    /api/products/{id}
  DELETE /api/products/{id}

Categories
  GET    /api/categories
  GET    /api/categories/{id}
  POST   /api/categories
```

### 2. Endpoint Detayları

Her endpoint için:
- **HTTP Method**: GET, POST, PUT, DELETE
- **Path**: `/api/products/{id}`
- **Parameters**: Query, path, body parametreleri
- **Request Body**: Request model şeması
- **Response**: Response model şeması
- **Try it out**: Endpoint'i test etme butonu

### 3. Model Şemaları

Request ve response modellerinin şemaları:

```json
{
  "ProductDto": {
    "type": "object",
    "properties": {
      "id": { "type": "string", "format": "uuid" },
      "name": { "type": "string" },
      "description": { "type": "string", "nullable": true },
      "price": { "type": "number", "format": "decimal" },
      "imageUrl": { "type": "string", "nullable": true },
      "categoryId": { "type": "string", "format": "uuid" },
      "categoryName": { "type": "string" }
    }
  }
}
```

### 4. Test Etme

"Try it out" butonu ile:
- Endpoint'i çağırabilirsiniz
- Parametreleri girebilirsiniz
- Request body gönderebilirsiniz
- Response'u görebilirsiniz

---

## Örnek: ProductsController Endpoint'leri

### Swagger UI'da Görünenler

```
📋 Catalog API v1

Products
  GET /api/products
    Parameters:
      - PageNumber (query, int, default: 1)
      - PageSize (query, int, default: 10)
      - CategoryId (query, Guid, optional)
    Response: 200 OK
      [
        {
          "id": "guid",
          "name": "string",
          "price": 0.00
        }
      ]

  GET /api/products/{id}
    Parameters:
      - id (path, Guid, required)
    Response: 200 OK
      {
        "id": "guid",
        "name": "string",
        "price": 0.00
      }

  POST /api/products
    Request Body:
      {
        "name": "string",
        "description": "string",
        "price": 0.00,
        "categoryId": "guid"
      }
    Response: 201 Created
      "guid"
```

---

## Builder vs App Aşaması

### Builder Aşaması

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => { ... });
```

**Ne yapar:**
- Swagger servislerini DI container'a kaydeder
- API dokümantasyonunu oluşturur (controller'lardan)
- Henüz endpoint'ler aktif değil

### App Aşaması

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();        // JSON endpoint'ini ekle
    app.UseSwaggerUI(...);   // Web UI'ı ekle
}
```

**Ne yapar:**
- Swagger middleware'lerini pipeline'a ekler
- JSON ve UI endpoint'lerini aktifleştirir
- Sadece Development ortamında çalışır

---

## Önemli Noktalar

### 1. Sadece Development'ta

```csharp
if (app.Environment.IsDevelopment())  // ← Sadece Development
{
    app.UseSwagger();
    app.UseSwaggerUI(...);
}
```

**Neden?**
- **Güvenlik**: Production'da API dokümantasyonunu gizler
- **Performans**: Gereksiz middleware'i kaldırır
- **Best Practice**: Swagger sadece development'ta kullanılmalı

### 2. RoutePrefix = string.Empty

```csharp
c.RoutePrefix = string.Empty;  // Root URL'de göster
```

**Sonuç:**
- `http://localhost:5001/` → Swagger UI
- `http://localhost:5001/swagger` → Swagger UI (eğer `"swagger"` olsaydı)

### 3. SwaggerEndpoint

```csharp
c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API v1");
```

**Açıklama:**
- **İlk parametre**: JSON dosyasının yolu
- **İkinci parametre**: UI'da görünecek başlık

### 4. İkisi Birlikte Gerekli

```csharp
// ✅ DOĞRU - İkisi de var
builder.Services.AddSwaggerGen(...);  // Dokümantasyon oluştur
app.UseSwagger();                     // JSON endpoint'i ekle
app.UseSwaggerUI(...);                // Web UI'ı ekle

// ❌ YANLIŞ - Sadece biri var
builder.Services.AddSwaggerGen(...);
// UseSwagger() yok → JSON endpoint'i yok
// UseSwaggerUI() yok → Web UI yok
```

---

## Özet Tablo

| Kısım | Aşama | Ne Yapar |
|-------|-------|----------|
| `AddEndpointsApiExplorer()` | Builder | Endpoint'leri keşfetmek için servisleri ekler |
| `AddSwaggerGen(...)` | Builder | Swagger dokümantasyonunu oluşturur |
| `if (IsDevelopment())` | App | Sadece Development ortamında çalışır |
| `UseSwagger()` | App | OpenAPI JSON endpoint'ini ekler |
| `UseSwaggerUI(...)` | App | Swagger web arayüzünü ekler |
| `SwaggerEndpoint` | App | JSON dosyasının yolunu belirtir |
| `RoutePrefix` | App | UI'ın hangi URL'de gösterileceğini belirler |

---

## Sonuç

Bu yapılandırma sayesinde:

1. **Development ortamında** Swagger UI aktif olur
2. **`http://localhost:5001/`** adresinde Swagger UI açılır
3. **API endpoint'leri** görselleştirilir ve test edilebilir
4. **OpenAPI JSON** dokümantasyonu erişilebilir olur
5. **Production'da** Swagger kapalıdır (güvenlik)

---

## Avantajlar

1. **Otomatik Dokümantasyon**: Controller'lardan otomatik oluşturulur
2. **Test Edilebilirlik**: Tarayıcıdan direkt test edilebilir
3. **Standart Format**: OpenAPI standardına uygun
4. **Client Kod Üretimi**: OpenAPI generator ile client kod üretilebilir
5. **Güvenlik**: Production'da kapalı

## Dikkat Edilmesi Gerekenler

1. **Sadece Development**: Production'da Swagger kapalı olmalı
2. **RoutePrefix**: Root URL'de göstermek güvenlik riski olabilir (sadece development'ta)
3. **Performans**: Swagger middleware'i hafif bir overhead ekler
4. **Versiyonlama**: Birden fazla API versiyonu için farklı SwaggerDoc'lar kullanılabilir
