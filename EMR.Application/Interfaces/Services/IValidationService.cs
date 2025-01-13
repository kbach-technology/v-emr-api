namespace EMR.Application.Interfaces.Services;

public interface IValidationService
{
    bool IsValidPhoneNumber(string phoneNumber);
    bool IsValidEmailAddress(string emailAddress);
}