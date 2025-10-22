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
                BucketName = _awsOptions.BucketName,
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

        // Validate file size based on type
        var fileSizeMB = file.Length / (1024.0 * 1024.0);
        var mediaType = DetermineMediaType(fileExtension);

        if (mediaType == "Image" && fileSizeMB > _awsOptions.MaxImageSizeMB)
            throw new ArgumentException(
                _localizer["Image file size exceeds maximum allowed size of {0}MB", _awsOptions.MaxImageSizeMB],
                nameof(file));

        if (mediaType == "Video" && fileSizeMB > _awsOptions.MaxVideoSizeMB)
            throw new ArgumentException(
                _localizer["Video file size exceeds maximum allowed size of {0}MB", _awsOptions.MaxVideoSizeMB],
                nameof(file));

        if (mediaType == "Document" && fileSizeMB > _awsOptions.MaxDocumentSizeMB)
            throw new ArgumentException(
                _localizer["Document file size exceeds maximum allowed size of {0}MB", _awsOptions.MaxDocumentSizeMB],
                nameof(file));

        var fileName = $"{folderName}/{Guid.NewGuid()}{fileExtension}";

        // Use multipart upload for large files (better performance and reliability)
        if (fileSizeMB >= _awsOptions.MultipartThresholdMB)
        {
            await UploadLargeFileAsync(file, fileName, cancellationToken);
        }
        else
        {
            // Use streaming upload for smaller files (no memory buffering)
            await using var fileStream = file.OpenReadStream();
            var request = new PutObjectRequest
            {
                BucketName = _awsOptions.BucketName,
                Key = fileName,
                InputStream = fileStream,
                ContentType = file.ContentType
            };

            await _s3Client.PutObjectAsync(request, cancellationToken);
        }

        // Trigger MediaConvert job if it's a video file and transcoding is enabled
        if (_awsOptions.EnableVideoTranscoding &&
            _mediaConvertClient != null &&
            allowedVideoExtensions.Contains(fileExtension))
        {
            // Upload to input bucket for MediaConvert processing
            if (!string.IsNullOrEmpty(_awsOptions.InputBucket))
            {
                await using var fileStream = file.OpenReadStream();
                var inputRequest = new PutObjectRequest
                {
                    BucketName = _awsOptions.InputBucket,
                    Key = fileName,
                    InputStream = fileStream,
                    ContentType = file.ContentType
                };

                await _s3Client.PutObjectAsync(inputRequest, cancellationToken);
                fileName = await ConvertVideoAsync(fileName, cancellationToken);
            }
        }

        return await Result<string>.SuccessAsync(fileName, _localizer["File uploaded successfully."]);
    }

    public async Task<Result<IEnumerable<MediaResponse>>> UploadFilesAsync(IEnumerable<IFormFile> files,
        string? folderName, CancellationToken cancellationToken)
    {
        var uploadResults = new List<MediaResponse>();
        var fileList = files.ToList();

        // Parallelize uploads for better performance
        var uploadTasks = fileList.Select(async file =>
        {
            try
            {
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var result = await UploadMediaAsync(file, folderName, cancellationToken);

                if (result.Succeeded)
                {
                    return new MediaResponse(
                        result.Data,
                        file.ContentType,
                        (int)file.Length / 1000 + "KB",
                        fileExtension
                    );
                }
            }
            catch (Exception)
            {
                // Log error but continue with other uploads
            }

            return null;
        });

        var results = await Task.WhenAll(uploadTasks);
        uploadResults.AddRange(results.Where(r => r != null));

        return await Result<IEnumerable<MediaResponse>>.SuccessAsync(uploadResults,
            _localizer["Files uploaded successfully."]);
    }

    public async Task<Result<string>> GeneratePresignedUrl(string fileName)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _awsOptions.BucketName,
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

    private async Task UploadLargeFileAsync(IFormFile file, string fileName, CancellationToken cancellationToken)
    {
        // Use multipart upload for large files (better performance, supports resume, parallel upload)
        var initiateRequest = new InitiateMultipartUploadRequest
        {
            BucketName = _awsOptions.BucketName,
            Key = fileName,
            ContentType = file.ContentType
        };

        var initiateResponse = await _s3Client.InitiateMultipartUploadAsync(initiateRequest, cancellationToken);
        var uploadId = initiateResponse.UploadId;

        try
        {
            var partSize = _awsOptions.MultipartChunkSizeMB * 1024 * 1024; // Convert MB to bytes
            var fileSize = file.Length;
            var partCount = (int)Math.Ceiling((double)fileSize / partSize);

            var uploadTasks = new List<Task<UploadPartResponse>>();
            var completedParts = new List<PartETag>();

            await using var fileStream = file.OpenReadStream();

            // Upload parts in parallel for maximum performance
            for (int partNumber = 1; partNumber <= partCount; partNumber++)
            {
                var partLength = Math.Min(partSize, fileSize - ((partNumber - 1) * partSize));
                var buffer = new byte[partLength];
                await fileStream.ReadAsync(buffer, 0, (int)partLength, cancellationToken);

                var uploadRequest = new UploadPartRequest
                {
                    BucketName = _awsOptions.BucketName,
                    Key = fileName,
                    UploadId = uploadId,
                    PartNumber = partNumber,
                    InputStream = new MemoryStream(buffer)
                };

                uploadTasks.Add(_s3Client.UploadPartAsync(uploadRequest, cancellationToken));

                // Limit concurrency to avoid overwhelming the server
                if (uploadTasks.Count >= 4)
                {
                    var completedTask = await Task.WhenAny(uploadTasks);
                    uploadTasks.Remove(completedTask);
                    var response = await completedTask;
                    completedParts.Add(new PartETag(response.PartNumber, response.ETag));
                }
            }

            // Wait for remaining uploads to complete
            var remainingResponses = await Task.WhenAll(uploadTasks);
            completedParts.AddRange(remainingResponses.Select(r => new PartETag(r.PartNumber, r.ETag)));

            // Complete the multipart upload
            var completeRequest = new CompleteMultipartUploadRequest
            {
                BucketName = _awsOptions.BucketName,
                Key = fileName,
                UploadId = uploadId,
                PartETags = completedParts.OrderBy(p => p.PartNumber).ToList()
            };

            await _s3Client.CompleteMultipartUploadAsync(completeRequest, cancellationToken);
        }
        catch (Exception)
        {
            // Abort the multipart upload on failure to avoid storage charges
            var abortRequest = new AbortMultipartUploadRequest
            {
                BucketName = _awsOptions.BucketName,
                Key = fileName,
                UploadId = uploadId
            };

            await _s3Client.AbortMultipartUploadAsync(abortRequest, cancellationToken);
            throw;
        }
    }

    private async Task<string> ConvertVideoAsync(string inputFileName, CancellationToken cancellationToken)
    {
        if (_mediaConvertClient == null || string.IsNullOrEmpty(_awsOptions.InputBucket) ||
            string.IsNullOrEmpty(_awsOptions.OutputBucket))
        {
            // Video transcoding not configured, return original filename
            return inputFileName;
        }

        var jobSettings = new JobSettings
        {
            Inputs = new List<Input>
            {
                new Input
                {
                    FileInput = $"s3://{_awsOptions.InputBucket}/{inputFileName}",
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
                            Destination = $"s3://{_awsOptions.OutputBucket}/",
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
            Role = _awsOptions.MediaConvertRole ?? "arn:aws:iam::876782314171:role/service-role/MediaConvert_Default_Role",
            Settings = jobSettings
        };

        var createJobResponse = await _mediaConvertClient.CreateJobAsync(createJobRequest, cancellationToken);

        var jobId = createJobResponse.Job.Id;

        return jobId;
    }
}