using System.Net;
using System.Reflection;
using EMR.Application.Configurations;
using EMR.Application.Interfaces.Repositories;
using EMR.Application.Interfaces.Serialization.Options;
using EMR.Application.Interfaces.Serialization.Serializers;
using EMR.Application.Interfaces.Serialization.Settings;
using EMR.Application.Repositories;
using EMR.Application.Serialization.JsonConverters;
using EMR.Application.Serialization.Options;
using EMR.Application.Serialization.Serializers;
using EMR.Application.Serialization.Settings;
using EMR.Domain.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using IServiceCollection = Microsoft.Extensions.DependencyInjection.IServiceCollection;

namespace EMR.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddInfrastructureMappings(this IServiceCollection services)
    {
        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AllowNullCollections = true;
            mc.ShouldMapMethod = m => false;

            mc.AddMaps(Assembly.GetExecutingAssembly());
        });

        var mapper = mapperConfig.CreateMapper();
        services.AddSingleton(mapper);

        services.AddAutoMapper(Assembly.GetExecutingAssembly());
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        return services
            .AddTransient(typeof(IUnitOfWork<>), typeof(UnitOfWork<>))
            .AddTransient(typeof(IRepositoryAsync<,>), typeof(RepositoryAsync<,>));
    }

    public static void AddApplicationLayer(this IServiceCollection services)
    {
        services.AddMediatR(Assembly.GetExecutingAssembly());
    }

    public static void AddApiVersion(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new HeaderApiVersionReader("x-api-version"),
                new MediaTypeApiVersionReader("v")
            );
        });
    }

    public static IServiceCollection AddSerialization(this IServiceCollection services)
    {
        services
            .AddScoped<IJsonSerializerOptions, SystemTextJsonOptions>()
            .Configure<SystemTextJsonOptions>(configureOptions =>
            {
                if (!configureOptions.JsonSerializerOptions.Converters.Any(c =>
                        c.GetType() == typeof(TimespanJsonConverter)))
                    configureOptions.JsonSerializerOptions.Converters.Add(new TimespanJsonConverter());
            });
        services.AddScoped<IJsonSerializerSettings, NewtonsoftJsonSettings>();

        services
            .AddScoped<IJsonSerializer, SystemTextJsonSerializer>(); // you can change it
        return services;
    }

    public static IServiceCollection AddForwarding(this IServiceCollection services, IConfiguration configuration)
    {
        var applicationSettingsConfiguration = configuration.GetSection(nameof(AppConfiguration));
        var config = applicationSettingsConfiguration.Get<AppConfiguration>();
        if (config is not null)
            if (config.BehindSSLProxy)
                services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    if (!string.IsNullOrWhiteSpace(config.ProxyIP))
                    {
                        var ipCheck = config.ProxyIP;
                        if (IPAddress.TryParse(ipCheck, out var proxyIP))
                            options.KnownProxies.Add(proxyIP);
                        else
                            Log.Logger.Warning("Invalid Proxy IP of {IpCheck}, Not Loaded", ipCheck);
                    }
                });

        return services;
    }
}