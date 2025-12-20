using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Catalog.API.Data;
using Catalog.API.Dtos;

namespace Catalog.API.Features.Products.Queries.GetProducts;

public class GetProductsHandler : IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>
{
    private readonly CatalogDbContext _context;
    private readonly IMapper _mapper;

    public GetProductsHandler(CatalogDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // 1. Query oluştur (Category navigation property'yi Include et)
        var query = _context.Products.Include(p => p.Category).AsQueryable();

        // 2. CategoryId filtresi uygula (varsa)
        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        // 3. Sayfalama uygula
        var products = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // 4. Entity → DTO mapping  -  Product -> ProductDto
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }
}

