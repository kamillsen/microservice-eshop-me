# Redis Cache-Aside Pattern ile Sepet Yönetimi

## Giriş

Bu doküman, **Microsoft'un Azure Architecture Center'da önerdiği "Cache-Aside (Edilgen Önbellek)" pattern'ının** sepet yönetimi için uygulanmasını açıklar. Bu yaklaşım, "önce cache dene, yoksa DB'den çekip cache'e koy" akışını takip eder.

Bu pattern, Microsoft, AWS ve Redis tarafından **en yaygın ve önerilen** cache kullanım yaklaşımı olarak kabul edilir.

## Temel Varsayımlar

- **DB** = Asıl gerçek veri kaynağı (system of record)
- **Redis** = Hız için tutulan kopya (cache)
- **Redis Key Formatı**: `cart:{userId}` (içinde sepet item'ları)

## Cache-Aside Pattern Özeti

### Okuma (Read) İşlemleri
```
Redis → Cache Hit ise → Kullanıcı
Redis → Cache Miss ise → DB → Redis'e yaz → Kullanıcı
```

### Yazma (Write) İşlemleri
```
DB'ye yaz → Redis key'ini sil (invalidate) → Sonraki okumada DB'den taze data cache'e dolar
```

## Senaryolar

### Senaryo 1: Kullanıcı Sepete Girer (GET /cart)

#### 1A) Redis Doluysa (Cache Hit)

**Akış:**
```
Redis → Uygulama → Kullanıcı
```

**Adımlar:**
1. Uygulama Redis'ten `cart:{userId}` okur
2. Bulursa **DB'ye gitmez**, direkt döner

#### 1B) Redis Boşsa (Cache Miss)

**Akış:**
```
Redis (yok) → DB → Redis'e yaz → Kullanıcı
```

**Adımlar:**
1. Redis'te yoksa DB'den sepeti çeker
2. Aynı veriyi Redis'e yazar
3. Kullanıcıya döner

---

### Senaryo 2: Ürün Ekleme / Çıkarma / Adet Güncelleme (POST /cart/items)

#### Önerilen Yöntem: "DB'ye Yaz, Redis'i Sil (Invalidate)"

**Akış:**
```
Uygulama → DB'ye yaz (başarılı) → Redis DEL cart:{userId}
```

**Adımlar:**
1. İstek gelir (ürün eklendi/çıkarıldı/adet değişti)
2. **DB güncellenir** (başarılı olmalı)
3. Sonra Redis'teki sepet key'i **silinir**
4. Kullanıcı tekrar sepete girince **Senaryo 1B** çalışır: DB'den okunur, Redis yeniden dolar

Bu, cache-aside'ın yazma tarafında çok kullanılan **"invalidate sonra lazy load"** yaklaşımıdır.

> **Not:** DB'ye yazıp Redis'i güncel haliyle doldurmak için DB'den tekrar çekmeye gerek yok. Redis'i zaten siliyorsun; bir sonraki okumada DB'den "taze" hali gelir ve cache dolmuş olur.

---

### Senaryo 3: Uygulama Açıldı - "Önceden Sepette Ürün Var mı?"

Bu aslında **Senaryo 1** ile aynıdır:

- Redis varsa hızlıca göster
- Yoksa DB'den getir, Redis'e yaz, göster

---

### Senaryo 4: Redis Key "Kendiliğinden Yok Oldu" (TTL / Eviction / Restart)

Bu normal bir durumdur. Redis bellek limiti veya policy yüzünden key'i silebilir (eviction).

**Ne Olur?**
- Bir sonraki GET'te Redis miss olur → DB'den çekilip tekrar cache'e yazılır (Senaryo 1B)

---

### Senaryo 5: Redis Çöktü / Erişilemiyor

**GET /cart:**
- Redis yoksa direkt DB'den okursun (daha yavaş ama çalışır)

**POST /cart/items:**
- DB'ye yazmaya devam edersin
- Redis olmadığı için invalidate edemezsin ama sorun değil (Redis gelince ilk GET'te zaten DB'den dolar)

---

### Senaryo 6: DB Çöktü / Erişilemiyor

Bu modelde DB "asıl gerçek" olduğu için:

- Sepet güncelleme (POST) **yapılamaz** (çünkü kalıcı kaynağa yazamıyorsun)
- Eğer Redis'te eski sepet varsa GET ile gösterebilirsin ama bunun "eski" olabileceğini kabul etmiş olursun

---

## Özet

### Tek Cümle Özet

- **Okuma:** Redis → yoksa DB → Redis'e yaz → dön
- **Yazma:** DB'ye yaz → Redis'i sil → sonraki okumada DB'den taze data cache'e dolar

### Neden Bu Yaklaşım?

Bu yaklaşım, **tek bir, adı konmuş ve yaygın önerilen pattern'in** (Cache-Aside / Lazy Loading) tam uygulamasıdır:

- **Microsoft (Azure Architecture Center)**: "Önce cache'e bak, yoksa asıl datastore'dan (DB) al ve cache'e koy"
- **AWS**: "Lazy caching / cache-aside" - en yaygın yaklaşım
- **Redis**: "Redis'i cache olarak kullanmanın en yaygın yolu cache-aside'dır; uygulama hem DB hem cache ile konuşur"

### "DB'ye Yaz → Redis'i Sil" Stratejisi

Cache-Aside'da **yazma anında** cache'i iki şekilde ele alırsın:
- **Invalidate etmek (silmek)** ✅ (Önerilen - En basit ve en az hata çıkaran)
- **Refresh/update etmek**

Microsoft'un güncel pratik rehberi de "write'ta DB'yi güncelle, sonra ilgili cache key'ini invalidate et veya refresh et" diye açıkça söylüyor.

---

## Referanslar

[1]: https://learn.microsoft.com/en-us/azure/architecture/patterns/cache-aside "Cache-Aside Pattern - Azure Architecture Center"

[2]: https://azure.microsoft.com/en-us/products/cache "Azure Cache for Redis"

[3]: https://redis.io/docs/latest/operate/rs/databases/memory-performance/eviction-policy/ "Eviction policy | Docs - Redis"

[4]: https://aws.amazon.com/caching/best-practices/ "Caching Best Practices"

[5]: https://redis.io/solutions/caching/ "Caching"

[6]: https://techcommunity.microsoft.com/blog/azure-managed-redis/azure-managed-redis--azure-cosmos-db-with-cache%E2%80%91aside-a-practical-guide/4475007 "Azure Managed Redis & Azure Cosmos DB with cache‑aside"
