using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OMS.Application.DTOs;
using OMS.Application.Interfaces;

namespace OMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase {
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService) {
        _orderService = orderService;
    }

    /// <summary>Get paginated list of orders</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<OrderDto>>> GetOrders([FromQuery] PagedRequest request) {
        return Ok(await _orderService.GetOrdersAsync(request));
    }

    /// <summary>Get order by ID</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id) {
        var order = await _orderService.GetOrderByIdAsync(id);
        return order == null ? NotFound() : Ok(order);
    }

    /// <summary>Create a new order</summary>
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderRequest request) {
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;
        var order = await _orderService.CreateOrderAsync(request, userId);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    /// <summary>Update an existing order</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<OrderDto>> UpdateOrder(int id, [FromBody] UpdateOrderRequest request) {
        var order = await _orderService.UpdateOrderAsync(id, request);
        return order == null ? NotFound() : Ok(order);
    }

    /// <summary>Delete an order</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteOrder(int id) {
        return await _orderService.DeleteOrderAsync(id) ? NoContent() : NotFound();
    }

    /// <summary>Fulfill an order (mark as shipped, deduct inventory)</summary>
    [HttpPost("{id}/fulfill")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderDto>> FulfillOrder(int id) {
        var order = await _orderService.FulfillOrderAsync(id);
        return order == null ? NotFound() : Ok(order);
    }

    /// <summary>Get dashboard statistics</summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> GetDashboard() {
        return Ok(await _orderService.GetDashboardAsync());
    }
}
