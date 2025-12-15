using AutoMapper;
using Catalog.API.Entities;
using Catalog.API.Features.Products.Commands.CreateProduct;
using Catalog.API.Features.Products.Commands.UpdateProduct;

namespace Catalog.API.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Command → Entity
        CreateMap<CreateProductCommand, Product>();
        CreateMap<UpdateProductCommand, Product>();
        
        // Entity → DTO (DTO'lar henüz oluşturulmadı, Faz 3.4'te eklenecek)
        // CreateMap<Product, ProductDto>();
    }
}

