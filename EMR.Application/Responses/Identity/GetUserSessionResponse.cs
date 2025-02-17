namespace EMR.Application.Responses.Identity;

public class GetUserSessionResponse
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string UserId { get; set; }
    public string IpAddress { get; set; }
    public long Start { get; set; }
    public long LastAccess { get; set; }
    public string Browser { get; set; }
    public bool Current { get; set; }
    public string Device { get; set; }
    public string OperatingSystem { get; set; }
    public bool RememberMe { get; set; }
}