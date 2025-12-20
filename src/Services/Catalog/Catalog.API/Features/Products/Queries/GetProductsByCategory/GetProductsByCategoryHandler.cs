using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Catalog.API.Data;
using Catalog.API.Dtos;

namespace Catalog.API.Features.Products.Queries.GetProductsByCategory;

public class GetProductsByCategoryHandler : IRequestHandler<GetProductsByCategoryQuery, IEnumerable<ProductDto>>
{
    private readonly CatalogDbContext _context;
    private readonly IMapper _mapper;

    public GetProductsByCategoryHandler(CatalogDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductDto>> Handle(GetProductsByCategoryQuery request, CancellationToken cancellationToken)
    {
        // 1. Kategoriye ait ürünleri sorgula (Category navigation property'yi Include et)
        var products = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.CategoryId == request.CategoryId)
            .ToListAsync(cancellationToken);

        // 2. Entity → DTO mapping
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }
}

