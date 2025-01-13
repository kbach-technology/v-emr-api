using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace EMR.Application.Atrributes;

public class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(IEnumerable<string> permissions)
    {
        Permissions = permissions;
    }

    public IEnumerable<string> Permissions { get; }
}

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext.User.Identity.IsAuthenticated)
        {
            var token = await httpContext.GetTokenAsync("access_token");
            if (string.IsNullOrEmpty(token))
            {
                context.Fail();
                return;
            }

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var audience = jwtToken.Claims.FirstOrDefault(c => c.Type == "azp")?.Value;

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_configuration["Keycloak:Client:Authority"]}/realms/{_configuration["Keycloak:Client:Realm"]}/protocol/openid-connect/token");

            var formData = new List<KeyValuePair<string, string?>>
            {
                new("audience", audience),
                new("grant_type", "urn:ietf:params:oauth:grant-type:uma-ticket"),
                new("response_mode", "decision"),
                new("permission", requirement.Permissions.FirstOrDefault()?.Replace('.', '#'))
            };

            request.Content = new FormUrlEncodedContent(formData);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                context.Succeed(requirement);
                return;
            }

            context.Fail();
        }
        else
        {
            context.Fail();
        }
    }
}