using System.Collections.Concurrent;
using EMR.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EMR.BackgroundService.Services;

public class BackgroundFileUploadService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly ILogger<BackgroundFileUploadService> _logger;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ConcurrentQueue<(IFormFile file, string folderName)> _fileQueue;

    public BackgroundFileUploadService(ILogger<BackgroundFileUploadService> logger,
        IBlobStorageService blobStorageService)
    {
        _logger = logger;
        _blobStorageService = blobStorageService;
        _fileQueue = new ConcurrentQueue<(IFormFile file, string folderName)>();
    }

    public void QueueFileUpload(IFormFile file, string folderName)
    {
        _fileQueue.Enqueue((file, folderName));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_fileQueue.TryDequeue(out var fileUpload))
            {
                try
                {
                    await _blobStorageService.UploadMediaAsync(fileUpload.file, fileUpload.folderName, stoppingToken);
                    _logger.LogInformation("File uploaded successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file.");
                }
            }

            await Task.Delay(1000, stoppingToken); // Adjust the delay as needed
        }
    }
}