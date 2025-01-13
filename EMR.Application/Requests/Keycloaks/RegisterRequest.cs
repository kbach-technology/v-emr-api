using EMR.Domain.Enums;

namespace EMR.Application.Requests.Keycloaks;

public record RegisterRequest(
    string Identifier,
    string Pin,
    IdentifierType IdentifierType,
    string RoleName,
    bool IsExternalLogin);