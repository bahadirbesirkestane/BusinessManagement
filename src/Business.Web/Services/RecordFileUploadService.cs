using Business.Domain.Entities;
using Business.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Business.Web.Services;

public class RecordFileUploadService : IRecordFileUploadService
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedUploadTypes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ["application/pdf"],
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".png"] = ["image/png"],
        [".webp"] = ["image/webp"],
        [".gif"] = ["image/gif"],
        [".doc"] = ["application/msword"],
        [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
        [".xls"] = ["application/vnd.ms-excel"],
        [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
        [".csv"] = ["text/csv", "application/csv", "application/vnd.ms-excel", "text/plain"],
        [".txt"] = ["text/plain"],
        [".zip"] = ["application/zip", "application/x-zip-compressed", "multipart/x-zip", "application/octet-stream"],
        [".mp4"] = ["video/mp4", "application/mp4", "application/octet-stream"],
        [".dwg"] = ["image/vnd.dwg", "application/acad", "application/x-acad", "application/autocad_dwg", "application/octet-stream"],
        [".dxf"] = ["image/vnd.dxf", "application/dxf", "application/x-dxf", "application/octet-stream"]
    };

    private readonly IWebHostEnvironment _environment;

    public RecordFileUploadService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public long MaxUploadSizeBytes => 50L * 1024 * 1024;

    public string GetAllowedExtensionsText()
    {
        return string.Join(", ", AllowedUploadTypes.Keys.Order(StringComparer.OrdinalIgnoreCase));
    }

    public bool TryValidateFiles(IEnumerable<IFormFile>? files, out string errorMessage)
    {
        errorMessage = string.Empty;

        var validFiles = files?
            .Where(file => file is not null && file.Length > 0)
            .ToList() ?? [];

        foreach (var file in validFiles)
        {
            if (file.Length > MaxUploadSizeBytes)
            {
                errorMessage = $"Her bir dosya en fazla {MaxUploadSizeBytes / 1024 / 1024} MB olabilir.";
                return false;
            }

            var originalFileName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(originalFileName))
            {
                errorMessage = "Geçersiz dosya adı tespit edildi.";
                return false;
            }

            if (originalFileName.Length > 260)
            {
                errorMessage = "Dosya adı çok uzun.";
                return false;
            }

            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            if (!IsAllowedUploadType(extension, file.ContentType))
            {
                errorMessage = $"Bu dosya türü yüklenemez. İzin verilen türler: {GetAllowedExtensionsText()}";
                return false;
            }
        }

        return true;
    }

    public async Task<IReadOnlyList<RecordFile>> SaveFilesAsync(
        RecordOwnerType ownerType,
        Guid ownerId,
        IEnumerable<IFormFile>? files,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var validFiles = files?
            .Where(file => file is not null && file.Length > 0)
            .ToList() ?? [];

        var uploadRoot = Path.Combine(_environment.WebRootPath, "uploads", ownerType.ToString(), ownerId.ToString("N"));
        Directory.CreateDirectory(uploadRoot);

        var savedFiles = new List<RecordFile>();

        foreach (var file in validFiles)
        {
            var originalFileName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(uploadRoot, storedFileName);

            await using (var stream = System.IO.File.Create(physicalPath))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            savedFiles.Add(new RecordFile
            {
                OwnerType = ownerType,
                OwnerId = ownerId,
                OriginalFileName = originalFileName,
                StoredFileName = storedFileName,
                RelativePath = $"/uploads/{ownerType}/{ownerId:N}/{storedFileName}",
                ContentType = file.ContentType,
                Size = file.Length,
                Description = description
            });
        }

        return savedFiles;
    }

    private static bool IsAllowedUploadType(string extension, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(extension) || !AllowedUploadTypes.TryGetValue(extension, out var allowedContentTypes))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            return true;
        }

        return allowedContentTypes.Contains(contentType.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
