# DbContextOptions ve Dependency Injection - Basit Açıklama

## 🎯 Bu Dokümanda Ne Öğreneceksiniz?

1. `AddDbContext` ne yapar?
2. `DbContextOptions` nedir ve neden gerekli?
3. Handler'larda DbContext nasıl kullanılır?
4. Tüm bu parçalar nasıl birbirine bağlanır?

---

## 📋 Projedeki Kodlar

### 1. Program.cs - DbContext Kaydı

```csharp
// Program.cs - Satır 41-42
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));
```

### 2. appsettings.json - Connection String

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5436;Database=CatalogDb;Username=postgres;Password=postgres"
  }
}
```

### 3. CatalogDbContext.cs - Constructor

```csharp
// CatalogDbContext.cs - Satır 8
public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
{
}
```

### 4. GetProductsHandler.cs - Kullanım

```csharp
// GetProductsHandler.cs - Satır 11, 14, 23
private readonly CatalogDbContext _context;

public GetProductsHandler(CatalogDbContext context, IMapper mapper)
{
    _context = context;
}

// Satır 23
var query = _context.Products.Include(p => p.Category).AsQueryable();
```

---

## 🔍 Adım Adım Ne Oluyor?

### ADIM 1: Program.cs'de Kayıt

```csharp
// Program.cs - Satır 41-42
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));
```

**Bu satır ne yapar?**

1. **Connection String'i Okur:**
   ```csharp
   builder.Configuration.GetConnectionString("Database")
   // Sonuç: "Host=localhost;Port=5436;Database=CatalogDb;Username=postgres;Password=postgres"
   ```

2. **DbContextOptions Oluşturur:**
   ```csharp
   // Arka planda şu oluyor:
   var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
   optionsBuilder.UseNpgsql("Host=localhost;Port=5436;...");
   var options = optionsBuilder.Options;
   // options = DbContextOptions<CatalogDbContext> (connection string içerir)
   ```

3. **DI Container'a Kaydeder:**
   ```csharp
   // İki şey kaydedilir:
   
   // 1. DbContextOptions (Singleton - tek instance)
   services.AddSingleton<DbContextOptions<CatalogDbContext>>(options);
   
   // 2. CatalogDbContext (Scoped - her request için yeni)
   services.AddScoped<CatalogDbContext>(sp =>
   {
       var opts = sp.GetRequiredService<DbContextOptions<CatalogDbContext>>();
       return new CatalogDbContext(opts);
   });
   ```

**Sonuç:** Artık DI container'da `CatalogDbContext` var ve kullanılabilir!

---

### ADIM 2: CatalogDbContext Constructor

```csharp
// CatalogDbContext.cs - Satır 8
public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
{
}
```

**Bu constructor ne yapar?**

1. **Options'ı Alır:**
   - `DbContextOptions<CatalogDbContext>` parametresi gelir
   - Bu parametre DI container'dan otomatik gelir
   - İçinde connection string var: `"Host=localhost;Port=5436;..."`

2. **Base Class'a Geçirir:**
   ```csharp
   : base(options)
   // ↑
   // DbContext base class'ına options geçirilir
   ```

3. **Base Class Ne Yapar?**
   ```csharp
   // DbContext (base class) içinde:
   public class DbContext
   {
       protected DbContext(DbContextOptions options)
       {
           // Options'ı sakla
           _options = options;
           
           // Connection string'i al
           var connectionString = options.GetConnectionString();
           // "Host=localhost;Port=5436;Database=CatalogDb;..."
           
           // PostgreSQL provider'ı hazırla
           // (henüz bağlanmaz, sadece hazırlar)
       }
   }
   ```

**Sonuç:** CatalogDbContext artık connection string'i biliyor!

---

### ADIM 3: Handler Oluşturulurken

```csharp
// GetProductsHandler.cs - Satır 14
public GetProductsHandler(CatalogDbContext context, IMapper mapper)
{
    _context = context;
}
```

**Handler oluşturulurken ne oluyor?**

1. **DI Container Devreye Girer:**
   ```csharp
   // MediatR handler'ı oluştururken:
   // "GetProductsHandler için CatalogDbContext lazım, DI'dan al!"
   ```

2. **DI Container CatalogDbContext'i Oluşturur:**
   ```csharp
   // DI container içinde:
   
   // 1. DbContextOptions'ı al (Singleton'dan - tek instance)
   var options = serviceProvider.GetRequiredService<DbContextOptions<CatalogDbContext>>();
   // options içinde: "Host=localhost;Port=5436;Database=CatalogDb;..."
   
   // 2. CatalogDbContext instance'ı oluştur (her request için yeni)
   var context = new CatalogDbContext(options);
   //                                  ↑
   //                    Constructor'a options geçirilir
   ```

3. **Handler'a Inject Edilir:**
   ```csharp
   var handler = new GetProductsHandler(context, mapper);
   //                            ↑
   //            CatalogDbContext instance'ı handler'a verilir
   ```

**Sonuç:** Handler'ın `_context` field'ı artık dolu ve kullanılabilir!

---

### ADIM 4: Handler'da Veritabanına Erişim

```csharp
// GetProductsHandler.cs - Satır 23
var query = _context.Products.Include(p => p.Category).AsQueryable();
```

**Bu satır çalışırken ne oluyor?**

1. **`_context.Products` Çağrılır:**
   ```csharp
   // CatalogDbContext.cs - Satır 12
   public DbSet<Product> Products { get; set; }
   // ↑
   // Bu DbSet, Products tablosuna erişim sağlar
   ```

2. **Connection String Kullanılır:**
   ```csharp
   // _context içinde:
   // - DbContextOptions var
   // - Options içinde connection string var
   // - Connection string kullanılarak PostgreSQL'e bağlanır
   ```

3. **SQL Sorgusu Çalıştırılır:**
   ```csharp
   var products = await query.ToListAsync(cancellationToken);
   // ↑
   // PostgreSQL'e şu sorgu gönderilir:
   // SELECT * FROM "Products" p
   // INNER JOIN "Categories" c ON p."CategoryId" = c."Id"
   ```

**Sonuç:** Veritabanından veri çekilir ve handler'a döner!

---

## 🎨 Görsel Akış - Basit Versiyon

```
┌─────────────────────────────────────────────────────────┐
│ 1. Program.cs - AddDbContext                            │
│    builder.Services.AddDbContext<CatalogDbContext>(...) │
│    ↓                                                     │
│    • Connection string'i appsettings.json'dan okur       │
│    • DbContextOptions oluşturur (connection string ile) │
│    • DI container'a kaydeder:                           │
│      - DbContextOptions (Singleton)                     │
│      - CatalogDbContext (Scoped)                        │
└─────────────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ 2. CatalogDbContext Constructor                         │
│    public CatalogDbContext(DbContextOptions<...> options)│
│    ↓                                                     │
│    • Options alınır (connection string içerir)         │
│    • base(options) ile DbContext'e geçirilir             │
│    • Connection string hazır                            │
└─────────────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ 3. Handler Oluşturulurken                                │
│    public GetProductsHandler(CatalogDbContext context) │
│    ↓                                                     │
│    • DI container CatalogDbContext'i oluşturur:          │
│      1. DbContextOptions'ı alır (Singleton'dan)        │
│      2. new CatalogDbContext(options) çağrılır          │
│    • Handler'a inject edilir                            │
└─────────────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ 4. Handler'da Kullanım                                  │
│    _context.Products.ToListAsync()                     │
│    ↓                                                     │
│    • Connection string kullanılır                       │
│    • PostgreSQL'e bağlanır                             │
│    • SQL sorgusu çalıştırılır                           │
│    • Veriler döner                                      │
└─────────────────────────────────────────────────────────┘
```

---

## 🍽️ Restoran Analojisi

### DbContextOptions (Singleton) = MENÜ

- ✅ **Sabit, değişmez** - Menü her zaman aynı
- ✅ **Herkes aynı menüyü kullanır** - Tüm müşteriler aynı menüye bakar
- ✅ **Ucuz (tek kopya)** - Sadece 1 menü var, herkes paylaşır

### CatalogDbContext (Scoped) = MASA/GARSON

- ✅ **Her müşteri için yeni masa** - Her request yeni masa
- ✅ **Her müşterinin siparişi ayrı** - Her request'in kendi verileri
- ✅ **Müşteri gidince masa temizlenir** - Request bitince dispose edilir

**Örnek:**
```
Müşteri 1 gelir:
  Menü (Singleton) → Aynı menü
  Masa 1 (Scoped) → Yeni masa

Müşteri 2 gelir:
  Menü (Singleton) → Aynı menü (tekrar kullanılır)
  Masa 2 (Scoped) → Yeni masa

Müşteri 3 gelir:
  Menü (Singleton) → Aynı menü (tekrar kullanılır)
  Masa 3 (Scoped) → Yeni masa
```

---

## 🔧 Arka Plandaki Kod (Sadeleştirilmiş)

```csharp
// AddDbContext metodunun arka plandaki kodu (basitleştirilmiş):
public static IServiceCollection AddDbContext<TContext>(
    this IServiceCollection services,
    Action<DbContextOptionsBuilder> optionsAction)
    where TContext : DbContext
{
    // 1. OPTIONS → SINGLETON (Tek instance, herkes paylaşır)
    services.AddSingleton<DbContextOptions<TContext>>(sp =>
    {
        // Options builder oluştur
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        
        // Kullanıcının lambda'sını çalıştır
        optionsAction(optionsBuilder);
        // optionsBuilder.UseNpgsql("Host=localhost;...")
        
        // Immutable options oluştur
        return optionsBuilder.Options;
    });
    
    // 2. DBCONTEXT → SCOPED (Her request için yeni instance)
    services.AddScoped<TContext>(sp =>
    {
        // Singleton'dan options'ı al
        var options = sp.GetRequiredService<DbContextOptions<TContext>>();
        
        // DbContext instance'ı oluştur
        return ActivatorUtilities.CreateInstance<TContext>(sp, options);
        // new CatalogDbContext(options)
    });
    
    return services;
}
```

---

## 📊 Özet Tablo

| Adım | Ne Oluyor | Nerede |
|------|-----------|--------|
| **1. Kayıt** | `AddDbContext` → DbContextOptions oluşturur ve DI'a kaydeder | Program.cs |
| **2. Options** | Connection string içeren options oluşturulur | DI Container (Singleton) |
| **3. Constructor** | CatalogDbContext options'ı alır | CatalogDbContext.cs |
| **4. Handler** | DI container CatalogDbContext'i oluşturur | Handler oluşturulurken |
| **5. Kullanım** | `_context.Products` ile veritabanına erişilir | Handler içinde |

---

## 🎯 Basit Özet

### Ne Yapıyoruz?

1. **Program.cs'de:** DbContext'i DI container'a kaydediyoruz
2. **CatalogDbContext'te:** Options'ı alıp base class'a geçiriyoruz
3. **Handler'larda:** DI'dan CatalogDbContext'i alıp kullanıyoruz

### Neden Bu Şekilde?

- **Connection String Tek Yerde:** appsettings.json'da
- **Her Request Yeni DbContext:** Güvenli ve temiz
- **DI Kullanımı:** Constructor injection ile temiz kod

### Sonuç

```
Program.cs (AddDbContext)
    ↓
DbContextOptions oluşturulur (connection string ile)
    ↓
CatalogDbContext constructor (options alır)
    ↓
Handler'da DI'dan CatalogDbContext alınır
    ↓
Veritabanına erişilir!
```

---

## 💡 Önemli Noktalar

### 1. DbContextOptions → Singleton

- **Neden?** Immutable, thread-safe, herkes aynı options'ı kullanabilir
- **Ne zaman oluşturulur?** **Başta (uygulama başlangıcında) 1 kez oluşturulur**
- **İstek gelince oluşturulur mu?** **Hayır!** İstek gelmeden önce hazırdır
- **Kaç tane var?** 1 tane (tüm request'ler paylaşır)

**Akış:**

```
1. Uygulama başlar
   ↓
2. Program.cs çalışır
   ↓
3. AddDbContext çağrılır
   ↓
4. DbContextOptions<CatalogDbContext> oluşturulur (1 kez) ✅
   ↓
5. DI container'a Singleton olarak kaydedilir
   ↓
6. Uygulama hazır (henüz request gelmedi)
   ↓
7. Request 1 gelir → DbContextOptions kullanılır (aynı instance)
   ↓
8. Request 2 gelir → DbContextOptions kullanılır (aynı instance)
   ↓
9. Request 3 gelir → DbContextOptions kullanılır (aynı instance)
```

**Önemli:**
- ✅ **Başta 1 kez oluşturulur** (Singleton)
- ❌ **Her request'te yeniden oluşturulmaz**
- ✅ **Tüm request'ler aynı instance'ı kullanır**

### 2. CatalogDbContext → Scoped

- **Neden?** Her request için yeni instance, stateful (tracking)
- **Ne zaman oluşturulur?** **Her HTTP request'te yeni oluşturulur**
- **Kaç tane var?** Her request için 1 tane (request bitince dispose)

**Önemli:**
- ✅ **Her request'te yeni oluşturulur** (Scoped)
- ✅ **Ama her seferinde aynı DbContextOptions instance'ını kullanır**
- ✅ **Request bitince dispose edilir**

### 3. Connection String Nereden Geliyor?

```csharp
// Program.cs
builder.Configuration.GetConnectionString("Database")
//                                 ↑
//                    appsettings.json'dan okunur
```

```json
// appsettings.json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5436;Database=CatalogDb;..."
  }
}
```

---

## ✅ Kontrol Listesi

- [x] Program.cs'de `AddDbContext` var mı?
- [x] appsettings.json'da connection string var mı?
- [x] CatalogDbContext constructor'da `DbContextOptions` parametresi var mı?
- [x] Handler'larda `CatalogDbContext` inject ediliyor mu?
- [x] `_context.Products` gibi DbSet'ler kullanılıyor mu?

**Hepsi varsa → Her şey çalışıyor! ✅**

---

## 🚀 Sonuç

**Basitçe:**

1. `AddDbContext` → DbContext'i DI'a kaydeder
2. `DbContextOptions` → Connection string'i taşır (Singleton)
3. `CatalogDbContext` → Options'ı alır ve veritabanına bağlanır (Scoped)
4. Handler → DI'dan CatalogDbContext'i alır ve kullanır

**Bu kadar!** 🎉
