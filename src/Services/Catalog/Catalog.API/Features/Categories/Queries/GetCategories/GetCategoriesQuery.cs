using MediatR;
using Catalog.API.Dtos;

namespace Catalog.API.Features.Categories.Queries.GetCategories;

public class GetCategoriesQuery : IRequest<IEnumerable<CategoryDto>>
{
}

