using System;
using EMR.Domain.Contracts;

namespace EMR.Domain.Entities.Users;
public class UserSession : AuditableEntity<string>
{
    public string UserId { get; set; } = default!;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = default!;
}