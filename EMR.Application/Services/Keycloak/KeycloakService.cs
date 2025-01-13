using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EMR.Application.Interfaces.Services;
using EMR.Application.Interfaces.Services.Keycloak;
using EMR.Application.Requests;
using EMR.Application.Requests.Identity;
using EMR.Application.Requests.Keycloaks;
using EMR.Domain.Enums;
using EMR.Shared.Constants;
using EMR.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace EMR.Application.Services.Keycloak;

public class KeycloakService : IKeycloakService
{
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IStringLocalizer<KeycloakService> _localizer;
    private readonly IOtpService _otpService;
    private readonly IRedisStorageService _redisStorageService;
    private readonly ILogger _trace;

    public KeycloakService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IStringLocalizer<KeycloakService> localizer,
        ICurrentUserService currentUserService,
        IOtpService otpService,
        IRedisStorageService redisStorageService,
        ILogger trace)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _otpService = otpService;
        _redisStorageService = redisStorageService;
        _trace = trace;
    }

    public async Task<Result<string>> GetKeycloakAdminCliTokenAsync(CancellationToken cancellationToken)
    {
        // var key = "adminToken";
        // if (await _redisStorageService.GetValueAsync(key) != null)
        // {
        //     _trace.Debug("Getting admin token from cache");
        //     return await Result<string>.SuccessAsync(data: await _redisStorageService.GetValueAsync(key));
        // }

        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_configuration["Keycloak:Admin:Authority"]}/realms/{_configuration["Keycloak:Admin:Realm"]}/protocol/openid-connect/token");

        var formData = new List<KeyValuePair<string, string>>
        {
            new("client_id", _configuration["Keycloak:Admin:ClientId"]),
            new("client_secret", _configuration["Keycloak:Admin:ClientSecret"]),
            new("grant_type", _configuration["Keycloak:Admin:GrantType"]),
            new("scope", _configuration["Keycloak:Admin:Scope"]),
            new("username", _configuration["Keycloak:Admin:Username"]),
            new("password", _configuration["Keycloak:Admin:Password"])
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
                // _trace.Debug("Storing admin token in cache");
                // await _redisStorageService.StoreValueAsync(token, key);
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
            $"{_configuration["Keycloak:Client:Authority"]}/admin/realms/{_configuration["Keycloak:Client:Realm"]}/clients?clientId={clientId}");
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

    public async Task<Result<string>> GetUserIdAsync(string loginId, string adminToken,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_configuration["Keycloak:Client:Authority"]}/admin/realms/{_configuration["Keycloak:Client:Realm"]}/users?username={loginId}");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await client.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var keyUsers = JArray.Parse(responseContent);
            var keyUser = keyUsers.FirstOrDefault();

            if (keyUser != null)
                return await Result<string>.SuccessAsync(data: keyUser["id"].ToString());
        }

        return await Result<string>.FailAsync("Failed to get user id");
    }

    public async Task<Result<object>> GetKeycloakRolesAsync(string roleName, string adminToken,
        CancellationToken cancellationToken)
    {
        var clientId =
            await GetClientIdAsync(_configuration["Keycloak:Client:ClientId"], adminToken, cancellationToken);
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_configuration["Keycloak:Admin:Authority"]}/admin/realms/{_configuration["Keycloak:Client:Realm"]}/clients/{clientId.Data}/roles/{roleName}");

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

    public async Task<Result<string>> AssignRoleToUserAsync(string userId, string roleName, string adminToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var clientId = await GetClientIdAsync(_configuration["Keycloak:Client:ClientId"], adminToken,
                cancellationToken);
            if (adminToken == null)
                return await Result<string>.FailAsync(_localizer["Failed to obtain Keycloak token"]);

            var rolesResult = await GetKeycloakRolesAsync(roleName, adminToken, cancellationToken);
            if (!rolesResult.Succeeded)
                return await Result<string>.FailAsync(_localizer["Failed to retrieve roles from Keycloak"]);

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_configuration["Keycloak:Admin:Authority"]}/admin/realms/{_configuration["Keycloak:Client:Realm"]}/users/{userId}/role-mappings/clients/{clientId.Data}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var rolesJson = JsonConvert.SerializeObject(rolesResult.Data);
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

    public async Task<Result<JObject>> LoginAsync(LoginRequest req, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var tokenEndpoint =
                $"{_configuration["Keycloak:Client:Authority"]}/realms/{_configuration["Keycloak:Client:Realm"]}/protocol/openid-connect/token";

            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);

            var formData = new List<KeyValuePair<string, string>>
            {
                new("client_id", _configuration["Keycloak:Client:ClientId"]),
                new("client_secret", _configuration["Keycloak:Client:Secret"]),
                new("grant_type", "password"),
                new("scope", _configuration["Keycloak:Client:Scope"])
            };

            // Determine the correct parameter name based on the identifier type
            IdentifierType username;
            switch (req.IdentifierType)
            {
                case IdentifierType.PhoneNumber:
                    username = IdentifierType.PhoneNumber;
                    break;
                case IdentifierType.Email:
                    username = IdentifierType.Email;
                    break;
                case IdentifierType.Username:
                default:
                    username = IdentifierType.Username;
                    break;
            }

            formData.Add(new KeyValuePair<string, string>("username", req.Identifier));
            formData.Add(new KeyValuePair<string, string>("password", req.Pin));

            request.Content = new FormUrlEncodedContent(formData);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await client.SendAsync(request, cancellationToken);

            // OTP verification (if required for phone-based login)
            if (req.IdentifierType == IdentifierType.PhoneNumber)
            {
                var verifyOtp = await _otpService.IsVerifiedOtp(req.Identifier, cancellationToken);
                if (!verifyOtp.Succeeded)
                    return await Result<JObject>.FailAsync(verifyOtp.Message);
            }

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseJson = JObject.Parse(responseContent);

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

    public async Task<Result<string>> RegisterAsync(RegisterRequest req, CancellationToken cancellationToken)
    {
        try
        {
            var token = await GetKeycloakAdminCliTokenAsync(cancellationToken);
            if (token == null)
                return await Result<string>.FailAsync(_localizer["Failed to obtain Keycloak token"]);

            var user = new Dictionary<string, object>
            {
                { "enabled", true },
                {
                    "credentials", new[]
                    {
                        new { type = "password", value = req.Pin, temporary = false }
                    }
                }
            };
            switch (req.IdentifierType)
            {
                case IdentifierType.Username:
                    user["username"] = req.Identifier;
                    break;
                case IdentifierType.PhoneNumber:
                    user["username"] = req.Identifier;
                    break;
                case IdentifierType.Email:
                    user["email"] = req.Identifier;
                    user["username"] = req.Identifier;
                    break;
            }

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_configuration["Keycloak:Admin:Authority"]}/admin/realms/{_configuration["Keycloak:Client:Realm"]}/users");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
            request.Content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
            var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode && response.Headers.Location != null)
            {
                var locationHeader = response.Headers.Location.ToString();
                var userId = locationHeader.Substring(locationHeader.LastIndexOf('/') + 1);

                await AssignRoleToUserAsync(userId, Roles.Merchant, token.Data, cancellationToken);

                // Only request OTP for phone number registration, Except for external login
                if (req.IdentifierType == IdentifierType.PhoneNumber && req.IsExternalLogin == false)
                    await _otpService.RequestOtpAsync(new OtpRequest(req.Identifier, OTPAction.RegisterCode),
                        cancellationToken);

                return await Result<string>.SuccessAsync(userId, _localizer["User created successfully"]);
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
                return await Result<string>.FailAsync(_localizer["User already registered"]);

            return await Result<string>.FailAsync(_localizer["Failed to create user"]);
        }
        catch (Exception e)
        {
            return await Result<string>.FailAsync(e.Message);
        }
    }

    public async Task<Result<JObject>> RefreshTokenAsync(RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Post,
                $"{_configuration["Keycloak:Client:Authority"]}/realms/{_configuration["Keycloak:Client:Realm"]}/protocol/openid-connect/token");

            var formData = new List<KeyValuePair<string, string>>
            {
                new("client_id", _configuration.GetSection("Keycloak:Client:ClientId").Value),
                new("client_secret", _configuration.GetSection("Keycloak:Client:Secret").Value),
                new("grant_type", "refresh_token"),
                new("scope", _configuration.GetSection("Keycloak:Client:Scope").Value),
                new("refresh_token", request.RefreshToken)
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

    public async Task<Result<string>> ChangePinAsync(ChangPinRequest req, CancellationToken cancellationToken)
    {
        try
        {
            var token = await GetKeycloakAdminCliTokenAsync(cancellationToken);
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Put,
                $"{_configuration["Keycloak:Admin:Authority"]}/admin/realms/{_configuration["Keycloak:Client:Realm"]}/users/{_currentUserService.UserId}/reset-password");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
            request.Content =
                new StringContent(
                    JsonConvert.SerializeObject(new { type = "password", value = req.NewPin, temporary = false }),
                    Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
                return await Result<string>.SuccessAsync(_localizer["Pin code changed successfully"]);
            if (response.StatusCode == HttpStatusCode.BadRequest)
                return await Result<string>.FailAsync(
                    _localizer["Pin code must be different from the previous one and must be 6 digits"]);

            return await Result<string>.FailAsync(_localizer["Failed to change pin code"]);
        }
        catch (Exception e)
        {
            return await Result<string>.FailAsync(e.Message);
        }
    }

    // This method is working event if you enable Edit username option in keycloak
    public async Task<Result<string>> ChangeLoginIdAsync(ChangeLoginIdRequest req,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await GetKeycloakAdminCliTokenAsync(cancellationToken);
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Put,
                $"{_configuration["Keycloak:Admin:Authority"]}/admin/realms/{_configuration["Keycloak:Client:Realm"]}/users/{req.UserId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
            request.Content =
                new StringContent(JsonConvert.SerializeObject(new { username = req.NewUsername }), Encoding.UTF8,
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

    public async Task<Result<string>> ResetPinAsync(ResetPinRequest req, CancellationToken cancellationToken)
    {
        try
        {
            var token = await GetKeycloakAdminCliTokenAsync(cancellationToken);
            var userId = await GetUserIdAsync(req.LoginId, token.Data, cancellationToken);
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Put,
                $"{_configuration["Keycloak:Admin:Authority"]}/admin/realms/{_configuration["Keycloak:Client:Realm"]}/users/{userId.Data}/reset-password");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Data);
            request.Content =
                new StringContent(
                    JsonConvert.SerializeObject(new { type = "password", value = req.NewPin, temporary = false }),
                    Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
                return await Result<string>.SuccessAsync(_localizer["Pin code reset successfully"]);
            if (response.StatusCode == HttpStatusCode.BadRequest)
                return await Result<string>.FailAsync(
                    _localizer["Pin code must be different from the previous one and must be 6 digits"]);

            return await Result<string>.FailAsync(_localizer["Failed to reset pin code"]);
        }
        catch (Exception e)
        {
            return await Result<string>.FailAsync(e.Message);
        }
    }

    public async Task<Result<JObject>> ExternalLoginAsync(ExternalLoginRequest req, CancellationToken cancellationToken)
    {
        try
        {
            var token = await GetKeycloakAdminCliTokenAsync(cancellationToken);
            var user = await GetUserIdAsync(req.PhoneNumber, token.Data, cancellationToken);
            if (!user.Succeeded)
                await RegisterAsync(
                    new RegisterRequest(req.PhoneNumber, req.Pin, IdentifierType.PhoneNumber, Roles.Merchant, true),
                    cancellationToken);
            return await LoginAsync(new LoginRequest(req.PhoneNumber, req.Pin, IdentifierType.PhoneNumber),
                cancellationToken);
        }
        catch (Exception e)
        {
            return await Result<JObject>.FailAsync(e.Message);
        }
    }
}