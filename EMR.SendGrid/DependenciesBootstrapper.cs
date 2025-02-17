using EMR.Application.Interfaces.Services;
using EMR.SendGrid.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EMR.SendGrid;

public static class DependenciesBootstrapper
{
    public static IServiceCollection AddSendGrid(this IServiceCollection services)
    {
        services.AddTransient<ISendGridService, SendGridService>();

        return services;
    }
}