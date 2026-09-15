using System;

namespace MotorPortal.Domain.Entities;

public class AuditLog
{
    public long AuditId { get; set; }
    public long UserId { get; set; }
    public string EntityName { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string? RefId { get; set; }
    public DateTime Timestamp { get; set; }

    public UserMaster? User { get; set; }
}
