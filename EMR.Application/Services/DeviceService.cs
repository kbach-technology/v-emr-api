using EMR.Application.Abstractions;
using EMR.Application.Interfaces.Repositories;
using EMR.Application.Interfaces.Services.Devices;
using EMR.Application.Requests;
using EMR.Application.Responses;
using EMR.Domain.Entities.Settings;
using EMR.Shared.Interfaces;
using Serilog;

namespace EMR.Application.Services;

public class DeviceService : BaseService<DeviceService>, IDeviceService
{
    public DeviceService(
        IUnitOfWork<string> unitOfWork,
        ICurrentUserService currentUserService,
        IStringLocalizer<DeviceService> localizer,
        IDateTimeService dateTimeService,
        IMapper mapper,
        ILogger trace) : base(unitOfWork, currentUserService, localizer, dateTimeService, mapper, trace)
    {
    }

    public async Task<Result<string>> AddAsync(DeviceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var succeed = 0;
            var existing = await _unitOfWork.Repository<Device>().Entities
                .FirstOrDefaultAsync(x =>
                    x.UserId == _currentUserService.UserId
                    && x.Platform == request.Platform, cancellationToken);
            if (existing != null)
            {
                existing.UserId = _currentUserService.UserId;
                existing.DeviceToken = request.DeviceToken;
                existing.DeviceName = request.DeviceName;
                existing.Manufacturer = request.Manufacturer;
                existing.UserAgent = request.UserAgent;
                existing.Model = request.Model;
                existing.SerialNumber = request.SerialNumber;

                await _unitOfWork.Repository<Device>().UpdateAsync(existing);
                succeed = await _unitOfWork.Commit(cancellationToken);

                if (succeed > 0)
                    return await Result<string>.SuccessAsync(_localizer["Device Updated"]);
                return await Result<string>.FailAsync(_localizer["Device Not Updated"]);
            }

            var device = _mapper.Map<Device>(request);
            device.Id = Guid.NewGuid().ToString();
            device.UserId = _currentUserService.UserId;

            await _unitOfWork.Repository<Device>().AddAsync(device);
            succeed = await _unitOfWork.Commit(cancellationToken);

            if (succeed > 0)
                return await Result<string>.SuccessAsync(_localizer["Device Saved"]);
            return await Result<string>.FailAsync(_localizer["Device Not Saved"]);
        }
        catch (Exception ex)
        {
            _trace.Debug(ex, "Error occurred while saving device.");
            return await Result<string>.FailAsync(_localizer["An error occurred while saving the device."]);
        }
    }

    public async Task<Result<string>> DeleteAsync(string deviceToken, CancellationToken cancellationToken)
    {
        var device = await _unitOfWork.Repository<Device>().Entities
            .FirstOrDefaultAsync(x => x.DeviceToken == deviceToken, cancellationToken);
        if (device == null)
            return await Result<string>.FailAsync(_localizer["Device not found."]);

        await _unitOfWork.Repository<Device>().DeleteAsync(device);
        var succeed = await _unitOfWork.Commit(cancellationToken);

        if (succeed > 0)
            return await Result<string>.SuccessAsync(_localizer["Logout Successful"]);
        return await Result<string>.FailAsync(_localizer["Device Not Deleted"]);
    }

    public async Task<PaginatedResult<DeviceResponse>> GetAllAsync(int pageNumber, int pageSize,
        string? searchString, CancellationToken cancellationToken)
    {
        var devices = await _unitOfWork.Repository<Device>().Entities
            .Where(x => string.IsNullOrWhiteSpace(searchString)
                        || x.UserId.Contains(searchString))
            .OrderByDescending(x => x.CreatedOn)
            .AsNoTracking()
            .Select(c => new DeviceResponse(
                    c.Id,
                    c.UserId,
                    c.DeviceToken,
                    c.Platform,
                    c.DeviceName,
                    c.Manufacturer,
                    c.UserAgent,
                    c.Model,
                    c.SerialNumber,
                    c.CreatedBy,
                    c.CreatedOn
                )
            )
            .ToPaginatedListAsync(pageNumber, pageSize);

        return devices;
    }
}