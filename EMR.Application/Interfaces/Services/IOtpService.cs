using EMR.Application.Requests;

namespace EMR.Application.Interfaces.Services;

public interface IOtpService
{
    Task<Result<string>> RequestOtpAsync(OtpRequest request, CancellationToken cancellationToken);
    Task<Result<string>> RequestEmailOtpAsync(EmailOtpRequest request, CancellationToken cancellationToken);
    Task<Result<string>> ResendOtpAsync(ResendOtpRequest request, CancellationToken cancellationToken);
    Task<Result<string>> ResendEmailOtpAsync(ResendEmailOtpRequest request, CancellationToken cancellationToken);
    Task<Result<string>> ValidateOtpAsync(ValidateOtpRequest request, CancellationToken cancellationToken);
    Task<Result<string>> ValidateEmailOtpAsync(ValidateEmailOtpRequest request, CancellationToken cancellationToken);
    Task<Result<string>> IsVerifiedOtp(string phoneNumber, CancellationToken cancellationToken);
    Task<Result<string>> IsVerifiedEmailOtp(string email, CancellationToken cancellationToken);
}