# AutoMapper Mekanizması - Detaylı Açıklama

> Bu dokümantasyon, AutoMapper'ın nasıl çalıştığını, Program.cs'de yaptığımız kayıtların ne sağladığını, MappingProfile'daki kuralların nasıl kaydedildiğini ve arka planda olanları detaylı olarak açıklar.

---

## Soru: AutoMapper'ı Register Ettik Ama Hiçbir Yerde Kullanmıyoruz, Nasıl Otomatik Çalışıyor?

Bu soru çok önemli! AutoMapper'ı `AddAutoMapper` ile kaydettik ama Controller'da veya Handler'da manuel olarak mapping yapmıyoruz. Peki nasıl otomatik çalışıyor?

**Cevap:** `AddAutoMapper` çağrısı, `MappingProfile` sınıfını bulur ve mapping kurallarını kaydeder. Handler'larda `_mapper.Map<T>()` çağrıldığında AutoMapper bu kuralları kullanarak dönüşümü yapar.

---

## Zaman Çizelgesi: Uygulama Başlangıcı

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Uygulama Başlar                                           │
│    └─ Program.cs çalışır                                     │
│                                                               │
│ 2. builder.Services.AddAutoMapper(...) çağrılır (Satır 24)  │
│    └─ Bu satır ne yapıyor?                                   │
│                                                               │
│ 3. MappingProfile constructor çalışır                        │
│    └─ CreateMap'ler kaydedilir                              │
│                                                               │
│ 4. IMapper servisi DI container'a eklenir                    │
│                                                               │
│ 5. Uygulama hazır (app.Run() çalışır)                        │
│                                                               │
│ 6. HTTP Request gelir                                        │
│    └─ Handler'da _mapper.Map() çağrılır                     │
│       └─ Mapping kuralları kullanılır                       │
└─────────────────────────────────────────────────────────────┘
```

---

## 1. Program.cs'de Ne Yapıyoruz?

### Kod (Program.cs, Satır 24):

```csharp
builder.Services.AddAutoMapper(typeof(Program).Assembly);
```

### Bu Satır Ne Yapıyor? (İçeride Ne Oluyor?)

AutoMapper'ın `AddAutoMapper` metodu şunları yapar:

#### 1.1. Assembly'yi Alıyor

```csharp
// AutoMapper içinde
Assembly assembly = typeof(Program).Assembly;
// → Catalog.API.dll assembly'si alınır
```

**Görsel:**
```
┌─────────────────────────────────────┐
│ Catalog.API Assembly (DLL)         │
│                                     │
│  Tüm class'lar burada:             │
│  ├─ Program.cs                     │
│  ├─ MappingProfile.cs              │
│  ├─ Product.cs                     │
│  ├─ ProductDto.cs                  │
│  ├─ CreateProductHandler.cs        │
│  └─ ... (diğer tüm class'lar)       │
└─────────────────────────────────────┘
```

#### 1.2. Assembly'deki Tüm Class'ları Tarıyor

```csharp
// AutoMapper içinde
var allTypes = assembly.GetTypes()
    .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
    .ToList();
// → [Program, MappingProfile, Product, ProductDto, CreateProductHandler, ...]
```

**Filtreleme Kriterleri:**
- `t.IsClass` → Sadece class'lar (interface, struct değil)
- `!t.IsAbstract` → Abstract class'lar hariç
- `t.IsPublic` → Public class'lar

**Önemli:** Bu aşamada sadece class'lar toplanır, henüz Profile kontrolü yapılmaz.

#### 1.3. Profile Sınıfını Miras Alan Class'ları Arıyor

```csharp
// AutoMapper içinde
foreach (var type in allTypes)
{
    // KRİTİK SORU: Bu class Profile sınıfını miras alıyor mu?
    // (Yani: Bu class Profile'dan türüyor mu?)
    if (typeof(Profile).IsAssignableFrom(type))
    {
        // ✅ EVET! Bu class Profile'dan türüyor
        // → type = typeof(MappingProfile)
        
        // Şimdi ne yapıyor?
        // → MappingProfile instance'ı oluşturuyor
    }
}
```

**`IsAssignableFrom` Kontrolü:**
- `typeof(Profile).IsAssignableFrom(typeof(MappingProfile))` → `true` ✅
  - Çünkü: `MappingProfile : Profile` (kalıtım var)
- `typeof(Profile).IsAssignableFrom(typeof(Product))` → `false` ❌
  - Çünkü: `Product` Profile'dan türemiyor

**Görsel:**
```
┌─────────────────────────────────────────────┐
│ AutoMapper: Assembly'yi tarıyor...          │
│                                             │
│ Tüm class'lar:                              │
│  ┌──────────────────────────────────────┐  │
│  │ 1. typeof(Program)                  │  │
│  │    typeof(Profile).IsAssignableFrom │  │
│  │    → false ❌ (Program : Profile değil)│
│  │    → ATLA                            │  │
│  └──────────────────────────────────────┘  │
│                                             │
│  ┌──────────────────────────────────────┐  │
│  │ 2. typeof(Product)                   │  │
│  │    typeof(Profile).IsAssignableFrom  │  │
│  │    → false ❌ (Product : Profile değil)│
│  │    → ATLA                            │  │
│  └──────────────────────────────────────┘  │
│                                             │
│  ┌──────────────────────────────────────┐  │
│  │ 3. typeof(MappingProfile)            │  │
│  │    typeof(Profile).IsAssignableFrom  │  │
│  │    → true ✅ (MappingProfile : Profile)│
│  │    → BULUNDU! ✅                      │  │
│  └──────────────────────────────────────┘  │
│                                             │
│  ... (diğer class'lar da kontrol edilir)  │
│                                             │
│ ✅ SONUÇ: MappingProfile BULUNDU!          │
│    (Profile sınıfını miras alan tek class) │
└─────────────────────────────────────────────┘
```

**Özet:** AutoMapper, assembly'deki tüm class'ları tarar ve **Profile sınıfını miras alan (inherit eden) class'ları** bulur. Bizim projemizde sadece `MappingProfile` bu kriteri sağlar.

#### 1.4. MappingProfile Instance'ı Oluşturuyor

```csharp
// AutoMapper içinde
var profileInstance = Activator.CreateInstance(typeof(MappingProfile));
// → new MappingProfile() çağrısı yapılır
```

Bu noktada `MappingProfile` constructor çalışır.

---

## 2. MappingProfile Constructor Çalışıyor

### Ne Zaman Çalışır?

`AddAutoMapper` içinde `new MappingProfile()` çağrıldığında.

### Kod (MappingProfile.cs, Satır 12-23):

```csharp
public MappingProfile()
{
    // Command → Entity
    CreateMap<CreateProductCommand, Product>();
    CreateMap<UpdateProductCommand, Product>();
    CreateMap<CreateCategoryCommand, Category>();
    
    // Entity → DTO
    CreateMap<Product, ProductDto>()
        .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty));
    CreateMap<Category, CategoryDto>();
}
```

### Her Satır Ne Yapıyor?

#### Satır 15: `CreateMap<CreateProductCommand, Product>();`

**Ne yapıyor:**
- `CreateProductCommand` → `Product` mapping kuralını kaydeder.

**Nasıl çalışıyor:**
```csharp
// AutoMapper içinde (basitleştirilmiş)
public IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
{
    // 1. Mapping kuralını oluştur
    var mappingExpression = new MappingExpression<CreateProductCommand, Product>();
    
    // 2. Property eşleştirmelerini otomatik yap (convention-based)
    // → Aynı isimli property'leri eşleştir:
    //   CreateProductCommand.Name → Product.Name
    //   CreateProductCommand.Description → Product.Description
    //   CreateProductCommand.Price → Product.Price
    //   CreateProductCommand.ImageUrl → Product.ImageUrl
    //   CreateProductCommand.CategoryId → Product.CategoryId
    
    // 3. Mapping kuralını configuration'a kaydet
    // → Kaynak tip: CreateProductCommand
    // → Hedef tip: Product
    // → Bu bilgiler configuration'a eklenir
    configuration.AddMapping(mappingExpression);
    
    return mappingExpression;
}
```

**Nereye kaydediliyor:**
```
┌─────────────────────────────────────────────┐
│ AutoMapper Configuration (Memory'de)        │
│                                             │
│ Mapping Kuralları:                         │
│  ✅ CreateProductCommand → Product         │
│     Kaynak Tip: CreateProductCommand        │
│     Hedef Tip: Product                      │
│     Property Eşleştirmeleri:                │
│       • Name → Name                         │
│       • Description → Description            │
│       • Price → Price                       │
│       • ImageUrl → ImageUrl                  │
│       • CategoryId → CategoryId             │
└─────────────────────────────────────────────┘
```

#### Satır 16: `CreateMap<UpdateProductCommand, Product>();`

**Ne yapıyor:**
- `UpdateProductCommand` → `Product` mapping kuralını kaydeder.

**Nasıl çalışıyor:**
- Aynı mantık: property'ler otomatik eşleştirilir ve configuration'a eklenir.

**Nereye kaydediliyor:**
```
┌─────────────────────────────────────────────┐
│ AutoMapper Configuration (Memory'de)        │
│                                             │
│ Mapping Kuralları:                         │
│  ✅ CreateProductCommand → Product         │
│  ✅ UpdateProductCommand → Product        │  ← YENİ
│     Kaynak Tip: UpdateProductCommand        │
│     Hedef Tip: Product                      │
└─────────────────────────────────────────────┘
```

#### Satır 17: `CreateMap<CreateCategoryCommand, Category>();`

**Ne yapıyor:**
- `CreateCategoryCommand` → `Category` mapping kuralını kaydeder.

#### Satır 20-21: `CreateMap<Product, ProductDto>()` + `ForMember`

**Ne yapıyor:**
- `Product` → `ProductDto` mapping kuralını kaydeder.
- `CategoryName` için özel kural ekler.

**Nasıl çalışıyor:**
```csharp
// Satır 20: Temel mapping kuralı
CreateMap<Product, ProductDto>()
// → Product → ProductDto mapping'i oluştur
// → Property'leri otomatik eşleştir:
//   Product.Id → ProductDto.Id
//   Product.Name → ProductDto.Name
//   Product.Description → ProductDto.Description
//   Product.Price → ProductDto.Price
//   Product.ImageUrl → ProductDto.ImageUrl
//   Product.CategoryId → ProductDto.CategoryId

// Satır 21: Özel kural ekle
.ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty));
// → ProductDto.CategoryName için özel kural:
//   Eğer Product.Category null değilse → Product.Category.Name
//   Eğer Product.Category null ise → string.Empty
```

**Nereye kaydediliyor:**
```
┌─────────────────────────────────────────────┐
│ AutoMapper Configuration (Memory'de)        │
│                                             │
│ Mapping Kuralları:                         │
│  ✅ CreateProductCommand → Product         │
│  ✅ UpdateProductCommand → Product         │
│  ✅ CreateCategoryCommand → Category       │
│  ✅ Product → ProductDto                    │  ← YENİ
│     Kaynak Tip: Product                    │
│     Hedef Tip: ProductDto                   │
│     Property Eşleştirmeleri:                │
│       • Id → Id                            │
│       • Name → Name                         │
│       • Description → Description            │
│       • Price → Price                       │
│       • ImageUrl → ImageUrl                  │
│       • CategoryId → CategoryId             │
│     Özel Kurallar:                          │
│       • CategoryName → Özel kural:          │
│         src.Category != null ?              │
│           src.Category.Name : string.Empty   │
└─────────────────────────────────────────────┘
```

#### Satır 22: `CreateMap<Category, CategoryDto>();`

**Ne yapıyor:**
- `Category` → `CategoryDto` mapping kuralını kaydeder.

---

## 3. IMapper Servisi DI Container'a Ekleniyor

### Ne Zaman?

Constructor çalıştıktan ve tüm `CreateMap` kuralları kaydedildikten sonra.

### Ne Yapıyor?

```csharp
// AutoMapper içinde
builder.Services.AddSingleton<IMapper>(sp =>
{
    // 1. Tüm mapping kurallarını içeren configuration'ı oluştur
    var configuration = new MapperConfiguration(cfg =>
    {
        cfg.AddProfile(profileInstance); // MappingProfile'daki tüm kurallar
    });
    
    // 2. IMapper instance'ı oluştur
    var mapper = configuration.CreateMapper();
    
    // 3. DI container'a ekle
    return mapper;
});
```

**Sonuç:**
```
┌─────────────────────────────────────────────┐
│ DI Container (Service Provider)            │
│                                             │
│ Servisler:                                 │
│  ✅ IMapper → Mapper instance              │
│     └─ İçinde tüm mapping kuralları var   │
│        • CreateProductCommand → Product     │
│        • UpdateProductCommand → Product    │
│        • CreateCategoryCommand → Category   │
│        • Product → ProductDto              │
│        • Category → CategoryDto            │
└─────────────────────────────────────────────┘
```

---

## 4. Uygulama Hazır (app.Run() Çalışıyor)

Bu aşamada:
- Tüm mapping kuralları kaydedildi
- `IMapper` servisi DI container'da hazır
- Handler'lar `IMapper`'ı kullanabilir

---

## 5. HTTP Request Geliyor - Handler Çalışıyor

### Senaryo 1: CreateProduct Request

#### 5.1. HTTP Request

```
POST /api/products
{
  "name": "iPhone 15",
  "description": "Apple iPhone 15 128GB",
  "price": 35000.00,
  "imageUrl": "https://example.com/iphone15.jpg",
  "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

#### 5.2. Controller → Handler Çağrısı

```csharp
// Controller (gelecekte oluşturulacak)
var command = new CreateProductCommand { ... };
var productId = await _mediator.Send(command);
```

#### 5.3. CreateProductHandler Çalışıyor

**Kod (CreateProductHandler.cs, Satır 19-31):**

```csharp
public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
{
    // 1. Command'den Entity oluştur
    var product = _mapper.Map<Product>(request);
    //    ↑ IMapper (DI'dan geliyor, içinde tüm mapping kuralları var)
    //              ↑ Hedef tip (Product)
    //                        ↑ Kaynak (CreateProductCommand)
    
    product.Id = Guid.NewGuid();
    
    // 2. Veritabanına ekle
    _context.Products.Add(product);
    await _context.SaveChangesAsync(cancellationToken);
    
    return product.Id;
}
```

#### 5.4. `_mapper.Map<Product>(request)` Çağrıldığında Ne Oluyor?

**Kritik Soru: AutoMapper Hangi Mapping Kuralını Kullanacak?**

AutoMapper, kaynak ve hedef tiplere göre doğru mapping kuralını bulur:

```csharp
// AutoMapper içinde
public TDestination Map<TDestination>(object source)
{
    // 1. Kaynak tipi al
    Type sourceType = source.GetType();
    // → typeof(CreateProductCommand)
    
    // 2. Hedef tipi al
    Type destinationType = typeof(TDestination);
    // → typeof(Product)
    
    // 3. Mapping kuralını bul (configuration'dan)
    // → Kaynak tip: CreateProductCommand
    // → Hedef tip: Product
    // → Bu iki tip eşleşen kuralı ara
    var mapping = configuration.GetAllTypeMaps()
        .FirstOrDefault(m => 
            m.SourceType == typeof(CreateProductCommand) && 
            m.DestinationType == typeof(Product));
    // → ✅ BULUNDU! (Satır 15'te kaydedilmişti)
    
    // 4. Yeni Product instance'ı oluştur
    var product = new Product();
    
    // 5. Property'leri eşleştir (kaydedilmiş kurallara göre)
    product.Name = request.Name;
    // → "iPhone 15"
    
    product.Description = request.Description;
    // → "Apple iPhone 15 128GB"
    
    product.Price = request.Price;
    // → 35000.00m
    
    product.ImageUrl = request.ImageUrl;
    // → "https://example.com/iphone15.jpg"
    
    product.CategoryId = request.CategoryId;
    // → Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6")
    
    // 6. Product instance'ı döndür
    return product;
}
```

**Görsel: Mapping Kuralı Bulma Süreci**

```
┌─────────────────────────────────────────────────────────┐
│ _mapper.Map<Product>(request) çağrıldı                  │
│                                                          │
│ 1. Kaynak tip: CreateProductCommand                     │
│ 2. Hedef tip: Product                                   │
│                                                          │
│ 3. Configuration'daki kuralları ara:                   │
│  ┌────────────────────────────────────────────────┐   │
│  │ Kural 1: CreateProductCommand → Product        │   │
│  │   Kaynak: CreateProductCommand ✅                │   │
│  │   Hedef: Product ✅                              │   │
│  │   → EŞLEŞTİ! ✅                                  │   │
│  └────────────────────────────────────────────────┘   │
│                                                          │
│  ┌────────────────────────────────────────────────┐   │
│  │ Kural 2: UpdateProductCommand → Product         │   │
│  │   Kaynak: UpdateProductCommand ❌                │   │
│  │   Hedef: Product ✅                              │   │
│  │   → EŞLEŞMEDİ! ❌                                │   │
│  └────────────────────────────────────────────────┘   │
│                                                          │
│  ┌────────────────────────────────────────────────┐   │
│  │ Kural 3: Product → ProductDto                  │   │
│  │   Kaynak: Product ❌                            │   │
│  │   Hedef: ProductDto ❌                           │   │
│  │   → EŞLEŞMEDİ! ❌                                │   │
│  └────────────────────────────────────────────────┘   │
│                                                          │
│ ✅ SONUÇ: Kural 1 kullanılacak!                        │
│    CreateProductCommand → Product                       │
└─────────────────────────────────────────────────────────┘
```

**Sonuç:**

```csharp
var product = new Product
{
    Id = Guid.Empty,  // (henüz set edilmedi)
    Name = "iPhone 15",
    Description = "Apple iPhone 15 128GB",
    Price = 35000.00m,
    ImageUrl = "https://example.com/iphone15.jpg",
    CategoryId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    Category = null  // (Command'de yok, ignore edilir)
};
```

---

### Senaryo 2: GetProductById Request

#### 5.1. HTTP Request

```
GET /api/products/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

#### 5.2. GetProductByIdHandler Çalışıyor

**Kod (GetProductByIdHandler.cs, Satır 22-35):**

```csharp
public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
{
    // 1. Ürünü bul (Category navigation property'yi Include et)
    var product = await _context.Products
        .Include(p => p.Category)
        .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
    
    // 2. Ürün bulunamazsa NotFoundException fırlat
    if (product == null)
        throw new NotFoundException(nameof(Product), request.Id);
    
    // 3. Entity → DTO mapping
    return _mapper.Map<ProductDto>(product);
    //    ↑ IMapper
    //              ↑ Hedef tip (ProductDto)
    //                            ↑ Kaynak (Product Entity)
}
```

#### 5.3. `_mapper.Map<ProductDto>(product)` Çağrıldığında Ne Oluyor?

**Kritik Soru: AutoMapper Hangi Mapping Kuralını Kullanacak?**

AutoMapper, kaynak ve hedef tiplere göre doğru mapping kuralını bulur:

```csharp
// AutoMapper içinde
public ProductDto Map<ProductDto>(Product source)
{
    // 1. Kaynak tipi al
    Type sourceType = source.GetType();
    // → typeof(Product)
    
    // 2. Hedef tipi al
    Type destinationType = typeof(ProductDto);
    // → typeof(ProductDto)
    
    // 3. Mapping kuralını bul (configuration'dan)
    // → Kaynak tip: Product
    // → Hedef tip: ProductDto
    // → Bu iki tip eşleşen kuralı ara
    var mapping = configuration.GetAllTypeMaps()
        .FirstOrDefault(m => 
            m.SourceType == typeof(Product) && 
            m.DestinationType == typeof(ProductDto));
    // → ✅ BULUNDU! (Satır 20-21'de kaydedilmişti)
    
    // 4. Yeni ProductDto instance'ı oluştur
    var productDto = new ProductDto();
    
    // 5. Property'leri eşleştir (kaydedilmiş kurallara göre)
    productDto.Id = product.Id;
    productDto.Name = product.Name;
    productDto.Description = product.Description;
    productDto.Price = product.Price;
    productDto.ImageUrl = product.ImageUrl;
    productDto.CategoryId = product.CategoryId;
    
    // 6. ÖZEL KURAL: CategoryName (Satır 21'deki ForMember)
    productDto.CategoryName = product.Category != null 
        ? product.Category.Name 
        : string.Empty;
    // → Eğer product.Category null değilse → "Elektronik"
    // → Eğer product.Category null ise → ""
    
    // 7. ProductDto instance'ı döndür
    return productDto;
}
```

**Görsel: Mapping Kuralı Bulma Süreci**

```
┌─────────────────────────────────────────────────────────┐
│ _mapper.Map<ProductDto>(product) çağrıldı                │
│                                                           │
│ 1. Kaynak tip: Product                                    │
│ 2. Hedef tip: ProductDto                                  │
│                                                           │
│ 3. Configuration'daki kuralları ara:                     │
│  ┌────────────────────────────────────────────────┐     │
│  │ Kural 1: CreateProductCommand → Product        │     │
│  │   Kaynak: CreateProductCommand ❌                │     │
│  │   Hedef: Product ❌                              │     │
│  │   → EŞLEŞMEDİ! ❌                                │     │
│  └────────────────────────────────────────────────┘     │
│                                                           │
│  ┌────────────────────────────────────────────────┐     │
│  │ Kural 2: UpdateProductCommand → Product         │     │
│  │   Kaynak: UpdateProductCommand ❌                │     │
│  │   Hedef: Product ❌                              │     │
│  │   → EŞLEŞMEDİ! ❌                                │     │
│  └────────────────────────────────────────────────┘     │
│                                                           │
│  ┌────────────────────────────────────────────────┐     │
│  │ Kural 3: Product → ProductDto                  │     │
│  │   Kaynak: Product ✅                            │     │
│  │   Hedef: ProductDto ✅                           │     │
│  │   → EŞLEŞTİ! ✅                                  │     │
│  └────────────────────────────────────────────────┘     │
│                                                           │
│ ✅ SONUÇ: Kural 3 kullanılacak!                         │
│    Product → ProductDto                                 │
│    + Özel kural: CategoryName                          │
└─────────────────────────────────────────────────────────┘
```

**Sonuç:**

```csharp
var productDto = new ProductDto
{
    Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    Name = "iPhone 15",
    Description = "Apple iPhone 15 128GB",
    Price = 35000.00m,
    ImageUrl = "https://example.com/iphone15.jpg",
    CategoryId = Guid.Parse("..."),
    CategoryName = "Elektronik"  // ← Özel kural sayesinde
};
```

---

## Özet: Kodun Çalışma Sırası ve Mapping Kuralı Seçimi

### Kodun Çalışma Sırası:

```
1. Uygulama Başlar
   └─ Program.cs çalışır

2. Satır 24: AddAutoMapper(typeof(Program).Assembly)
   └─ Assembly'yi alır
   └─ Tüm class'ları tarar
   └─ Profile sınıfını miras alan class'ları bulur (MappingProfile)
   └─ new MappingProfile() yapar

3. MappingProfile() Constructor Çalışır
   └─ CreateMap<CreateProductCommand, Product>() → Kural kaydedilir
   └─ CreateMap<UpdateProductCommand, Product>() → Kural kaydedilir
   └─ CreateMap<CreateCategoryCommand, Category>() → Kural kaydedilir
   └─ CreateMap<Product, ProductDto>() → Kural kaydedilir
   └─ .ForMember(...) → Özel kural eklenir
   └─ CreateMap<Category, CategoryDto>() → Kural kaydedilir

4. IMapper Servisi DI Container'a Eklenir
   └─ Tüm mapping kuralları içinde

5. Uygulama Hazır (app.Run())

6. HTTP Request Gelir
   └─ Handler çalışır
   └─ _mapper.Map<TDestination>(source) çağrılır
   └─ AutoMapper kaynak ve hedef tiplere göre doğru kuralı bulur
   └─ Mapping yapılır
```

### Mapping Kuralı Seçimi (Hangi Kuralın Çalışacağı):

**AutoMapper, `_mapper.Map<TDestination>(source)` çağrıldığında:**

1. **Kaynak tipi alır:** `source.GetType()` → `typeof(CreateProductCommand)`
2. **Hedef tipi alır:** `typeof(TDestination)` → `typeof(Product)`
3. **Configuration'daki tüm mapping kurallarını tarar**
4. **Kaynak tip ve hedef tip eşleşen kuralı bulur:**
   - `m.SourceType == typeof(CreateProductCommand)` ✅
   - `m.DestinationType == typeof(Product)` ✅
   - → Bu kural kullanılır!
5. **Bulunan kurala göre mapping yapar**

**Örnek:**
- `_mapper.Map<Product>(createProductCommand)` → `CreateProductCommand → Product` kuralı kullanılır
- `_mapper.Map<ProductDto>(product)` → `Product → ProductDto` kuralı kullanılır

**Sonuç:** AutoMapper, kaynak ve hedef tiplere göre doğru mapping kuralını otomatik bulur ve kullanır.

---

## Felsefe ve Mantık: Neden Bu Yaklaşım?

### 1. DRY (Don't Repeat Yourself)

**Sorun:** Her handler'da manuel mapping yapmak:
```csharp
// ❌ Manuel mapping (yapmıyoruz)
var productDto = new ProductDto
{
    Id = product.Id,
    Name = product.Name,
    Description = product.Description,
    // ... her property için tek tek yazmak gerekir
};
```

**Çözüm:** AutoMapper ile tek satır:
```csharp
// ✅ Otomatik mapping
var productDto = _mapper.Map<ProductDto>(product);
```

### 2. Separation of Concerns (Sorumlulukların Ayrılması)

**Sorun:** Handler'da hem iş mantığı hem mapping olursa:
- ❌ Kod karmaşık olur
- ❌ Test etmek zor olur
- ❌ Değişiklik yapmak zor olur

**Çözüm:** Mapping kuralları merkezi (MappingProfile'da):
- ✅ Handler sadece iş mantığını içerir
- ✅ Mapping kuralları bir yerde (MappingProfile)
- ✅ Her biri bağımsız test edilebilir

### 3. Type-Safe Mapping

**Avantaj:**
- ✅ Compile-time'da tip kontrolü
- ✅ Hatalı mapping compile-time'da yakalanır
- ✅ IntelliSense desteği

---

## Sonuç

**Kısa Cevap:** `AddAutoMapper` çağrısı, `MappingProfile` sınıfını bulur ve mapping kurallarını kaydeder. Handler'larda `_mapper.Map<T>()` çağrıldığında AutoMapper bu kuralları kullanarak dönüşümü yapar.

**Uzun Cevap:** Program.cs'deki kayıt sayesinde:
- `MappingProfile` otomatik bulunuyor (reflection)
- `CreateMap` kuralları kaydediliyor
- `IMapper` servisi DI container'a ekleniyor
- Handler'larda `_mapper.Map<T>()` ile otomatik mapping yapılıyor
- Convention-based mapping (aynı isimli property'ler otomatik)
- Custom mapping (`ForMember` ile özel kurallar)
- Kaynak ve hedef tiplere göre doğru kural otomatik bulunuyor

**Felsefe:** DRY (Don't Repeat Yourself) ve Separation of Concerns sayesinde kod daha temiz, bakımı kolay ve genişletilebilir oluyor.

---

**Son Güncelleme:** Aralık 2024

