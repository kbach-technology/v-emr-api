using EMR.Application.Interfaces.Serialization.Serializers;
using EMR.Application.Serialization.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace EMR.Application.Serialization.Serializers;

public class NewtonSoftJsonSerializer(IOptions<NewtonsoftJsonSettings> settings) : IJsonSerializer
{
    private readonly JsonSerializerSettings _settings = settings.Value.JsonSerializerSettings;

    public T Deserialize<T>(string text)
    {
        return JsonConvert.DeserializeObject<T>(text, _settings);
    }

    public string Serialize<T>(T obj)
    {
        return JsonConvert.SerializeObject(obj, _settings);
    }
}