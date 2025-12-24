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

    /// <summary>
    /// GET /api/baskets/{userName} - Kullanıcı adına göre sepet bilgilerini getirir.
    /// </summary>
    [HttpGet("{userName}")]
    [ProducesResponseType(typeof(ShoppingCartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShoppingCartDto>> GetBasket(string userName)
    {
        var basket = await _mediator.Send(new GetBasketQuery(userName));
        return Ok(basket);
    }

    /// <summary>
    /// POST /api/baskets - Yeni bir sepet kaydeder veya mevcut sepeti günceller.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ShoppingCartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShoppingCartDto>> StoreBasket([FromBody] ShoppingCartDto basket)
    {
        var result = await _mediator.Send(new StoreBasketCommand(basket));
        return Ok(result);
    }

    /// <summary>
    /// DELETE /api/baskets/{userName} - Kullanıcı adına göre sepeti siler.
    /// </summary>
    [HttpDelete("{userName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteBasket(string userName)
    {
        var deleted = await _mediator.Send(new DeleteBasketCommand(userName));
        if (!deleted)
            return NotFound();
        
        return NoContent();
    }

    /// <summary>
    /// POST /api/baskets/checkout - Sepeti ödeme işlemine gönderir ve sipariş oluşturur.
    /// </summary>
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

