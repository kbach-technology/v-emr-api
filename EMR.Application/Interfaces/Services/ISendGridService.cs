namespace EMR.Application.Interfaces.Services;

public interface ISendGridService
{
    Task SendGridEmailAsync(string email, string subject, string message, CancellationToken cancellationToken);

    Task SendPinEmailAsync(string to, string toUsername, string subject, string body,
        CancellationToken cancellationToken);

    Task SendUsernameEmailAsync(string to, string toUsername, string subject, string body,
        CancellationToken cancellationToken);
}