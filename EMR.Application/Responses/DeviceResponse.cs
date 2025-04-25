using EMR.Domain.Enums;

namespace EMR.Application.Responses;

public record DeviceResponse(
    string Id,
    string UserId,
    string DeviceToken,
    PlatformType PlatformType,
    string DeviceName,
    string Manufacturer,
    string UserAgent,
    string Model,
    string SerialNumber,
    string CreatedBy,
    DateTime CreatedOn
);