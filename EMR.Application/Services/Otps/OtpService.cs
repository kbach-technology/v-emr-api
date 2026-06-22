using System.Collections.Generic;
using EMR.Application.Abstractions;
using EMR.Application.Requests;
using EMR.Domain.Entities.Settings;
using EMR.Domain.Entities.Users;
using EMR.Domain.Enums;
using EMR.Shared.Interfaces;
using Serilog;

namespace EMR.Application.Services.Otps;

public class OtpService : BaseService<OtpService>, IOtpService
{
    private readonly ISmsService _smsService;
    private readonly ISendGridService _sendGridService;

    public OtpService(
        IUnitOfWork<string> unitOfWork,
        ICurrentUserService currentUserService,
        IStringLocalizer<OtpService> localizer,
        IDateTimeService dateTimeService,
        IMapper mapper,
        ILogger trace,
        ISmsService smsService,
        ISendGridService sendGridService) : base(unitOfWork, currentUserService, localizer, dateTimeService, mapper,
        trace)
    {
        _smsService = smsService;
        _sendGridService = sendGridService;
    }

    public async Task<Result<string>> RequestOtpAsync(OtpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _unitOfWork.Repository<OTP>().Entities
                .FirstOrDefaultAsync(x =>
                    x.PhoneNumber == request.PhoneNumber
                    && x.Action == OTPAction.RegisterCode
                    && x.IsValid == false
                    && x.ExpiredOn > _dateTimeService.NowUtc, cancellationToken);

            if (existing != null) return await Result<string>.SuccessAsync(_localizer["Otp Already Sent"]);

            // Generate OTP with retry for collision handling
            string otpCode;
            bool isDuplicate;
            int maxAttempts = 5;
            int attempt = 0;

            do
            {
                otpCode = GenerateRandomOtp();

                // Check if this OTP code already exists for this phone and is still valid
                isDuplicate = await _unitOfWork.Repository<OTP>().Entities
                    .AnyAsync(x =>
                        x.PhoneNumber == request.PhoneNumber
                        && x.Code == otpCode
                        && x.ExpiredOn > _dateTimeService.NowUtc,
                        cancellationToken);

                attempt++;
            } while (isDuplicate && attempt < maxAttempts);

            if (isDuplicate)
            {
                // Extremely rare - 1 in 1 million chance per attempt
                _trace.Warning("Failed to generate unique OTP after {MaxAttempts} attempts for phone {PhoneNumber}",
                    maxAttempts, request.PhoneNumber);
                return await Result<string>.FailAsync(_localizer["Unable to generate OTP, please try again"]);
            }

            var otp = _mapper.Map<OTP>(request);
            otp.Id = Guid.NewGuid().ToString();
            otp.ExpiredOn = _dateTimeService.NowUtc.AddMinutes(2);
            otp.Action = request.otpAction;
            otp.IsValid = false;
            otp.Code = otpCode;
            otp.IpAddress = _currentUserService.CurrentIp;

            await _unitOfWork.Repository<OTP>().AddAsync(otp);
            var succeed = await _unitOfWork.Commit(cancellationToken);

            if (succeed > 0)
            {
                _smsService.SendOtpAsync(otp.PhoneNumber, otp.Code);
                return await Result<string>.SuccessAsync(_localizer["Otp Saved"]);
            }

            return await Result<string>.FailAsync(_localizer["Otp Not Saved"]);
        }
        catch (Exception e)
        {
            _trace.Error(e, e.Message);
            return await Result<string>.FailAsync(_localizer["Otp Not Saved"]);
        }
    }

    public async Task<Result<string>> RequestEmailOtpAsync(EmailOtpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var account = await _unitOfWork.Repository<User>().Entities
                .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

            if (account == null)
                return await Result<string>.FailAsync(_localizer["User with email {0} not found", request.Email]);

            var existing = await _unitOfWork.Repository<OTP>().Entities
                .FirstOrDefaultAsync(x =>
                    x.Email == request.Email
                    && x.Action == request.otpAction
                    && x.IsValid == false
                    && x.ExpiredOn > _dateTimeService.NowUtc, cancellationToken);

            if (existing != null)
            {
                await _sendGridService.SendPinEmailAsync(request.Email, account.FullName, "Password reset requested",
                    existing.Code,
                    cancellationToken);

                return await Result<string>.SuccessAsync(_localizer["Code Already Sent"]);
            }

            // Generate OTP with retry for collision handling
            string otpCode;
            bool isDuplicate;
            int maxAttempts = 5;
            int attempt = 0;

            do
            {
                otpCode = GenerateRandomOtp();

                // Check if this OTP code already exists for this email and is still valid
                isDuplicate = await _unitOfWork.Repository<OTP>().Entities
                    .AnyAsync(x =>
                        x.Email == request.Email
                        && x.Code == otpCode
                        && x.ExpiredOn > _dateTimeService.NowUtc,
                        cancellationToken);

                attempt++;
            } while (isDuplicate && attempt < maxAttempts);

            if (isDuplicate)
            {
                _trace.Warning("Failed to generate unique OTP after {MaxAttempts} attempts for email {Email}",
                    maxAttempts, request.Email);
                return await Result<string>.FailAsync(_localizer["Unable to generate OTP, please try again"]);
            }

            var otp = _mapper.Map<OTP>(request);
            otp.Id = Guid.NewGuid().ToString();
            otp.ExpiredOn = _dateTimeService.NowUtc.AddMinutes(5);
            otp.Action = request.otpAction;
            otp.IsValid = false;
            otp.Code = otpCode;
            otp.IpAddress = _currentUserService.CurrentIp;

            await _unitOfWork.Repository<OTP>().AddAsync(otp);
            await _unitOfWork.Commit(cancellationToken);

            await _sendGridService.SendPinEmailAsync(request.Email, account.FullName, "Password reset requested",
                otp.Code, // Use the same OTP code that was stored
                cancellationToken);

            return await Result<string>.SuccessAsync(_localizer["Otp Saved"]);
        }
        catch (Exception e)
        {
            _trace.Error(e, e.Message);
            return await Result<string>.FailAsync(_localizer[e.Message]);
        }
    }

    public async Task<Result<string>> ResendOtpAsync(ResendOtpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Generate OTP with retry for collision handling
            string otpCode;
            bool isDuplicate;
            int maxAttempts = 5;
            int attempt = 0;

            do
            {
                otpCode = GenerateRandomOtp();

                // Check if this OTP code already exists for this phone and is still valid
                isDuplicate = await _unitOfWork.Repository<OTP>().Entities
                    .AnyAsync(x =>
                        x.PhoneNumber == request.PhoneNumber
                        && x.Code == otpCode
                        && x.ExpiredOn > _dateTimeService.NowUtc,
                        cancellationToken);

                attempt++;
            } while (isDuplicate && attempt < maxAttempts);

            if (isDuplicate)
            {
                _trace.Warning("Failed to generate unique OTP after {MaxAttempts} attempts for phone {PhoneNumber}",
                    maxAttempts, request.PhoneNumber);
                return await Result<string>.FailAsync(_localizer["Unable to generate OTP, please try again"]);
            }

            var otp = _mapper.Map<OTP>(request);
            otp.Id = Guid.NewGuid().ToString();
            otp.ExpiredOn = _dateTimeService.NowUtc.AddMinutes(5);
            otp.Action = OTPAction.RegisterCode;
            otp.IsValid = false;
            otp.Code = otpCode;

            await _unitOfWork.Repository<OTP>().AddAsync(otp);
            await _unitOfWork.Commit(cancellationToken);

            _smsService.SendOtpAsync(otp.PhoneNumber, otp.Code);
            return await Result<string>.SuccessAsync(_localizer["Otp Saved"]);
        }
        catch (Exception e)
        {
            _trace.Error(e, e.Message);
            return await Result<string>.FailAsync(_localizer["Otp Not Saved"]);
        }
    }

    public async Task<Result<string>> ResendEmailOtpAsync(ResendEmailOtpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var account = await _unitOfWork.Repository<User>().Entities
                .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

            if (account == null)
                return await Result<string>.FailAsync(_localizer["User with email {0} not found", request.Email]);

            var existing = await _unitOfWork.Repository<OTP>().Entities
                .FirstOrDefaultAsync(x =>
                    x.Email == request.Email
                    && x.Action == OTPAction.ResendCode
                    && x.IsValid == false
                    && x.ExpiredOn > _dateTimeService.NowUtc, cancellationToken);

            if (existing != null)
            {
                await _sendGridService.SendPinEmailAsync(request.Email, account.FullName, "Password reset requested",
                    existing.Code,
                    cancellationToken);

                return await Result<string>.SuccessAsync(_localizer["Code Already Sent"]);
            }

            // Generate OTP with retry for collision handling
            string otpCode;
            bool isDuplicate;
            int maxAttempts = 5;
            int attempt = 0;

            do
            {
                otpCode = GenerateRandomOtp();

                // Check if this OTP code already exists for this email and is still valid
                isDuplicate = await _unitOfWork.Repository<OTP>().Entities
                    .AnyAsync(x =>
                        x.Email == request.Email
                        && x.Code == otpCode
                        && x.ExpiredOn > _dateTimeService.NowUtc,
                        cancellationToken);

                attempt++;
            } while (isDuplicate && attempt < maxAttempts);

            if (isDuplicate)
            {
                _trace.Warning("Failed to generate unique OTP after {MaxAttempts} attempts for email {Email}",
                    maxAttempts, request.Email);
                return await Result<string>.FailAsync(_localizer["Unable to generate OTP, please try again"]);
            }

            var otp = _mapper.Map<OTP>(request);
            otp.Id = Guid.NewGuid().ToString();
            otp.ExpiredOn = _dateTimeService.NowUtc.AddMinutes(5);
            otp.Action = OTPAction.ResendCode;
            otp.IsValid = false;
            otp.Code = otpCode;

            await _unitOfWork.Repository<OTP>().AddAsync(otp);
            await _unitOfWork.Commit(cancellationToken);

            await _sendGridService.SendPinEmailAsync(request.Email, account.FullName,
                _localizer["Password reset requested"],
                otp.Code, // Use the same OTP code that was stored
                cancellationToken);
            return await Result<string>.SuccessAsync(_localizer["Code Saved"]);
        }
        catch (Exception e)
        {
            _trace.Error(e, e.Message);
            return await Result<string>.FailAsync(_localizer["Code Not Saved"]);
        }
    }

    public async Task<Result<string>> ValidateOtpAsync(ValidateOtpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var otp = await _unitOfWork.Repository<OTP>().Entities
                .OrderByDescending(x => x.CreatedOn)
                .FirstOrDefaultAsync(x =>
                    x.PhoneNumber == request.PhoneNumber
                    && x.Code == request.Code
                    && x.IsValid == false
                    && x.ExpiredOn > _dateTimeService.NowUtc, cancellationToken);

            if (otp == null) return await Result<string>.FailAsync(_localizer["Invalid Otp"]);

            otp.IsValid = true;
            await _unitOfWork.Repository<OTP>().UpdateAsync(otp);
            var succeed = await _unitOfWork.Commit(cancellationToken);

            if (succeed > 0) return await Result<string>.SuccessAsync(_localizer["Otp Validated"]);

            return await Result<string>.FailAsync(_localizer["Otp Not Validated"]);
        }
        catch (Exception e)
        {
            _trace.Error(e, e.Message);
            return await Result<string>.FailAsync(_localizer["Otp Not Validated"]);
        }
    }

    public async Task<Result<string>> ValidateEmailOtpAsync(ValidateEmailOtpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var otp = await _unitOfWork.Repository<OTP>().Entities
                .OrderByDescending(x => x.ExpiredOn)
                .Where(x =>
                    x.Email == request.Email
                    && x.IsValid == false
                    && x.ExpiredOn > _dateTimeService.NowUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (otp == null || otp.Code != request.Code)
                return await Result<string>.FailAsync(_localizer["Invalid Code"]);

            otp.IsValid = true;
            await _unitOfWork.Repository<OTP>().UpdateAsync(otp);
            await _unitOfWork.Commit(cancellationToken);

            return await Result<string>.SuccessAsync(_localizer["Code Validated"]);
        }
        catch (Exception e)
        {
            _trace.Error(e, e.Message);
            return await Result<string>.FailAsync(_localizer["Code Not Validated"]);
        }
    }

    public async Task<Result<string>> IsVerifiedOtp(string phoneNumber, CancellationToken cancellationToken)
    {
        try
        {
            var otp = await _unitOfWork.Repository<OTP>().Entities
                .FirstOrDefaultAsync(x =>
                    x.PhoneNumber == phoneNumber
                    && x.IsValid == false
                    && x.ExpiredOn > _dateTimeService.NowUtc, cancellationToken);

            return otp == null
                ? await Result<string>.SuccessAsync(_localizer["Otp Verified"])
                : await Result<string>.FailAsync(_localizer["Otp Not Verified"]);
        }
        catch (Exception e)
        {
            _trace.Error(e, e.Message);
            return await Result<string>.FailAsync(_localizer["Otp Not Verified"]);
        }
    }

    public async Task<Result<string>> IsVerifiedEmailOtp(string email, CancellationToken cancellationToken)
    {
        try
        {
            var otp = await _unitOfWork.Repository<OTP>().Entities
                .OrderByDescending(x => x.ExpiredOn)
                .FirstOrDefaultAsync(x =>
                    x.Email == email
                    && x.ExpiredOn > _dateTimeService.NowUtc, cancellationToken);

            // If there's no OTP or the latest OTP is marked as valid, then it's verified
            return (otp == null || otp.IsValid)
                ? await Result<string>.SuccessAsync(_localizer["Otp Verified"])
                : await Result<string>.FailAsync(_localizer["Otp Not Verified"]);
        }
        catch (Exception e)
        {
            _trace.Error(e, e.Message);
            return await Result<string>.FailAsync(_localizer["Code Not Verified"]);
        }
    }


    /// <summary>
    /// Generates a random 6-digit OTP code.
    /// Database constraints ensure uniqueness within validity window.
    /// </summary>
    private static string GenerateRandomOtp()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }
}