using MediatR;
using Catalog.API.Dtos;

namespace Catalog.API.Features.Categories.Queries.GetCategoryById;

// IRequest<CategoryDto> : CategoryDto dönecek.=> IRequest<out TResponse>
public class GetCategoryByIdQuery : IRequest<CategoryDto>
{
    public Guid Id { get; set; }
}

