using OMS.Domain.Enums;

namespace OMS.Domain.Entities;

public class User : BaseEntity {
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? GoogleId { get; set; }
    public string? ZitadelId { get; set; }
    public UserRole Role { get; set; } = UserRole.User;

    // Navigation
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
