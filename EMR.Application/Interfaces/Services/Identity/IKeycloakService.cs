using System.Collections.Generic;
using EMR.Application.Requests.Identity;
using EMR.Application.Requests.Keycloaks;
using EMR.Application.Responses.Identity;
using EMR.Application.Services.Identity;
using Nest;
using Newtonsoft.Json.Linq;
using ChangePasswordRequest = EMR.Application.Requests.Identity.ChangePasswordRequest;

namespace EMR.Application.Interfaces.Services.Identity;

public interface IKeycloakService
{
    Task<Result<JObject>> LoginAsync(LoginRequest req, CancellationToken cancellationToken);
    Task<Result<string>> CreatAsync(KeyCloakCreateUserRequest req, CancellationToken cancellationToken);
    Task<Result<JObject>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task<Result<string>> UpdateUserAsync(string id, string email, CancellationToken cancellationToken);
    Task<Result<string>> ChangePasswordAsync(ChangePasswordRequest req, CancellationToken cancellationToken);

    Task<Result<string>> ChangeEmailAsync(ChangeEmailRequest req,
        CancellationToken cancellationToken);

    Task<Result<string>> ResetPasswordAsync(ResetPasswordRequest req, CancellationToken cancellationToken);
    Task<Result<string>> ValidateCurrentPassword(string currentPassword, CancellationToken cancellationToken);
    Task<Result<List<GetUserSessionResponse>>> GetUserSessionAsync(string id, CancellationToken cancellationToken);

    Task<Result<string>> GetKeycloakAdminCliTokenAsync(CancellationToken cancellationToken);

    Task<Result<string>> GetClientIdAsync(string clientId, string adminToken,
        CancellationToken cancellationToken);
}