namespace EMR.Application.Requests.Identity;

public record ResetPasswordRequest(string Email, string NewPassword);