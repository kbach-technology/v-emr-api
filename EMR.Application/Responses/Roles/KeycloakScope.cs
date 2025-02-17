using Newtonsoft.Json;

namespace EMR.Application.Responses.Roles;

public class KeycloakScope
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("displayName")]
    public string DisplayName { get; set; }
}