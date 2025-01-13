using System.Collections.Generic;
using EMR.Application.Responses;
using Microsoft.AspNetCore.Http;

namespace EMR.Application.Interfaces.Services;

public interface IBlobStorageService
{
    Task<Result<string>> UploadMediaAsync(IFormFile file, string folderName,
        CancellationToken cancellationToken);

    Task<Result<IEnumerable<MediaResponse>>> UploadFilesAsync(IEnumerable<IFormFile> files,
        string folderName, CancellationToken cancellationToken);

    Task<Result<string>> DeleteFileAsync(string mediaUrl, CancellationToken cancellationToken);

    Task<Result<string>> GeneratePresignedUrl(string fileName);
}