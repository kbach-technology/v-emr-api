using System.Collections.Generic;
using EMR.Application.Requests.Roles;
using EMR.Application.Responses.Roles;

namespace EMR.Application.Interfaces.Services.Identity;

public interface IKeycloakAuthorizationService
{
    Task<Result<string>> CreateResourceAsync(string name, List<string> scopes, CancellationToken cancellationToken);

    Task<Result<string>> CreateRoleWithPermissionsAsync(string roleName,
        Dictionary<string, List<string>> modulePermissions, CancellationToken cancellationToken);

    Task<Result<ModulePermissionsResponse>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken);
    Task<Result<bool>> InitializeModuleResourcesAsync(CancellationToken cancellationToken);

    Task<Result<bool>> UpdatePermissionsAsync(string roleId, Dictionary<string, List<string>> modulePermissions,
        CancellationToken cancellationToken);

    Task<Result<List<KeycloakRoleResponse>>> GetAllRolesAsync(CancellationToken cancellationToken);
    Task<Result<bool>> DeleteRoleAsync(string roleId, CancellationToken cancellationToken);

    Task<Result<bool>> AssignRolesToUserAsync(string userId, AssignRoleUserRequest request,
        CancellationToken cancellationToken);

    Task<Result<bool>> UnAssignRolesToUserAsync(string userId, AssignRoleUserRequest request,
        CancellationToken cancellationToken);

    Task<Result<ModulePermissionsResponse>> GetPermissionsByRoleIdAsync(string roleId,
        CancellationToken cancellationToken);
}