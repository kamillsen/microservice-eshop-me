using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ordering.API.Dtos;
using Ordering.API.Features.Orders.Commands.CreateOrder;
using Ordering.API.Features.Orders.Commands.DeleteOrder;
using Ordering.API.Features.Orders.Commands.UpdateOrder;
using Ordering.API.Features.Orders.Queries.GetOrderById;
using Ordering.API.Features.Orders.Queries.GetOrders;
using Ordering.API.Features.Orders.Queries.GetOrdersByUser;

namespace Ordering.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
    {
        var orders = await _mediator.Send(new GetOrdersQuery());
        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrderById(Guid id)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(id));
        if (order == null)
            return NotFound();
        return Ok(order);
    }

    [HttpGet("user/{userName}")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByUser(string userName)
    {
        var orders = await _mediator.Send(new GetOrdersByUserQuery(userName));
        return Ok(orders);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateOrder(CreateOrderCommand command)
    {
        var orderId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetOrderById), new { id = orderId }, orderId);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrder(Guid id, UpdateOrderCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID uyuşmuyor");

        var result = await _mediator.Send(command);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrder(Guid id)
    {
        var result = await _mediator.Send(new DeleteOrderCommand(id));
        if (!result)
            return NotFound();

        return NoContent();
    }
}

