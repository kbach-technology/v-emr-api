using EMR.Domain.Enums;

namespace EMR.Application.Requests;

public record OtpRequest(string PhoneNumber, OTPAction otpAction);

public record EmailOtpRequest(string Email, OTPAction otpAction);

public record ValidateOtpRequest(string Code, string PhoneNumber);

public record ValidateEmailOtpRequest(string Code, string Email);

public record ResendOtpRequest(string PhoneNumber);

public record ResendEmailOtpRequest(string Email);