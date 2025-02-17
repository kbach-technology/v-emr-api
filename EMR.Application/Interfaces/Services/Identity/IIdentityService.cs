using EMR.Application.Requests.Identity;
using EMR.Application.Requests.Keycloaks;
using EMR.Application.Responses.Identity;

namespace EMR.Application.Interfaces.Services.Identity;

public interface IIdentityService
{
    Task<Result<string>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<Result<string>> AmendUserAsync(AmendUserRequest request, CancellationToken cancellationToken);
    Task<Result<string>> ToggleUserAsync(string id, CancellationToken cancellationToken);
    Task<PaginatedResult<GetUsersResponse>> GetUsersAsync(int pageNumber, int pageSize, bool? isActive, string? searchString, CancellationToken cancellationToken);
    Task<Result<GetUserResponse>> GetUserAsync(string id, CancellationToken cancellationToken);
    Task<Result<string>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken);
    Task<Result<string>> SelfResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
    Task<Result<string>> AdminResetUserPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
}