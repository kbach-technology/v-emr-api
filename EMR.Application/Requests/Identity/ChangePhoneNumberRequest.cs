namespace EMR.Application.Requests.Identity;

public record ChangeLoginIdRequest(string UserId, string NewUsername);