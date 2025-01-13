using System.Runtime.Serialization;

namespace EMR.Domain.Enums;

public enum Gender
{
    [EnumMember(Value = "male")] Male = 1,

    [EnumMember(Value = "female")] Female = 2,

    [EnumMember(Value = "other")] Other = 3
}