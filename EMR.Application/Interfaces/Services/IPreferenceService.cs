using EMR.Application.Requests;
using EMR.Application.Resources;

namespace EMR.Application.Interfaces.Services;

public interface IPreferenceService
{
    Task<Result<string>> AddAsync(PreferenceRequest request, CancellationToken cancellationToken);
    Task<Result<PerferenceResponse>> GetByIdAsync(CancellationToken cancellationToken);
}