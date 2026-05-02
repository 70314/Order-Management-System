using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OMS.Application.DTOs;
using OMS.Application.Services;
using OMS.Domain.Entities;
using OMS.Domain.Enums;
using OMS.Domain.Interfaces;
using OMS.Infrastructure.Data;

namespace OMS.Tests;

public class OrderServiceTests {
    private AppDbContext CreateContext() {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private (OrderService service, AppDbContext context) CreateService() {
        var context = CreateContext();
        var uow = new OMS.Infrastructure.UnitOfWork(context);
        var logger = Mock.Of<ILogger<OrderService>>();
        return (new OrderService(uow, context, logger), context);
    }

    [Fact]
    public async Task CreateOrder_ValidOrder_ReturnsOrderDto() {
        var (service, context) = CreateService();

        // Seed
        context.Customers.Add(new Customer { Id = 1, Name = "Test Customer", Email = "test@test.com" });
        context.Products.Add(new Product { Id = 1, Name = "Widget", SKU = "W001", Price = 10.00m, StockQuantity = 50 });
        await context.SaveChangesAsync();

        var request = new CreateOrderRequest {
            CustomerId = 1,
            Notes = "Test order",
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = 1, Quantity = 3 }
            }
        };

        var result = await service.CreateOrderAsync(request, null);

        Assert.NotNull(result);
        Assert.Equal(30.00m, result.TotalAmount);
        Assert.Equal("Pending", result.Status);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task CreateOrder_InsufficientStock_ThrowsException() {
        var (service, context) = CreateService();

        context.Customers.Add(new Customer { Id = 1, Name = "Test", Email = "test@test.com" });
        context.Products.Add(new Product { Id = 1, Name = "Widget", SKU = "W001", Price = 10.00m, StockQuantity = 2 });
        await context.SaveChangesAsync();

        var request = new CreateOrderRequest {
            CustomerId = 1,
            Items = new List<CreateOrderItemRequest> { new() { ProductId = 1, Quantity = 5 } }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateOrderAsync(request, null));
    }

    [Fact]
    public async Task FulfillOrder_DeductsStock() {
        var (service, context) = CreateService();

        context.Customers.Add(new Customer { Id = 1, Name = "Test", Email = "test@test.com" });
        context.Products.Add(new Product { Id = 1, Name = "Widget", SKU = "W001", Price = 10.00m, StockQuantity = 50 });
        await context.SaveChangesAsync();

        // Create order first
        var order = await service.CreateOrderAsync(new CreateOrderRequest {
            CustomerId = 1,
            Items = new List<CreateOrderItemRequest> { new() { ProductId = 1, Quantity = 5 } }
        }, null);

        // Fulfill
        var fulfilled = await service.FulfillOrderAsync(order.Id);

        Assert.NotNull(fulfilled);
        Assert.Equal("Fulfilled", fulfilled!.Status);

        // Check stock was deducted
        var product = await context.Products.FindAsync(1);
        Assert.Equal(45, product!.StockQuantity);
    }

    [Fact]
    public async Task FulfillOrder_AlreadyFulfilled_ThrowsException() {
        var (service, context) = CreateService();

        context.Customers.Add(new Customer { Id = 1, Name = "Test", Email = "test@test.com" });
        context.Products.Add(new Product { Id = 1, Name = "Widget", SKU = "W001", Price = 10.00m, StockQuantity = 50 });
        await context.SaveChangesAsync();

        var order = await service.CreateOrderAsync(new CreateOrderRequest {
            CustomerId = 1,
            Items = new List<CreateOrderItemRequest> { new() { ProductId = 1, Quantity = 1 } }
        }, null);

        await service.FulfillOrderAsync(order.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.FulfillOrderAsync(order.Id));
    }

    [Fact]
    public async Task DeleteOrder_FulfilledOrder_ThrowsException() {
        var (service, context) = CreateService();

        context.Customers.Add(new Customer { Id = 1, Name = "Test", Email = "test@test.com" });
        context.Products.Add(new Product { Id = 1, Name = "Widget", SKU = "W001", Price = 10.00m, StockQuantity = 50 });
        await context.SaveChangesAsync();

        var order = await service.CreateOrderAsync(new CreateOrderRequest {
            CustomerId = 1,
            Items = new List<CreateOrderItemRequest> { new() { ProductId = 1, Quantity = 1 } }
        }, null);

        await service.FulfillOrderAsync(order.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteOrderAsync(order.Id));
    }

    [Fact]
    public async Task GetDashboard_ReturnsCorrectStats() {
        var (service, context) = CreateService();

        context.Customers.Add(new Customer { Id = 1, Name = "C1", Email = "c1@test.com" });
        context.Customers.Add(new Customer { Id = 2, Name = "C2", Email = "c2@test.com" });
        context.Products.Add(new Product { Id = 1, Name = "P1", SKU = "P001", Price = 10, StockQuantity = 5 });
        await context.SaveChangesAsync();

        var dashboard = await service.GetDashboardAsync();

        Assert.Equal(2, dashboard.TotalCustomers);
        Assert.Equal(1, dashboard.TotalProducts);
        Assert.Equal(1, dashboard.LowStockProducts); // StockQuantity < 10
    }
}

public class ProductServiceTests {
    private (ProductService service, AppDbContext context) CreateService() {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        var uow = new OMS.Infrastructure.UnitOfWork(context);
        var logger = Mock.Of<ILogger<ProductService>>();
        return (new ProductService(uow, logger), context);
    }

    [Fact]
    public async Task CreateProduct_ValidProduct_ReturnsDto() {
        var (service, _) = CreateService();

        var result = await service.CreateProductAsync(new CreateProductRequest {
            Name = "Test Product",
            SKU = "TP001",
            Price = 29.99m,
            StockQuantity = 100
        });

        Assert.Equal("Test Product", result.Name);
        Assert.Equal("TP001", result.SKU);
        Assert.Equal(29.99m, result.Price);
    }

    [Fact]
    public async Task CreateProduct_DuplicateSKU_ThrowsException() {
        var (service, _) = CreateService();

        await service.CreateProductAsync(new CreateProductRequest { Name = "P1", SKU = "DUPE", Price = 10, StockQuantity = 10 });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateProductAsync(new CreateProductRequest { Name = "P2", SKU = "DUPE", Price = 20, StockQuantity = 5 }));
    }
}

public class CustomerServiceTests {
    private (CustomerService service, AppDbContext context) CreateService() {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        var uow = new OMS.Infrastructure.UnitOfWork(context);
        var logger = Mock.Of<ILogger<CustomerService>>();
        return (new CustomerService(uow, logger), context);
    }

    [Fact]
    public async Task CreateCustomer_Valid_ReturnsDto() {
        var (service, _) = CreateService();

        var result = await service.CreateCustomerAsync(new CreateCustomerRequest { Name = "John Doe", Email = "john@example.com", Address = "123 Main St", Phone = "+1-555-0100" });

        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@example.com", result.Email);
    }

    [Fact]
    public async Task CreateCustomer_DuplicateEmail_ThrowsException() {
        var (service, _) = CreateService();

        await service.CreateCustomerAsync(new CreateCustomerRequest { Name = "A", Email = "same@test.com" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateCustomerAsync(new CreateCustomerRequest { Name = "B", Email = "same@test.com" }));
    }
}
