using AutoMapper;
using Catalog.API.Entities;
using Catalog.API.Dtos;
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
        
        // Entity → DTO
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty));
    }
}

