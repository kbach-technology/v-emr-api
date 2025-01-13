namespace EMR.Application.Interfaces.Services;

public interface ISmsService
{
    void SendOtpAsync(string receiver, string code);
}