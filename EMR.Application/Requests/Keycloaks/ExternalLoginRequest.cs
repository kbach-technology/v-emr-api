namespace EMR.Application.Requests.Keycloaks;

public record ExternalLoginRequest([Required] string PhoneNumber, [Required] string Pin);