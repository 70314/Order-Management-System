using OMS.Domain.Entities;

namespace OMS.Domain.Interfaces;

public interface IUnitOfWork : IDisposable {
    IRepository<User> Users { get; }
    IRepository<Customer> Customers { get; }
    IRepository<Product> Products { get; }
    IRepository<Order> Orders { get; }
    IRepository<OrderItem> OrderItems { get; }
    IRepository<AuditLog> AuditLogs { get; }
    Task<int> SaveChangesAsync();
}
