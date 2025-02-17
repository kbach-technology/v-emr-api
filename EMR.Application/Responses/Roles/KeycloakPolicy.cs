using System.Collections.Generic;
using Newtonsoft.Json;

namespace EMR.Application.Responses.Roles;

public class KeycloakPolicy
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("config")]
    public Dictionary<string, string> Config { get; set; }
}