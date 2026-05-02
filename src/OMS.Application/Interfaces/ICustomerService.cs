using OMS.Application.DTOs;

namespace OMS.Application.Interfaces {
    public interface ICustomerService {
        Task<PagedResponse<CustomerDto>> GetCustomersAsync(PagedRequest request);
        Task<CustomerDto?> GetCustomerByIdAsync(int id);
        Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request);
        Task<CustomerDto?> UpdateCustomerAsync(int id, UpdateCustomerRequest request);
        Task<bool> DeleteCustomerAsync(int id);
    }
}
