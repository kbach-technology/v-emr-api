using System.Collections.Generic;
using Newtonsoft.Json;

namespace EMR.Application.Responses.Roles;

public class KeycloakResource
{
    [JsonProperty("_id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("scopes")]
    public List<KeycloakScope> Scopes { get; set; } = new List<KeycloakScope>();
}