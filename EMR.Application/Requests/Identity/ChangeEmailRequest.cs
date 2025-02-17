namespace EMR.Application.Requests.Identity;

public record ChangeEmailRequest(string UserId, string NewEmail);