using Serilog;

namespace EMR.API.Extentions;

internal static class HostBuilderExtensions
{
    internal static IHostBuilder UseSerilog(this IHostBuilder builder)
    {
        try
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            return builder.UseSerilog(Log.Logger);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application start-up failed");
            throw;
        }
    }
    
    internal static IHostBuilder ConfigureAppConfiguration(this IHostBuilder builder)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        builder.ConfigureAppConfiguration((hostingContext, config) =>
        {
            var env = hostingContext.HostingEnvironment;
            config.SetBasePath(env.ContentRootPath);
            config.AddJsonFile($"appsettings.{environment}.json", true, true);
            config.AddEnvironmentVariables();
        });

        return builder;
    }
}