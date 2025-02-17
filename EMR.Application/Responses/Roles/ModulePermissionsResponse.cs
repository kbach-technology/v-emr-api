using System.Collections.Generic;

namespace EMR.Application.Responses.Roles;

public class ModulePermissionsResponse
{
    public Dictionary<string, ModuleResponse> Modules { get; set; }
}