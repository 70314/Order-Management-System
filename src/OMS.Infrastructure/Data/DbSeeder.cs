using Microsoft.EntityFrameworkCore;
using OMS.Domain.Entities;
using OMS.Domain.Enums;

namespace OMS.Infrastructure.Data;

public static class DbSeeder {
    public static void Seed(ModelBuilder modelBuilder) {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Users
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Email = "admin@oms.com", Name = "System Admin", PasswordHash = BCryptHash("Admin@123"), Role = UserRole.Admin, CreatedAt = now, UpdatedAt = now },
            new User { Id = 2, Email = "user@oms.com", Name = "Regular User", PasswordHash = BCryptHash("User@123"), Role = UserRole.User, CreatedAt = now, UpdatedAt = now }
        );

        // Customers
        modelBuilder.Entity<Customer>().HasData(
            new Customer { Id = 1, Name = "Acme Corporation", Email = "orders@acme.com", Address = "123 Business Ave, New York, NY 10001", Phone = "+1-555-0101", CreatedAt = now, UpdatedAt = now },
            new Customer { Id = 2, Name = "TechStart Inc.", Email = "purchasing@techstart.io", Address = "456 Innovation Blvd, San Francisco, CA 94105", Phone = "+1-555-0202", CreatedAt = now, UpdatedAt = now },
            new Customer { Id = 3, Name = "Global Traders Ltd.", Email = "buy@globaltraders.com", Address = "789 Commerce St, Chicago, IL 60601", Phone = "+1-555-0303", CreatedAt = now, UpdatedAt = now }
        );

        // Products
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Wireless Keyboard", SKU = "KB-W001", Description = "Ergonomic wireless keyboard with backlight", Price = 79.99m, StockQuantity = 150, CreatedAt = now, UpdatedAt = now },
            new Product { Id = 2, Name = "USB-C Monitor 27\"", SKU = "MN-U027", Description = "4K USB-C monitor, 27 inch IPS display", Price = 449.99m, StockQuantity = 45, CreatedAt = now, UpdatedAt = now },
            new Product { Id = 3, Name = "Mechanical Mouse", SKU = "MS-M001", Description = "Precision mechanical mouse, 6 buttons", Price = 59.99m, StockQuantity = 200, CreatedAt = now, UpdatedAt = now },
            new Product { Id = 4, Name = "Laptop Stand", SKU = "LS-A001", Description = "Adjustable aluminum laptop stand", Price = 39.99m, StockQuantity = 300, CreatedAt = now, UpdatedAt = now },
            new Product { Id = 5, Name = "Webcam HD 1080p", SKU = "WC-H001", Description = "Full HD webcam with built-in microphone", Price = 89.99m, StockQuantity = 80, CreatedAt = now, UpdatedAt = now }
        );

        // Orders
        modelBuilder.Entity<Order>().HasData(
            new Order { Id = 1, CustomerId = 1, UserId = 1, OrderDate = now, Status = OrderStatus.Fulfilled, TotalAmount = 609.97m, Notes = "Rush delivery requested", CreatedAt = now, UpdatedAt = now },
            new Order { Id = 2, CustomerId = 2, UserId = 2, OrderDate = now.AddDays(1), Status = OrderStatus.Processing, TotalAmount = 169.98m, CreatedAt = now, UpdatedAt = now },
            new Order { Id = 3, CustomerId = 3, UserId = 1, OrderDate = now.AddDays(2), Status = OrderStatus.Pending, TotalAmount = 539.97m, CreatedAt = now, UpdatedAt = now }
        );

        // OrderItems
        modelBuilder.Entity<OrderItem>().HasData(
            new OrderItem { Id = 1, OrderId = 1, ProductId = 1, Quantity = 2, UnitPrice = 79.99m, CreatedAt = now, UpdatedAt = now },
            new OrderItem { Id = 2, OrderId = 1, ProductId = 2, Quantity = 1, UnitPrice = 449.99m, CreatedAt = now, UpdatedAt = now },
            new OrderItem { Id = 3, OrderId = 2, ProductId = 3, Quantity = 1, UnitPrice = 59.99m, CreatedAt = now, UpdatedAt = now },
            new OrderItem { Id = 4, OrderId = 2, ProductId = 5, Quantity = 1, UnitPrice = 89.99m, CreatedAt = now, UpdatedAt = now },
            new OrderItem { Id = 5, OrderId = 3, ProductId = 2, Quantity = 1, UnitPrice = 449.99m, CreatedAt = now, UpdatedAt = now },
            new OrderItem { Id = 6, OrderId = 3, ProductId = 5, Quantity = 1, UnitPrice = 89.99m, CreatedAt = now, UpdatedAt = now }
        );
    }

    // Simple hash for seed data — in production use proper BCrypt from service
    private static string BCryptHash(string password) {
        // Using a pre-computed hash for seed data to avoid BCrypt dependency in Domain
        // These correspond to "Admin@123" and "User@123" respectively
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "OMS_SALT_2026"));
        return Convert.ToBase64String(bytes);
    }
}
