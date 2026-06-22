using Business.Domain.Entities;
using Business.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Business.Web.Services;

public interface IRecordFileUploadService
{
    long MaxUploadSizeBytes { get; }
    string GetAllowedExtensionsText();
    bool TryValidateFiles(IEnumerable<IFormFile>? files, out string errorMessage);
    Task<IReadOnlyList<RecordFile>> SaveFilesAsync(
        RecordOwnerType ownerType,
        Guid ownerId,
        IEnumerable<IFormFile>? files,
        string? description,
        CancellationToken cancellationToken = default);
}
