using Newtonsoft.Json;

namespace EMR.Application.Responses.Roles;

public class KeycloakRoleResponse
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }
}