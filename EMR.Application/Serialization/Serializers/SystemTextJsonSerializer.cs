using System.Text.Json;
using EMR.Application.Interfaces.Serialization.Serializers;
using EMR.Application.Serialization.Options;
using Microsoft.Extensions.Options;

namespace EMR.Application.Serialization.Serializers;

public class SystemTextJsonSerializer(IOptions<SystemTextJsonOptions> options) : IJsonSerializer
{
    private readonly JsonSerializerOptions _options = options.Value.JsonSerializerOptions;

    public T Deserialize<T>(string data)
    {
        return JsonSerializer.Deserialize<T>(data, _options);
    }

    public string Serialize<T>(T data)
    {
        return JsonSerializer.Serialize(data, _options);
    }
}