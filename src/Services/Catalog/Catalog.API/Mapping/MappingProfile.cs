using AutoMapper;
using Catalog.API.Entities;
using Catalog.API.Dtos;
using Catalog.API.Features.Products.Commands.CreateProduct;
using Catalog.API.Features.Products.Commands.UpdateProduct;
using Catalog.API.Features.Categories.Commands.CreateCategory;

namespace Catalog.API.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {

        //CreateMap<TSource, TDestination>() methodu ile mapping kuralları oluşturulur.
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

