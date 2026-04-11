using OMS.Application.DTOs;

namespace OMS.Application.Interfaces;

public interface IAuthService {
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request);
}

public interface ICustomerService {
    Task<PagedResponse<CustomerDto>> GetCustomersAsync(PagedRequest request);
    Task<CustomerDto?> GetCustomerByIdAsync(int id);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request);
    Task<CustomerDto?> UpdateCustomerAsync(int id, UpdateCustomerRequest request);
    Task<bool> DeleteCustomerAsync(int id);
}

public interface IProductService {
    Task<PagedResponse<ProductDto>> GetProductsAsync(PagedRequest request);
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<ProductDto> CreateProductAsync(CreateProductRequest request);
    Task<ProductDto?> UpdateProductAsync(int id, UpdateProductRequest request);
    Task<bool> DeleteProductAsync(int id);
}

public interface IOrderService {
    Task<PagedResponse<OrderDto>> GetOrdersAsync(PagedRequest request);
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, int? userId);
    Task<OrderDto?> UpdateOrderAsync(int id, UpdateOrderRequest request);
    Task<bool> DeleteOrderAsync(int id);
    Task<OrderDto?> FulfillOrderAsync(int id);
    Task<DashboardDto> GetDashboardAsync();
}
