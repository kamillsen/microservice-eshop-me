using MediatR;
using AutoMapper;
using Catalog.API.Data;
using Catalog.API.Entities;

namespace Catalog.API.Features.Categories.Commands.CreateCategory;

public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly CatalogDbContext _context;
    private readonly IMapper _mapper;

    public CreateCategoryHandler(CatalogDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        // 1. Command'den Entity oluştur
        var category = _mapper.Map<Category>(request);
        category.Id = Guid.NewGuid();

        // 2. Veritabanına ekle
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        // 3. ID döndür
        return category.Id;
    }
}

