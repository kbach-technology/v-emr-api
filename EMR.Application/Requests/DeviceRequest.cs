using EMR.Domain.Enums;

namespace EMR.Application.Requests;

public record DeviceRequest(
    string DeviceToken,
    Platform Platform,
    string DeviceName,
    string Manufacturer,
    string UserAgent,
    string Model,
    string SerialNumber
);