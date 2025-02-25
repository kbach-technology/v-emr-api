using AutoMapper;
using EMR.Application.Abstractions;
using EMR.Application.Interfaces.Services.Identity;
using EMR.Application.Requests.Identity;
using EMR.Application.Requests.Keycloaks;
using EMR.Application.Responses.Identity;
using EMR.Domain.Entities.Users;
using EMR.Domain.Shared;
using EMR.Shared.Constants.Prefix;
using EMR.Shared.Interfaces;
using EMR.Shared.Wrapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;

namespace EMR.Application.Services.Identity;

public class IdentityService : BaseService<IdentityService>, IIdentityService
{
    private readonly INumericService _numericService;
    private readonly IKeycloakService _keycloakService;
    private readonly IOtpService _otpService;

    public IdentityService(
        IUnitOfWork<string> unitOfWork,
        ICurrentUserService currentUserService,
        IStringLocalizer<IdentityService> localizer,
        IDateTimeService dateTimeService,
        IMapper mapper,
        ILogger trace,
        INumericService numericService,
        IKeycloakService keycloakService,
        IOtpService otpService)
        : base(unitOfWork, currentUserService, localizer, dateTimeService, mapper, trace)
    {
        _numericService = numericService;
        _keycloakService = keycloakService;
        _otpService = otpService;
    }

    public async Task<Result<string>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var number = await _numericService.GenerateNumberAsync<User>(x => x.UserNo, Prefix.UserId, 2);

            if (await CheckExitsUserAsync(request, cancellationToken))
                return await Result<string>.FailAsync(_localizer["User with email {0} already exists", request.Email]);

            var keycloak =
                await _keycloakService.CreatAsync(
                    new KeyCloakCreateUserRequest(request.FullName, request.Email, request.Password, ""),
                    cancellationToken);

            if (keycloak == null)
                return await Result<string>.FailAsync(_localizer["Identity user not created"]);

            var user = new User
            {
                Id = keycloak.Data,
                KeycloakId = keycloak.Data,
                UserNo = number,
                Email = request.Email,
                Phone = request.Phone,
                FullName = request.FullName,
                Gender = Gender.Create(request.Gender),
                DateOfBirth = DateOfBirth.Create(request.DateOfBirth),
                ProImg = request.ProImg,
                IsActive = true
            };

            await _unitOfWork.Repository<User>().AddAsync(user);
            await _unitOfWork.Commit(cancellationToken);

            return await Result<string>.SuccessAsync(user.Id, _localizer["User created"]);
        }
        catch (Exception ex)
        {
            _trace.Error(ex.Message);
            return await Result<string>.FailAsync(ex.Message);
        }
    }

    public async Task<Result<string>> AmendUserAsync(AmendUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _unitOfWork.Repository<User>().Entities
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            if (user == null)
                return await Result<string>.FailAsync(_localizer["User not found"]);

            user.FullName = request.FullName;
            user.Phone = request.Phone;
            user.Email = request.Email;
            user.Gender = Gender.Create(request.Gender);
            user.DateOfBirth = DateOfBirth.Create(request.DateOfBirth);
            user.ProImg = request.ProImg;
            user.IsActive = request.IsActive;

            await _unitOfWork.Repository<User>().UpdateAsync(user);
            await _unitOfWork.Commit(cancellationToken);

            var keycloakResult = await _keycloakService.UpdateUserAsync(request.Id, request.Email, cancellationToken);
            if (!keycloakResult.Succeeded)
                _trace.Warning($"Keycloak update failed for user {request.Id}: {keycloakResult.Message}");

            return await Result<string>.SuccessAsync(user.Id, _localizer["User updated"]);
        }
        catch (Exception ex)
        {
            _trace.Error(ex.Message);
            return await Result<string>.FailAsync(ex.Message);
        }
    }

    public async Task<Result<string>> ToggleUserAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _unitOfWork.Repository<User>().Entities
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (user == null)
                return await Result<string>.FailAsync(_localizer["User not found"]);

            user.IsActive = !user.IsActive;

            await _unitOfWork.Repository<User>().UpdateAsync(user);
            await _unitOfWork.Commit(cancellationToken);

            return await Result<string>.SuccessAsync(user.Id, _localizer["User status updated"]);
        }
        catch (Exception ex)
        {
            _trace.Error(ex.Message);
            return await Result<string>.FailAsync(ex.Message);
        }
    }

    public async Task<PaginatedResult<GetUsersResponse>> GetUsersAsync(int pageNumber, int pageSize, bool? isActive,
        string? searchString, CancellationToken cancellationToken)
    {
        var data = await _unitOfWork.Repository<User>().Entities
            .Where(x => x.IsActive == isActive || isActive == null)
            .Where(x => (string.IsNullOrEmpty(searchString)
                         || x.FullName.Contains(searchString)
                         || x.Email.Contains(searchString)
                         || x.Phone.Contains(searchString)
                         || x.UserNo.Contains(searchString)))
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedOn)
            .Select(x => new GetUsersResponse
            (
                x.Id,
                x.UserNo,
                x.FullName,
                x.Email,
                x.Phone,
                x.DateOfBirth,
                x.Gender,
                x.ProImg,
                x.IsActive
            ))
            .AsNoTracking()
            .ToPaginatedListAsync(pageNumber, pageSize);

        return data;
    }

    public async Task<Result<GetUserResponse>> GetUserAsync(string id, CancellationToken cancellationToken)
    {
        var data = await _unitOfWork.Repository<User>().Entities
            .Select(x => new GetUserResponse
            {
                Id = x.Id,
                UserNo = x.UserNo,
                FullName = x.FullName,
                Email = x.Email,
                Phone = x.Phone,
                DateOfBirth = x.DateOfBirth,
                Gender = x.Gender,
                ProImg = x.ProImg,
                IsActive = x.IsActive,
                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy,
                LastModifiedBy = x.LastModifiedBy,
                LastModifiedOn = x.LastModifiedOn
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (data == null)
            return await Result<GetUserResponse>.FailAsync(_localizer["User not found"]);

        return await Result<GetUserResponse>.SuccessAsync(data, _localizer["User found"]);
    }

    public async Task<Result<string>> ChangePasswordAsync(ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var changePassword = await _keycloakService.ChangePasswordAsync(request, cancellationToken);

        if (!changePassword.Succeeded)
            return await Result<string>.FailAsync(_localizer["Password not changed"]);

        return await Result<string>.SuccessAsync(_localizer["Password changed"]);
    }

    public async Task<Result<string>> SelfResetPasswordAsync(ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        // Before allow to reset password, ensure that user confirmed OTP code send to email
        var confirmOtp = await _otpService.IsVerifiedEmailOtp(request.Email, cancellationToken);

        if (!confirmOtp.Succeeded)
            return await Result<string>.FailAsync(_localizer["OTP code not confirmed"]);

        var resetPassword = await _keycloakService.ResetPasswordAsync(request, cancellationToken);

        if (!resetPassword.Succeeded)
            return await Result<string>.FailAsync(_localizer[resetPassword.Message]);
        return await Result<string>.SuccessAsync(_localizer["Password reset"]);
    }

    public async Task<Result<string>> AdminResetUserPasswordAsync(ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var resetPassword = await _keycloakService.ResetPasswordAsync(request, cancellationToken);

        if (!resetPassword.Succeeded)
            return await Result<string>.FailAsync(_localizer[resetPassword.Message]);
        return await Result<string>.SuccessAsync(_localizer["Password reset"]);
    }

    private async Task<bool> CheckExitsUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.Repository<User>().Entities
            .AnyAsync(x => x.Email == request.Email, cancellationToken);
    }
}