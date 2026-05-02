using OMS.Application.DTOs;

namespace OMS.Application.Interfaces {
    public interface IProductService {
        Task<PagedResponse<ProductDto>> GetProductsAsync(PagedRequest request);
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<ProductDto> CreateProductAsync(CreateProductRequest request);
        Task<ProductDto?> UpdateProductAsync(int id, UpdateProductRequest request);
        Task<bool> DeleteProductAsync(int id);
    }
}
