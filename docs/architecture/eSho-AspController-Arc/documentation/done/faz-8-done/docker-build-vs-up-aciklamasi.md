# Docker Build vs Docker Compose Up - Detaylı Açıklama

## 🎯 Temel Kavramlar

### IMAGE (Görüntü/Şablon)
- **Ne?** Çalıştırılabilir paket, şablon gibi düşün
- **Ne zaman oluşur?** `docker build` komutu ile
- **Ne içerir?** Uygulama kodları, bağımlılıklar, runtime
- **Durum:** **Durağan** (çalışmıyor, sadece hazır bekliyor)
- **Analoji:** Yemek tarifi kağıdı (tarif var ama henüz yemek yok)

### CONTAINER (Konteyner/Çalışan Örnek)
- **Ne?** Image'dan oluşturulmuş çalışan örnek
- **Ne zaman oluşur?** `docker run` veya `docker-compose up` ile
- **Ne içerir?** Image'daki her şey + çalışma zamanı durumu
- **Durum:** **Aktif** (çalışıyor, request alıyor, işlem yapıyor)
- **Analoji:** Tarife göre yapılmış yemek (yemek hazır ve yenebilir)

---

## 📦 docker build - Ne İşe Yarar?

### Komut:
```bash
docker build -f src/Services/Catalog/Catalog.API/Dockerfile -t catalogapi .
```

### Ne Yapar?
1. **Dockerfile'ı okur** → İçindeki talimatları görür
2. **Kaynak kodları alır** → Solution root'tan dosyaları kopyalar
3. **Projeyi derler** → dotnet restore, build, publish
4. **Image oluşturur** → Tüm dosyaları bir araya getirir
5. **Image'ı kaydeder** → Docker'a `catalogapi:latest` adıyla kaydeder

### Sonuç:
✅ **Image oluştu** ama **henüz çalışmıyor!**
- Disk'te duruyor
- Hazır bekliyor
- İstediğin zaman container olarak başlatabilirsin

### Örnek:
```bash
$ docker build -f ... -t catalogapi .
[+] Building 182.7s (21/21) FINISHED
 => => naming to docker.io/library/catalogapi:latest

$ docker images catalogapi
REPOSITORY    TAG       SIZE
catalogapi    latest    346MB
```
→ Image oluştu, ama **çalışmıyor!**

---

## 🚀 docker-compose up - Ne İşe Yarar?

### Komut:
```bash
docker-compose up -d
```

### Ne Yapar?
1. **docker-compose.yml'i okur** → Hangi servisler var?
2. **Image'ları kontrol eder** → Image var mı? Yoksa build eder
3. **Container'ları oluşturur** → Her image'dan bir container yaratır
4. **Container'ları başlatır** → Her container'da uygulamayı çalıştırır
5. **Ağ bağlantılarını yapar** → Container'lar birbiriyle konuşabilir

### Sonuç:
✅ **Container'lar çalışıyor!**
- Uygulamalar aktif
- Request alıyorlar
- İşlem yapıyorlar
- Birbirleriyle iletişim kurabiliyorlar

### Örnek:
```bash
$ docker-compose up -d
[+] Running 10/10
 ✔ Container catalogdb      Started
 ✔ Container catalog.api    Started

$ docker-compose ps
NAME              STATUS
catalogdb         Up (healthy)
catalog.api       Up (healthy)
```
→ **Container'lar çalışıyor!** 🎉

---

## 🔄 İlişki ve Sıralama

### 1. Adım: BUILD (Image Oluştur)
```bash
docker build -f ... -t catalogapi .
```
**Sonuç:** Image hazır (ama çalışmıyor)

### 2. Adım: UP (Container Başlat)
```bash
docker-compose up -d
```
**Sonuç:** Image'dan container oluşturuldu ve başlatıldı (çalışıyor!)

---

## 📋 docker-compose.yml'de Build

### İki Senaryo:

#### Senaryo 1: Image Önceden Build Edilmiş
```yaml
services:
  catalog.api:
    image: catalogapi  # ← Bu image'ı kullan (eğer yoksa hata verir)
    container_name: catalog.api
    ports:
      - "5001:8080"
```

**Çalışma:**
```bash
# 1. Önce build et
docker build -f ... -t catalogapi .

# 2. Sonra up yap (image'ı bulur ve container oluşturur)
docker-compose up -d
```

#### Senaryo 2: Build Otomatik (docker-compose.yml içinde)
```yaml
services:
  catalog.api:
    build:  # ← Image yoksa build eder
      context: .
      dockerfile: src/Services/Catalog/Catalog.API/Dockerfile
    container_name: catalog.api
    ports:
      - "5001:8080"
```

**Çalışma:**
```bash
# Sadece up yap (image yoksa otomatik build eder)
docker-compose up -d

# Veya manuel build:
docker-compose build catalog.api
```

---

## 🎬 Tam Senaryo Örneği

### Senaryo: Catalog.API'yi Çalıştırmak

#### Adım 1: Build (Image Oluştur)
```bash
docker build -f src/Services/Catalog/Catalog.API/Dockerfile -t catalogapi .
```

**Ne oldu?**
- ✅ Dockerfile okundu
- ✅ Kaynak kodlar kopyalandı
- ✅ Proje derlendi (dotnet build/publish)
- ✅ Image oluşturuldu: `catalogapi:latest`
- ❌ Henüz **çalışmıyor!**

**Kontrol:**
```bash
$ docker images catalogapi
REPOSITORY    TAG       SIZE
catalogapi    latest    346MB

$ docker ps
# Boş - container yok!
```

#### Adım 2: Up (Container Başlat)
```bash
docker-compose up -d catalog.api
```

**docker-compose.yml:**
```yaml
catalog.api:
  image: catalogapi  # ← Yukarıda build ettiğimiz image'ı kullan
  container_name: catalog.api
  ports:
    - "5001:8080"
  environment:
    - ConnectionStrings__Database=Host=catalogdb;Port=5432;...
```

**Ne oldu?**
- ✅ `catalogapi` image'ı bulundu
- ✅ Image'dan container oluşturuldu
- ✅ Container başlatıldı
- ✅ Port mapping yapıldı (5001:8080)
- ✅ Environment variables ayarlandı
- ✅ Uygulama çalışmaya başladı!

**Kontrol:**
```bash
$ docker ps
CONTAINER ID   IMAGE         STATUS
abc123         catalogapi    Up 2 minutes (healthy)

$ curl http://localhost:5001/health
{"status":"Healthy"}  # ← Çalışıyor! 🎉
```

---

## 🔍 Build vs Up - Karşılaştırma

| Özellik | docker build | docker-compose up |
|---------|--------------|-------------------|
| **Ne yapar?** | Image oluşturur | Container başlatır |
| **Ne zaman?** | Kod değiştiğinde | Sistem çalıştırmak istediğinde |
| **Sonuç** | Image (durağan) | Container (çalışan) |
| **Çalışıyor mu?** | ❌ Hayır | ✅ Evet |
| **Sıklık** | Nadir (kod değişince) | Sık (her çalıştırmada) |
| **Süre** | Uzun (derleme var) | Kısa (image varsa) |

---

## 💡 Önemli Noktalar

### 1. Build Olmadan Up Yaparsan?
```bash
# Image yoksa:
$ docker-compose up -d catalog.api
ERROR: image catalogapi:latest not found
```
❌ **Hata!** Önce build etmelisin.

### 2. Build Yapmadan Up Yaparsan? (docker-compose.yml'de build varsa)
```yaml
services:
  catalog.api:
    build:  # ← Build tanımı var
      context: .
      dockerfile: src/Services/Catalog/Catalog.API/Dockerfile
```
```bash
$ docker-compose up -d catalog.api
[+] Building 182.7s  # ← Otomatik build eder!
[+] Running 1/1
 ✔ Container catalog.api Started
```
✅ **Otomatik build eder!**

### 3. Build Edip Up Yapmazsan?
```bash
$ docker build -f ... -t catalogapi .
✅ Image oluştu

$ docker ps
# Boş - container yok, uygulama çalışmıyor!
```
⚠️ **Image hazır ama çalışmıyor!** Up yapman lazım.

### 4. Up Yapınca Ne Olur?
```bash
$ docker-compose up -d
```
1. Image var mı kontrol eder
2. Yoksa build eder (eğer docker-compose.yml'de build tanımı varsa)
3. Image'dan container oluşturur
4. Container'ı başlatır
5. Uygulama çalışmaya başlar

---

## 🎯 Özet

### docker build
- **Ne yapar?** Image oluşturur (şablon hazırlar)
- **Ne zaman?** Kod değiştiğinde (nadir)
- **Sonuç:** Image hazır ama **çalışmıyor**

### docker-compose up
- **Ne yapar?** Container'ları başlatır (uygulamayı çalıştırır)
- **Ne zaman?** Sistem çalıştırmak istediğinde (sık)
- **Sonuç:** Container'lar **çalışıyor!**

### İlişki:
```
BUILD → Image oluştur (hazırla)
  ↓
 UP   → Container başlat (çalıştır)
```

**Örnek:**
```bash
# 1. Build (bir kez veya kod değişince)
docker build -f ... -t catalogapi .

# 2. Up (her çalıştırmada)
docker-compose up -d  # ← Image'dan container oluşturur ve başlatır
```

---

## 🚀 Pratik Kullanım

### Senaryo 1: İlk Kez Çalıştırma
```bash
# 1. Tüm image'ları build et
docker build -f src/Services/Catalog/Catalog.API/Dockerfile -t catalogapi .
docker build -f src/Services/Basket/Basket.API/Dockerfile -t basketapi .
# ... diğerleri

# 2. Tüm sistemi başlat
docker-compose up -d
```

### Senaryo 2: Kod Değişti
```bash
# 1. Sadece değişen servisi rebuild et
docker build -f src/Services/Catalog/Catalog.API/Dockerfile -t catalogapi .

# 2. Container'ı yeniden başlat
docker-compose up -d --force-recreate catalog.api
```

### Senaryo 3: Her Şeyi Yeniden Başlat
```bash
# 1. Tümünü durdur
docker-compose down

# 2. Tümünü yeniden başlat (image'lar cache'den kullanılır)
docker-compose up -d
```

---

## ✅ Sonuç

**docker build:**
- Image oluşturur (hazırlar)
- Çalıştırmaz, sadece hazırlar
- Kod değişince yapılır

**docker-compose up:**
- Image'dan container oluşturur ve başlatır (çalıştırır)
- Image varsa hızlı, yoksa önce build eder
- Her çalıştırmada yapılır

**İlişki:** Build = Hazırlık, Up = Çalıştırma

