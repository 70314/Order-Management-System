using System.ComponentModel.DataAnnotations;

namespace OMS.Application.DTOs;

// === Pagination ===
public class PagedRequest {
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
    public string? Search { get; set; }
}

public class PagedResponse<T> {
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

// === Auth ===
public class LoginRequest {
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest {
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

public class GoogleLoginRequest {
    [Required]
    public string IdToken { get; set; } = string.Empty;
}

public class ZitadelLoginRequest {
    [Required]
    public string AccessToken { get; set; } = string.Empty;
}

public class AuthResponse {
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string AuthProvider { get; set; } = "InApp"; // TODO: Will make enums
    public DateTime Expiration { get; set; }
}

// === Customer ===
public class CustomerDto {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OrderCount { get; set; }
}

public class CreateCustomerRequest {
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Address { get; set; }
    [MaxLength(20)]
    public string? Phone { get; set; }
}

public class UpdateCustomerRequest {
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Address { get; set; }
    [MaxLength(20)]
    public string? Phone { get; set; }
}

// === Product ===
public class ProductDto {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProductRequest {
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required, MaxLength(50)]
    public string SKU { get; set; } = string.Empty;
    [MaxLength(1000)]
    public string? Description { get; set; }
    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Price must be positive")]
    public decimal Price { get; set; }
    [Required, Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }
}

public class UpdateProductRequest {
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required, MaxLength(50)]
    public string SKU { get; set; } = string.Empty;
    [MaxLength(1000)]
    public string? Description { get; set; }
    [Required, Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }
    [Required, Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }
}

// === Order ===
public class OrderDto {
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto {
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CreateOrderRequest {
    [Required]
    public int CustomerId { get; set; }
    [MaxLength(1000)]
    public string? Notes { get; set; }
    [Required, MinLength(1, ErrorMessage = "At least one item is required")]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest {
    [Required]
    public int ProductId { get; set; }
    [Required, Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
}

public class UpdateOrderRequest {
    [Required]
    public int CustomerId { get; set; }
    [MaxLength(1000)]
    public string? Notes { get; set; }
    [Required, MinLength(1)]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

// === Dashboard ===
public class DashboardDto {
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int FulfilledOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int TotalCustomers { get; set; }
    public List<OrderDto> RecentOrders { get; set; } = new();
}
