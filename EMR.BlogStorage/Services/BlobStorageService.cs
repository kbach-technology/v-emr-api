using Amazon;
using Amazon.MediaConvert;
using Amazon.MediaConvert.Model;
using Amazon.S3;
using Amazon.S3.Model;
using EMR.Application.Interfaces.Services;
using EMR.Application.Responses;
using EMR.Shared.Interfaces;
using EMR.Shared.Wrapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Result = Nest.Result;

namespace EMR.BlogStorage.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly AWSOptions _awsOptions;
    private readonly IConfiguration _configuration;
    private readonly IDateTimeService _dateTimeService;
    private readonly IStringLocalizer<BlobStorageService> _localizer;
    private readonly IAmazonS3 _s3Client;
    private readonly IAmazonMediaConvert _mediaConvertClient;

    public BlobStorageService(
        IOptions<AWSOptions> awsOptions,
        IAmazonS3 s3Client,
        IAmazonMediaConvert mediaConvertClient,
        IConfiguration configuration,
        IDateTimeService dateTimeService,
        IStringLocalizer<BlobStorageService> localizer)
    {
        _awsOptions = awsOptions.Value;
        _configuration = configuration;
        _dateTimeService = dateTimeService;
        _localizer = localizer;
        _s3Client = s3Client;
        _mediaConvertClient = mediaConvertClient;
    }

    public async Task<Result<string>> DeleteFileAsync(string mediaUrl, CancellationToken cancellationToken)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _configuration.GetSection("AWS:BucketName").Value,
                Key = mediaUrl
            };

            await _s3Client.DeleteObjectAsync(request, cancellationToken);

            return await Result<string>.SuccessAsync(_localizer["File deleted successfully."]);
        }
        catch (Exception ex)
        {
            return await Result<string>.FailAsync(_localizer["File deletion failed: {0}", ex.Message]);
        }
    }

    public async Task<Result<string>> UploadMediaAsync(IFormFile file, string? folderName,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException(_localizer["Invalid file"], nameof(file));

        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        var allowedImageExtensions = new[] { ".jpg", ".png", ".jpeg", ".gif" };
        var allowedVideoExtensions = new[] { ".mp4", ".mov", ".avi" };
        var allowedDocumentExtensions = new[] { ".pdf", ".docx", ".xlsx" };

        var allAllowedExtensions =
            allowedImageExtensions.Concat(allowedVideoExtensions).Concat(allowedDocumentExtensions);

        if (!allAllowedExtensions.Contains(fileExtension))
            throw new ArgumentException(_localizer["Invalid file type"], nameof(file));

        var fileName = $"{folderName}/{Guid.NewGuid()}{fileExtension}";

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var request = new PutObjectRequest
        {
            BucketName = _configuration.GetSection("AWS:BucketName").Value,
            Key = fileName,
            InputStream = memoryStream,
            ContentType = file.ContentType
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);

        // Trigger MediaConvert job if it's a video file
        if (fileExtension == ".mp4" || fileExtension == ".mov" || fileExtension == ".avi")
        {
            request = new PutObjectRequest
            {
                BucketName = _configuration.GetSection("AWS:Bucket:Input").Value,
                Key = fileName,
                InputStream = memoryStream,
                ContentType = file.ContentType
            };

            await _s3Client.PutObjectAsync(request, cancellationToken);

            fileName = await ConvertVideoAsync(fileName, cancellationToken);
        }

        return await Result<string>.SuccessAsync(fileName, _localizer["File uploaded successfully."]);
    }

    public async Task<Result<IEnumerable<MediaResponse>>> UploadFilesAsync(IEnumerable<IFormFile> files,
        string? folderName, CancellationToken cancellationToken)
    {
        var uploadResults = new List<MediaResponse>();

        foreach (var file in files)
        {
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            var result = await UploadMediaAsync(file, folderName, cancellationToken);
            if (result.Succeeded)
                uploadResults.Add(new MediaResponse(
                        result.Data,
                        file.ContentType,
                        (int)file.Length / 1000 + "KB",
                        fileExtension
                    )
                );
        }

        return await Result<IEnumerable<MediaResponse>>.SuccessAsync(uploadResults,
            _localizer["Files uploaded successfully."]);
    }

    public async Task<Result<string>> GeneratePresignedUrl(string fileName)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _configuration.GetSection("AWS:BucketName").Value,
            Key = fileName,
            Expires = _dateTimeService.NowUtc.AddDays(1),
            Verb = HttpVerb.GET
        };

        return await Result<string>.SuccessAsync(_s3Client.GetPreSignedURL(request),
            _localizer["Presigned URL generated successfully."]);
    }

    private string DetermineMediaType(string fileExtension)
    {
        var allowedImageExtensions = new[] { ".jpg", ".png", ".jpeg", ".gif" };
        var allowedVideoExtensions = new[] { ".mp4", ".mov", ".avi" };

        if (allowedImageExtensions.Contains(fileExtension))
            return "Image";
        if (allowedVideoExtensions.Contains(fileExtension))
            return "Video";
        return "Document";
    }

    private async Task<string> ConvertVideoAsync(string inputFileName, CancellationToken cancellationToken)
    {
        var jobSettings = new JobSettings
        {
            Inputs = new List<Input>
            {
                new Input
                {
                    FileInput = $"s3://{_configuration.GetSection("AWS:Bucket:Input").Value}/{inputFileName}",
                    AudioSelectors = new Dictionary<string, AudioSelector>
                    {
                        { "Audio Selector 1", new AudioSelector { DefaultSelection = AudioDefaultSelection.DEFAULT } }
                    }
                }
            },
            OutputGroups = new List<OutputGroup>
            {
                new OutputGroup
                {
                    Name = "HLS Group",
                    OutputGroupSettings = new OutputGroupSettings
                    {
                        Type = OutputGroupType.HLS_GROUP_SETTINGS,
                        HlsGroupSettings = new HlsGroupSettings
                        {
                            Destination = $"s3://{_configuration.GetSection("AWS:Bucket:Output").Value}/",
                            SegmentLength = 10,
                            MinSegmentLength = 0
                        }
                    },
                    Outputs = new List<Output>
                    {
                        new Output
                        {
                            NameModifier = "_360p",
                            VideoDescription = new VideoDescription
                            {
                                Height = 360,
                                CodecSettings = new VideoCodecSettings
                                {
                                    Codec = VideoCodec.H_264,
                                    H264Settings = new H264Settings
                                    {
                                        FramerateControl = H264FramerateControl.SPECIFIED,
                                        FramerateNumerator = 30,
                                        FramerateDenominator = 1,
                                        Bitrate = 1500000
                                    }
                                }
                            },
                            AudioDescriptions = new List<AudioDescription>
                            {
                                new AudioDescription
                                {
                                    AudioSourceName = "Audio Selector 1",
                                    CodecSettings = new AudioCodecSettings
                                    {
                                        Codec = AudioCodec.AAC,
                                        AacSettings = new AacSettings
                                        {
                                            Bitrate = 96000,
                                            CodingMode = AacCodingMode.CODING_MODE_2_0,
                                            SampleRate = 48000
                                        }
                                    }
                                }
                            },
                            ContainerSettings = new ContainerSettings
                            {
                                Container = ContainerType.M3U8
                            }
                        },
                        new Output
                        {
                            NameModifier = "_480p",
                            VideoDescription = new VideoDescription
                            {
                                Height = 480,
                                CodecSettings = new VideoCodecSettings
                                {
                                    Codec = VideoCodec.H_264,
                                    H264Settings = new H264Settings
                                    {
                                        FramerateControl = H264FramerateControl.SPECIFIED,
                                        FramerateNumerator = 30,
                                        FramerateDenominator = 1,
                                        Bitrate = 2000000
                                    }
                                }
                            },
                            AudioDescriptions = new List<AudioDescription>
                            {
                                new AudioDescription
                                {
                                    AudioSourceName = "Audio Selector 1",
                                    CodecSettings = new AudioCodecSettings
                                    {
                                        Codec = AudioCodec.AAC,
                                        AacSettings = new AacSettings
                                        {
                                            Bitrate = 96000,
                                            CodingMode = AacCodingMode.CODING_MODE_2_0,
                                            SampleRate = 48000
                                        }
                                    }
                                }
                            },
                            ContainerSettings = new ContainerSettings
                            {
                                Container = ContainerType.M3U8
                            }
                        },

                        new Output
                        {
                            NameModifier = "_720p",
                            VideoDescription = new VideoDescription
                            {
                                Height = 720,
                                CodecSettings = new VideoCodecSettings
                                {
                                    Codec = VideoCodec.H_264,
                                    H264Settings = new H264Settings
                                    {
                                        FramerateControl = H264FramerateControl.SPECIFIED,
                                        FramerateNumerator = 30,
                                        FramerateDenominator = 1,
                                        Bitrate = 3000000
                                    }
                                }
                            },
                            AudioDescriptions = new List<AudioDescription>
                            {
                                new AudioDescription
                                {
                                    AudioSourceName = "Audio Selector 1",
                                    CodecSettings = new AudioCodecSettings
                                    {
                                        Codec = AudioCodec.AAC,
                                        AacSettings = new AacSettings
                                        {
                                            Bitrate = 96000,
                                            CodingMode = AacCodingMode.CODING_MODE_2_0,
                                            SampleRate = 48000
                                        }
                                    }
                                }
                            },
                            ContainerSettings = new ContainerSettings
                            {
                                Container = ContainerType.M3U8
                            }
                        },
                        new Output
                        {
                            NameModifier = "_1080p",
                            VideoDescription = new VideoDescription
                            {
                                Height = 1080,
                                CodecSettings = new VideoCodecSettings
                                {
                                    Codec = VideoCodec.H_264,
                                    H264Settings = new H264Settings
                                    {
                                        FramerateControl = H264FramerateControl.SPECIFIED,
                                        FramerateNumerator = 30,
                                        FramerateDenominator = 1,
                                        Bitrate = 4000000
                                    }
                                }
                            },
                            AudioDescriptions = new List<AudioDescription>
                            {
                                new AudioDescription
                                {
                                    AudioSourceName = "Audio Selector 1",
                                    CodecSettings = new AudioCodecSettings
                                    {
                                        Codec = AudioCodec.AAC,
                                        AacSettings = new AacSettings
                                        {
                                            Bitrate = 96000,
                                            CodingMode = AacCodingMode.CODING_MODE_2_0,
                                            SampleRate = 48000
                                        }
                                    }
                                }
                            },
                            ContainerSettings = new ContainerSettings
                            {
                                Container = ContainerType.M3U8
                            }
                        }
                    }
                }
            }
        };

        var createJobRequest = new CreateJobRequest
        {
            Role = "arn:aws:iam::876782314171:role/service-role/MediaConvert_Default_Role",
            Settings = jobSettings
        };

        var createJobResponse = await _mediaConvertClient.CreateJobAsync(createJobRequest, cancellationToken);

        var jobId = createJobResponse.Job.Id;

        return jobId;
    }
}