using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Services;

public class ProjectDriveUploadService : IProjectDriveUploadService
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
    private readonly IProjectDriveService _projectDriveService;
    private readonly Business.Infrastructure.Data.ApplicationDbContext _context;

    public ProjectDriveUploadService(
        IWebHostEnvironment environment,
        IProjectDriveService projectDriveService,
        Business.Infrastructure.Data.ApplicationDbContext context)
    {
        _environment = environment;
        _projectDriveService = projectDriveService;
        _context = context;
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

    public async Task<IReadOnlyList<ProjectDriveFile>> SaveFilesAsync(
        Guid projectId,
        Guid? folderId,
        IEnumerable<IFormFile>? files,
        string? description,
        bool canViewAdminOnlyRecords,
        CancellationToken cancellationToken = default)
    {
        var validFiles = files?
            .Where(file => file is not null && file.Length > 0)
            .ToList() ?? [];

        if (validFiles.Count == 0)
        {
            return [];
        }

        if (!TryValidateFiles(validFiles, out var errorMessage))
        {
            throw new InvalidOperationException(errorMessage);
        }

        await _projectDriveService.GetFolderContentAsync(projectId, folderId, canViewAdminOnlyRecords, cancellationToken);

        var uploadRoot = GetProjectUploadRoot(projectId);
        Directory.CreateDirectory(uploadRoot);

        var createdPhysicalPaths = new List<string>();
        var savedRecords = new List<ProjectDriveFile>();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var file in validFiles)
            {
                var originalFileName = Path.GetFileName(file.FileName);
                var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
                var storedFileName = $"{Guid.NewGuid():N}{extension}";
                var relativePath = BuildRelativePath(projectId, storedFileName);
                var physicalPath = GetSafePhysicalPath(relativePath);

                var parentDirectory = Path.GetDirectoryName(physicalPath)
                    ?? throw new InvalidOperationException("Dosya klasörü oluşturulamadı.");
                Directory.CreateDirectory(parentDirectory);

                await using (var stream = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await file.CopyToAsync(stream, cancellationToken);
                }

                createdPhysicalPaths.Add(physicalPath);

                var record = await _projectDriveService.CreateFileRecordAsync(new ProjectDriveFile
                {
                    ProjectId = projectId,
                    FolderId = folderId,
                    OriginalFileName = originalFileName,
                    StoredFileName = storedFileName,
                    RelativePath = relativePath,
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? null : file.ContentType.Trim(),
                    Size = file.Length,
                    Description = description
                }, canViewAdminOnlyRecords, cancellationToken);

                savedRecords.Add(record);
            }

            await transaction.CommitAsync(cancellationToken);
            return savedRecords;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            await CleanupFilesAsync(createdPhysicalPaths);
            throw;
        }
    }

    public async Task DeleteFileAsync(Guid fileId, bool canViewAdminOnlyRecords, CancellationToken cancellationToken = default)
    {
        var file = await _context.ProjectDriveFiles
            .Include(x => x.Project)
            .FirstOrDefaultAsync(x => x.Id == fileId, cancellationToken)
            ?? throw new KeyNotFoundException("Dosya bulunamadı.");

        if (!canViewAdminOnlyRecords && file.Project.Visibility != RecordVisibility.General)
        {
            throw new UnauthorizedAccessException("Bu projeye erişim yetkiniz yok.");
        }

        var physicalPath = GetSafePhysicalPath(file.RelativePath);
        string? quarantinePath = null;

        if (File.Exists(physicalPath))
        {
            var trashRoot = Path.Combine(_environment.WebRootPath, "uploads", "project-drive", ".trash");
            Directory.CreateDirectory(trashRoot);
            quarantinePath = Path.Combine(trashRoot, $"{Guid.NewGuid():N}_{Path.GetFileName(physicalPath)}");
            File.Move(physicalPath, quarantinePath);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _context.ProjectDriveFiles.Remove(file);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(quarantinePath) && File.Exists(quarantinePath))
            {
                File.Delete(quarantinePath);
            }
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(quarantinePath) && File.Exists(quarantinePath) && !File.Exists(physicalPath))
            {
                File.Move(quarantinePath, physicalPath);
            }

            throw;
        }
    }

    private string GetProjectUploadRoot(Guid projectId)
    {
        return Path.Combine(_environment.WebRootPath, "uploads", "project-drive", projectId.ToString("N"));
    }

    private static string BuildRelativePath(Guid projectId, string storedFileName)
    {
        return $"/uploads/project-drive/{projectId:N}/{storedFileName}";
    }

    private string GetSafePhysicalPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || !relativePath.StartsWith('/'))
        {
            throw new InvalidOperationException("Geçersiz dosya yolu.");
        }

        var normalizedRelativePath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var rootPath = Path.GetFullPath(_environment.WebRootPath);
        var rootPathWithSeparator = rootPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var combinedPath = Path.GetFullPath(Path.Combine(rootPath, normalizedRelativePath));

        if (!combinedPath.StartsWith(rootPathWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Güvensiz dosya yolu tespit edildi.");
        }

        return combinedPath;
    }

    private static async Task CleanupFilesAsync(IEnumerable<string> physicalPaths)
    {
        await Task.CompletedTask;

        foreach (var physicalPath in physicalPaths)
        {
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }
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
