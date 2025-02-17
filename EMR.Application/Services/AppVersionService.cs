using System.Collections.Generic;
using EMR.Application.Requests;
using EMR.Application.Responses;
using EMR.Domain.Entities.Settings;
using EMR.Domain.Enums;
using EMR.Shared.Interfaces;
using Serilog;

namespace EMR.Application.Services;

public class AppVersionService : IAppVersionService
{
    private readonly IUnitOfWork<string> _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IStringLocalizer<AppVersionService> _localizer;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger _logger;

    public AppVersionService(
        IUnitOfWork<string> unitOfWork,
        IMapper mapper,
        IStringLocalizer<AppVersionService> localizer,
        IDateTimeService dateTimeService,
        ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _localizer = localizer;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    public async Task<Result<string>> CreateAsync(AppVersionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var validation = ValidateVersionRequest(request);
            if (!validation.Succeeded) return validation;

            var appVersion = new AppVersion
            {
                Id = Guid.NewGuid().ToString(),
                VersionNumber = request.VersionNumber,
                BuildNumber = request.BuildNumber,
                Platform = request.Platform,
                UpdateMessage = request.UpdateMessage,
                IsForceUpdate = request.IsForceUpdate,
                ReleaseDate = _dateTimeService.NowUtc
            };

            await _unitOfWork.Repository<AppVersion>().AddAsync(appVersion);
            await _unitOfWork.CommitAndThrowOnFailure(cancellationToken);

            return await Result<string>.SuccessAsync(appVersion.Id, _localizer["AppVersion Created"]);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error creating app version");
            return await Result<string>.FailAsync(_localizer["Failed to create app version"]);
        }
    }

    public async Task<Result<string>> UpdateAsync(string id, AppVersionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var validation = ValidateVersionRequest(request);
            if (!validation.Succeeded) return validation;

            var appVersion = await GetAppVersionByIdAsync(id, cancellationToken);
            if (appVersion == null)
                return await Result<string>.FailAsync(_localizer["AppVersion Not Found"]);

            UpdateAppVersionProperties(appVersion, request);

            await _unitOfWork.Repository<AppVersion>().UpdateAsync(appVersion);
            await _unitOfWork.CommitAndThrowOnFailure(cancellationToken);

            return await Result<string>.SuccessAsync(appVersion.Id, _localizer["AppVersion Updated"]);
        }
        catch (DbUpdateConcurrencyException)
        {
            return await Result<string>.FailAsync(_localizer["AppVersion was modified by another user"]);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating app version");
            return await Result<string>.FailAsync(_localizer["Failed to update app version"]);
        }
    }

    public async Task<Result<string>> ToggleStatus(string id, CancellationToken cancellationToken)
    {
        try
        {
            var appVersion = await GetAppVersionByIdAsync(id, cancellationToken);
            if (appVersion == null)
                return await Result<string>.FailAsync(_localizer["AppVersion Not Found"]);

            appVersion.IsForceUpdate = !appVersion.IsForceUpdate;
            await _unitOfWork.Repository<AppVersion>().UpdateAsync(appVersion);
            await _unitOfWork.CommitAndThrowOnFailure(cancellationToken);

            return await Result<string>.SuccessAsync(appVersion.Id, _localizer["AppVersion Status Updated"]);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error toggling app version status");
            return await Result<string>.FailAsync(_localizer["Failed to toggle status"]);
        }
    }

    public async Task<Result<AppVersionResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        try
        {
            var appVersion = await GetAppVersionByIdAsync(id, cancellationToken);
            if (appVersion == null)
                return await Result<AppVersionResponse>.FailAsync(_localizer["AppVersion Not Found"]);

            var response = _mapper.Map<AppVersionResponse>(appVersion);
            return await Result<AppVersionResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error retrieving app version");
            return await Result<AppVersionResponse>.FailAsync(_localizer["Failed to retrieve app version"]);
        }
    }

    public async Task<PaginatedResult<AppVersionResponse>> GetAllAsync(int pageNumber, int pageSize,
        string? searchString)
    {
        try
        {
            var query = _unitOfWork.Repository<AppVersion>().Entities
                .WhereIf(!string.IsNullOrWhiteSpace(searchString),
                    x => x.VersionNumber.Contains(searchString));

            var paginatedResult = await query
                .OrderByDescending(x => x.ReleaseDate)
                .Select(x => _mapper.Map<AppVersionResponse>(x))
                .ToPaginatedListAsync(pageNumber, pageSize);

            return paginatedResult;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error retrieving app versions");
            var errors = new List<string> { _localizer["Failed to retrieve app versions"] };
            return PaginatedResult<AppVersionResponse>.Failure(errors);
        }
    }

    public async Task<Result<AppVersionResponse>> GetLatestVersionAsync(int platform)
    {
        try
        {
            var appVersion = await _unitOfWork.Repository<AppVersion>().Entities
                .Where(x => x.Platform == (Platform)platform)
                .OrderByDescending(x => x.ReleaseDate)
                .FirstOrDefaultAsync();

            if (appVersion == null)
                return await Result<AppVersionResponse>.FailAsync(_localizer["AppVersion Not Found"]);

            var response = _mapper.Map<AppVersionResponse>(appVersion);
            return await Result<AppVersionResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error retrieving latest app version");
            return await Result<AppVersionResponse>.FailAsync(_localizer["Failed to retrieve latest version"]);
        }
    }

    private Result<string> ValidateVersionRequest(AppVersionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VersionNumber))
            return Result<string>.Fail(_localizer["Version Number is required"]);

        if (string.IsNullOrWhiteSpace(request.BuildNumber.ToString()))
            return Result<string>.Fail(_localizer["Build Number is required"]);

        if (!Enum.IsDefined(typeof(Platform), request.Platform))
            return Result<string>.Fail(_localizer["Invalid Platform"]);

        return Result<string>.Success();
    }

    private async Task<AppVersion?> GetAppVersionByIdAsync(string id, CancellationToken cancellationToken)
    {
        return await _unitOfWork.Repository<AppVersion>().Entities
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    private void UpdateAppVersionProperties(AppVersion appVersion, AppVersionRequest request)
    {
        appVersion.VersionNumber = request.VersionNumber;
        appVersion.BuildNumber = request.BuildNumber;
        appVersion.Platform = request.Platform;
        appVersion.UpdateMessage = request.UpdateMessage;
        appVersion.IsForceUpdate = request.IsForceUpdate;
        appVersion.ReleaseDate = _dateTimeService.NowUtc;
    }
}