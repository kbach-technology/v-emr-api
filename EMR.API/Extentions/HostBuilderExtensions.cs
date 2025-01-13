using Serilog;

namespace EMR.API.Extentions;

internal static class HostBuilderExtensions
{
    internal static IHostBuilder UseSerilog(this IHostBuilder builder)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json")
            .AddJsonFile("appsettings.Staging.json")
            .AddJsonFile("appsettings.Production.json")
            .AddEnvironmentVariables()
            .Build();

        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
        SerilogHostBuilderExtensions.UseSerilog(builder);
        return builder;
    }
}