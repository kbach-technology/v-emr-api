using System.Runtime.Serialization;

namespace EMR.Domain.Enums;

public enum Language
{
    [EnumMember(Value = "en-US")] EN = 1,

    [EnumMember(Value = "km-KH")] KM = 2
}