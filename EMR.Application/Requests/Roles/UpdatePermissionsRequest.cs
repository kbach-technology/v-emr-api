using System.Collections.Generic;

namespace EMR.Application.Requests.Roles;

public class UpdatePermissionsRequest
{
    public string RoleId { get; set; }
    public string RoleName { get; set; }
    public Dictionary<string, List<string>> ModulePermissions { get; set; }
}