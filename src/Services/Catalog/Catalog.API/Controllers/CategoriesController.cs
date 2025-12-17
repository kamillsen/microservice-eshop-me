using MediatR;
using Microsoft.AspNetCore.Mvc;
using Catalog.API.Dtos;
using Catalog.API.Features.Categories.Queries.GetCategories;
using Catalog.API.Features.Categories.Queries.GetCategoryById;
using Catalog.API.Features.Categories.Commands.CreateCategory;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/categories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
    {
        var categories = await _mediator.Send(new GetCategoriesQuery());
        return Ok(categories);
    }

    // GET /api/categories/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategoryById(Guid id)
    {
        var category = await _mediator.Send(new GetCategoryByIdQuery { Id = id });
        return Ok(category);
    }

    // POST /api/categories
    [HttpPost]
    public async Task<ActionResult<Guid>> CreateCategory([FromBody] CreateCategoryCommand command)
    {
        var categoryId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetCategoryById), new { id = categoryId }, categoryId);
    }
}

