using System.Collections.Generic;
using System.Reflection.PortableExecutable;

namespace EMR.Shared.Constants.Permission;

public static class Modules
{
    public const string AppVersion = nameof(AppVersion);
    public const string User = nameof(User);
    public const string Role = nameof(Role);


    public static IEnumerable<string> All => new[] 
    { 
        AppVersion, 
        User,
        Role
    };
}

public static class Operations
{
    public const string Manage = "Manage";
    public const string Create = "Create";
    public const string Edit = "Edit";
    public const string View = "View";
    public const string ViewAny = "ViewAny";

    public static IEnumerable<string> All => new[] 
    { 
        Manage,
        Create, 
        Edit, 
        View, 
        ViewAny 
    };
}

public static class Permissions
{
    public static string GeneratePermission(string module, string operation)
        => $"{module}.{operation}";

    public static class AppVersion
    {
        public const string Create = $"{Modules.AppVersion}.{Operations.Create}";
        public const string Amend = $"{Modules.AppVersion}.{Operations.Edit}";
        public const string View = $"{Modules.AppVersion}.{Operations.View}";
        public const string ViewAny = $"{Modules.AppVersion}.{Operations.ViewAny}";

        public static IEnumerable<string> All => new[] 
        { 
            Create, Amend, View, ViewAny 
        };
    }

    public static class User
    {
        public const string Create = $"{Modules.User}.{Operations.Create}";
        public const string Edit = $"{Modules.User}.{Operations.Edit}";
        public const string View = $"{Modules.User}.{Operations.View}";
        public const string ViewAny = $"{Modules.User}.{Operations.ViewAny}";

        public static IEnumerable<string> All => new[] 
        { 
            Create, Edit, View, ViewAny 
        };
    }
    
    public static class Role
    {
        public const string Manage = $"{Modules.Role}.{Operations.Manage}";
        public const string View = $"{Modules.Role}.{Operations.View}";

        public static IEnumerable<string> All => new[] 
        { 
            Manage, View
        };
    }
}