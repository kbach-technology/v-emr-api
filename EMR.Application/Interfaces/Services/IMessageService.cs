namespace EMR.Application.Interfaces.Services;

public interface IMessageService
{
    Task<int> SendDefaultMessageAsync();
    Task<int> SendApprovedMessageAsync(string userId, string doctorId);
}