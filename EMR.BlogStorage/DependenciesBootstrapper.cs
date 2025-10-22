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
        // Bind AWS/OBS options
        services.Configure<AWSOptions>(configuration.GetSection("AWS"));

        var awsOptions = new AWSOptions();
        configuration.GetSection("AWS").Bind(awsOptions);

        var credentials = new BasicAWSCredentials(awsOptions.AccessKey, awsOptions.Secret);

        // Configure S3 client for AWS S3 or Huawei OBS
        services.AddSingleton<IAmazonS3>(sp =>
        {
            if (awsOptions.UseHuaweiObs && !string.IsNullOrEmpty(awsOptions.ServiceUrl))
            {
                // Huawei OBS configuration with custom endpoint
                var s3Config = new AmazonS3Config
                {
                    ServiceURL = awsOptions.ServiceUrl,
                    ForcePathStyle = true, // Required for OBS compatibility
                    SignatureVersion = "4",
                    SignatureMethod = Amazon.Runtime.SigningAlgorithm.HmacSHA256,
                    UseHttp = false // Always use HTTPS for security
                };

                return new AmazonS3Client(credentials, s3Config);
            }
            else
            {
                // Standard AWS S3 configuration
                return new AmazonS3Client(
                    credentials,
                    RegionEndpoint.GetBySystemName(awsOptions.Region)
                );
            }
        });

        // Only configure MediaConvert if video transcoding is enabled (AWS only)
        if (awsOptions.EnableVideoTranscoding && !awsOptions.UseHuaweiObs)
        {
            services.AddSingleton<IAmazonMediaConvert>(sp => new AmazonMediaConvertClient(
                credentials,
                RegionEndpoint.GetBySystemName(awsOptions.Region)
            ));
        }
        else
        {
            // Provide a null MediaConvert client when disabled
            services.AddSingleton<IAmazonMediaConvert>(sp => null);
        }

        services.AddTransient<IBlobStorageService, BlobStorageService>();

        return services;
    }
}