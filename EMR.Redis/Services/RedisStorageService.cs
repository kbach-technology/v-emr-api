using EMR.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;

namespace EMR.Redis.Services;

public class RedisStorageService : IRedisStorageService
{
    private const string KeyPrefix = "emr:";
    private readonly IDistributedCache _cache;

    public RedisStorageService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task StoreValueAsync(string value, string uniqueKey)
    {
        var key = $"{KeyPrefix}{uniqueKey}";
        await _cache.SetStringAsync(key, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(14)
        });
    }

    public async Task<string> GetValueAsync(string uniqueKey)
    {
        var key = $"{KeyPrefix}{uniqueKey}";
        return await _cache.GetStringAsync(key);
    }
}