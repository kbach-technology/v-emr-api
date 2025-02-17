using System.Collections.Generic;

namespace EMR.Shared.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    bool EmailVerified { get; }
    List<string> Roles { get; }
    string CurrentIp { get; }
}