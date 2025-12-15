using MediatR;
using Catalog.API.Dtos;

namespace Catalog.API.Features.Products.Queries.GetProducts;

public class GetProductsQuery : IRequest<IEnumerable<ProductDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? CategoryId { get; set; }
}

