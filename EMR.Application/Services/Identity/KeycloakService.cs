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
using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using RefreshTokenRequest = EMR.Application.Requests.Keycloaks.RefreshTokenRequest;

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
            // Try to get from Redis first
            var cacheKey = $"refresh_token:{request.RefreshToken}";
            var cached = await _redisStorageService.GetAsync<JObject>(cacheKey);
            if (cached != null)
            {
                return await Result<JObject>.SuccessAsync(cached);
            }

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

                // Cache the response in Redis for the token's lifetime
                int expiresIn = responseJson["expires_in"]?.Value<int>() ?? 300; // default 5 min
                await _redisStorageService.SetAsync(cacheKey, responseJson, TimeSpan.FromSeconds(expiresIn));

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
                new StringContent(JsonConvert.SerializeObject(new
                {
                    username = req.NewEmail,
                    email = req.NewEmail,
                    emailVerified = true
                }), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
                return await Result<string>.SuccessAsync(_localizer["Email changed successfully"]);

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return await Result<string>.FailAsync($"{_localizer["Failed to change email"]}: {errorContent}");
        }
        catch (Exception e)
        {
            _trace.Error(e, "Error changing email in Keycloak");
            return await Result<string>.FailAsync(e.Message);
        }
    }

    public async Task<Result<string>> ValidateCurrentPassword(string currentPassword,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.Email))
                return await Result<string>.FailAsync(_localizer["User email not found"]);

            if (string.IsNullOrEmpty(currentPassword))
                return await Result<string>.FailAsync(_localizer["Password cannot be empty"]);

            var client = _httpClientFactory.CreateClient();
            var tokenEndpoint = $"{_keycloakConfig.Client.TokenEndpoint}";

            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);

            var formData = new List<KeyValuePair<string, string>>
            {
                new("client_id", _keycloakConfig.Client.ClientId),
                new("client_secret", _keycloakConfig.Client.Secret),
                new("grant_type", "password"),
                new("scope", _keycloakConfig.Client.Scope),
                new("username", _currentUserService.Email),
                new("password", currentPassword)
            };

            request.Content = new FormUrlEncodedContent(formData);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return await Result<string>.FailAsync(response.StatusCode == HttpStatusCode.Unauthorized
                    ? _localizer["Password is incorrect"]
                    : $"{_localizer["Failed to validate password"]}: {errorContent}");
            }

            return await Result<string>.SuccessAsync(_localizer["Password is correct"]);
        }
        catch (Exception ex)
        {
            _trace.Error(ex, "Error validating current password");
            return await Result<string>.FailAsync(_localizer[ex.Message]);
        }
    }

    public async Task<Result<string>> ResetPasswordAsync(ResetPasswordRequest req, CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetUserIdAsync(req.Email, cancellationToken);
            if (string.IsNullOrEmpty(userId))
                return await Result<string>.FailAsync(_localizer["User not found"]);

            var token = await GetKeycloakAdminCliTokenAsync(cancellationToken);
            if (!token.Succeeded)
                return await Result<string>.FailAsync(_localizer["Failed to obtain Keycloak token"]);

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

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.StatusCode == HttpStatusCode.BadRequest)
                return await Result<string>.FailAsync(
                    _localizer["Password must be at least 6 characters long"]);

            return await Result<string>.FailAsync($"{_localizer["Failed to reset password"]}: {errorContent}");
        }
        catch (Exception e)
        {
            _trace.Error(e, "Error resetting password in Keycloak");
            return await Result<string>.FailAsync(_localizer[e.Message]);
        }
    }

    public async Task<Result<List<GetUserSessionResponse>>> GetUserSessionAsync(string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await GetKeycloakAdminCliTokenAsync(cancellationToken);
            if (!token.Succeeded)
                return await Result<List<GetUserSessionResponse>>.FailAsync(
                    _localizer["Failed to obtain Keycloak token"]);

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);

            var response = await client.GetAsync(
                $"{_keycloakConfig.Client.Authority}/admin/realms/{_keycloakConfig.Client.Realm}/users/{id}/sessions",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return await Result<List<GetUserSessionResponse>>.FailAsync(
                    $"{_localizer["Failed to get user sessions"]}: {errorContent}");
            }

            var data = await response.Content.ReadFromJsonAsync<List<GetUserSessionResponse>>(cancellationToken);
            if (data == null)
                return await Result<List<GetUserSessionResponse>>.FailAsync(_localizer["No session data found"]);

            return await Result<List<GetUserSessionResponse>>.SuccessAsync(data, _localizer["User session found"]);
        }
        catch (Exception e)
        {
            _trace.Error(e, "Error getting user sessions from Keycloak");
            return await Result<List<GetUserSessionResponse>>.FailAsync(e.Message);
        }
    }

    public async Task<Result<string>> GetKeycloakAdminCliTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_keycloakConfig.Admin.ClientId) ||
                string.IsNullOrEmpty(_keycloakConfig.Admin.Secret) ||
                string.IsNullOrEmpty(_keycloakConfig.Admin.Username) ||
                string.IsNullOrEmpty(_keycloakConfig.Admin.Password))
            {
                _trace.Error("Missing required Keycloak admin configuration");
                return await Result<string>.FailAsync("Invalid Keycloak admin configuration");
            }

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

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _trace.Error($"Failed to get admin token: {errorContent}");
                return await Result<string>.FailAsync("Failed to get keycloak admin token");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            try
            {
                var tokenObject = JObject.Parse(responseContent);
                var token = tokenObject["access_token"]?.ToString();

                if (string.IsNullOrEmpty(token))
                {
                    _trace.Error("Admin token response missing access_token");
                    return await Result<string>.FailAsync("Invalid admin token response");
                }

                return await Result<string>.SuccessAsync(data: token);
            }
            catch (JsonReaderException ex)
            {
                _trace.Error(ex, "Error parsing admin token response");
                return await Result<string>.FailAsync("Invalid admin token response format");
            }
        }
        catch (Exception ex)
        {
            _trace.Error(ex, "Error getting admin token");
            return await Result<string>.FailAsync("Failed to get keycloak admin token");
        }
    }

    public async Task<Result<string>> GetClientIdAsync(string clientId, string adminToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(adminToken))
            return await Result<string>.FailAsync("Invalid client ID or admin token");

        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_configuration["Keycloak:Admin:Authority"]}/admin/realms/{_configuration["Keycloak:Client:Realm"]}/clients?clientId={clientId}");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        try
        {
            var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var keyClients = JArray.Parse(responseContent);
                var keyClient = keyClients.FirstOrDefault() as JObject;

                if (keyClient != null && keyClient.ContainsKey("id"))
                    return await Result<string>.SuccessAsync(data: keyClient["id"].ToString());

                return await Result<string>.FailAsync("Client not found");
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return await Result<string>.FailAsync($"Failed to get client ID: {errorContent}");
        }
        catch (JsonReaderException ex)
        {
            _trace.Error(ex, "Error parsing client response from Keycloak");
            return await Result<string>.FailAsync("Invalid response format from Keycloak");
        }
        catch (Exception ex)
        {
            _trace.Error(ex, "Error getting client ID from Keycloak");
            return await Result<string>.FailAsync("Failed to get client ID");
        }
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
            try
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                using (var document = JsonDocument.Parse(responseContent))
                {
                    var root = document.RootElement;

                    if (!root.TryGetProperty("id", out var idElement) ||
                        !root.TryGetProperty("description", out var descElement))
                    {
                        return await Result<object>.FailAsync("Invalid role data received from Keycloak");
                    }

                    var roleId = idElement.GetString();
                    var roleDescription = descElement.GetString();

                    if (string.IsNullOrEmpty(roleId))
                    {
                        return await Result<object>.FailAsync("Role ID cannot be empty");
                    }

                    var roles = new[]
                    {
                        new { id = roleId, name = roleName, description = roleDescription }
                    };

                    return await Result<object>.SuccessAsync(roles);
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                _trace.Error(ex, "Error parsing Keycloak role response");
                return await Result<object>.FailAsync("Failed to parse Keycloak role data");
            }
        }

        return await Result<object>.FailAsync("Failed to get keycloak roles");
    }

    private async Task<Result<string>> AssignRoleToUserAsync(string userId, string roleName, string adminToken,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(adminToken))
                return await Result<string>.FailAsync(_localizer["Failed to obtain Keycloak token"]);

            var clientId = await GetClientIdAsync(_keycloakConfig.Client.ClientId, adminToken,
                cancellationToken);
            if (!clientId.Succeeded || string.IsNullOrEmpty(clientId.Data))
                return await Result<string>.FailAsync(_localizer["Failed to obtain client ID"]);

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

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return await Result<string>.FailAsync($"{_localizer["Failed to assign role to user"]}: {errorContent}");
        }
        catch (Exception e)
        {
            _trace.Error(e, "Error assigning role to user in Keycloak");
            return await Result<string>.FailAsync(e.Message);
        }
    }
}