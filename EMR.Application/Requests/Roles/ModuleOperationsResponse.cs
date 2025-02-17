using System.Collections.Generic;

namespace EMR.Application.Requests.Roles;

public class ModuleOperationsResponse
{
    public string ModuleName { get; set; }
    public List<string> AvailableOperations { get; set; }
}