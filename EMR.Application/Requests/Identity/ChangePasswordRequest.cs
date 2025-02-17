namespace EMR.Application.Requests.Identity;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record ValidateCurrentPasswordRequest(string CurrentPassword);