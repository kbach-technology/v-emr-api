using EMR.Application.Requests;
using EMR.Application.Responses;

namespace EMR.Application.Interfaces.Services.Devices;

public interface IDeviceService
{
    Task<PaginatedResult<DeviceResponse>> GetAllAsync(int pageNumber, int pageSize,
        string? searchString, CancellationToken cancellationToken);

    Task<Result<string>> AddAsync(DeviceRequest request, CancellationToken cancellationToken);
    Task<Result<string>> DeleteAsync(string deviceToken, CancellationToken cancellationToken);
}