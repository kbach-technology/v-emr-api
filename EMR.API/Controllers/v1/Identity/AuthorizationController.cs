using EMR.Application.Interfaces.Services.Identity;
using EMR.Application.Requests.Roles;
using EMR.Application.Responses.Roles;
using Microsoft.Extensions.Localization;

namespace EMR.API.Controllers.v1.Identity;

[ApiController]
[Route("api/authorization")]
//[Authorize]
public class AuthorizationController : ControllerBase
{
    private readonly IKeycloakAuthorizationService _authorizationService;
    private readonly ILogger<AuthorizationController> _logger;
    private readonly IStringLocalizer<AuthorizationController> _localizer;

    public AuthorizationController(
        IKeycloakAuthorizationService authorizationService,
        ILogger<AuthorizationController> logger,
        IStringLocalizer<AuthorizationController> localizer)
    {
        _authorizationService = authorizationService;
        _logger = logger;
        _localizer = localizer;
    }

    /// <summary>
    /// Initialize module resources in Keycloak
    /// </summary>
    [HttpPost("initialize")]
    //[Authorize(Policy = Permissions.Role.Manage)]
    public async Task<IActionResult> InitializeModuleResources(CancellationToken cancellationToken)
    {
        var result = await _authorizationService.InitializeModuleResourcesAsync(cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get available modules and their operations
    /// </summary>
    [HttpGet("modules")]
    //[Authorize(Policy = Permissions.Role.View)]
    public IActionResult GetAvailableModules()
    {
        var modulePermissions = new ModulePermissionsResponse
        {
            Modules = Modules.All.ToDictionary(
                module => module.ToLower(),
                module => new ModuleResponse
                {
                    Operations = Operations.All.Select(operation => new ModuleOperationResponse
                    {
                        Name = operation,
                        Key = operation,
                        IsAssigned = false
                    }).ToList()
                }
            )
        };

        return Ok(new
        {
            data = modulePermissions,
            message = "Permissions retrieved successfully",
            succeeded = true
        });
    }

    /// <summary>
    /// Create a new role with permissions
    /// </summary>
    [HttpPost("roles")]
    //[Authorize(Policy = Permissions.Role.Manage)]
    public async Task<IActionResult> CreateRoleWithPermissions(
        [FromBody] CreateRoleWithPermissionsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate request
            if (string.IsNullOrEmpty(request.RoleName))
                return BadRequest(_localizer["Role name is required"]);

            if (request.ModulePermissions == null || !request.ModulePermissions.Any())
                return BadRequest(_localizer["Module permissions are required"]);

            // Validate modules and operations
            foreach (var (module, operations) in request.ModulePermissions)
            {
                if (!Modules.All.Contains(module))
                    return BadRequest(_localizer["Invalid module: {0}", module]);

                if (operations.Any(op => !Operations.All.Contains(op)))
                    return BadRequest(_localizer["Invalid operation in module {0}", module]);
            }

            var result = await _authorizationService.CreateRoleWithPermissionsAsync(
                request.RoleName,
                request.ModulePermissions,
                cancellationToken);

            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role with permissions");
            return StatusCode(500, _localizer["An error occurred while processing your request"]);
        }
    }

    /// <summary>
    /// Update permissions for an existing role
    /// </summary>
    [HttpPut("roles/{roleId}/permissions")]
    //[Authorize(Policy = Permissions.Role.Manage)]
    public async Task<IActionResult> UpdatePermissions(
        string roleId,
        [FromBody] UpdatePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(roleId))
                return BadRequest(_localizer["Role ID is required"]);

            if (request.ModulePermissions == null || !request.ModulePermissions.Any())
                return BadRequest(_localizer["Module permissions are required"]);

            // Validate modules and operations
            foreach (var (module, operations) in request.ModulePermissions)
            {
                if (!Modules.All.Contains(module))
                    return BadRequest(_localizer["Invalid module: {0}", module]);

                if (operations.Any(op => !Operations.All.Contains(op)))
                    return BadRequest(_localizer["Invalid operation in module {0}", module]);
            }

            var result = await _authorizationService.UpdatePermissionsAsync(
                roleId,
                request.ModulePermissions,
                cancellationToken);

            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permissions for role {RoleId}", roleId);
            return StatusCode(500, _localizer["An error occurred while processing your request"]);
        }
    }

    /// <summary>
    /// Get permissions for a user
    /// </summary>
    [HttpGet("users/{userId}/permissions")]
    //[Authorize(Policy = Permissions.Role.View)]
    public async Task<IActionResult> GetUserPermissions(
        string userId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest(_localizer["User ID is required"]);

            var result = await _authorizationService.GetUserPermissionsAsync(userId, cancellationToken);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
            return StatusCode(500, _localizer["An error occurred while processing your request"]);
        }
    }
    
    
    [HttpGet("roles")]
    public async Task<IActionResult> GetAllRoles(CancellationToken cancellationToken)
    {
        var result = await _authorizationService.GetAllRolesAsync(cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    
    [HttpDelete("roles/{roleId}")]
    public async Task<IActionResult> DeleteRole(string roleId, CancellationToken cancellationToken)
    {
        var result = await _authorizationService.DeleteRoleAsync(roleId, cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    
    [HttpPost("users/{userId}/roles")]
    public async Task<IActionResult> AssignRolesToUser(string userId, AssignRoleUserRequest roles, CancellationToken cancellationToken)
    {
        var result = await _authorizationService.AssignRolesToUserAsync(userId, roles, cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    
    [HttpDelete("users/{userId}/roles")]
    public async Task<IActionResult> UnAssignRolesToUser(string userId, AssignRoleUserRequest roles, CancellationToken cancellationToken)
    {
        var result = await _authorizationService.UnAssignRolesToUserAsync(userId, roles, cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    
    [HttpGet("roles/{roleId}/permissions")]
    public async Task<IActionResult> GetPermissionsByRoleId(string roleId, CancellationToken cancellationToken)
    {
        var result = await _authorizationService.GetPermissionsByRoleIdAsync(roleId, cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}