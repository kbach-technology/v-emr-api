using Amazon;
using Amazon.MediaConvert;
using Amazon.Runtime;
using Amazon.S3;
using EMR.Application.Interfaces.Services;
using EMR.BlogStorage.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EMR.BlogStorage;

public static class DependenciesBootstrapper
{
    public static IServiceCollection AddBlobStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var awsOptions = new AWSOptions(); 
        configuration.GetSection("AWS").Bind(awsOptions);
        
        var credentials = new BasicAWSCredentials(awsOptions.AccessKey, awsOptions.Secret);

        services.AddSingleton<IAmazonS3>(sp => new AmazonS3Client(
            credentials,
            RegionEndpoint.GetBySystemName(awsOptions.Region)
        ));

        services.AddSingleton<IAmazonMediaConvert>(sp => new AmazonMediaConvertClient(
            credentials,
            RegionEndpoint.GetBySystemName(awsOptions.Region)
        ));

        services.AddTransient<IBlobStorageService, BlobStorageService>();

        return services;
    }
}