using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EMR.Firebase;

public static class DependenciesBootstrapper
{
    public static IServiceCollection AddRedisCacheStorage(this IServiceCollection services,
        IConfiguration configuration)
    {
        return services;
    }
}