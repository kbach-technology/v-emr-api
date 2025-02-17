namespace EMR.Shared.Configurations;

public class KeycloakConfiguration
{
    public KeyConfig Secret { get; set; } = new();
    public ClientConfig Client { get; set; } = new();
    public AdminConfig Admin { get; set; } = new();
    
    public class KeyConfig
    {
        public string Key { get; set; } = string.Empty;
    }

    public class ClientConfig
    {
        public string Authority { get; set; }
        public string Realm { get; set; } = string.Empty;
        public bool RequireHttpsMetadata { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public string GrantType { get; set; } = "password";
        public string Scope { get; set; } = "openid email profile";
        public string TokenEndpoint => $"{Authority}/realms/{Realm}/protocol/openid-connect/token";
        public string UserEndpoint => $"{Authority}/admin/realms/{Realm}/users";
    }

    public class AdminConfig
    {
        public string Authority { get; set; } = string.Empty;
        public string Realm { get; set; } = "master";
        public string ClientId { get; set; } = "admin-cli";
        public string Secret { get; set; } = string.Empty;
        public bool RequireHttpsMetadata { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string GrantType { get; set; } = "password";
        public string Scope { get; set; } = "openid profile email";
        public string AdminTokenEndpoint => $"{Authority}/realms/{Realm}/protocol/openid-connect/token";
    }
}