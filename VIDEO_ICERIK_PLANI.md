# 🎬 5 Dakikalık Video İçerik Planı
## E-Shop Microservice Mimarisi - Proje Tanıtımı ve Canlı Demo

---

## ⏱️ Zaman Dağılımı

| Bölüm | Süre | Toplam |
|-------|------|--------|
| **1. Giriş ve Proje Tanıtımı** | 0:30 | 0:30 |
| **2. Mimari Genel Bakış** | 1:00 | 1:30 |
| **3. Canlı Demo - Sistem Kullanımı** | 3:00 | 4:30 |
| **4. Teknik Özet ve Kapanış** | 0:30 | 5:00 |

---

## 📝 DETAYLI İÇERİK PLANI

### 🎯 BÖLÜM 1: Giriş ve Proje Tanıtımı (0:00 - 0:30)

**Görsel:**
- Proje logosu/başlık ekranı
- Docker Compose terminal ekranı (tüm servisler çalışıyor)

**Söylenecekler:**
```
"Merhaba, bugün sizlere eğitim amaçlı geliştirdiğim bir e-ticaret 
microservice mimarisi projesini tanıtacağım. Bu proje, modern 
mikroservis mimarisi prensiplerini ve teknolojilerini öğrenmek 
için tasarlanmış bir e-ticaret uygulamasıdır.

Projede 5 ana servis bulunuyor:
- Catalog Service (Ürün kataloğu)
- Basket Service (Sepet yönetimi)
- Ordering Service (Sipariş yönetimi)
- Discount Service (İndirim servisi)
- API Gateway (Merkezi giriş noktası)

Tüm sistem Docker Compose ile tek komutla çalışıyor."
```

**Gösterilecekler:**
- Terminal: `docker compose ps` komutu (tüm container'lar çalışıyor)
- Port listesi (5000, 5001, 5002, 5003, 5004)

---

### 🏗️ BÖLÜM 2: Mimari Genel Bakış (0:30 - 1:30)

**Görsel:**
- Mimari diyagram (README.md'deki mermaid diagram)
- Servisler arası iletişim akışı

**Söylenecekler:**
```
"Projenin mimarisine bakalım. Sistem 3 katmanlı bir yapıya sahip:

1. FRONTEND KATMANI:
   - Blazor WebAssembly ile geliştirilmiş modern web arayüzü
   - Port 3000'de çalışıyor

2. API GATEWAY KATMANI:
   - YARP (Yet Another Reverse Proxy) kullanıyor
   - Tüm servislere tek giriş noktası sağlıyor
   - Port 5000'de çalışıyor

3. MICROSERVICE KATMANI:
   - Catalog API: Ürün ve kategori yönetimi (Port 5001)
   - Basket API: Sepet yönetimi, Redis cache kullanıyor (Port 5002)
   - Ordering API: Sipariş yönetimi (Port 5003)
   - Discount gRPC: İndirim servisi, yüksek performans için gRPC (Port 5004)

SERVİSLER ARASI İLETİŞİM:
- Senkron: Basket → Discount (gRPC ile indirim sorgulama)
- Asenkron: Basket → Ordering (RabbitMQ ile checkout event)

VERİTABANI YAPISI:
- Her servis kendi veritabanına sahip (Database per Service pattern)
- Catalog: PostgreSQL
- Basket: Redis (cache) + PostgreSQL (source of truth)
- Ordering: PostgreSQL
- Discount: PostgreSQL"
```

**Gösterilecekler:**
- Mimari diyagram ekranı
- Docker Compose servis listesi
- Veritabanı portları (5434, 5435, 5436, 5437)

---

### 🎮 BÖLÜM 3: Canlı Demo - Sistem Kullanımı (1:30 - 4:30)

#### 3.1. Web UI'ye Giriş ve Ürün Listeleme (1:30 - 2:00)

**Görsel:**
- Web UI ana sayfası (http://localhost:3000)
- Ürün listesi

**Söylenecekler:**
```
"Şimdi sistemi canlı olarak kullanalım. Web arayüzüne giriş yapıyoruz.

Ana sayfada ürünler listeleniyor. Bu veriler Catalog Service'ten geliyor.
API Gateway üzerinden Catalog Service'e istek atılıyor ve ürünler 
görüntüleniyor.

Bir ürünün detayına bakalım..."
```

**Gösterilecekler:**
- Web UI ana sayfa
- Ürün kartları
- Ürün detay sayfası

**Yapılacaklar:**
- Ana sayfayı göster
- Bir ürünün detayına tıkla
- Ürün bilgilerini göster

---

#### 3.2. Sepete Ürün Ekleme ve İndirim Sistemi (2:00 - 2:45)

**Görsel:**
- Sepete ürün ekleme
- Sepet sayfası (indirim gösterimi)
- Terminal: Redis cache logları (opsiyonel)

**Söylenecekler:**
```
"Şimdi sepete ürün ekleyelim. 'Sepete Ekle' butonuna tıklıyorum.

Bu işlem şunları tetikliyor:
1. Basket Service'e istek gidiyor
2. Basket Service, ürün için indirim var mı kontrol ediyor
3. Discount Service'e gRPC ile bağlanıyor (çok hızlı)
4. İndirim varsa sepete uygulanıyor
5. Sepet hem Redis'e (cache) hem PostgreSQL'e (source of truth) kaydediliyor

Sepet sayfasına gidelim. Görüyorsunuz, indirim otomatik olarak uygulandı.
Toplam fiyat ve indirim miktarı gösteriliyor.

Burada Cache-aside Pattern kullanılıyor:
- Önce Redis'e bakılıyor (hızlı)
- Redis'te yoksa PostgreSQL'den alınıyor ve Redis'e yazılıyor"
```

**Gösterilecekler:**
- Sepete ürün ekleme
- Sepet sayfası (toplam fiyat, indirim)
- Header'da sepet sayacı (canlı güncelleme)

**Yapılacaklar:**
- 2-3 ürün sepete ekle
- Sepet sayfasını göster
- İndirim uygulanmış fiyatı göster
- Ürün adetini güncelle

---

#### 3.3. Checkout İşlemi ve Event-Driven Mimari (2:45 - 3:45)

**Görsel:**
- Checkout sayfası
- Ödeme formu
- RabbitMQ Management UI (event gösterimi)
- Ordering Service logları (opsiyonel)

**Söylenecekler:**
```
"Şimdi siparişi tamamlayalım. 'Ödemeye Geç' butonuna tıklıyorum.

Checkout sayfasında ödeme ve teslimat bilgilerini giriyorum.

Bu işlem çok önemli bir mimari özelliği gösteriyor: Event-Driven Architecture.

Checkout yapıldığında:
1. Basket Service, sepetteki bilgileri alıyor
2. BasketCheckoutEvent oluşturuyor
3. Event'i RabbitMQ'ya gönderiyor (asenkron)
4. Sepeti siliyor
5. Ordering Service, RabbitMQ'dan event'i dinliyor
6. Event geldiğinde otomatik olarak sipariş oluşturuyor

Bu asenkron yapı sayesinde:
- Basket Service hızlıca cevap veriyor
- Ordering Service bağımsız çalışıyor
- Sistem daha ölçeklenebilir oluyor

RabbitMQ Management UI'da event'i görebiliriz..."
```

**Gösterilecekler:**
- Checkout sayfası
- Form doldurma
- RabbitMQ Management UI (http://localhost:15673)
- Queue'da event görünümü
- Sipariş başarı mesajı

**Yapılacaklar:**
- Checkout sayfasına git
- Formu doldur (örnek veriler)
- Siparişi tamamla
- RabbitMQ UI'da event'i göster (opsiyonel)
- Başarı mesajını göster

---

#### 3.4. Sipariş Listeleme (3:45 - 4:15)

**Görsel:**
- Siparişlerim sayfası
- Sipariş listesi
- Sipariş detayı

**Söylenecekler:**
```
"Siparişlerim sayfasına gidelim. Görüyorsunuz, az önce oluşturduğumuz 
sipariş burada listeleniyor.

Bu veriler Ordering Service'ten geliyor. Ordering Service, RabbitMQ'dan 
gelen event'i işleyerek siparişi PostgreSQL veritabanına kaydetmiş.

Sipariş detayına bakalım. Tüm bilgiler burada: ürünler, toplam fiyat, 
indirim miktarı, teslimat adresi..."
```

**Gösterilecekler:**
- Siparişlerim sayfası
- Sipariş listesi
- Sipariş detayı

**Yapılacaklar:**
- Siparişlerim sayfasına git
- Sipariş listesini göster
- Bir siparişin detayına bak

---

#### 3.5. API Gateway ve Servis İzleme (4:15 - 4:30)

**Görsel:**
- Swagger UI (Catalog, Basket, Ordering)
- Health Check endpoint'leri
- pgAdmin (veritabanı görünümü - opsiyonel)

**Söylenecekler:**
```
"Sistemin teknik detaylarına bakalım. Her servisin kendi Swagger UI'si var.
API Gateway üzerinden de erişilebilir.

Health Check endpoint'leri ile servislerin sağlık durumunu kontrol 
edebiliriz. Tüm servisler sağlıklı çalışıyor.

pgAdmin ile veritabanlarını inceleyebiliriz. Her servis kendi 
veritabanına sahip, bu microservice mimarisinin temel prensibi."
```

**Gösterilecekler:**
- Swagger UI (Catalog API)
- Health Check endpoint (http://localhost:5000/health)
- pgAdmin (opsiyonel, hızlıca göster)

---

### 🎓 BÖLÜM 4: Teknik Özet ve Kapanış (4:30 - 5:00)

**Görsel:**
- Teknoloji stack listesi
- Proje GitHub linki (opsiyonel)

**Söylenecekler:**
```
"Özet olarak, bu projede şu teknolojiler kullanıldı:

BACKEND:
- .NET 9.0, ASP.NET Core
- CQRS + MediatR pattern
- Entity Framework Core
- PostgreSQL, Redis

İLETİŞİM:
- gRPC (senkron, yüksek performans)
- RabbitMQ + MassTransit (asenkron, event-driven)
- YARP (API Gateway)

FRONTEND:
- Blazor WebAssembly
- Radzen Blazor UI components

INFRASTRUCTURE:
- Docker & Docker Compose
- Health Checks

Bu proje, microservice mimarisinin temel prensiplerini ve modern 
teknolojileri öğrenmek için mükemmel bir örnektir.

Proje GitHub'da açık kaynak olarak paylaşılmıştır. 
İzlediğiniz için teşekkürler!"
```

**Gösterilecekler:**
- Teknoloji listesi (ekran görüntüsü)
- GitHub linki (opsiyonel)
- Son ekran (teşekkür mesajı)

---

## 🎬 VİDEO ÇEKİMİ İÇİN NOTLAR

### Ön Hazırlık:
1. ✅ Tüm servislerin çalıştığından emin ol (`docker compose ps`)
2. ✅ Web UI'nin erişilebilir olduğunu kontrol et (http://localhost:3000)
3. ✅ Örnek verilerin yüklü olduğunu kontrol et (ürünler, indirimler)
4. ✅ RabbitMQ Management UI'ya erişim hazır (http://localhost:15673)
5. ✅ Terminal pencereleri hazır (log izleme için)

### Çekim Sırasında:
- 🎯 Her bölümde net geçişler yap
- 🎯 Ekran görüntülerini net göster
- 🎯 Mouse hareketlerini yavaş ve belirgin yap
- 🎯 Önemli noktalarda duraklama yap
- 🎯 Terminal komutlarını yavaş yaz/göster

### Post-Production:
- 🎬 Bölümler arası geçişler ekle
- 🎬 Önemli noktalarda zoom/pan yap
- 🎬 Alt yazı ekle (opsiyonel)
- 🎬 Arka plan müziği ekle (hafif, dikkat dağıtmayan)

---

## 📋 DEMO SENARYOSU (Adım Adım)

### Senaryo: Tam Bir Alışveriş Akışı

1. **Ana Sayfa** (0:10)
   - Web UI'yi aç
   - Ürün listesini göster
   - "Sistemde X ürün var" de

2. **Ürün Detayı** (0:15)
   - Bir ürün seç (örn: iPhone 15)
   - Detay sayfasını göster
   - Fiyat, açıklama göster

3. **Sepete Ekleme** (0:20)
   - "Sepete Ekle" butonuna tıkla
   - Header'da sepet sayacının güncellendiğini göster
   - "Sepet sayacı canlı güncelleniyor" de

4. **Sepet Yönetimi** (0:30)
   - Sepet sayfasına git
   - Ürünleri göster
   - İndirim uygulanmış fiyatı göster
   - "İndirim otomatik uygulandı, gRPC ile sorgulandı" de
   - Ürün adetini güncelle (2'ye çıkar)
   - Toplam fiyatın güncellendiğini göster

5. **Checkout** (0:40)
   - "Ödemeye Geç" butonuna tıkla
   - Formu doldur:
     - İsim: Test User
     - Email: test@example.com
     - Adres: Test Address
     - Kart: 1234 5678 9012 3456
   - "Siparişi Tamamla" butonuna tıkla
   - "Event RabbitMQ'ya gönderildi" de

6. **Sipariş Onayı** (0:20)
   - Başarı mesajını göster
   - "Siparişlerim" sayfasına git
   - Sipariş listesini göster
   - Sipariş detayını göster

7. **Teknik Gösterim** (0:25)
   - Swagger UI'yi göster (Catalog API)
   - Health Check endpoint'ini göster
   - "Tüm servisler sağlıklı" de

---

## 🎯 VURGULANACAK NOKTALAR

### Mimari Özellikler:
1. ✅ **Microservice Mimarisi**: Her servis bağımsız
2. ✅ **API Gateway Pattern**: Tek giriş noktası
3. ✅ **Database per Service**: Her servis kendi DB'si
4. ✅ **CQRS Pattern**: Command/Query ayrımı
5. ✅ **Event-Driven Architecture**: RabbitMQ ile asenkron iletişim
6. ✅ **gRPC**: Yüksek performanslı senkron iletişim
7. ✅ **Cache-aside Pattern**: Redis + PostgreSQL

### Teknolojiler:
1. ✅ .NET 9.0, ASP.NET Core
2. ✅ Docker & Docker Compose
3. ✅ PostgreSQL, Redis
4. ✅ RabbitMQ, MassTransit
5. ✅ gRPC
6. ✅ YARP (API Gateway)
7. ✅ Blazor WebAssembly

### İş Akışları:
1. ✅ Ürün listeleme → Catalog Service
2. ✅ Sepete ekleme → Basket Service + Discount gRPC
3. ✅ Checkout → Basket Service → RabbitMQ → Ordering Service
4. ✅ Sipariş listeleme → Ordering Service

---

## 📊 ZAMAN YÖNETİMİ İPUÇLARI

- ⏱️ **Giriş**: Maksimum 30 saniye (hızlı geç)
- ⏱️ **Mimari**: 1 dakika (diyagram göster, hızlı anlat)
- ⏱️ **Demo**: 3 dakika (en önemli kısım, detaylı göster)
- ⏱️ **Kapanış**: 30 saniye (özet, hızlı)

**Toplam: 5 dakika**

Eğer süre yetmezse:
- Mimari bölümünü kısalt (45 saniye)
- Teknik gösterimi kaldır (Swagger, pgAdmin)
- Sadece temel akışı göster (ürün → sepet → sipariş)

---

## ✅ KONTROL LİSTESİ (Çekim Öncesi)

- [ ] Tüm Docker container'lar çalışıyor
- [ ] Web UI erişilebilir (http://localhost:3000)
- [ ] API Gateway çalışıyor (http://localhost:5000)
- [ ] Örnek ürünler yüklü
- [ ] Örnek indirimler yüklü
- [ ] RabbitMQ Management UI erişilebilir
- [ ] Terminal pencereleri hazır
- [ ] Ekran kayıt yazılımı hazır
- [ ] Mikrofon test edildi
- [ ] Demo senaryosu hazır (adım adım)

---

## 🎬 SON NOTLAR

- Video **5 dakikayı geçmemeli**
- **Canlı demo** en önemli kısım (3 dakika)
- Mimari anlatımı **kısa ve öz** olmalı
- **Pratik örnekler** gösterilmeli
- **Teknik detaylar** kapanışta özetlenmeli

**Başarılar! 🚀**
