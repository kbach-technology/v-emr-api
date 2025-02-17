using System.Collections.Generic;
using Newtonsoft.Json;

namespace EMR.Application.Responses.Roles;

public class PermissionResource
{
    public string Name { get; set; }

    [JsonProperty("_id")] // Map "_id" in JSON to "Id" in C#
    public string Id { get; set; }
}

public class PermissionScope
{
    public string Id { get; set; }
    public string Name { get; set; }
}