using OMS.Domain.Entities;
using OMS.Domain.Interfaces;
using OMS.Infrastructure.Data;
using OMS.Infrastructure.Repositories;

namespace OMS.Infrastructure;

public class UnitOfWork : IUnitOfWork {
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context) {
        _context = context;
        Users = new Repository<User>(context);
        Customers = new Repository<Customer>(context);
        Products = new Repository<Product>(context);
        Orders = new Repository<Order>(context);
        OrderItems = new Repository<OrderItem>(context);
        AuditLogs = new Repository<AuditLog>(context);
    }

    public IRepository<User> Users { get; }
    public IRepository<Customer> Customers { get; }
    public IRepository<Product> Products { get; }
    public IRepository<Order> Orders { get; }
    public IRepository<OrderItem> OrderItems { get; }
    public IRepository<AuditLog> AuditLogs { get; }

    public async Task<int> SaveChangesAsync() {
        return await _context.SaveChangesAsync();
    }

    public void Dispose() {
        _context.Dispose();
    }
}
