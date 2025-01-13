using EMR.Application.Interfaces.Serialization.Settings;
using Newtonsoft.Json;

namespace EMR.Application.Serialization.Settings;

public class NewtonsoftJsonSettings : IJsonSerializerSettings
{
    public JsonSerializerSettings JsonSerializerSettings { get; } = new();
}