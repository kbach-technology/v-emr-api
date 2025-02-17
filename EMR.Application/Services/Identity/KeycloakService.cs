using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EMR.Application.Interfaces.Services.Identity;
using EMR.Application.Requests.Identity;
using EMR.Application.Requests.Keycloaks;
using EMR.Application.Responses.Identity;
using EMR.Domain.Entities.Users;
using EMR.Shared.Configurations;
using EMR.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace EMR.Application.Services.Identity;

public class KeycloakService : IKeycloakService
{
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork<string> _unitOfWork;
    private readonly KeycloakConfiguration _keycloakConfig;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IStringLocalizer<KeycloakService> _localizer;
    private readonly IOtpService _otpService;
    private readonly IRedisStorageService _redisStorageService;
    private readonly ILogger _trace;

    public KeycloakService(
        IUnitOfWork<string> _unitOfWork,
        KeycloakConfiguration keycloakConfig,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IStringLocalizer<KeycloakService> localizer,
        ICurrentUserService currentUserService,
        IOtpService otpService,
        IRedisStorageService redisStorageService,
        ILogger trace)
    {
        this._unitOfWork = _unitOfWork;
        _keycloakConfig = keycloakConfig;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _otpService = otpService;
        _redisStorageService = redisStorageService;
        _trace = trace;
    }


    public async Task<Result<string>> CreatAsync(KeyCloakCreateUserRequest req, CancellationToken cancellationToken)
    {
        try
        {
            var token = await GetKeycloakAdminCliTokenAsync(cancellationToken);
            if (!token.Succeeded)
                return await Result<string>.FailAsync(token.Message);

            using var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_keycloakConfig.Client.UserEndpoint));

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);

            var user = new Dictionary<string, object>
            {
                { "enabled", true },
                { "credentials", new[] { new { type = "password", value = req.Password, temporary = false } } },
                { "username", req.PhoneNumber },
                { "email", req.Email },
                { "emailVerified", true }
            };

            request.Content = new StringContent(
                JsonConvert.SerializeObject(user),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode && response.Headers.Location != null)
            {
                var locationHeader = response.Headers.Location.ToString();
                var id = locationHeader.Substring(locationHeader.LastIndexOf('/') + 1);

                var roleResult = await AssignRoleToUserAsync(id, req.RoleName, token.Data, cancellationToken);
                if (!roleResult.Succeeded)
                    return roleResult;

                return await Result<string>.SuccessAsync(id, _localizer["User created successfully"]);
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
                return await Result<string>.FailAsync($"{_localizer["User already registered"]} - {content}");

            return await Result<string>.FailAsync($"{_localizer["Failed to create user"]} - {content}");
        }
        catch (Exception e)
        {
            _trace.Error(e, "Error creating user in Keycloak");
            return await Result<string>.FailAsync(_localizer["An error occurred while creating the user"]);
        }
    }

    public async Task<Result<JObject>> LoginAsync(LoginRequest req, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var tokenEndpoint = $"{_keycloakConfig.Client.TokenEndpoint}";

            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);

            var formData = new List<KeyValuePair<string, string>>
            {
                new("client_id", _keycloakConfig.Client.ClientId),
                new("client_secret", _keycloakConfig.Client.Secret),
                new("grant_type", "password"),
                new("scope", _keycloakConfig.Client.Scope)
            };

            formData.Add(new KeyValuePair<string, string>("username", req.Identifier));
            formData.Add(new KeyValuePair<string, string>("password", req.Password));

            request.Content = new FormUrlEncodedContent(formData);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseJson = JObject.Parse(responseContent);

                var accessToken = responseJson["access_token"]?.ToString();
                var handler = new JwtSecurityTokenHandler();

                if (handler.ReadToken(accessToken) is JwtSecurityToken jsonToken)
                    responseJson["user_id"] = jsonToken.Claims.FirstOrDefault(claim => claim.Type == "sub")?.Value;

                return await Result<JObject>.SuccessAsync(responseJson);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return await Result<JObject>.FailAsync(_localizer["Invalid credentials"]);

            return await Result<JObject>.FailAsync(_localizer["Failed to login user"]);
        }
        catch (Exception e)
        {
            return await Result<JObject>.FailAsync(e.Message);
        }
    }

    public async Task<Result<JObject>> RefreshTokenAsync(RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Post,
                $"{_keycloakConfig.Client.TokenEndpoint}");

            var formData = new Dictionary<string, string>()
            {
                { "client_id", _keycloakConfig.Client.ClientId },
                { "client_secret", _keycloakConfig.Client.Secret },
                { "grant_type", "refresh_token" },
                { "refresh_token", request.RefreshToken }
            };

            req.Content = new FormUrlEncodedContent(formData);
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await client.SendAsync(req, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseJson = JObject.Parse(responseContent);

                return await Result<JObject>.SuccessAsync(responseJson);
            }

            return await Result<JObject>.FailAsync(_localizer["Failed to refresh token"]);
        }
        catch (Exception e)
        {
            return await Result<JObject>.FailAsync(e.Message);
        }
    }

    public async Task<Result<string>> UpdateUserAsync(string id, string email, CancellationToken cancellationToken)
    {
        try
        {
            var token = await GetKeycloakAdminCliTokenAsync(cancellationToken);
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Put,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/users/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
            request.Content =
                new StringContent(JsonConvert.SerializeObject(
                        new
                        {
                            username = email,
                            email = email,
                        }), Encoding.UTF8,
                    "application/json");

            var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
                return await Result<string>.SuccessAsync(_localizer["User updated successfully"]);

            return await Result<string>.FailAsync(_localizer["Failed to update user"]);
        }
        catch (Exception e)
        {
            return await Result<string>.FailAsync(e.Message);
        }
    }

    public async Task<Result<string>> ChangePasswordAsync(ChangePasswordRequest req,
        CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await ValidateCurrentPassword(req.CurrentPassword, cancellationToken);
            if (!validationResult.Succeeded)
            {
                return await Result<string>.FailAsync(_localizer["Current password is incorrect"]);
            }

            var token = await GetKeycloakAdminCliTokenAsync(cancellationToken);
            if (!token.Succeeded)
            {
                return await Result<string>.FailAsync(_localizer["Failed to obtain Keycloak token"]);
            }

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Put,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/users/{_currentUserService.UserId}/reset-password");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
            request.Content =
                new StringContent(
                    JsonConvert.SerializeObject(new { type = "password", value = req.NewPassword, temporary = false }),
                    Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
                return await Result<string>.SuccessAsync(_localizer["Password changed successfully"]);
            if (response.StatusCode == HttpStatusCode.BadRequest)
                return await Result<string>.FailAsync(
                    _localizer["Password must be different from the previous one and at lease 6 digits"]);

            return await Result<string>.FailAsync(_localizer["Failed to change pin code"]);
        }
        catch (Exception e)
        {
            return await Result<string>.FailAsync(e.Message);
        }
    }

    // This method is working event if you enable Edit username option in keycloak
    public async Task<Result<string>> ChangeEmailAsync(ChangeEmailRequest req,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await GetKeycloakAdminCliTokenAsync(cancellationToken);
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Put,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/users/{req.UserId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
            request.Content =
                new StringContent(JsonConvert.SerializeObject(new { username = req.NewEmail }), Encoding.UTF8,
                    "application/json");

            var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
                return await Result<string>.SuccessAsync(_localizer["Username changed successfully"]);

            return await Result<string>.FailAsync(_localizer["Failed to change username"]);
        }
        catch (Exception e)
        {
            return await Result<string>.FailAsync(e.Message);
        }
    }

    public async Task<Result<string>> ValidateCurrentPassword(string currentPassword,
        CancellationToken cancellationToken)
    {
        try
        {
            var userid = _currentUserService.UserId;
            var client = _httpClientFactory.CreateClient();
            var tokenEndpoint = $"{_keycloakConfig.Client.TokenEndpoint}";

            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);

            var formData = new List<KeyValuePair<string, string>>
            {
                new("client_id", _keycloakConfig.Client.ClientId),
                new("client_secret", _keycloakConfig.Client.Secret),
                new("grant_type", "password"),
                new("scope", _keycloakConfig.Client.Scope)
            };

            formData.Add(new KeyValuePair<string, string>("username", _currentUserService.Email));
            formData.Add(new KeyValuePair<string, string>("password", currentPassword));

            request.Content = new FormUrlEncodedContent(formData);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await client.SendAsync(request, cancellationToken);


            return response.IsSuccessStatusCode
                ? await Result<string>.SuccessAsync(_localizer["Password is correct"])
                : await Result<string>.FailAsync(_localizer["Password is incorrect"]);
        }
        catch (Exception ex)
        {
            return await Result<string>.FailAsync(_localizer[ex.Message]);
        }
    }

    public async Task<Result<string>> ResetPasswordAsync(ResetPasswordRequest req, CancellationToken cancellationToken)
    {
        try
        {
            var token = await GetKeycloakAdminCliTokenAsync(cancellationToken);
            var userId = await GetUserIdAsync(req.Email, cancellationToken);
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Put,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/users/{userId}/reset-password");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
            request.Content =
                new StringContent(
                    JsonConvert.SerializeObject(new { type = "password", value = req.NewPassword, temporary = false }),
                    Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
                return await Result<string>.SuccessAsync(_localizer["Password reset successfully"]);
            if (response.StatusCode == HttpStatusCode.BadRequest)
                return await Result<string>.FailAsync(
                    _localizer["Pin code must be different from the previous one and at lease 6 digits"]);

            return await Result<string>.FailAsync(_localizer["Failed to reset pin code"]);
        }
        catch (Exception e)
        {
            return await Result<string>.FailAsync(e.Message);
        }
    }

    public async Task<Result<List<GetUserSessionResponse>>> GetUserSessionAsync(string id,
        CancellationToken cancellationToken)
    {
        var token = await GetKeycloakAdminCliTokenAsync(cancellationToken);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);

        var response = await client.GetAsync(
            $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/users/{id}/sessions",
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<List<GetUserSessionResponse>>(cancellationToken);

        return await Result<List<GetUserSessionResponse>>.SuccessAsync(data, _localizer["User session found"]);
    }

    public async Task<Result<string>> GetKeycloakAdminCliTokenAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, _keycloakConfig.Admin.AdminTokenEndpoint);

        var formData = new Dictionary<string, string>
        {
            ["client_id"] = _keycloakConfig.Admin.ClientId,
            ["client_secret"] = _keycloakConfig.Admin.Secret,
            ["grant_type"] = _keycloakConfig.Admin.GrantType,
            ["scope"] = _keycloakConfig.Admin.Scope,
            ["username"] = _keycloakConfig.Admin.Username,
            ["password"] = _keycloakConfig.Admin.Password
        };

        request.Content = new FormUrlEncodedContent(formData);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var response = await client.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var token = JObject.Parse(responseContent)["access_token"]?.ToString();

            if (token != null)
            {
                return await Result<string>.SuccessAsync(data: token);
            }
        }

        return await Result<string>.FailAsync("Failed to get keycloak admin token");
    }

    public async Task<Result<string>> GetClientIdAsync(string clientId, string adminToken,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_configuration["Keycloak:Admin:Authority"]}/admin/realms/{_configuration["Keycloak:Client:Realm"]}/clients?clientId={clientId}");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await client.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var keyClients = JArray.Parse(responseContent);
            var keyClient = keyClients.FirstOrDefault();

            if (client != null)
                return await Result<string>.SuccessAsync(data: keyClient["id"].ToString());
        }

        return await Result<string>.FailAsync("Failed to get client id");
    }

    private async Task<string> GetUserIdAsync(string loginId, CancellationToken cancellationToken)
    {
        return await _unitOfWork.Repository<User>().Entities
            .Where(x => x.Email == loginId)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Result<object>> GetKeycloakRoleDetailByRoleName(string roleName, string adminToken,
        CancellationToken cancellationToken)
    {
        var clientId =
            await GetClientIdAsync(_keycloakConfig.Client.ClientId, adminToken, cancellationToken);
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/clients/{clientId.Data}/roles/{roleName}");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await client.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            using (var document = JsonDocument.Parse(responseContent))
            {
                var root = document.RootElement;

                var roleId = root.GetProperty("id").GetString();
                var roleDescription = root.GetProperty("description").GetString();

                var roles = new[]
                {
                    new { id = roleId, name = roleName, description = roleDescription }
                };

                return await Result<object>.SuccessAsync(roles);
            }
        }

        return await Result<object>.FailAsync("Failed to get keycloak roles");
    }

    private async Task<Result<string>> AssignRoleToUserAsync(string userId, string roleName, string adminToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var clientId = await GetClientIdAsync(_keycloakConfig.Client.ClientId, adminToken,
                cancellationToken);
            if (adminToken == null)
                return await Result<string>.FailAsync(_localizer["Failed to obtain Keycloak token"]);

            var role = await GetKeycloakRoleDetailByRoleName(roleName, adminToken, cancellationToken);
            if (!role.Succeeded)
                return await Result<string>.FailAsync(_localizer["Failed to retrieve roles from Keycloak"]);

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/users/{userId}/role-mappings/clients/{clientId.Data}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var rolesJson = JsonConvert.SerializeObject(role.Data);
            request.Content = new StringContent(rolesJson, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
                return await Result<string>.SuccessAsync(_localizer["Role assigned to user"]);

            return await Result<string>.FailAsync(_localizer["Failed to assign role to user"]);
        }
        catch (Exception e)
        {
            return await Result<string>.FailAsync(e.Message);
        }
    }
}