using EMR.Application.Requests;
using EMR.Application.Responses;

namespace EMR.Application.Interfaces.Services;

public interface IAppVersionService
{
    Task<Result<AppVersionResponse>> GetLatestVersionAsync(int platform);
    Task<Result<string>> CreateAsync(AppVersionRequest request, CancellationToken cancellationToken);
    Task<Result<string>> UpdateAsync(string id, AppVersionRequest request, CancellationToken cancellationToken);
    Task<Result<string>> ToggleStatus(string id, CancellationToken cancellationToken);

    Task<PaginatedResult<AppVersionResponse>> GetAllAsync(int pageNumber, int pageSize,
        string? searchString);

    Task<Result<AppVersionResponse>> GetById(string id, CancellationToken cancellationToken);
}