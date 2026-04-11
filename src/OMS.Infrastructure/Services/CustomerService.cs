using Microsoft.Extensions.Logging;
using OMS.Application.DTOs;
using OMS.Application.Interfaces;
using OMS.Domain.Entities;
using OMS.Domain.Interfaces;

namespace OMS.Infrastructure.Services;

public class CustomerService : ICustomerService {
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(IUnitOfWork uow, ILogger<CustomerService> logger) {
        _uow = uow;
        _logger = logger;
    }

    public async Task<PagedResponse<CustomerDto>> GetCustomersAsync(PagedRequest request) {
        var (items, totalCount) = await _uow.Customers.GetPagedAsync(
            request.Page, request.PageSize,
            filter: string.IsNullOrEmpty(request.Search) ? null
                : c => c.Name.Contains(request.Search) || c.Email.Contains(request.Search),
            orderBy: q => request.SortBy?.ToLower() switch {
                "name" => request.SortDescending ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name),
                "email" => request.SortDescending ? q.OrderByDescending(c => c.Email) : q.OrderBy(c => c.Email),
                "createdat" => request.SortDescending ? q.OrderByDescending(c => c.CreatedAt) : q.OrderBy(c => c.CreatedAt),
                _ => q.OrderByDescending(c => c.CreatedAt)
            },
            includeProperties: "Orders");

        return new PagedResponse<CustomerDto> {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(int id) {
        var customers = await _uow.Customers.FindAsync(c => c.Id == id);
        var customer = customers.FirstOrDefault();
        return customer == null ? null : MapToDto(customer);
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request) {
        var existing = await _uow.Customers.FindAsync(c => c.Email == request.Email);
        if (existing.Any())
            throw new InvalidOperationException("A customer with this email already exists.");

        var customer = new Customer {
            Name = request.Name,
            Email = request.Email,
            Address = request.Address,
            Phone = request.Phone
        };

        await _uow.Customers.AddAsync(customer);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Customer created: {Name} ({Email})", customer.Name, customer.Email);
        return MapToDto(customer);
    }

    public async Task<CustomerDto?> UpdateCustomerAsync(int id, UpdateCustomerRequest request) {
        var customer = await _uow.Customers.GetByIdAsync(id);
        if (customer == null) return null;

        var duplicate = await _uow.Customers.FindAsync(c => c.Email == request.Email && c.Id != id);
        if (duplicate.Any())
            throw new InvalidOperationException("Another customer with this email already exists.");

        customer.Name = request.Name;
        customer.Email = request.Email;
        customer.Address = request.Address;
        customer.Phone = request.Phone;

        _uow.Customers.Update(customer);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Customer updated: {Id}", id);
        return MapToDto(customer);
    }

    public async Task<bool> DeleteCustomerAsync(int id) {
        var customer = await _uow.Customers.GetByIdAsync(id);
        if (customer == null) return false;

        _uow.Customers.Delete(customer);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Customer deleted: {Id}", id);
        return true;
    }

    private static CustomerDto MapToDto(Customer c) => new() {
        Id = c.Id,
        Name = c.Name,
        Email = c.Email,
        Address = c.Address,
        Phone = c.Phone,
        CreatedAt = c.CreatedAt,
        OrderCount = c.Orders?.Count ?? 0
    };
}
