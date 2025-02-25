namespace EMR.Application.Responses.Roles;

public class ModuleOperationResponse
{
    public string Name { get; set; } // Display name
    public string Key { get; set; } // Operation key (View, Edit, etc.)
    public bool IsAssigned { get; set; }
}