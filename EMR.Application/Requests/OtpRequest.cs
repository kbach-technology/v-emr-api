using EMR.Domain.Enums;

namespace EMR.Application.Requests;

public record OtpRequest(string PhoneNumber, OTPAction otpAction);

public record ValidateOtpRequest(string Code, string PhoneNumber);

public record ResendOtpRequest(string PhoneNumber);