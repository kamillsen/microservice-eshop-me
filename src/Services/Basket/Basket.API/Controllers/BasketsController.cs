using Basket.API.Dtos;
using Basket.API.Features.Basket.Commands.CheckoutBasket;
using Basket.API.Features.Basket.Commands.DeleteBasket;
using Basket.API.Features.Basket.Commands.StoreBasket;
using Basket.API.Features.Basket.Queries.GetBasket;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Basket.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BasketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BasketsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{userName}")]
    [ProducesResponseType(typeof(ShoppingCartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShoppingCartDto>> GetBasket(string userName)
    {
        var basket = await _mediator.Send(new GetBasketQuery(userName));
        return Ok(basket);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ShoppingCartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShoppingCartDto>> StoreBasket([FromBody] ShoppingCartDto basket)
    {
        var result = await _mediator.Send(new StoreBasketCommand(basket));
        return Ok(result);
    }

    [HttpDelete("{userName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteBasket(string userName)
    {
        var deleted = await _mediator.Send(new DeleteBasketCommand(userName));
        if (!deleted)
            return NotFound();
        
        return NoContent();
    }

    [HttpPost("checkout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> CheckoutBasket([FromBody] CheckoutBasketCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result)
            return BadRequest("Basket not found");
        
        return Ok(result);
    }
}

