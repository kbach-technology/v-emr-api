using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using EMR.Application.Interfaces.Services.Identity;
using EMR.Application.Requests.Roles;
using EMR.Application.Responses.Roles;
using EMR.Shared.Configurations;
using EMR.Shared.Constants.Permission;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EMR.Application.Services.Identity;

public class KeycloakAuthorizationService : IKeycloakAuthorizationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakConfiguration _keycloakConfig;
    private readonly ILogger<KeycloakAuthorizationService> _logger;
    private readonly IStringLocalizer<KeycloakAuthorizationService> _localizer;
    private readonly IKeycloakService _keycloakService;

    public KeycloakAuthorizationService(
        IHttpClientFactory httpClientFactory,
        KeycloakConfiguration keycloakConfig,
        ILogger<KeycloakAuthorizationService> logger,
        IStringLocalizer<KeycloakAuthorizationService> localizer,
        IKeycloakService keycloakService)
    {
        _httpClientFactory = httpClientFactory;
        _keycloakConfig = keycloakConfig;
        _logger = logger;
        _localizer = localizer;
        _keycloakService = keycloakService;
    }

    public async Task<Result<string>> CreateResourceAsync(string name, List<string> scopes,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await _keycloakService.GetKeycloakAdminCliTokenAsync(cancellationToken);
            if (!token.Succeeded)
                return await Result<string>.FailAsync(_localizer["Failed to obtain Keycloak token"]);

            var clientId =
                await _keycloakService.GetClientIdAsync(_keycloakConfig.Client.ClientId, token.Data, cancellationToken);
            if (!clientId.Succeeded)
                return await Result<string>.FailAsync(_localizer["Failed to get client ID"]);

            // First check if resource exists
            var client = _httpClientFactory.CreateClient();
            var checkRequest = new HttpRequestMessage(HttpMethod.Get,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId.Data}/authz/resource-server/resource?name={name}");

            checkRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
            var checkResponse = await client.SendAsync(checkRequest, cancellationToken);

            if (checkResponse.IsSuccessStatusCode)
            {
                var content = await checkResponse.Content.ReadAsStringAsync(cancellationToken);
                var existingResources = JsonConvert.DeserializeObject<List<KeycloakResource>>(content);

                if (existingResources?.Any() == true)
                {
                    // Resource already exists, return its ID
                    return await Result<string>.SuccessAsync(existingResources.First().Id,
                        _localizer["Resource already exists"]);
                }
            }

            // Resource doesn't exist, create it
            var createRequest = new HttpRequestMessage(HttpMethod.Post,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId.Data}/authz/resource-server/resource");

            var resource = new
            {
                name = name,
                displayName = name,
                scopes = scopes.Select(s => new { name = s }).ToList(),
                attributes = new { }
            };

            createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
            createRequest.Content =
                new StringContent(JsonConvert.SerializeObject(resource), Encoding.UTF8, "application/json");

            var createResponse = await client.SendAsync(createRequest, cancellationToken);

            if (createResponse.IsSuccessStatusCode)
            {
                var locationHeader = createResponse.Headers.Location?.ToString();
                var resourceId = locationHeader?.Substring(locationHeader.LastIndexOf('/') + 1);
                return await Result<string>.SuccessAsync(resourceId, _localizer["Resource created successfully"]);
            }

            var errorContent = await createResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to create resource. Status: {StatusCode}, Content: {Content}",
                createResponse.StatusCode, errorContent);

            return await Result<string>.FailAsync(_localizer["Failed to create resource"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating resource {ResourceName}", name);
            return await Result<string>.FailAsync(_localizer["An error occurred while creating the resource"]);
        }
    }

    public async Task<Result<bool>> InitializeModuleResourcesAsync(CancellationToken cancellationToken)
    {
        try
        {
            foreach (var module in Modules.All)
            {
                var createResourceResult = await CreateResourceAsync(
                    module,
                    Operations.All.ToList(),
                    cancellationToken);

                if (!createResourceResult.Succeeded)
                {
                    _logger.LogError("Failed to initialize resource for module {Module}", module);
                    return await Result<bool>.FailAsync(_localizer["Failed to initialize module resources"]);
                }
            }

            return await Result<bool>.SuccessAsync(true, _localizer["Module resources initialized successfully"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing module resources");
            return await Result<bool>.FailAsync(_localizer["An error occurred while initializing module resources"]);
        }
    }

    public async Task<Result<string>> CreateRoleWithPermissionsAsync(
        string roleName,
        Dictionary<string, List<string>> modulePermissions,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await _keycloakService.GetKeycloakAdminCliTokenAsync(cancellationToken);
            if (!token.Succeeded)
                return await Result<string>.FailAsync(_localizer["Failed to obtain Keycloak token"]);

            // 1. Get client id
            var clientId =
                await _keycloakService.GetClientIdAsync(_keycloakConfig.Client.ClientId, token.Data, cancellationToken);
            if (!clientId.Succeeded)
                return await Result<string>.FailAsync(_localizer["Failed to get client ID"]);

            var roleResult = await CreateClientRoleAsync(roleName, clientId.Data, token.Data, cancellationToken);
            if (!roleResult.Succeeded)
                return await Result<string>.FailAsync(_localizer[roleResult.Message]);

            // 2. Create policy for the role
            var policyResult = await CreatePolicyAsync($"{roleName}_Policy", roleResult.Data, cancellationToken);
            if (!policyResult.Succeeded)
                return await Result<string>.FailAsync(_localizer["Failed to create policy"]);

            // 3. Create permissions for each module
            foreach (var (module, operations) in modulePermissions)
            {
                var resourceId = await GetResourceIdByNameAsync(module, token.Data, cancellationToken);
                if (!resourceId.Succeeded)
                    continue;

                var permissionResult = await CreatePermissionAsync(
                    $"{roleName}_{module}_Permission",
                    resourceId.Data,
                    operations,
                    policyResult.Data,
                    cancellationToken);

                if (!permissionResult.Succeeded)
                {
                    _logger.LogWarning("Failed to create permission for module {Module}", module);
                }
            }

            return await Result<string>.SuccessAsync(roleResult.Data,
                _localizer["Role and permissions created successfully"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role with permissions {RoleName}", roleName);
            return await Result<string>.FailAsync(_localizer["An error occurred while creating role and permissions"]);
        }
    }

    public async Task<Result<ModulePermissionsResponse>> GetUserPermissionsAsync(string userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await _keycloakService.GetKeycloakAdminCliTokenAsync(cancellationToken);
            if (!token.Succeeded)
                return await Result<ModulePermissionsResponse>.FailAsync(_localizer["Failed to obtain Keycloak token"]);

            var clientId =
                await _keycloakService.GetClientIdAsync(_keycloakConfig.Client.ClientId, token.Data, cancellationToken);
            if (!clientId.Succeeded)
                return await Result<ModulePermissionsResponse>.FailAsync(_localizer["Failed to get client ID"]);

            var client = _httpClientFactory.CreateClient();

            // Get all permissions first
            var permissionsRequest = new HttpRequestMessage(HttpMethod.Get,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId.Data}/authz/resource-server/permission/scope");

            permissionsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
            var permissionsResponse = await client.SendAsync(permissionsRequest, cancellationToken);
            var permissionsContent = await permissionsResponse.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("All permissions response: {Content}", permissionsContent);

            var allPermissions = JsonConvert.DeserializeObject<List<KeycloakPermissionResponse>>(permissionsContent)
                                 ?? new List<KeycloakPermissionResponse>();

            // Get resources
            var resourcesRequest = new HttpRequestMessage(HttpMethod.Get,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId.Data}/authz/resource-server/resource");

            resourcesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
            var resourcesResponse = await client.SendAsync(resourcesRequest, cancellationToken);
            var resourcesContent = await resourcesResponse.Content.ReadAsStringAsync(cancellationToken);

            var resources = JsonConvert.DeserializeObject<List<KeycloakResource>>(resourcesContent)
                            ?? new List<KeycloakResource>();
            var modulePermissions = new Dictionary<string, ModuleResponse>();

            // Get user's assigned roles to check against permissions
            var rolesRequest = new HttpRequestMessage(HttpMethod.Get,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/users/{userId}/role-mappings/clients/{clientId.Data}");

            rolesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
            var rolesResponse = await client.SendAsync(rolesRequest, cancellationToken);
            var rolesContent = await rolesResponse.Content.ReadAsStringAsync(cancellationToken);
            var userRoles = JsonConvert.DeserializeObject<List<KeycloakRole>>(rolesContent);
            var userRoleNames = userRoles?.Select(r => r.Name).ToList() ?? new List<string>();

            foreach (var resource in resources.Where(r =>
                         r.Name != "default Resource" &&
                         Modules.All.Contains(r.Name, StringComparer.OrdinalIgnoreCase)))
            {
                _logger.LogInformation("Processing resource {ResourceName} with ID {ResourceId}",
                    resource.Name, resource.Id);

                // Find permission for this resource
                var permission = allPermissions.FirstOrDefault(p => p.Name == $"{resource.Name}_Permission");
                if (permission != null)
                {
                    // Get permission resources
                    var permissionResourcesRequest = new HttpRequestMessage(HttpMethod.Get,
                        $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId.Data}/authz/resource-server/permission/scope/{permission.Id}/resources");

                    permissionResourcesRequest.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", token.Data);
                    var permissionResourcesResponse =
                        await client.SendAsync(permissionResourcesRequest, cancellationToken);
                    var permissionResourcesContent =
                        await permissionResourcesResponse.Content.ReadAsStringAsync(cancellationToken);

                    // Get permission scopes
                    var permissionScopesRequest = new HttpRequestMessage(HttpMethod.Get,
                        $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId.Data}/authz/resource-server/permission/scope/{permission.Id}/scopes");

                    permissionScopesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
                    var permissionScopesResponse = await client.SendAsync(permissionScopesRequest, cancellationToken);
                    var permissionScopesContent =
                        await permissionScopesResponse.Content.ReadAsStringAsync(cancellationToken);

                    // Deserialize permission resources and scopes
                    var permissionResources =
                        JsonConvert.DeserializeObject<List<PermissionResource>>(permissionResourcesContent) ??
                        new List<PermissionResource>();
                    var permissionScopes =
                        JsonConvert.DeserializeObject<List<PermissionScope>>(permissionScopesContent) ??
                        new List<PermissionScope>();

                    // Generate operations for the current resource
                    var operations = Operations.All.Select(operation =>
                    {
                        // Check if the resource ID exists in permissionResources
                        bool isResourceAssigned = permissionResources.Any(pr => pr.Id == resource.Id);

                        // Check if the operation name exists in permissionScopes
                        bool isOperationAssigned = permissionScopes.Any(ps => ps.Name == operation);

                        // Determine if the operation is assigned based on resource and scope only
                        bool isAssigned = isResourceAssigned && isOperationAssigned;

                        _logger.LogInformation("Operation {Operation} for resource {Resource} isAssigned: {IsAssigned}",
                            operation, resource.Name, isAssigned);

                        return new ModuleOperationResponse
                        {
                            Name = operation,
                            Key = operation,
                            IsAssigned = isAssigned
                        };
                    }).ToList(); // Removed the Where clause to include all operations

                    modulePermissions[resource.Name.ToLower()] = new ModuleResponse
                    {
                        Operations = operations.Where(op => op.IsAssigned).ToList()
                    };
                }
                else
                {
                    _logger.LogWarning("No permission found for resource {ResourceName}", resource.Name);
                    modulePermissions[resource.Name.ToLower()] = new ModuleResponse
                    {
                        Operations = new List<ModuleOperationResponse>()
                    };
                }
            }

            var result = new ModulePermissionsResponse
            {
                Modules = modulePermissions
            };

            return await Result<ModulePermissionsResponse>.SuccessAsync(result,
                _localizer["Permissions retrieved successfully"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
            return await Result<ModulePermissionsResponse>.FailAsync(
                _localizer["An error occurred while getting permissions"]);
        }
    }

    public async Task<Result<bool>> UpdatePermissionsAsync(
        string roleId,
        Dictionary<string, List<string>> modulePermissions,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await _keycloakService.GetKeycloakAdminCliTokenAsync(cancellationToken);
            if (!token.Succeeded)
                return await Result<bool>.FailAsync(_localizer["Failed to obtain Keycloak token"]);

            // Get client ID
            var clientId =
                await _keycloakService.GetClientIdAsync(_keycloakConfig.Client.ClientId, token.Data, cancellationToken);
            if (!clientId.Succeeded)
                return await Result<bool>.FailAsync(_localizer["Failed to get client ID"]);

            // Remove existing permissions
            await RemoveExistingPermissionsAsync(roleId, token.Data, cancellationToken);

            // Get policy ID by role Id
            var policyId = await GetPolicyIdByRoleIdAsync(roleId, token.Data, cancellationToken);

            // Create new permissions
            foreach (var (module, operations) in modulePermissions)
            {
                var resourceId = await GetResourceIdByNameAsync(module, token.Data, cancellationToken);
                if (!resourceId.Succeeded)
                {
                    _logger.LogWarning("Resource not found for module {Module}", module);
                    continue;
                }

                var permissionResult = await CreatePermissionAsync(
                    $"{module}_Permission",
                    resourceId.Data,
                    operations,
                    policyId.Data,
                    cancellationToken);

                if (!permissionResult.Succeeded)
                {
                    _logger.LogWarning("Failed to update permission for module {Module}: {Error}", module,
                        permissionResult.Message);
                }
            }

            return await Result<bool>.SuccessAsync(true, _localizer["Permissions updated successfully"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permissions for role {RoleId}", roleId);
            return await Result<bool>.FailAsync(_localizer["An error occurred while updating permissions"]);
        }
    }

    public async Task<Result<string>> CreatePolicyAsync(string name, string roleId, CancellationToken cancellationToken)
    {
        try
        {
            var token = await _keycloakService.GetKeycloakAdminCliTokenAsync(cancellationToken);
            if (!token.Succeeded)
                return await Result<string>.FailAsync(_localizer["Failed to obtain Keycloak token"]);

            var clientId =
                await _keycloakService.GetClientIdAsync(_keycloakConfig.Client.ClientId, token.Data, cancellationToken);
            if (!clientId.Succeeded)
                return await Result<string>.FailAsync(_localizer["Failed to get client ID"]);

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId.Data}/authz/resource-server/policy/role");

            var policy = new
            {
                name = name,
                description = $"Policy for {name}",
                type = "role",
                logic = "POSITIVE",
                decisionStrategy = "UNANIMOUS",
                roles = new[]
                {
                    new { id = roleId }
                }
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
            request.Content = new StringContent(JsonConvert.SerializeObject(policy), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                string policyId;
                var locationHeader = response.Headers.Location?.ToString();

                if (locationHeader != null)
                {
                    policyId = locationHeader?.Substring(locationHeader.LastIndexOf('/') + 1)
                               ?? throw new InvalidOperationException("Location header is missing from response");
                }
                else
                {
                    // Try to get policy ID from response content
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    try
                    {
                        var policyResponse = JsonConvert.DeserializeObject<dynamic>(content);
                        policyId = policyResponse?.id?.ToString();

                        if (string.IsNullOrEmpty(policyId))
                        {
                            _logger.LogWarning("Created policy but could not extract ID from response content");
                            return await Result<string>.FailAsync(_localizer["Policy created but ID not found"]);
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to parse policy response content");
                        return await Result<string>.FailAsync(_localizer["Policy created but failed to get ID"]);
                    }
                }

                return await Result<string>.SuccessAsync(policyId, _localizer["Policy created successfully"]);
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to create policy. Status: {StatusCode}, Content: {Content}",
                response.StatusCode, errorContent);

            return await Result<string>.FailAsync(_localizer["Failed to create policy"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating policy {PolicyName}", name);
            return await Result<string>.FailAsync(_localizer["An error occurred while creating the policy"]);
        }
    }

    public async Task<Result<string>> CreatePermissionAsync(
        string name,
        string resourceId,
        List<string> scopes,
        string policyId,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await _keycloakService.GetKeycloakAdminCliTokenAsync(cancellationToken);
            if (!token.Succeeded)
                return await Result<string>.FailAsync(_localizer["Failed to obtain Keycloak token"]);

            var clientId =
                await _keycloakService.GetClientIdAsync(_keycloakConfig.Client.ClientId, token.Data, cancellationToken);
            if (!clientId.Succeeded)
                return await Result<string>.FailAsync(_localizer["Failed to get client ID"]);

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId.Data}/authz/resource-server/permission/scope");

            var permission = new
            {
                name = name,
                description = $"Scope-based permission for {name}",
                type = "scope",
                decisionStrategy = "AFFIRMATIVE",
                logic = "POSITIVE",
                resources = new[] { resourceId },
                resourceType = string.Empty, // Since we're not using "Apply to resource type"
                scopes = scopes,
                policies = new[] { policyId }
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
            request.Content =
                new StringContent(JsonConvert.SerializeObject(permission), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
                return await Result<string>.SuccessAsync(permission.name,
                    _localizer["Permission created successfully"]);

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to create permission. Status: {StatusCode}, Content: {Content}",
                response.StatusCode, errorContent);

            return await Result<string>.FailAsync(_localizer["Failed to create permission"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission {PermissionName}", name);
            return await Result<string>.FailAsync(_localizer["An error occurred while creating the permission"]);
        }
    }

    public async Task<Result<List<KeycloakRoleResponse>>> GetAllRolesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var token = await _keycloakService.GetKeycloakAdminCliTokenAsync(cancellationToken);
            if (!token.Succeeded)
                return await Result<List<KeycloakRoleResponse>>.FailAsync(
                    _localizer["Failed to obtain Keycloak token"]);

            var clientId =
                await _keycloakService.GetClientIdAsync(_keycloakConfig.Client.ClientId, token.Data, cancellationToken);
            if (!clientId.Succeeded)
                return await Result<List<KeycloakRoleResponse>>.FailAsync(_localizer["Failed to get client ID"]);

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId.Data}/roles");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);

            var response = await client.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get roles. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, content);
                return await Result<List<KeycloakRoleResponse>>.FailAsync(_localizer["Failed to get roles"]);
            }

            var roles = JsonConvert.DeserializeObject<List<KeycloakRoleResponse>>(content);

            // Filter out default Keycloak roles
            var customRoles = roles.Where(r => !IsDefaultKeycloakRole(r.Name)).ToList();

            return await Result<List<KeycloakRoleResponse>>.SuccessAsync(customRoles,
                _localizer["Roles retrieved successfully"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles");
            return await Result<List<KeycloakRoleResponse>>.FailAsync(
                _localizer["An error occurred while getting roles"]);
        }
    }

    public async Task<Result<bool>> DeleteRoleAsync(string roleId, CancellationToken cancellationToken)
    {
        try
        {
            var token = await _keycloakService.GetKeycloakAdminCliTokenAsync(cancellationToken);
            if (!token.Succeeded)
                return await Result<bool>.FailAsync(_localizer["Failed to obtain Keycloak token"]);

            var clientId =
                await _keycloakService.GetClientIdAsync(_keycloakConfig.Client.ClientId, token.Data, cancellationToken);
            if (!clientId.Succeeded)
                return await Result<bool>.FailAsync(_localizer["Failed to get client ID"]);

            // First remove all permissions associated with this role
            await RemoveExistingPermissionsAsync(roleId, token.Data, cancellationToken);

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Delete,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId.Data}/roles/{roleId}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);

            var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to delete role. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, content);
                return await Result<bool>.FailAsync(_localizer["Failed to delete role"]);
            }

            return await Result<bool>.SuccessAsync(true, _localizer["Role deleted successfully"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleId}", roleId);
            return await Result<bool>.FailAsync(_localizer["An error occurred while deleting role"]);
        }
    }

    public async Task<Result<bool>> AssignRolesToUserAsync(string userId, AssignRoleUserRequest req,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await _keycloakService.GetKeycloakAdminCliTokenAsync(cancellationToken);
            if (!token.Succeeded)
                return await Result<bool>.FailAsync(_localizer["Failed to obtain Keycloak token"]);

            var clientId =
                await _keycloakService.GetClientIdAsync(_keycloakConfig.Client.ClientId, token.Data, cancellationToken);
            if (!clientId.Succeeded)
                return await Result<bool>.FailAsync(_localizer["Failed to get client ID"]);

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/users/{userId}/role-mappings/clients/{clientId.Data}");

            var roles = req.Roles.Select(r => new { id = r.Id, name = r.Name, description = r.Description }).ToList();

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
            request.Content = new StringContent(JsonConvert.SerializeObject(roles), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to assign roles. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, content);
                return await Result<bool>.FailAsync(_localizer["Failed to assign roles to user"]);
            }

            return await Result<bool>.SuccessAsync(true, _localizer["Roles assigned successfully"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning roles to user {UserId}", userId);
            return await Result<bool>.FailAsync(_localizer["An error occurred while assigning roles"]);
        }
    }

    public async Task<Result<bool>> UnAssignRolesToUserAsync(string userId, AssignRoleUserRequest req,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await _keycloakService.GetKeycloakAdminCliTokenAsync(cancellationToken);
            if (!token.Succeeded)
                return await Result<bool>.FailAsync(_localizer["Failed to obtain Keycloak token"]);

            var clientId =
                await _keycloakService.GetClientIdAsync(_keycloakConfig.Client.ClientId, token.Data, cancellationToken);
            if (!clientId.Succeeded)
                return await Result<bool>.FailAsync(_localizer["Failed to get client ID"]);

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Delete,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/users/{userId}/role-mappings/clients/{clientId.Data}");

            var roles = req.Roles.Select(r => new { id = r.Id, name = r.Name, description = r.Description }).ToList();

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
            request.Content = new StringContent(JsonConvert.SerializeObject(roles), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to unassign roles. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, content);
                return await Result<bool>.FailAsync(_localizer["Failed to unassign roles from user"]);
            }

            return await Result<bool>.SuccessAsync(true, _localizer["Roles unassigned successfully"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning roles from user {UserId}", userId);
            return await Result<bool>.FailAsync(_localizer["An error occurred while unassigning roles"]);
        }
    }

    public async Task<Result<ModulePermissionsResponse>> GetPermissionsByRoleIdAsync(string roleId,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await _keycloakService.GetKeycloakAdminCliTokenAsync(cancellationToken);
            if (!token.Succeeded)
                return await Result<ModulePermissionsResponse>.FailAsync(_localizer["Failed to obtain Keycloak token"]);

            var clientId =
                await _keycloakService.GetClientIdAsync(_keycloakConfig.Client.ClientId, token.Data, cancellationToken);
            if (!clientId.Succeeded)
                return await Result<ModulePermissionsResponse>.FailAsync(_localizer["Failed to get client ID"]);

            var client = _httpClientFactory.CreateClient();

            // Get all permissions first
            var permissionsRequest = new HttpRequestMessage(HttpMethod.Get,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId.Data}/authz/resource-server/permission/scope");

            permissionsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
            var permissionsResponse = await client.SendAsync(permissionsRequest, cancellationToken);
            var permissionsContent = await permissionsResponse.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("All permissions response: {Content}", permissionsContent);

            var allPermissions = JsonConvert.DeserializeObject<List<KeycloakPermissionResponse>>(permissionsContent)
                                 ?? new List<KeycloakPermissionResponse>();

            // Get resources
            var resourcesRequest = new HttpRequestMessage(HttpMethod.Get,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId.Data}/authz/resource-server/resource");

            resourcesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
            var resourcesResponse = await client.SendAsync(resourcesRequest, cancellationToken);
            var resourcesContent = await resourcesResponse.Content.ReadAsStringAsync(cancellationToken);

            var resources = JsonConvert.DeserializeObject<List<KeycloakResource>>(resourcesContent)
                            ?? new List<KeycloakResource>();
            var modulePermissions = new Dictionary<string, ModuleResponse>();

            foreach (var resource in resources.Where(r =>
                         r.Name != "default Resource" &&
                         Modules.All.Contains(r.Name, StringComparer.OrdinalIgnoreCase)))
            {
                _logger.LogInformation("Processing resource {ResourceName} with ID {ResourceId}",
                    resource.Name, resource.Id);

                // Find permission for this resource
                var permission = allPermissions.FirstOrDefault(p => p.Name == $"{resource.Name}_Permission");
                if (permission != null)
                {
                    // Get permission resources
                    var permissionResourcesRequest = new HttpRequestMessage(HttpMethod.Get,
                        $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId.Data}/authz/resource-server/permission/scope/{permission.Id}/resources");

                    permissionResourcesRequest.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", token.Data);
                    var permissionResourcesResponse =
                        await client.SendAsync(permissionResourcesRequest, cancellationToken);
                    var permissionResourcesContent =
                        await permissionResourcesResponse.Content.ReadAsStringAsync(cancellationToken);

                    _logger.LogInformation("Permission resources for {ResourceName}: {Content}",
                        resource.Name, permissionResourcesContent);

                    // Get permission scopes
                    var permissionScopesRequest = new HttpRequestMessage(HttpMethod.Get,
                        $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId.Data}/authz/resource-server/permission/scope/{permission.Id}/scopes");

                    permissionScopesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
                    var permissionScopesResponse = await client.SendAsync(permissionScopesRequest, cancellationToken);
                    var permissionScopesContent =
                        await permissionScopesResponse.Content.ReadAsStringAsync(cancellationToken);

                    _logger.LogInformation("Permission scopes for {ResourceName}: {Content}",
                        resource.Name, permissionScopesContent);

                    // Deserialize permission resources and scopes
                    var permissionResources =
                        JsonConvert.DeserializeObject<List<PermissionResource>>(permissionResourcesContent) ??
                        new List<PermissionResource>();
                    var permissionScopes =
                        JsonConvert.DeserializeObject<List<PermissionScope>>(permissionScopesContent) ??
                        new List<PermissionScope>();

                    // Generate operations for the current resource
                    var operations = Operations.All.Select(operation =>
                    {
                        // Check if the resource ID exists in permissionResources
                        bool isResourceAssigned = permissionResources.Any(pr => pr.Id == resource.Id);

                        // Check if the operation name exists in permissionScopes
                        bool isOperationAssigned = permissionScopes.Any(ps => ps.Name == operation);

                        // Determine if the operation is assigned
                        bool isAssigned = isResourceAssigned && isOperationAssigned;

                        // Log the result
                        _logger.LogInformation("Operation {Operation} for resource {Resource} isAssigned: {IsAssigned}",
                            operation, resource.Name, isAssigned);

                        // Return the ModuleOperationResponse
                        return new ModuleOperationResponse
                        {
                            Name = operation,
                            Key = operation,
                            IsAssigned = isAssigned
                        };
                    }).ToList();

                    // Add the operations to the modulePermissions dictionary
                    modulePermissions[resource.Name.ToLower()] = new ModuleResponse
                    {
                        Operations = operations
                    };
                }
                else
                {
                    _logger.LogWarning("No permission found for resource {ResourceName}", resource.Name);
                    modulePermissions[resource.Name.ToLower()] = new ModuleResponse
                    {
                        Operations = Operations.All.Select(operation => new ModuleOperationResponse
                        {
                            Name = operation,
                            Key = operation,
                            IsAssigned = false
                        }).ToList()
                    };
                }
            }

            var result = new ModulePermissionsResponse
            {
                Modules = modulePermissions
            };

            return await Result<ModulePermissionsResponse>.SuccessAsync(result,
                _localizer["Permissions retrieved successfully"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for role {RoleId}", roleId);
            return await Result<ModulePermissionsResponse>.FailAsync(
                _localizer["An error occurred while getting permissions"]);
        }
    }

    private async Task<Result<string>> CreateClientRoleAsync(string roleName, string clientId, string token,
        CancellationToken cancellationToken)
    {
        try
        {
            // First check if role exists
            var existingRole = await GetRoleIdAsync(roleName, clientId, token, cancellationToken);
            if (existingRole.Succeeded)
            {
                return await Result<string>.FailAsync(_localizer["Role already exists"]);
            }

            // Role doesn't exist, create it
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId}/roles");

            var role = new
            {
                name = roleName,
                description = $"Role for {roleName}"
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(JsonConvert.SerializeObject(role), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var newRole = await GetRoleIdAsync(roleName, clientId, token, cancellationToken);
                if (newRole.Succeeded)
                {
                    return await Result<string>.SuccessAsync(newRole.Data, _localizer["Role created successfully"]);
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to create role. Status: {StatusCode}, Content: {Content}",
                response.StatusCode, errorContent);

            return await Result<string>.FailAsync(_localizer["Failed to create role"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role {RoleName}", roleName);
            return await Result<string>.FailAsync(_localizer["An error occurred while creating the role"]);
        }
    }

    private async Task<Result<string>> GetResourceIdByNameAsync(string resourceName, string token,
        CancellationToken cancellationToken)
    {
        try
        {
            var clientId =
                await _keycloakService.GetClientIdAsync(_keycloakConfig.Client.ClientId, token, cancellationToken);
            if (!clientId.Succeeded)
                return await Result<string>.FailAsync(_localizer["Failed to get client ID"]);

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId.Data}/authz/resource-server/resource?name={resourceName}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return await Result<string>.FailAsync(_localizer["Failed to get resource ID"]);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var resources = JsonConvert.DeserializeObject<List<KeycloakResource>>(content);
            var resource = resources.FirstOrDefault();

            return resource != null
                ? await Result<string>.SuccessAsync(resource.Id, _localizer["Resource retrieved successfully"])
                : await Result<string>.FailAsync(_localizer["Resource not found"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resource ID for {ResourceName}", resourceName);
            return await Result<string>.FailAsync(_localizer[ex.Message]);
        }
    }

    private async Task<Result<string>> GetRoleIdAsync(string roleName, string clientId, string token,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId}/roles/{roleName}");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return await Result<string>.FailAsync(_localizer["Failed to get role ID"]);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var role = JsonConvert.DeserializeObject<KeycloakRole>(content);

        return await Result<string>.SuccessAsync(role.Id, _localizer["Role retrieved successfully"]);
    }

    private async Task RemoveExistingPermissionsAsync(string roleId, string token, CancellationToken cancellationToken)
    {
        var clientId =
            await _keycloakService.GetClientIdAsync(_keycloakConfig.Client.ClientId, token, cancellationToken);

        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId.Data}/authz/resource-server/permission?type=scope&role={roleId}");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var permissions = JsonConvert.DeserializeObject<List<KeycloakPermission>>(content)
                          ?? new List<KeycloakPermission>();

        foreach (var permission in permissions)
        {
            await DeletePermissionAsync(permission.Id, token, cancellationToken);
        }
    }

    private async Task DeletePermissionAsync(string permissionId, string token, CancellationToken cancellationToken)
    {
        var clientId =
            await _keycloakService.GetClientIdAsync(_keycloakConfig.Client.ClientId, token, cancellationToken);
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId.Data}/authz/resource-server/permission/scope/{permissionId}");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.SendAsync(request, cancellationToken);
    }

    private async Task<Result<string>> GetPolicyIdByRoleIdAsync(string roleId, string token,
        CancellationToken cancellationToken)
    {
        try
        {
            var clientId =
                await _keycloakService.GetClientIdAsync(_keycloakConfig.Client.ClientId, token, cancellationToken);
            if (!clientId.Succeeded)
                return await Result<string>.FailAsync(_localizer["Failed to get client ID"]);

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId.Data}/authz/resource-server/policy?permission=false&type=role&first=0&max=20");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get policies. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, content);
                return await Result<string>.FailAsync(_localizer["Failed to get policy ID"]);
            }

            var policies = JsonConvert.DeserializeObject<List<KeycloakPolicy>>(content);
            var policy = policies?.FirstOrDefault(p => p.Config.ContainsKey("roles")
                                                       && p.Config["roles"].Contains(roleId));

            return policy != null
                ? await Result<string>.SuccessAsync(policy.Id, _localizer["Policy found successfully"])
                : await Result<string>.FailAsync(_localizer["Policy not found"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting policy ID for role {RoleId}", roleId);
            return await Result<string>.FailAsync(_localizer["An error occurred while getting policy ID"]);
        }
    }

    private bool IsDefaultKeycloakRole(string roleName)
    {
        var defaultRoles = new[]
        {
            "uma_protection",
            "uma_authorization",
            "offline_access",
            "default-roles",
            "create-realm",
            "query-users",
            "query-groups",
            "query-realms",
            // Add any other default roles you want to exclude
        };

        return defaultRoles.Any(r => roleName.Contains(r, StringComparison.OrdinalIgnoreCase));
    }
}