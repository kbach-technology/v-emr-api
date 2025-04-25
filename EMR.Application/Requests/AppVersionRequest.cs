using EMR.Domain.Enums;

namespace EMR.Application.Requests;

public record AppVersionRequest(
    string VersionNumber,
    int BuildNumber,
    PlatformType PlatformType,
    string UpdateMessage,
    bool IsForceUpdate
);