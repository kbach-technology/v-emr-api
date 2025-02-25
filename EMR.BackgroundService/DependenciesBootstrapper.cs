using Microsoft.Extensions.DependencyInjection;

namespace EMR.BackgroundService;

public static class DependenciesBootstrapper
{
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<Services.BackgroundFileUploadService>();
        return services;
    }
}