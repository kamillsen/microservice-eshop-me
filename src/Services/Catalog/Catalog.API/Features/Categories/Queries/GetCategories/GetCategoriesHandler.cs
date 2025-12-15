using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Catalog.API.Data;
using Catalog.API.Dtos;

namespace Catalog.API.Features.Categories.Queries.GetCategories;

public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, IEnumerable<CategoryDto>>
{
    private readonly CatalogDbContext _context;
    private readonly IMapper _mapper;

    public GetCategoriesHandler(CatalogDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        // Tüm kategorileri getir
        var categories = await _context.Categories.ToListAsync(cancellationToken);

        // Entity → DTO mapping
        return _mapper.Map<IEnumerable<CategoryDto>>(categories);
    }
}

