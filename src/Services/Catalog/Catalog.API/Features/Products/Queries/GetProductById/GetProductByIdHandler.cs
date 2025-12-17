using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Catalog.API.Data;
using Catalog.API.Entities;
using Catalog.API.Dtos;
using BuildingBlocks.Exceptions.Exceptions;

namespace Catalog.API.Features.Products.Queries.GetProductById;

public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly CatalogDbContext _context;
    private readonly IMapper _mapper;

    public GetProductByIdHandler(CatalogDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

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
    }
}

