using EMR.Domain.Enums;

namespace EMR.Application.Requests;

public record AppVersionRequest(
    string VersionNumber,
    int BuildNumber,
    Platform Platform,
    string UpdateMessage,
    bool IsForceUpdate
);