namespace OMS.Domain.Entities;

public class OrderItem : BaseEntity {
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;

    // Navigation
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
