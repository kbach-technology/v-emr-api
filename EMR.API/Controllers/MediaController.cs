using EMR.Application.Interfaces.Services;
using EMR.Application.Responses;
using EMR.Shared.Wrapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaController : ControllerBase
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<MediaController> _logger;

    public MediaController(IBlobStorageService blobStorageService, ILogger<MediaController> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a single image or video file
    /// </summary>
    /// <param name="file">The file to upload (images: jpg, png, jpeg, gif; videos: mp4, mov, avi)</param>
    /// <param name="folderName">Optional folder name to organize uploads</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The uploaded file path/URL</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(Result<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(524288000)] // 500MB limit
    [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
    public async Task<IActionResult> UploadMedia(
        [FromForm] IFormFile file,
        [FromForm] string? folderName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(await Result<string>.FailAsync("No file provided"));
            }

            _logger.LogInformation("Starting upload for file: {FileName}, Size: {FileSize}MB",
                file.FileName, file.Length / (1024.0 * 1024.0));

            var result = await _blobStorageService.UploadMediaAsync(file, folderName, cancellationToken);

            if (result.Succeeded)
            {
                _logger.LogInformation("File uploaded successfully: {FilePath}", result.Data);
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error during file upload");
            return BadRequest(await Result<string>.FailAsync(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(StatusCodes.Status500InternalServerError,
                await Result<string>.FailAsync("An error occurred while uploading the file"));
        }
    }

    /// <summary>
    /// Upload multiple images or video files in parallel
    /// </summary>
    /// <param name="files">The files to upload</param>
    /// <param name="folderName">Optional folder name to organize uploads</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of uploaded files with metadata</returns>
    [HttpPost("upload/batch")]
    [ProducesResponseType(typeof(Result<IEnumerable<MediaResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(524288000)] // 500MB total limit
    [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
    public async Task<IActionResult> UploadMultipleMedia(
        [FromForm] List<IFormFile> files,
        [FromForm] string? folderName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(await Result<IEnumerable<MediaResponse>>.FailAsync("No files provided"));
            }

            _logger.LogInformation("Starting batch upload for {FileCount} files", files.Count);

            var result = await _blobStorageService.UploadFilesAsync(files, folderName, cancellationToken);

            if (result.Succeeded)
            {
                _logger.LogInformation("Batch upload completed: {SuccessCount} files uploaded", result.Data.Count());
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading multiple files");
            return StatusCode(StatusCodes.Status500InternalServerError,
                await Result<IEnumerable<MediaResponse>>.FailAsync("An error occurred while uploading files"));
        }
    }

    /// <summary>
    /// Delete a previously uploaded file
    /// </summary>
    /// <param name="mediaUrl">The file path/key to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or failure result</returns>
    [HttpDelete("delete")]
    [ProducesResponseType(typeof(Result<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteMedia(
        [FromQuery] string mediaUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(mediaUrl))
            {
                return BadRequest(await Result<string>.FailAsync("Media URL is required"));
            }

            _logger.LogInformation("Deleting file: {MediaUrl}", mediaUrl);

            var result = await _blobStorageService.DeleteFileAsync(mediaUrl, cancellationToken);

            if (result.Succeeded)
            {
                _logger.LogInformation("File deleted successfully: {MediaUrl}", mediaUrl);
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file");
            return StatusCode(StatusCodes.Status500InternalServerError,
                await Result<string>.FailAsync("An error occurred while deleting the file"));
        }
    }

    /// <summary>
    /// Generate a temporary pre-signed URL for downloading a file
    /// </summary>
    /// <param name="fileName">The file path/key</param>
    /// <returns>A pre-signed URL valid for 24 hours</returns>
    [HttpGet("presigned-url")]
    [ProducesResponseType(typeof(Result<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPresignedUrl([FromQuery] string fileName)
    {
        try
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest(await Result<string>.FailAsync("File name is required"));
            }

            var result = await _blobStorageService.GeneratePresignedUrl(fileName);

            if (result.Succeeded)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL");
            return StatusCode(StatusCodes.Status500InternalServerError,
                await Result<string>.FailAsync("An error occurred while generating the presigned URL"));
        }
    }
}
