using System.Collections.Generic;

namespace EMR.Application.Responses.Roles;

public class ModuleResponse
{
    public List<ModuleOperationResponse> Operations { get; set; }
}