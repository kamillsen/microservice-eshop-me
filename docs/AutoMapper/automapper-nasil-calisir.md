# AutoMapper Nasıl Çalışır?

## Genel Bakış

AutoMapper, .NET'te nesneler arası otomatik mapping (dönüşüm) işlemlerini gerçekleştiren bir kütüphanedir. Catalog.API projesinde Entity ↔ DTO ve Command → Entity dönüşümleri için kullanılmaktadır.

## Program.cs'de Kayıt

```csharp
builder.Services.AddAutoMapper(typeof(Program).Assembly);
```

Bu satır ne yapar?

1. **Assembly Taraması**: Belirtilen assembly'deki tüm `Profile` sınıflarını bulur
2. **Profile Yükleme**: Her `Profile` sınıfının constructor'ını çalıştırır
3. **Configuration Oluşturma**: `CreateMap` çağrıları bir `MapperConfiguration` içinde saklanır
4. **Mapper Instance**: Bu configuration'dan bir `Mapper` instance'ı oluşturulur
5. **DI Kaydı**: `IMapper` interface'i olarak Dependency Injection container'a kaydedilir

## MappingProfile İçindekiler Nereye Kaydediliyor?

### MappingProfile Sınıfı

```csharp
public class MappingProfile : Profile
{
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
}
```

### İç Mekanizma

`CreateMap` çağrıları bir `MapperConfiguration` içinde saklanır:

```csharp
// AutoMapper internal olarak şöyle bir şey yapar:
var config = new MapperConfiguration(cfg => {
    cfg.AddProfile<MappingProfile>();  // Profile'ı ekler
    // MappingProfile constructor çalışır
    // CreateMap<CreateProductCommand, Product>() → config'e kaydedilir
});

var mapper = config.CreateMapper();  // Mapper instance oluşturulur
services.AddSingleton<IMapper>(mapper);  // DI'a kaydedilir
```

**Önemli Noktalar:**
- Her `CreateMap` çağrısı, kaynak tip ve hedef tip bilgisini bir dictionary benzeri yapıda saklar
- Mapping kuralları (property mapping, custom mapping vb.) bu yapıda tutulur
- `MapperConfiguration` tüm mapping'leri içeren bir konfigürasyon nesnesidir

## IMapper Üzerinden Nasıl Kullanılıyor?

### Handler'da Kullanım

```csharp
public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly CatalogDbContext _context;
    private readonly IMapper _mapper;  // ← DI'dan inject edilir

    public CreateCategoryHandler(CatalogDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        // Command'den Entity oluştur
        var category = _mapper.Map<Category>(request);
        //                    ↑
        //                    IMapper.Map<TDestination>(source)
        
        category.Id = Guid.NewGuid();
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return category.Id;
    }
}
```

### Map Metodunun Çalışma Adımları

`_mapper.Map<Category>(request)` çağrıldığında:

1. **Kaynak Tip Tespiti**: `CreateCategoryCommand` (request parametresi)
2. **Hedef Tip Tespiti**: `Category` (generic parametre)
3. **Mapping Arama**: Configuration'da `CreateMap<CreateCategoryCommand, Category>()` aranır
4. **Mapping Bulundu mu?**: 
   - ✅ **Evet**: Mapping kuralları uygulanır
   - ❌ **Hayır**: Hata fırlatılır veya convention-based mapping denenir
5. **Nesne Oluşturma**: Yeni `Category` instance'ı oluşturulur
6. **Property Mapping**: Kaynak nesnedeki property'ler hedef nesneye kopyalanır
7. **Sonuç Döndürme**: Oluşturulan `Category` nesnesi döndürülür

## Görsel Akış Diyagramı

```
┌─────────────────────────────────────────────────────────────┐
│ 1. AddAutoMapper(typeof(Program).Assembly) çağrılır        │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. MappingProfile bulunur ve constructor çalışır            │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. CreateMap<CreateProductCommand, Product>()              │
│    → MapperConfiguration'a kaydedilir                       │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. MapperConfiguration → Mapper instance oluşturulur       │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. IMapper olarak DI container'a kaydedilir                 │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. Handler'da IMapper inject edilir                         │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 7. _mapper.Map<Product>(command) çağrılır                   │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 8. AutoMapper configuration'dan mapping'i bulur             │
│    ve dönüşümü yapar                                        │
└─────────────────────────────────────────────────────────────┘
```

## Örnekler

### Örnek 1: Command → Entity

```csharp
// MappingProfile.cs
CreateMap<CreateProductCommand, Product>();

// Handler'da
var product = _mapper.Map<Product>(command);
// product.Name = command.Name
// product.Price = command.Price
// product.CategoryId = command.CategoryId
// vb.
```

### Örnek 2: Entity → DTO (Custom Mapping)

```csharp
// MappingProfile.cs
CreateMap<Product, ProductDto>()
    .ForMember(dest => dest.CategoryName, 
               opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty));

// Handler'da
var productDto = _mapper.Map<ProductDto>(product);
// productDto.CategoryName = product.Category?.Name ?? string.Empty
```

### Örnek 3: Collection Mapping

```csharp
// MappingProfile.cs
CreateMap<Product, ProductDto>();

// Handler'da
var products = await _context.Products.Include(p => p.Category).ToListAsync();
var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);
// Tüm Product nesneleri ProductDto'ya dönüştürülür
```

## Özet

| Konsept | Açıklama |
|---------|----------|
| **AddAutoMapper** | Assembly'deki Profile sınıflarını bulur ve AutoMapper'ı yapılandırır |
| **MappingProfile** | `Profile` sınıfından türetilen, mapping kurallarını içeren sınıf |
| **CreateMap** | Kaynak ve hedef tip arasında mapping kuralı tanımlar |
| **MapperConfiguration** | Tüm mapping'lerin saklandığı konfigürasyon nesnesi |
| **IMapper** | Mapping işlemlerini gerçekleştiren interface |
| **Map<TDestination>** | Kaynak nesneyi hedef tipe dönüştüren metod |

## Avantajlar

1. **Manuel Mapping Gereksiz**: Her property'yi tek tek kopyalamaya gerek yok
2. **Tip Güvenliği**: Compile-time'da tip kontrolü yapılır
3. **Custom Mapping**: Karmaşık dönüşümler için `ForMember` kullanılabilir
4. **Performans**: Mapping'ler önceden derlenir (compiled expressions)
5. **Bakım Kolaylığı**: Mapping kuralları tek bir yerde toplanır

## Dikkat Edilmesi Gerekenler

1. **Null Check**: Navigation property'ler null olabilir, kontrol edilmeli
2. **Circular Reference**: Döngüsel referanslar sorun yaratabilir
3. **Performance**: Büyük collection'larda dikkatli kullanılmalı
4. **Configuration Validation**: Uygulama başlangıcında `config.AssertConfigurationIsValid()` ile doğrulanabilir
