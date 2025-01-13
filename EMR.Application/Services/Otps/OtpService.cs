using System.Collections.Generic;
using EMR.Application.Abstractions;
using EMR.Application.Interfaces.Repositories;
using EMR.Application.Interfaces.Services;
using EMR.Application.Requests;
using EMR.Domain.Entities.Settings;
using EMR.Domain.Enums;
using EMR.Shared.Interfaces;
using Serilog;

namespace EMR.Application.Services;

public class OtpService : BaseService<OtpService>, IOtpService
{
    private readonly ISmsService _smsService;

    public OtpService(
        IUnitOfWork<string> unitOfWork,
        ICurrentUserService currentUserService,
        IStringLocalizer<OtpService> localizer,
        IDateTimeService dateTimeService,
        IMapper mapper,
        ILogger trace,
        ISmsService smsService) : base(unitOfWork, currentUserService, localizer, dateTimeService, mapper, trace)
    {
        _smsService = smsService;
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

            var otp = _mapper.Map<OTP>(request);
            otp.Id = Guid.NewGuid().ToString();
            otp.ExpiredOn = _dateTimeService.NowUtc.AddMinutes(2);
            otp.Action = request.otpAction;
            otp.IsValid = false;
            otp.Code = new Codes().GenerateUniqueOtp();
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

    public async Task<Result<string>> ResendOtpAsync(ResendOtpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var otp = _mapper.Map<OTP>(request);
            otp.Id = Guid.NewGuid().ToString();
            otp.ExpiredOn = _dateTimeService.NowUtc.AddMinutes(2);
            otp.Action = OTPAction.RegisterCode;
            otp.IsValid = false;
            otp.Code = new Codes().GenerateUniqueOtp();

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

    public async Task<Result<string>> ValidateOtpAsync(ValidateOtpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var otp = await _unitOfWork.Repository<OTP>().Entities
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

    private class Codes
    {
        private readonly HashSet<string> _generatedOtps = new();
        private readonly Random _random = new();

        public string GenerateUniqueOtp()
        {
            string otp;
            do
            {
                otp = _random.Next(100000, 999999).ToString();
            } while (_generatedOtps.Contains(otp));

            _generatedOtps.Add(otp);
            return otp;
        }
    }
}