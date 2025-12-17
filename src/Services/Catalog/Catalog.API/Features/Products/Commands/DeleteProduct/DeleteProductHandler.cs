using MediatR;
using Catalog.API.Data;
using Catalog.API.Entities;
using BuildingBlocks.Exceptions.Exceptions;

namespace Catalog.API.Features.Products.Commands.DeleteProduct;

public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, Unit>
{
    private readonly CatalogDbContext _context;

    public DeleteProductHandler(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Ürünü bul
        var product = await _context.Products.FindAsync(request.Id, cancellationToken);
        
        if (product == null)
            throw new NotFoundException(nameof(Product), request.Id);

        // 2. Ürünü sil
        _context.Products.Remove(product);

        // 3. Değişiklikleri kaydet
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Unit döndür (hiçbir şey dönmez)
        return Unit.Value;
    }
}

