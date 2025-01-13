using System.Text.Json;
using EMR.Application.Interfaces.Serialization.Options;

namespace EMR.Application.Serialization.Options;

public class SystemTextJsonOptions : IJsonSerializerOptions
{
    public JsonSerializerOptions JsonSerializerOptions { get; } = new();
}