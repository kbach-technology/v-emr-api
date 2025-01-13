using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using EMR.Shared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace EMR.Shared.Services;

public class CurrentUserService : ICurrentUserService
{
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;

        UserId = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue("sub");
        UserName = user?.FindFirstValue("preferred_username");
        Email = user?.FindFirstValue(ClaimTypes.Email);
        EmailVerified = bool.TryParse(user?.FindFirstValue("email_verified"), out var verified) && verified;

        // Extract roles
        var realmAccess = user?.FindFirstValue("realm_access");
        var resourceAccess = user?.FindFirstValue("resource_access");

        var roles = new List<string>();

        if (!string.IsNullOrEmpty(realmAccess))
        {
            var realmRoles = JsonSerializer.Deserialize<JsonElement>(realmAccess);
            if (realmRoles.TryGetProperty("roles", out var realmRolesArray))
                roles.AddRange(realmRolesArray.EnumerateArray().Select(r => r.GetString()).Where(r => r != null)!);
        }

        if (!string.IsNullOrEmpty(resourceAccess))
        {
            var resourceRoles = JsonSerializer.Deserialize<JsonElement>(resourceAccess);
            if (resourceRoles.TryGetProperty("gojor-client", out var clientRoles) &&
                clientRoles.TryGetProperty("roles", out var clientRolesArray))
                roles.AddRange(clientRolesArray.EnumerateArray().Select(r => r.GetString()).Where(r => r != null)!);
        }

        Roles = roles;

        CurrentIp = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.MapToIPv4().ToString() ??
                    IPAddress.Loopback.ToString();
    }

    public List<string> Roles { get; }

    public string? UserId { get; }
    public string? UserName { get; }
    public string? Email { get; }
    public bool EmailVerified { get; }
    public bool IsMerchant { get; }
    public string CurrentIp { get; }
}