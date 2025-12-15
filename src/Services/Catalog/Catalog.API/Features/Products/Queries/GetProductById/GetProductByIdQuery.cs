using MediatR;
using Catalog.API.Dtos;

namespace Catalog.API.Features.Products.Queries.GetProductById;

public class GetProductByIdQuery : IRequest<ProductDto>
{
    public Guid Id { get; set; }
}

