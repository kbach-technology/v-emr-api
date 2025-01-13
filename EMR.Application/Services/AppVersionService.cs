using EMR.Application.Abstractions;
using EMR.Application.Requests;
using EMR.Application.Responses;
using EMR.Domain.Entities.Settings;
using EMR.Domain.Enums;
using EMR.Shared.Interfaces;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace EMR.Application.Services;

public class AppVersionService : BaseService<AppVersionService>, IAppVersionService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AppVersionService(
        IUnitOfWork<string> unitOfWork,
        ICurrentUserService currentUserService,
        IStringLocalizer<AppVersionService> localizer,
        IDateTimeService dateTimeService,
        IMapper mapper,
        ILogger logger,
        IHttpContextAccessor httpContextAccessor)
        : base(unitOfWork, currentUserService, localizer, dateTimeService, mapper, logger)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<string>> CreateAsync(AppVersionRequest request, CancellationToken cancellationToken)
    {
        var appVersion = _mapper.Map<AppVersion>(request);
        appVersion.Id = Guid.NewGuid().ToString();
        appVersion.ReleaseDate = _dateTimeService.NowUtc;

        await _unitOfWork.Repository<AppVersion>().AddAsync(appVersion);
        var succeed = await _unitOfWork.Commit(cancellationToken);

        return succeed > 0
            ? await Result<string>.SuccessAsync(appVersion.Id, _localizer["AppVersion Created"])
            : await Result<string>.FailAsync(_localizer["AppVersion Not Created"]);
    }

    public async Task<Result<string>> UpdateAsync(string id, AppVersionRequest request,
        CancellationToken cancellationToken)
    {
        var appVersion = await _unitOfWork.Repository<AppVersion>().Entities
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (appVersion == null) return await Result<string>.FailAsync(_localizer["AppVersion Not Found."]);

        appVersion.VersionNumber = request.VersionNumber;
        appVersion.BuildNumber = request.BuildNumber;
        appVersion.Platform = request.Platform;
        appVersion.UpdateMessage = request.UpdateMessage;
        appVersion.IsForceUpdate = request.IsForceUpdate;
        appVersion.ReleaseDate = _dateTimeService.NowUtc;

        await _unitOfWork.Repository<AppVersion>().UpdateAsync(appVersion);
        var succeed = await _unitOfWork.Commit(cancellationToken);

        if (succeed > 0)
            return await Result<string>.SuccessAsync(appVersion.Id, _localizer["AppVersion Updated"]);
        return await Result<string>.FailAsync(_localizer["AppVersion Not Updated"]);
    }

    public async Task<Result<string>> ToggleStatus(string id, CancellationToken cancellationToken)
    {
        var appVersion = await _unitOfWork.Repository<AppVersion>().Entities
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (appVersion == null) return await Result<string>.FailAsync(_localizer["AppVersion Not Found."]);

        appVersion.IsForceUpdate = !appVersion.IsForceUpdate;

        await _unitOfWork.Repository<AppVersion>().UpdateAsync(appVersion);
        await _unitOfWork.Commit(cancellationToken);
        return await Result<string>.SuccessAsync(appVersion.Id, _localizer["AppVersion Updated"]);
    }

    public async Task<Result<AppVersionResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        var appVersion = await _unitOfWork.Repository<AppVersion>().Entities
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (appVersion == null) return await Result<AppVersionResponse>.FailAsync(_localizer["AppVersion Not Found."]);

        var mappedAppVersion = _mapper.Map<AppVersionResponse>(appVersion);
        return await Result<AppVersionResponse>.SuccessAsync(mappedAppVersion);
    }

    public async Task<PaginatedResult<AppVersionResponse>> GetAllAsync(int pageNumber, int pageSize,
        string? searchString)
    {
        var appVersions = await _unitOfWork.Repository<AppVersion>().Entities
            .Where(x => string.IsNullOrWhiteSpace(searchString) || x.VersionNumber.Contains(searchString))
            .Select(app => new AppVersionResponse(
                app.Id,
                app.VersionNumber,
                app.BuildNumber,
                app.Platform,
                app.UpdateMessage,
                app.IsForceUpdate,
                app.ReleaseDate,
                app.CreatedBy,
                app.CreatedOn)
            )
            .ToPaginatedListAsync(pageNumber, pageSize);

        return appVersions;
    }

    public async Task<Result<AppVersionResponse>> GetLatestVersionAsync(int platform)
    {
        var appVersion = await _unitOfWork.Repository<AppVersion>().Entities
            .FirstOrDefaultAsync(x => x.Platform == (Platform)platform);
        if (appVersion == null) return await Result<AppVersionResponse>.FailAsync(_localizer["AppVersion Not Found."]);

        var mappedAppVersion = _mapper.Map<AppVersionResponse>(appVersion);
        return await Result<AppVersionResponse>.SuccessAsync(mappedAppVersion);
    }
}