namespace EMR.Application.Requests.Identity;

public record ResetPinRequest(string LoginId, string NewPin);