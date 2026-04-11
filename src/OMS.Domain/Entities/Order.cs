using OMS.Domain.Enums;

namespace OMS.Domain.Entities;

public class Order : BaseEntity {
    public int CustomerId { get; set; }
    public int? UserId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Customer Customer { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
