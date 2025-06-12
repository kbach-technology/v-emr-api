namespace EMR.Application.Interfaces.Services;

public interface IRedisStorageService
{
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task<T?> GetAsync<T>(string key);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
}