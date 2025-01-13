namespace EMR.Application.Interfaces.Services;

public interface IValidatorService
{
    Task<bool> ValidateUserStateAsync(string userId);
}