namespace EMR.Application.Interfaces.Services;

public interface IRedisStorageService
{
    Task StoreValueAsync(string value, string uniqueKey);
    Task<string> GetValueAsync(string uniqueKey);
}