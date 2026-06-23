using Business.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Business.Web.Services;

public interface IProjectDriveUploadService
{
    long MaxUploadSizeBytes { get; }
    string GetAllowedExtensionsText();
    bool TryValidateFiles(IEnumerable<IFormFile>? files, out string errorMessage);
    Task<IReadOnlyList<ProjectDriveFile>> SaveFilesAsync(
        Guid projectId,
        Guid? folderId,
        IEnumerable<IFormFile>? files,
        string? description,
        bool canViewAdminOnlyRecords,
        CancellationToken cancellationToken = default);
    Task DeletePhysicalFileIfExistsAsync(ProjectDriveFile file, CancellationToken cancellationToken = default);
}
