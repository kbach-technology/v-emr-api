using System.Collections.Generic;
using Newtonsoft.Json;

namespace EMR.Application.Responses.Roles;

public class KeycloakPermissionResponse
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("logic")]
    public string Logic { get; set; }

    [JsonProperty("decisionStrategy")]
    public string DecisionStrategy { get; set; }

    [JsonProperty("resourceType")]
    public string ResourceType { get; set; }

    // Add these properties
    [JsonProperty("config")]
    public Dictionary<string, string> Config { get; set; }

    [JsonProperty("resources")]
    public List<string> Resources { get; set; }

    [JsonProperty("scopes")]
    public List<string> Scopes { get; set; }

    [JsonProperty("policies")]
    public List<string> Policies { get; set; }
}