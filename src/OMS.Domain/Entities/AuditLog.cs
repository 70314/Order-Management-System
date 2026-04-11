namespace OMS.Domain.Entities;

public class AuditLog : BaseEntity {
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Changes { get; set; }
    public int? UserId { get; set; }

    // Navigation
    public User? User { get; set; }
}
