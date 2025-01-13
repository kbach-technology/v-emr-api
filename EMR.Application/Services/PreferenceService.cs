using EMR.Application.Abstractions;
using EMR.Application.Interfaces.Repositories;
using EMR.Application.Interfaces.Services;
using EMR.Application.Requests;
using EMR.Application.Resources;
using EMR.Domain.Entities.Settings;
using EMR.Shared.Interfaces;
using Serilog;

namespace EMR.Application.Services;

public class PreferenceService : BaseService<PreferenceService>, IPreferenceService
{
    public PreferenceService(
        IUnitOfWork<string> unitOfWork,
        ICurrentUserService currentUserService,
        IStringLocalizer<PreferenceService> localizer,
        IDateTimeService dateTimeService,
        IMapper mapper,
        ILogger trace) : base(unitOfWork, currentUserService, localizer, dateTimeService, mapper, trace)
    {
    }

    public async Task<Result<string>> AddAsync(PreferenceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var preference = await _unitOfWork.Repository<Preference>().Entities
                .FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);
            if (preference != null)
            {
                preference = _mapper.Map(request, preference);
                await _unitOfWork.Repository<Preference>().UpdateAsync(preference);
                var succeed = await _unitOfWork.Commit(cancellationToken);

                if (succeed > 0)
                    return await Result<string>.SuccessAsync(request.UserId, _localizer["Preference Updated"]);
                return await Result<string>.FailAsync(_localizer["Preference Not Updated"]);
            }
            else
            {
                preference = _mapper.Map<Preference>(request);
                preference.Id = Guid.NewGuid().ToString();
                await _unitOfWork.Repository<Preference>().AddAsync(preference);
                var succeed = await _unitOfWork.Commit(cancellationToken);

                if (succeed > 0)
                    return await Result<string>.SuccessAsync(request.UserId, _localizer["Preference Saved"]);
                return await Result<string>.FailAsync(_localizer["Preference Not Saved"]);
            }
        }
        catch (Exception ex)
        {
            _trace.Debug(ex, "Error occurred while saving preference.");
            return await Result<string>.FailAsync(_localizer["An error occurred while saving the preference."]);
        }
    }

    public async Task<Result<PerferenceResponse>> GetByIdAsync(CancellationToken cancellationToken)
    {
        var preference = await _unitOfWork.Repository<Preference>().Entities
            .Where(x => x.UserId == _currentUserService.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (preference == null)
            return await Result<PerferenceResponse>.FailAsync(_localizer["Preference not found."]);

        var response = _mapper.Map<PerferenceResponse>(preference);
        return await Result<PerferenceResponse>.SuccessAsync(response);
    }
}