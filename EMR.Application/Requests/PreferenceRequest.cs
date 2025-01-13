namespace EMR.Application.Requests;

public record PreferenceRequest([Required] string UserId, [Required] string LanguageCode);