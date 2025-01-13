using EMR.Domain.Contracts;

namespace EMR.Domain.Entities.Users;

public class Users : AuditableEntity<string>
{
    public required string IdpUserId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string ProfileImage { get; set; }
    public string CoverImage { get; set; }
    public bool IsActive { get; set; }
}