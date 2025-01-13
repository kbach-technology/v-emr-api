using EMR.Application.Interfaces.Services;
using EMR.Redis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EMR.Redis;

public static class DependenciesBootstrapper
{
    public static IServiceCollection AddRedisCacheStorage(this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisHost = configuration["Redis:Host"];
        var redisPort = configuration["Redis:Port"];
        var redisPassword = configuration["Redis:Password"];

        var redisConfiguration = $"{redisHost}:{redisPort},password={redisPassword}";

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = "https://dev-redis.gojor.app:6379, password=CHPdb@s168Redis";
            options.InstanceName = "GOJOR";
        });

        services.AddScoped<IRedisStorageService, RedisStorageService>();

        return services;
    }
}