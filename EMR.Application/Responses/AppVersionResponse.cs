using EMR.Domain.Enums;

namespace EMR.Application.Responses;

public record AppVersionResponse(
    string Id,
    string VersionNumber,
    int BuildNumber,
    Platform Platform,
    string UpdateMessage,
    bool IsForceUpdate,
    DateTime ReleaseDate,
    string CreatedBy,
    DateTime CreatedOn
);