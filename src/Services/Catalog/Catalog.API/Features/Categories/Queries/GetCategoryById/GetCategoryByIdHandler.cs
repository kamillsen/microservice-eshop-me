using MediatR;
using AutoMapper;
using Catalog.API.Data;
using Catalog.API.Entities;
using Catalog.API.Dtos;
using BuildingBlocks.Exceptions.Exceptions;

namespace Catalog.API.Features.Categories.Queries.GetCategoryById;

public class GetCategoryByIdHandler : IRequestHandler<GetCategoryByIdQuery, CategoryDto>
{
    private readonly CatalogDbContext _context;
    private readonly IMapper _mapper;

    public GetCategoryByIdHandler(CatalogDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<CategoryDto> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        // Kategoriyi bul
        var category = await _context.Categories.FindAsync(request.Id, cancellationToken);

        // Kategori bulunamazsa NotFoundException fırlat
        if (category == null)
            throw new NotFoundException(nameof(Category), request.Id);

        // Entity → DTO mapping
        return _mapper.Map<CategoryDto>(category);
    }
}

