using Microsoft.Extensions.Logging;
using OMS.Application.DTOs;
using OMS.Application.Interfaces;
using OMS.Domain.Entities;
using OMS.Domain.Interfaces;

namespace OMS.Application.Services;

public class ProductService : IProductService {
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IUnitOfWork uow, ILogger<ProductService> logger) {
        _uow = uow;
        _logger = logger;
    }

    public async Task<PagedResponse<ProductDto>> GetProductsAsync(PagedRequest request) {
        var (items, totalCount) = await _uow.Products.GetPagedAsync(
            request.Page, request.PageSize,
            filter: string.IsNullOrEmpty(request.Search) ? null
                : p => p.Name.Contains(request.Search) || p.SKU.Contains(request.Search),
            orderBy: q => request.SortBy?.ToLower() switch {
                "name" => request.SortDescending ? q.OrderByDescending(p => p.Name) : q.OrderBy(p => p.Name),
                "price" => request.SortDescending ? q.OrderByDescending(p => p.Price) : q.OrderBy(p => p.Price),
                "stock" => request.SortDescending ? q.OrderByDescending(p => p.StockQuantity) : q.OrderBy(p => p.StockQuantity),
                "sku" => request.SortDescending ? q.OrderByDescending(p => p.SKU) : q.OrderBy(p => p.SKU),
                _ => q.OrderByDescending(p => p.CreatedAt)
            });

        return new PagedResponse<ProductDto> {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id) {
        var product = await _uow.Products.GetByIdAsync(id);
        return product == null ? null : MapToDto(product);
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request) {
        var existing = await _uow.Products.FindAsync(p => p.SKU == request.SKU);
        if (existing.Any())
            throw new InvalidOperationException("A product with this SKU already exists.");

        var product = new Product {
            Name = request.Name,
            SKU = request.SKU,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity
        };

        await _uow.Products.AddAsync(product);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Product created: {Name} ({SKU})", product.Name, product.SKU);
        return MapToDto(product);
    }

    public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductRequest request) {
        var product = await _uow.Products.GetByIdAsync(id);
        if (product == null) return null;

        var duplicate = await _uow.Products.FindAsync(p => p.SKU == request.SKU && p.Id != id);
        if (duplicate.Any())
            throw new InvalidOperationException("Another product with this SKU already exists.");

        product.Name = request.Name;
        product.SKU = request.SKU;
        product.Description = request.Description;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;

        _uow.Products.Update(product);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Product updated: {Id}", id);
        return MapToDto(product);
    }

    public async Task<bool> DeleteProductAsync(int id) {
        var product = await _uow.Products.GetByIdAsync(id);
        if (product == null) return false;

        _uow.Products.Delete(product);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Product deleted: {Id}", id);
        return true;
    }

    private static ProductDto MapToDto(Product p) => new() {
        Id = p.Id,
        Name = p.Name,
        SKU = p.SKU,
        Description = p.Description,
        Price = p.Price,
        StockQuantity = p.StockQuantity,
        CreatedAt = p.CreatedAt
    };
}
