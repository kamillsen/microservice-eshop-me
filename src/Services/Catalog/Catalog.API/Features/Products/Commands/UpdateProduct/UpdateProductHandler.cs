using MediatR;
using AutoMapper;
using Catalog.API.Data;
using Catalog.API.Entities;
using BuildingBlocks.Exceptions.Exceptions;

namespace Catalog.API.Features.Products.Commands.UpdateProduct;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, Unit>
{
    private readonly CatalogDbContext _context;
    private readonly IMapper _mapper;

    public UpdateProductHandler(CatalogDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Unit> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Ürünü bul
        var product = await _context.Products.FindAsync(request.Id, cancellationToken);
        
        if (product == null)
            throw new NotFoundException(nameof(Product), request.Id);

        // 2. Command'den Entity'yi güncelle (AutoMapper ile)
        _mapper.Map(request, product);

        // 3. Değişiklikleri kaydet
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Unit döndür (hiçbir şey dönmez)
        return Unit.Value;
    }
}

