using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OMS.Application.DTOs;
using OMS.Application.Interfaces;

namespace OMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase {
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService) {
        _customerService = customerService;
    }

    /// <summary>Get paginated list of customers</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CustomerDto>>> GetCustomers([FromQuery] PagedRequest request) {
        return Ok(await _customerService.GetCustomersAsync(request));
    }

    /// <summary>Get customer by ID</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetCustomer(int id) {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        return customer == null ? NotFound() : Ok(customer);
    }

    /// <summary>Create a new customer</summary>
    [HttpPost]
    public async Task<ActionResult<CustomerDto>> CreateCustomer([FromBody] CreateCustomerRequest request) {
        var customer = await _customerService.CreateCustomerAsync(request);
        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
    }

    /// <summary>Update an existing customer</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<CustomerDto>> UpdateCustomer(int id, [FromBody] UpdateCustomerRequest request) {
        var customer = await _customerService.UpdateCustomerAsync(id, request);
        return customer == null ? NotFound() : Ok(customer);
    }

    /// <summary>Delete a customer</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCustomer(int id) {
        return await _customerService.DeleteCustomerAsync(id) ? NoContent() : NotFound();
    }
}
