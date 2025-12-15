using MediatR;
using Catalog.API.Dtos;

namespace Catalog.API.Features.Categories.Queries.GetCategoryById;

public class GetCategoryByIdQuery : IRequest<CategoryDto>
{
    public Guid Id { get; set; }
}

