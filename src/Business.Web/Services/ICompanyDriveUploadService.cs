using Business.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Business.Web.Services;

public interface ICompanyDriveUploadService
{
    long MaxUploadSizeBytes { get; }
    string GetAllowedExtensionsText();
    bool TryValidateFiles(IEnumerable<IFormFile>? files, out string errorMessage);
    Task<IReadOnlyList<CompanyFile>> SaveFilesAsync(
        Guid? folderId,
        IEnumerable<IFormFile>? files,
        string? description,
        CancellationToken cancellationToken = default);
    Task DeleteFileAsync(Guid fileId, CancellationToken cancellationToken = default);
    string GetSafePhysicalPath(string relativePath);
}
