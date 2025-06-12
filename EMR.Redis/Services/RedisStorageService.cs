using System.Text.Json;
using EMR.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace EMR.Redis.Services;

public class RedisStorageService : IRedisStorageService
{
    private readonly IDatabase _db;
    private readonly JsonSerializerOptions _serializerOptions;

    public RedisStorageService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value, _serializerOptions);
        await _db.StringSetAsync(key, json, expiry);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return default;
        return JsonSerializer.Deserialize<T>(value!, _serializerOptions);
    }

    public async Task RemoveAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _db.KeyExistsAsync(key);
    }
}