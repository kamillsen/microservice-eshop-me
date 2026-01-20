# Health Checks Yapılandırması

## Genel Bakış

Health Checks, uygulamanın ve bağımlılıklarının (veritabanı, API'ler vb.) sağlık durumunu kontrol eden bir mekanizmadır.

---

## Kod Açıklaması

```csharp
// Health Checks
app.MapHealthChecks("/health");
```

---

## 1. Health Checks Nedir?

Health Checks, uygulamanın ve bağımlılıklarının (veritabanı, API'ler vb.) sağlık durumunu kontrol eden bir mekanizmadır.

### Ne İşe Yarar?

1. **Uygulama sağlığını kontrol eder**
2. **Veritabanı bağlantısını test eder**
3. **Load balancer'lar ve orchestrator'lar** (Kubernetes, Docker Swarm) için kullanılır
4. **Monitoring sistemleri** için sağlık durumu bilgisi sağlar

---

## 2. Builder Aşaması (Satır 48-50)

```csharp
// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!);
```

### Ne Yapar?

- Health Checks servislerini DI container'a kaydeder
- PostgreSQL veritabanı için health check ekler

### `AddNpgSql(...)`

- PostgreSQL bağlantısını test eder
- Veritabanına bağlanıp bağlanamadığını kontrol eder
- Connection string'i kullanır

---

## 3. App Aşaması (Satır 90)

```csharp
app.MapHealthChecks("/health");
```

### Ne Yapar?

- `/health` endpoint'ini oluşturur
- Health check'leri çalıştırır ve sonucu döndürür

### Endpoint

```
GET /health
```

### Response Örnekleri

#### Sağlıklı Durum (200 OK)

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "npgsql": {
      "status": "Healthy",
      "description": "PostgreSQL connection is healthy",
      "duration": "00:00:00.0123456"
    }
  }
}
```

#### Sağlıksız Durum (503 Service Unavailable)

```json
{
  "status": "Unhealthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "npgsql": {
      "status": "Unhealthy",
      "description": "PostgreSQL connection failed",
      "duration": "00:00:00.0123456",
      "exception": "Connection timeout"
    }
  }
}
```

---

## Görsel Akış

```
┌─────────────────────────────────────────────────────────┐
│ 1. Builder Aşaması                                      │
│    builder.Services.AddHealthChecks()                   │
│    .AddNpgSql(...)                                      │
│    ↓                                                     │
│    • Health Checks servislerini kaydet                   │
│    • PostgreSQL health check'i ekle                     │
└─────────────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ 2. App Aşaması                                          │
│    app.MapHealthChecks("/health")                       │
│    ↓                                                     │
│    • GET /health endpoint'ini oluştur                  │
│    • Health check'leri çalıştırır                       │
└─────────────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ 3. HTTP Request: GET /health                            │
│    ↓                                                     │
│    • PostgreSQL bağlantısını test et                    │
│    • Sonucu JSON formatında döndür                      │
│    • 200 OK (Healthy) veya 503 (Unhealthy)              │
└─────────────────────────────────────────────────────────┘
```

---

## Kullanım Senaryoları

### 1. Kubernetes Liveness/Readiness Probes

```yaml
# kubernetes-deployment.yaml
livenessProbe:
  httpGet:
    path: /health
    port: 80
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health
    port: 80
  initialDelaySeconds: 10
  periodSeconds: 5
```

### 2. Docker Health Check

```dockerfile
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s \
  CMD curl -f http://localhost:80/health || exit 1
```

### 3. Load Balancer Health Check

Load balancer'lar `/health` endpoint'ini düzenli kontrol eder:
- **Sağlıklı ise** → Trafiği yönlendirir
- **Sağlıksız ise** → Trafiği keser

### 4. Monitoring Sistemleri

Prometheus, Grafana gibi monitoring sistemleri `/health` endpoint'ini izler.

---

## Örnek: Health Check Çalışması

### Senaryo 1: Veritabanı Bağlantısı Başarılı

```bash
$ curl http://localhost:5001/health
```

**Response:**
```json
{
  "status": "Healthy",
  "entries": {
    "npgsql": {
      "status": "Healthy",
      "description": "PostgreSQL connection is healthy"
    }
  }
}
```

**HTTP Status:** `200 OK`

### Senaryo 2: Veritabanı Bağlantısı Başarısız

```bash
$ curl http://localhost:5001/health
```

**Response:**
```json
{
  "status": "Unhealthy",
  "entries": {
    "npgsql": {
      "status": "Unhealthy",
      "description": "PostgreSQL connection failed",
      "exception": "Connection timeout"
    }
  }
}
```

**HTTP Status:** `503 Service Unavailable`

---

## Önemli Noktalar

### 1. Builder vs App Aşaması

**Builder Aşaması:**
```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(...);  // Health check'leri yapılandır
```

**App Aşaması:**
```csharp
app.MapHealthChecks("/health");  // Endpoint'i oluştur
```

### 2. Birden Fazla Health Check

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(...)           // PostgreSQL
    .AddRedis(...)            // Redis
    .AddHttp(...)             // External API
    .AddCheck<CustomHealthCheck>("custom");  // Custom check
```

### 3. Farklı Endpoint'ler

```csharp
// Genel health check
app.MapHealthChecks("/health");

// Sadece veritabanı için
app.MapHealthChecks("/health/db", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db")
});

// Sadece API'ler için
app.MapHealthChecks("/health/api", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("api")
});
```

---

## Özet

| Kısım | Açıklama |
|-------|----------|
| `AddHealthChecks()` | Health Checks servislerini DI container'a kaydeder |
| `.AddNpgSql(...)` | PostgreSQL veritabanı için health check ekler |
| `MapHealthChecks("/health")` | `/health` endpoint'ini oluşturur |
| **Sonuç** | `GET /health` ile uygulama sağlığı kontrol edilebilir |

Bu yapılandırma sayesinde:

1. **`GET /health`** endpoint'i oluşturulur
2. **PostgreSQL bağlantısı** test edilir
3. **Sağlık durumu** JSON formatında döndürülür
4. **Kubernetes, Docker, Load Balancer** gibi sistemler uygulama sağlığını kontrol edebilir

---

## Avantajlar

1. **Otomatik Sağlık Kontrolü**: Uygulama ve bağımlılıkların durumu kontrol edilir
2. **Orchestration Desteği**: Kubernetes, Docker Swarm gibi sistemlerle entegre çalışır
3. **Monitoring**: Monitoring sistemleri için sağlık durumu bilgisi sağlar
4. **Load Balancing**: Load balancer'lar sağlıksız instance'ları devre dışı bırakabilir

## Dikkat Edilmesi Gerekenler

1. **Performans**: Health check'ler hızlı olmalı (timeout ayarlanmalı)
2. **Güvenlik**: Production'da health check endpoint'leri güvenli olmalı
3. **Bağımlılıklar**: Sadece kritik bağımlılıklar kontrol edilmeli
4. **Timeout**: Health check'ler çok uzun sürmemeli

---

## Detaylı JSON Response (Opsiyonel)

Varsayılan olarak Health Checks sadece "Healthy" veya "Unhealthy" text döndürür. Detaylı JSON response için `ResponseWriter` yapılandırması gerekir:

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                exception = entry.Value.Exception?.Message,
                duration = entry.Value.Duration.ToString()
            }),
            totalDuration = report.TotalDuration
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});
```

**Not:** Detaylı JSON response için `AspNetCore.HealthChecks.UI.Client` paketi de kullanılabilir (resmi önerilen yöntem).

---

## Örnek: Program.cs'deki Kullanım

```csharp
// Builder Aşaması
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!);

var app = builder.Build();

// App Aşaması
app.MapHealthChecks("/health");
```

---

## Health Check Durumları

| Durum | Açıklama | HTTP Status Code |
|-------|----------|------------------|
| **Healthy** | Tüm health check'ler başarılı | 200 OK |
| **Unhealthy** | En az bir health check başarısız | 503 Service Unavailable |
| **Degraded** | Bazı health check'ler başarısız ama kritik değil | 200 OK |

---

## Örnek: Custom Health Check

```csharp
public class CustomHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        // Custom kontrol mantığı
        var isHealthy = await CheckSomethingAsync();
        
        if (isHealthy)
        {
            return HealthCheckResult.Healthy("Custom check passed");
        }
        
        return HealthCheckResult.Unhealthy("Custom check failed");
    }
}

// Kayıt
builder.Services.AddHealthChecks()
    .AddCheck<CustomHealthCheck>("custom");
```

---

## Kaynaklar

- [Microsoft Learn - Health Checks in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-9.0)
- [ASP.NET Core Health Checks GitHub](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks)
