using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OMS.Application.DTOs;
using OMS.Application.Interfaces;

namespace OMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase {
    private readonly IProductService _productService;

    public ProductsController(IProductService productService) {
        _productService = productService;
    }

    /// <summary>Get paginated list of products</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProductDto>>> GetProducts([FromQuery] PagedRequest request) {
        return Ok(await _productService.GetProductsAsync(request));
    }

    /// <summary>Get product by ID</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id) {
        var product = await _productService.GetProductByIdAsync(id);
        return product == null ? NotFound() : Ok(product);
    }

    /// <summary>Create a new product</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductRequest request) {
        var product = await _productService.CreateProductAsync(request);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    /// <summary>Update an existing product</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, [FromBody] UpdateProductRequest request) {
        var product = await _productService.UpdateProductAsync(id, request);
        return product == null ? NotFound() : Ok(product);
    }

    /// <summary>Delete a product</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(int id) {
        return await _productService.DeleteProductAsync(id) ? NoContent() : NotFound();
    }
}
