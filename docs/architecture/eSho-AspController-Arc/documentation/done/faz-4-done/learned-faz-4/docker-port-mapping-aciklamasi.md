# Docker Port Mapping Açıklaması

> **Tarih:** Aralık 2024  
> **Konu:** Docker Compose Port Mapping Formatı ve Çakışma Durumları

---

## Port Mapping Formatı

Docker Compose'da port mapping formatı:

```yaml
ports:
  - "HOST_PORT:CONTAINER_PORT"
```

### Örnek:
```yaml
discountdb:
  ports:
    - "5434:5432"
```

---

## İki Port'un Anlamı

### İlk Sayı (5434) = HOST PORT
- **Bilgisayarınızın (host) portu**
- Localhost'tan erişim için kullanılır
- `localhost:5434` → Container'a yönlendirilir
- Sistemdeki başka bir servis ile çakışmaması için farklı port seçilir

### İkinci Sayı (5432) = CONTAINER PORT
- **Container içindeki port**
- PostgreSQL'in default portu
- Container içinde PostgreSQL 5432 portunda çalışır

---

## Nasıl Çalışır?

```
┌─────────────────────────────────────────┐
│  Bilgisayarınız (Host)                  │
│                                          │
│  localhost:5434 ────────────────────┐   │
│                                     │   │
└─────────────────────────────────────┼───┘
                                      │
                                      ▼
┌─────────────────────────────────────────┐
│  Docker Container (discountdb)          │
│                                          │
│  PostgreSQL çalışıyor                   │
│  Port: 5432 (container içinde)          │
└─────────────────────────────────────────┘
```

---

## Container Port'ları Aynı Olabilir mi?

### ✅ EVET! Container port'ları aynı olabilir

**Örnek (docker-compose.yml):**
```yaml
catalogdb:   "5436:5432"  ← Container içinde 5432
orderingdb:  "5435:5432"  ← Container içinde 5432
discountdb:  "5434:5432"  ← Container içinde 5432
```

**Neden çakışmıyor?**
- Her container **kendi izole network'ünde** çalışır
- Container'lar birbirinden izole olduğu için aynı port kullanabilir
- Container içinde PostgreSQL her zaman 5432'de çalışır (default port)

---

## Host Port'ları Farklı Olmalı mı?

### ✅ EVET! Host port'ları farklı olmalı

**Örnek (docker-compose.yml):**
```yaml
catalogdb:   "5436:5432"  ← Host port: 5436 ✅
orderingdb:  "5435:5432"  ← Host port: 5435 ✅
discountdb:  "5434:5432"  ← Host port: 5434 ✅
```

**Neden farklı olmalı?**
- Host port'ları **bilgisayarınızın portlarıdır**
- Aynı host port'u iki container'a atanamaz
- Localhost'tan erişim için farklı port'lar gerekir

---

## Görsel Açıklama

```
┌─────────────────────────────────────────────────┐
│  Bilgisayarınız (Host)                           │
│                                                   │
│  localhost:5436 ───► catalogdb:5432  ✅          │
│  localhost:5435 ───► orderingdb:5432 ✅          │
│  localhost:5434 ───► discountdb:5432 ✅          │
│                                                   │
│  (Host port'ları farklı, çakışma yok)            │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│  Container Network (İzole)                      │
│                                                   │
│  catalogdb:5432   (kendi network'ünde)           │
│  orderingdb:5432  (kendi network'ünde)          │
│  discountdb:5432  (kendi network'ünde)          │
│                                                   │
│  (Container port'ları aynı olabilir, izole)      │
└─────────────────────────────────────────────────┘
```

---

## Özet Tablosu

| Port Türü | Durum | Neden? |
|-----------|-------|--------|
| **Container port (5432)** | Hepsi aynı ✅ | Container'lar izole, çakışmaz |
| **Host port (5434, 5435, 5436)** | Hepsi farklı ✅ | Host port'ları çakışmamalı |

---

## Connection String Kullanımı

### Localhost'tan Bağlanırken:
```csharp
// appsettings.json
"ConnectionStrings": {
  "Database": "Host=localhost;Port=5434;Database=DiscountDb;Username=postgres;Password=postgres"
}
```
**Not:** Host port kullanılır (5434)

### Container Network İçinden Bağlanırken:
```csharp
// Docker Compose environment variable
ConnectionStrings__Database=Host=discountdb;Port=5432;Database=DiscountDb;Username=postgres;Password=postgres
```
**Not:** Container port kullanılır (5432), container adı kullanılır (discountdb)

---

## Sonuç

- ✅ **Container port'ları aynı olabilir** → Container'lar izole
- ✅ **Host port'ları farklı olmalı** → Host port'ları çakışmamalı
- ✅ **Mevcut yapı doğru** → Her container farklı host port'u kullanıyor

---

**Son Güncelleme:** Aralık 2024

