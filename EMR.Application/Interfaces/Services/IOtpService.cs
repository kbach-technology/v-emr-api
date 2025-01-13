using EMR.Application.Requests;

namespace EMR.Application.Interfaces.Services;

public interface IOtpService
{
    Task<Result<string>> RequestOtpAsync(OtpRequest request, CancellationToken cancellationToken);
    Task<Result<string>> ResendOtpAsync(ResendOtpRequest request, CancellationToken cancellationToken);
    Task<Result<string>> ValidateOtpAsync(ValidateOtpRequest request, CancellationToken cancellationToken);

    Task<Result<string>> IsVerifiedOtp(string phoneNumber, CancellationToken cancellationToken);
}