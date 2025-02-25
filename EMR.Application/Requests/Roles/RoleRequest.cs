using System.Collections.Generic;

namespace EMR.Application.Requests.Roles;

public class AssignRoleUserRequest
{
    public List<RoleRequest> Roles { get; set; }
}

public class RoleRequest
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}