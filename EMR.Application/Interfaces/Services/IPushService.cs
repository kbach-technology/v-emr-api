using System.Collections.Generic;

namespace EMR.Application.Interfaces.Services;

public interface IPushService
{
    Task<Result<string>> PushForecastsAsync(string title, string body,
        Dictionary<string, string> data = null);

    Task<Result<string>> PushUsersAsync(List<string> deviceTokens, string title,
        string body, Dictionary<string, string> data = null);
}