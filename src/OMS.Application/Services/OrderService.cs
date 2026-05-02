using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OMS.Application.DTOs;
using OMS.Application.Interfaces;
using OMS.Domain.Entities;
using OMS.Domain.Enums;
using OMS.Domain.Interfaces;
using OMS.Infrastructure.Data;

namespace OMS.Application.Services;

public class OrderService : IOrderService {
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _context;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IUnitOfWork uow, AppDbContext context, ILogger<OrderService> logger) {
        _uow = uow;
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResponse<OrderDto>> GetOrdersAsync(PagedRequest request) {
        var query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.Search)) {
            query = query.Where(o =>
                o.Customer.Name.Contains(request.Search) ||
                o.Notes != null && o.Notes.Contains(request.Search));
        }

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch {
            "date" => request.SortDescending ? query.OrderByDescending(o => o.OrderDate) : query.OrderBy(o => o.OrderDate),
            "total" => request.SortDescending ? query.OrderByDescending(o => o.TotalAmount) : query.OrderBy(o => o.TotalAmount),
            "status" => request.SortDescending ? query.OrderByDescending(o => o.Status) : query.OrderBy(o => o.Status),
            "customer" => request.SortDescending ? query.OrderByDescending(o => o.Customer.Name) : query.OrderBy(o => o.Customer.Name),
            _ => query.OrderByDescending(o => o.OrderDate)
        };

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResponse<OrderDto> {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id) {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        return order == null ? null : MapToDto(order);
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, int? userId) {
        // Validate customer
        var customer = await _uow.Customers.GetByIdAsync(request.CustomerId);
        if (customer == null)
            throw new InvalidOperationException("Customer not found.");

        // Validate stock and calculate total
        decimal totalAmount = 0;
        var orderItems = new List<OrderItem>();

        foreach (var item in request.Items) {
            var product = await _uow.Products.GetByIdAsync(item.ProductId);
            if (product == null)
                throw new InvalidOperationException($"Product with ID {item.ProductId} not found.");
            if (product.StockQuantity < item.Quantity)
                throw new InvalidOperationException($"Insufficient stock for '{product.Name}'. Available: {product.StockQuantity}, Requested: {item.Quantity}");

            orderItems.Add(new OrderItem {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });
            totalAmount += product.Price * item.Quantity;
        }

        var order = new Order {
            CustomerId = request.CustomerId,
            UserId = userId,
            Status = OrderStatus.Pending,
            TotalAmount = totalAmount,
            Notes = request.Notes,
            OrderItems = orderItems
        };

        await _uow.Orders.AddAsync(order);

        // Log audit
        await _uow.AuditLogs.AddAsync(new AuditLog {
            EntityName = "Order",
            EntityId = 0, // Will be set after save
            Action = "Created",
            Changes = JsonSerializer.Serialize(new { request.CustomerId, request.Notes, request.Items }),
            UserId = userId
        });

        await _uow.SaveChangesAsync();

        _logger.LogInformation("Order created: {Id} for customer {CustomerId}", order.Id, order.CustomerId);

        return await GetOrderByIdAsync(order.Id) ?? MapToDto(order);
    }

    public async Task<OrderDto?> UpdateOrderAsync(int id, UpdateOrderRequest request) {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return null;

        if (order.Status == OrderStatus.Fulfilled || order.Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Cannot modify a fulfilled or cancelled order.");

        // Validate customer
        var customer = await _uow.Customers.GetByIdAsync(request.CustomerId);
        if (customer == null)
            throw new InvalidOperationException("Customer not found.");

        // Remove old items
        _context.OrderItems.RemoveRange(order.OrderItems);

        // Add new items and validate stock
        decimal totalAmount = 0;
        var newItems = new List<OrderItem>();

        foreach (var item in request.Items) {
            var product = await _uow.Products.GetByIdAsync(item.ProductId);
            if (product == null)
                throw new InvalidOperationException($"Product with ID {item.ProductId} not found.");
            if (product.StockQuantity < item.Quantity)
                throw new InvalidOperationException($"Insufficient stock for '{product.Name}'.");

            newItems.Add(new OrderItem {
                OrderId = id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });
            totalAmount += product.Price * item.Quantity;
        }

        order.CustomerId = request.CustomerId;
        order.Notes = request.Notes;
        order.TotalAmount = totalAmount;
        order.OrderItems = newItems;

        await _uow.AuditLogs.AddAsync(new AuditLog {
            EntityName = "Order",
            EntityId = id,
            Action = "Updated",
            Changes = JsonSerializer.Serialize(request)
        });

        await _uow.SaveChangesAsync();

        _logger.LogInformation("Order updated: {Id}", id);
        return await GetOrderByIdAsync(id);
    }

    public async Task<bool> DeleteOrderAsync(int id) {
        var order = await _uow.Orders.GetByIdAsync(id);
        if (order == null) return false;

        if (order.Status == OrderStatus.Fulfilled)
            throw new InvalidOperationException("Cannot delete a fulfilled order.");

        _uow.Orders.Delete(order);

        await _uow.AuditLogs.AddAsync(new AuditLog {
            EntityName = "Order",
            EntityId = id,
            Action = "Deleted"
        });

        await _uow.SaveChangesAsync();

        _logger.LogInformation("Order deleted: {Id}", id);
        return true;
    }

    public async Task<OrderDto?> FulfillOrderAsync(int id) {
        var order = await _context.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return null;

        if (order.Status == OrderStatus.Fulfilled)
            throw new InvalidOperationException("Order is already fulfilled.");
        if (order.Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Cannot fulfill a cancelled order.");

        // Deduct stock
        foreach (var item in order.OrderItems) {
            if (item.Product.StockQuantity < item.Quantity)
                throw new InvalidOperationException($"Insufficient stock for '{item.Product.Name}' to fulfill order.");

            item.Product.StockQuantity -= item.Quantity;
        }

        order.Status = OrderStatus.Fulfilled;

        await _uow.AuditLogs.AddAsync(new AuditLog {
            EntityName = "Order",
            EntityId = id,
            Action = "Fulfilled",
            Changes = JsonSerializer.Serialize(new { PreviousStatus = "Processing", NewStatus = "Fulfilled" })
        });

        await _uow.SaveChangesAsync();

        _logger.LogInformation("Order fulfilled: {Id}, stock updated", id);
        return MapToDto(order);
    }

    public async Task<DashboardDto> GetDashboardAsync() {
        var orders = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .ToListAsync();

        var products = await _context.Products.ToListAsync();
        var customerCount = await _context.Customers.CountAsync();

        return new DashboardDto {
            TotalOrders = orders.Count,
            PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending),
            ProcessingOrders = orders.Count(o => o.Status == OrderStatus.Processing),
            FulfilledOrders = orders.Count(o => o.Status == OrderStatus.Fulfilled),
            CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled),
            TotalRevenue = orders.Where(o => o.Status == OrderStatus.Fulfilled).Sum(o => o.TotalAmount),
            TotalProducts = products.Count,
            LowStockProducts = products.Count(p => p.StockQuantity < 10),
            TotalCustomers = customerCount,
            RecentOrders = orders.OrderByDescending(o => o.OrderDate).Take(5).Select(MapToDto).ToList()
        };
    }

    private static OrderDto MapToDto(Order o) => new() {
        Id = o.Id,
        CustomerId = o.CustomerId,
        CustomerName = o.Customer?.Name ?? "Unknown",
        OrderDate = o.OrderDate,
        Status = o.Status.ToString(),
        TotalAmount = o.TotalAmount,
        Notes = o.Notes,
        CreatedAt = o.CreatedAt,
        Items = o.OrderItems?.Select(oi => new OrderItemDto {
            Id = oi.Id,
            ProductId = oi.ProductId,
            ProductName = oi.Product?.Name ?? "Unknown",
            SKU = oi.Product?.SKU ?? "",
            Quantity = oi.Quantity,
            UnitPrice = oi.UnitPrice,
            TotalPrice = oi.Quantity * oi.UnitPrice
        }).ToList() ?? new()
    };
}
