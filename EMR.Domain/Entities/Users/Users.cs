using System.Collections.Generic;
using EMR.Domain.Contracts;
using EMR.Domain.Shared;

namespace EMR.Domain.Entities.Users;

public class User : AuditableEntity<string>
{
    public string KeycloakId { get; set; } = default!;
    
    public string UserNo { get; set; } = default!;
    public string FullName { get; set; }
    public string Email { get; set; } = default!;
    public string Phone { get; set; }
    
    public Gender Gender { get; set; }
    
    public DateOfBirth DateOfBirth { get; set; }
    
    public string? ProImg { get; set; }
    public bool IsActive { get; set; }
    
    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
}