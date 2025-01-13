using EMR.Application.Interfaces.Services;
using EMR.SMS.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EMR.SMS;

public static class DependenciesBootstrapper
{
    public static IServiceCollection AddSms(this IServiceCollection services)
    {
        services.AddTransient<ISmsService, SmsService>();

        return services;
    }
}