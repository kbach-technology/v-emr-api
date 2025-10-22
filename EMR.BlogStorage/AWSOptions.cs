namespace EMR.BlogStorage;

public class AWSOptions
{
    public string AccessKey { get; set; }
    public string Secret { get; set; }
    public string Region { get; set; }

    public string BucketName { get; set; }
    public string InputBucket { get; set; }
    public string OutputBucket { get; set; }

    public string Profile { get; set; }

    // Huawei OBS specific settings
    public string ServiceUrl { get; set; } // e.g., https://obs.ap-southeast-3.myhuaweicloud.com
    public bool UseHuaweiObs { get; set; } = false;

    // Video processing
    public string MediaConvertRole { get; set; }
    public bool EnableVideoTranscoding { get; set; } = false;

    // File size limits in MB
    public long MaxImageSizeMB { get; set; } = 10;
    public long MaxVideoSizeMB { get; set; } = 500;
    public long MaxDocumentSizeMB { get; set; } = 20;

    // Multipart upload threshold in MB (AWS/OBS recommends 100MB+)
    public long MultipartThresholdMB { get; set; } = 100;

    // Multipart upload part size in MB (minimum 5MB for S3/OBS)
    public long MultipartChunkSizeMB { get; set; } = 5;
}