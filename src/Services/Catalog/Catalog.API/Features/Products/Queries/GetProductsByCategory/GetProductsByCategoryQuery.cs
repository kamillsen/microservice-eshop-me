using MediatR;
using Catalog.API.Dtos;

namespace Catalog.API.Features.Products.Queries.GetProductsByCategory;

public class GetProductsByCategoryQuery : IRequest<IEnumerable<ProductDto>>
{
    public Guid CategoryId { get; set; }
}

