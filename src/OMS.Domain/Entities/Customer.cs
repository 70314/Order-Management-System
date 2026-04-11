namespace OMS.Domain.Entities;

public class Customer : BaseEntity {
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }

    // Navigation
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
