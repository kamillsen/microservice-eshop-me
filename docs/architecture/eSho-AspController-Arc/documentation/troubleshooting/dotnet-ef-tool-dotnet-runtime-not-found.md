# dotnet-ef Tool Sorunu: .NET Runtime Not Found

> **Tarih:** Aralık 2024  
> **Faz:** Faz 4 - Discount Service  
> **Sorun:** dotnet-ef tool migration oluştururken hata veriyor

---

## Sorun

`dotnet ef migrations add` komutu çalıştırıldığında şu hata alınıyor:

```
You must install .NET to run this application.

App: /home/kSEN/.dotnet/tools/dotnet-ef
Architecture: x64
App host version: 9.0.11
.NET location: Not found

Learn more:
https://aka.ms/dotnet/app-launch-failed
Failed to resolve libhostfxr.so [not found]. Error code: 0x80008083
```

---

## Sebep

**DOTNET_ROOT environment variable'ı yanlış ayarlanmış.**

- **Yanlış değer:** `/home/kSEN/.dotnet`
- **Doğru değer:** `/usr/lib64/dotnet` (veya sistemdeki gerçek .NET SDK yolu)

dotnet-ef tool'u .NET runtime'ı bulamıyor çünkü DOTNET_ROOT yanlış bir dizine işaret ediyor.

---

## Kontrol

Önce sistemdeki .NET SDK yolunu kontrol edin:

```bash
which dotnet
# Çıktı: /usr/bin/dotnet

dotnet --info | grep "Base Path"
# Çıktı: Base Path:   /usr/lib64/dotnet/sdk/9.0.112/
```

DOTNET_ROOT değerini kontrol edin:

```bash
echo $DOTNET_ROOT
# Çıktı: /home/kSEN/.dotnet (YANLIŞ!)
```

---

## Çözüm

### Geçici Çözüm (Sadece o terminal session için)

```bash
export DOTNET_ROOT=/usr/lib64/dotnet
cd src/Services/Discount/Discount.Grpc
dotnet ef migrations add InitialCreate --startup-project . --context DiscountDbContext
```

### Kalıcı Çözüm (Tüm terminal session'ları için)

`.bashrc` veya `.zshrc` dosyasına ekleyin:

```bash
# .bashrc veya .zshrc dosyasına ekle
export DOTNET_ROOT=/usr/lib64/dotnet
```

Sonra terminal'i yeniden başlatın veya:

```bash
source ~/.bashrc  # veya source ~/.zshrc
```

---

## Alternatif Çözüm: Local Tool Kullanımı

Eğer global tool sorunluysa, projeye local tool olarak ekleyebilirsiniz:

```bash
cd src/Services/Discount/Discount.Grpc
dotnet new tool-manifest
dotnet tool install dotnet-ef
dotnet ef migrations add InitialCreate --startup-project . --context DiscountDbContext
```

**Not:** Local tool kullanırken `dotnet ef` komutunu proje klasöründe çalıştırmanız gerekir.

---

## Doğrulama

Migration başarıyla oluşturulduğunu kontrol edin:

```bash
ls -la src/Services/Discount/Discount.Grpc/Migrations/
# Şu dosyaları görmeli:
# - 20251219041813_InitialCreate.cs
# - 20251219041813_InitialCreate.Designer.cs
# - DiscountDbContextModelSnapshot.cs
```

---

## Özet

| Sorun | Sebep | Çözüm |
|-------|-------|-------|
| dotnet-ef tool .NET runtime bulamıyor | DOTNET_ROOT yanlış ayarlanmış | `export DOTNET_ROOT=/usr/lib64/dotnet` |

---

## Notlar

- Bu sorun genellikle .NET SDK'nın sistem paket yöneticisi (dnf/yum) ile yüklendiğinde ve DOTNET_ROOT'un manuel olarak yanlış ayarlandığında ortaya çıkar.
- Fedora/RHEL sistemlerinde .NET SDK genellikle `/usr/lib64/dotnet` altında bulunur.
- `dotnet --info` komutu ile gerçek Base Path'i öğrenebilirsiniz.

---

**Son Güncelleme:** Aralık 2024

