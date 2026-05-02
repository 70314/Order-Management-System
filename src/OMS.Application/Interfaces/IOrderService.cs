using OMS.Application.DTOs;

namespace OMS.Application.Interfaces {
    public interface IOrderService {
        Task<PagedResponse<OrderDto>> GetOrdersAsync(PagedRequest request);
        Task<OrderDto?> GetOrderByIdAsync(int id);
        Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, int? userId);
        Task<OrderDto?> UpdateOrderAsync(int id, UpdateOrderRequest request);
        Task<bool> DeleteOrderAsync(int id);
        Task<OrderDto?> FulfillOrderAsync(int id);
        Task<DashboardDto> GetDashboardAsync();
    }
}
