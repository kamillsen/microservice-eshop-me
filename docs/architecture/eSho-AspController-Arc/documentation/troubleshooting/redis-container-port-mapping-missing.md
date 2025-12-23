# Redis Container - Port Mapping Eksik Sorunu

> **Tarih:** Aralık 2024  
> **Faz:** Faz 7 - API Gateway  
> **Sorun:** Redis container çalışıyor ama port mapping yok, RedisInsight UI'ya erişilemiyor

---

## Sorun

Redis container (`basketdb`) çalışıyor ancak port mapping eksik. `localhost:6379` (Redis) ve `localhost:8001` (RedisInsight UI) erişilemiyor.

### Hata Belirtileri

1. **RedisInsight UI açılmıyor:**
   ```
   http://localhost:8001 → Erişilemiyor
   ```

2. **Port mapping görünmüyor:**
   ```bash
   docker ps | grep basketdb
   # Çıktı: basketdb    Up   6379/tcp, 8001/tcp
   # Port mapping yok! (0.0.0.0:6379->6379/tcp görünmüyor)
   ```

3. **Container çalışıyor ama erişilemiyor:**
   ```bash
   curl http://localhost:8001
   # Sonuç: Connection refused veya timeout
   ```

### Neden Oluşur?

**Olası Nedenler:**

1. **Container manuel oluşturulmuş:**
   - `docker run` komutu ile oluşturulmuş ve port mapping belirtilmemiş
   - `docker-compose.yml` kullanılmamış

2. **Eski container versiyonu:**
   - Container eski bir `docker-compose.yml` versiyonuyla oluşturulmuş
   - `docker-compose.yml` güncellenmiş ama container yeniden oluşturulmamış

3. **Container başka bir yöntemle oluşturulmuş:**
   - Docker Desktop GUI ile oluşturulmuş
   - Port mapping ayarları eksik bırakılmış

---

## Kontrol

### 1. Container Durumunu Kontrol Et

```bash
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep basketdb
```

**Sorunlu Durum:**
```
basketdb    Up   6379/tcp, 8001/tcp
```
Port mapping yok! (`0.0.0.0:6379->6379/tcp` görünmüyor)

**Doğru Durum:**
```
basketdb    Up   0.0.0.0:6379->6379/tcp, 0.0.0.0:8001->8001/tcp
```
Port mapping var! ✅

### 2. Port Erişimini Kontrol Et

```bash
# RedisInsight UI
curl -I http://localhost:8001
# Sonuç: Connection refused veya timeout → Sorun var

# Redis CLI
docker exec basketdb redis-cli ping
# Sonuç: PONG → Container içinde çalışıyor ama host'tan erişilemiyor
```

### 3. docker-compose.yml Kontrol Et

```bash
cat docker-compose.yml | grep -A 10 basketdb
```

**Doğru Konfigürasyon:**
```yaml
basketdb:
  image: redis/redis-stack:latest
  container_name: basketdb
  ports:
    - "6379:6379"      # Redis
    - "8001:8001"      # RedisInsight UI
```

Port mapping `docker-compose.yml`'de tanımlı olmalı.

---

## Çözüm

### Geçici Çözüm (Container'ı Yeniden Başlat)

**Not:** Bu çözüm geçici değil, kalıcıdır. Container'ı `docker-compose.yml` ile yeniden oluşturduğumuz için port mapping kalıcı olarak uygulanır.

```bash
cd /path/to/project
docker compose stop basketdb
docker compose rm -f basketdb
docker compose up -d basketdb
```

**Adım Adım:**

1. **Container'ı durdur:**
   ```bash
   docker compose stop basketdb
   ```

2. **Container'ı sil:**
   ```bash
   docker compose rm -f basketdb
   ```
   `-f` flag'i: Force (onay beklemeden sil)

3. **Container'ı yeniden oluştur:**
   ```bash
   docker compose up -d basketdb
   ```
   `-d` flag'i: Detached mode (arka planda çalıştır)

4. **Kontrol et:**
   ```bash
   docker ps | grep basketdb
   # Çıktı: 0.0.0.0:6379->6379/tcp, 0.0.0.0:8001->8001/tcp ✅
   ```

### Alternatif: Tüm Servisleri Yeniden Başlat

Eğer birden fazla container'da sorun varsa:

```bash
docker compose down
docker compose up -d
```

**Not:** Bu komut tüm container'ları durdurur ve yeniden başlatır. Veriler volume'larda saklandığı için kaybolmaz.

---

## Doğrulama

### 1. Port Mapping Kontrolü

```bash
docker ps --format "table {{.Names}}\t{{.Ports}}" | grep basketdb
```

**Beklenen Çıktı:**
```
basketdb    0.0.0.0:6379->6379/tcp, 0.0.0.0:8001->8001/tcp
```

### 2. RedisInsight UI Erişimi

```bash
curl -I http://localhost:8001
```

**Beklenen Çıktı:**
```
HTTP/1.1 200 OK
Content-Type: text/html
```

Tarayıcıda aç: http://localhost:8001 → RedisInsight UI görünmeli

### 3. Redis CLI Erişimi

```bash
# Container içinden (her zaman çalışır)
docker exec basketdb redis-cli ping
# Sonuç: PONG

# Host'tan (port mapping varsa çalışır)
redis-cli -h localhost -p 6379 ping
# Sonuç: PONG
```

---

## Önleme

### Her Zaman docker-compose.yml Kullan

**✅ DOĞRU:**
```bash
# docker-compose.yml ile container oluştur
docker compose up -d basketdb
```

**❌ YANLIŞ:**
```bash
# Manuel container oluşturma (port mapping unutulabilir)
docker run -d --name basketdb redis/redis-stack:latest
```

### docker-compose.yml Değişikliklerini Uygula

`docker-compose.yml` dosyasında değişiklik yaptıktan sonra:

```bash
# Container'ı yeniden oluştur
docker compose up -d --force-recreate basketdb
```

`--force-recreate` flag'i: Container'ı silip yeniden oluşturur (ayarları günceller)

### Container'ları Düzenli Kontrol Et

```bash
# Tüm container'ların port mapping'lerini kontrol et
docker ps --format "table {{.Names}}\t{{.Ports}}"
```

Eksik port mapping görürsen, container'ı yeniden oluştur.

---

## Teknik Detaylar

### Port Mapping Nasıl Çalışır?

**Format:**
```
"host_port:container_port"
```

**Örnek:**
```yaml
ports:
  - "6379:6379"    # Host port 6379 → Container port 6379
  - "8001:8001"    # Host port 8001 → Container port 8001
```

**Açıklama:**
- Sol taraf (host port): Host makinede erişilecek port
- Sağ taraf (container port): Container içindeki port
- `0.0.0.0:6379` → Tüm network interface'lerinden erişilebilir
- `127.0.0.1:6379` → Sadece localhost'tan erişilebilir

### Container Label'ları

Container'ın `docker-compose.yml` ile yönetilip yönetilmediğini kontrol et:

```bash
docker inspect basketdb --format='{{.Config.Labels}}' | grep compose
```

**Beklenen Çıktı:**
```
com.docker.compose.service=basketdb
com.docker.compose.project=microservice-practice-me
```

Bu label'lar varsa, container `docker-compose.yml` ile yönetiliyor demektir.

---

## Özet

| Sorun | Sebep | Çözüm |
|-------|-------|-------|
| Redis container çalışıyor ama port mapping yok | Container manuel oluşturulmuş veya eski versiyon | `docker compose stop/rm/up` ile yeniden oluştur |
| RedisInsight UI erişilemiyor | Port mapping eksik | Container'ı `docker-compose.yml` ile yeniden oluştur |
| Port mapping kayboluyor | Container `docker-compose.yml` dışında yönetiliyor | Her zaman `docker-compose.yml` kullan |

---

## İlgili Dosyalar

- `docker-compose.yml` → Redis container konfigürasyonu
- `src/Services/Basket/Basket.API/appsettings.json` → Redis connection string

---

## Notlar

- Port mapping sadece container oluşturulurken yapılır
- Container'ı yeniden başlatmak (`docker restart`) port mapping'i korur
- Container'ı silip yeniden oluşturmak port mapping'i `docker-compose.yml`'den alır
- Volume'lar korunur, veri kaybı olmaz

---

**Son Güncelleme:** Aralık 2024

