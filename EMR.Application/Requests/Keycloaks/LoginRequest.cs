using EMR.Domain.Enums;

namespace EMR.Application.Requests.Keycloaks;

public record LoginRequest(string Identifier, string Pin, IdentifierType IdentifierType);