namespace EMR.Shared.Constants.Permission;

public static class Permissions
{
    public static class Device
    {
        public const string View = "Device.View";
        public const string Create = "Device.Create";
        public const string Edit = "Device.Edit";
        public const string Delete = "Device.Delete";
    }

    public static class Preferences
    {
        public const string ChangeLanguage = "Preference.ChangeLanguage";
    }

    public static class AuditTrail
    {
        public const string View = "AuditTrail.View";
        public const string Export = "AuditTrail.Export";
        public const string Search = "AuditTrail.Search";
    }

    public static class AppVersion
    {
        public const string View = "AppVersion.View";
        public const string Create = "AppVersion.Create";
        public const string Edit = "AppVersion.Edit";
        public const string Delete = "AppVersion.Delete";
    }

    public static class BlobStorage
    {
        public const string Upload = "BlobStorage.Upload";
        public const string Generate = "BlobStorage.Generate";
        public const string Delete = "BlobStorage.Delete";
    }

    public static class Preference
    {
        public const string View = "Preference.View";
        public const string Create = "Preference.Create";
        public const string Edit = "Preference.Edit";
    }

    public static class User
    {
        public const string Edit = "User.ChangePin";
    }
}