using EMR.Application.Requests.Identity;
using EMR.Application.Requests.Keycloaks;
using Newtonsoft.Json.Linq;

namespace EMR.Application.Interfaces.Services.Keycloak;

public interface IKeycloakService
{
    Task<Result<string>> GetKeycloakAdminCliTokenAsync(CancellationToken cancellationToken);
    Task<Result<object>> GetKeycloakRolesAsync(string roleName, string adminToken, CancellationToken cancellationToken);
    Task<Result<string>> GetUserIdAsync(string loginId, string adminToken, CancellationToken cancellationToken);
    Task<Result<string>> AssignRoleToUserAsync(string userId, string roleName, string adminToken,
        CancellationToken cancellationToken);
    Task<Result<string>> GetClientIdAsync(string clientId, string adminToken, CancellationToken cancellationToken);
    Task<Result<JObject>> LoginAsync(LoginRequest req, CancellationToken cancellationToken);
    Task<Result<JObject>> ExternalLoginAsync(ExternalLoginRequest req, CancellationToken cancellationToken);
    Task<Result<string>> RegisterAsync(RegisterRequest req, CancellationToken cancellationToken);
    Task<Result<JObject>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task<Result<string>> ChangePinAsync(ChangPinRequest req, CancellationToken cancellationToken);
    Task<Result<string>> ChangeLoginIdAsync(ChangeLoginIdRequest req,
        CancellationToken cancellationToken);
    Task<Result<string>> ResetPinAsync(ResetPinRequest req, CancellationToken cancellationToken);
}