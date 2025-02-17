using System.Collections.Generic;

namespace EMR.Application.Interfaces.Services.Identity;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(string userId, string permission);
    Task<List<string>> GetUserPermissionsAsync(string userId);
}