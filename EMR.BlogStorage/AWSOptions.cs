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

    public string MediaConvertRole { get; set; }

    // File size limits in MB
    public long MaxImageSizeMB { get; set; } = 10;
    public long MaxVideoSizeMB { get; set; } = 500;
    public long MaxDocumentSizeMB { get; set; } = 20;

    // Multipart upload threshold in MB (AWS recommends 100MB+)
    public long MultipartThresholdMB { get; set; } = 100;
}