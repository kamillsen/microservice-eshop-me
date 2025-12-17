using MediatR;
using Microsoft.AspNetCore.Mvc;
using Catalog.API.Dtos;
using Catalog.API.Features.Products.Queries.GetProducts;
using Catalog.API.Features.Products.Queries.GetProductById;
using Catalog.API.Features.Products.Queries.GetProductsByCategory;
using Catalog.API.Features.Products.Commands.CreateProduct;
using Catalog.API.Features.Products.Commands.UpdateProduct;
using Catalog.API.Features.Products.Commands.DeleteProduct;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts([FromQuery] GetProductsQuery query)
    {
        var products = await _mediator.Send(query);
        return Ok(products);
    }

    // GET /api/products/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProductById(Guid id)
    {
        var product = await _mediator.Send(new GetProductByIdQuery { Id = id });
        return Ok(product);
    }

    // GET /api/products/category/{categoryId}
    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsByCategory(Guid categoryId)
    {
        var products = await _mediator.Send(new GetProductsByCategoryQuery { CategoryId = categoryId });
        return Ok(products);
    }

    // POST /api/products
    [HttpPost]
    public async Task<ActionResult<Guid>> CreateProduct([FromBody] CreateProductCommand command)
    {
        var productId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetProductById), new { id = productId }, productId);
    }

    // PUT /api/products/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    // DELETE /api/products/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        await _mediator.Send(new DeleteProductCommand { Id = id });
        return NoContent();
    }
}

