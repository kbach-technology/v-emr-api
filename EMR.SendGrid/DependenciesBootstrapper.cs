using Microsoft.Extensions.DependencyInjection;

namespace EMR.SendGrid;

public static class DependenciesBootstrapper
{
    public static IServiceCollection AddSendGrid(this IServiceCollection services)
    {
        //services.AddTransient<IEmailService, SendGridService>();

        return services;
    }
}