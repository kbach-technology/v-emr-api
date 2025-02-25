using System.Collections.Generic;
using Newtonsoft.Json;

namespace EMR.Application.Responses.Roles;

public class KeycloakRole
{
    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("name")] public string Name { get; set; }
}

public class KeycloakPermission
{
    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("name")] public string Name { get; set; }
}