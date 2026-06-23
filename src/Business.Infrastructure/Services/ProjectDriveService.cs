using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Services;

public class ProjectDriveService : IProjectDriveService
{
    private readonly ApplicationDbContext _context;

    public ProjectDriveService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProjectDriveTreeNode>> GetFolderTreeAsync(Guid projectId, bool canViewAdminOnlyRecords, CancellationToken cancellationToken = default)
    {
        await EnsureProjectAccessibleAsync(projectId, canViewAdminOnlyRecords, cancellationToken);

        var folders = await _context.ProjectFolders
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return BuildTree(folders, null);
    }

    public async Task<ProjectDriveFolderContent> GetFolderContentAsync(Guid projectId, Guid? folderId, bool canViewAdminOnlyRecords, CancellationToken cancellationToken = default)
    {
        await EnsureProjectAccessibleAsync(projectId, canViewAdminOnlyRecords, cancellationToken);

        ProjectFolder? folder = null;
        if (folderId.HasValue)
        {
            folder = await GetFolderForProjectAsync(projectId, folderId.Value, cancellationToken);
        }

        var folders = await _context.ProjectFolders
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId && x.ParentFolderId == folderId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var files = await _context.ProjectDriveFiles
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId && x.FolderId == folderId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return new ProjectDriveFolderContent
        {
            ProjectId = projectId,
            FolderId = folderId,
            FolderName = folder?.Name,
            Folders = folders,
            Files = files
        };
    }

    public async Task<ProjectFolder> CreateFolderAsync(Guid projectId, Guid? parentFolderId, string name, bool canViewAdminOnlyRecords, CancellationToken cancellationToken = default)
    {
        await EnsureProjectAccessibleAsync(projectId, canViewAdminOnlyRecords, cancellationToken);

        var normalizedName = NormalizeFolderName(name);

        if (parentFolderId.HasValue)
        {
            await GetFolderForProjectAsync(projectId, parentFolderId.Value, cancellationToken);
        }

        await EnsureFolderNameIsUniqueAsync(projectId, parentFolderId, normalizedName, null, cancellationToken);

        var sortOrder = await _context.ProjectFolders
            .Where(x => x.ProjectId == projectId && x.ParentFolderId == parentFolderId)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var folder = new ProjectFolder
        {
            ProjectId = projectId,
            ParentFolderId = parentFolderId,
            Name = normalizedName,
            SortOrder = sortOrder + 1
        };

        _context.ProjectFolders.Add(folder);
        await _context.SaveChangesAsync(cancellationToken);
        return folder;
    }

    public async Task<ProjectFolder> RenameFolderAsync(Guid folderId, string name, bool canViewAdminOnlyRecords, CancellationToken cancellationToken = default)
    {
        var folder = await GetFolderWithProjectAsync(folderId, cancellationToken);
        EnsureProjectVisible(folder.Project, canViewAdminOnlyRecords);

        var normalizedName = NormalizeFolderName(name);
        await EnsureFolderNameIsUniqueAsync(folder.ProjectId, folder.ParentFolderId, normalizedName, folder.Id, cancellationToken);

        folder.Name = normalizedName;
        await _context.SaveChangesAsync(cancellationToken);
        return folder;
    }

    public async Task DeleteFolderAsync(Guid folderId, bool canViewAdminOnlyRecords, CancellationToken cancellationToken = default)
    {
        var folder = await GetFolderWithProjectAsync(folderId, cancellationToken);
        EnsureProjectVisible(folder.Project, canViewAdminOnlyRecords);

        var hasChildren = await _context.ProjectFolders.AnyAsync(x => x.ParentFolderId == folderId, cancellationToken);
        if (hasChildren)
        {
            throw new InvalidOperationException("Alt klasör içeren klasör silinemez.");
        }

        var hasFiles = await _context.ProjectDriveFiles.AnyAsync(x => x.FolderId == folderId, cancellationToken);
        if (hasFiles)
        {
            throw new InvalidOperationException("İçinde dosya bulunan klasör silinemez.");
        }

        _context.ProjectFolders.Remove(folder);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProjectDriveFile> CreateFileRecordAsync(ProjectDriveFile file, bool canViewAdminOnlyRecords, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        await EnsureProjectAccessibleAsync(file.ProjectId, canViewAdminOnlyRecords, cancellationToken);

        if (file.FolderId.HasValue)
        {
            await GetFolderForProjectAsync(file.ProjectId, file.FolderId.Value, cancellationToken);
        }

        file.OriginalFileName = NormalizeRequiredText(file.OriginalFileName, "Dosya adı zorunludur.", 260);
        file.StoredFileName = NormalizeRequiredText(file.StoredFileName, "Saklanan dosya adı zorunludur.", 260);
        file.RelativePath = NormalizeRequiredText(file.RelativePath, "Dosya yolu zorunludur.", 500);
        file.ContentType = NormalizeOptionalText(file.ContentType, 120);
        file.Description = NormalizeOptionalText(file.Description, 500);

        _context.ProjectDriveFiles.Add(file);
        await _context.SaveChangesAsync(cancellationToken);
        return file;
    }

    public async Task DeleteFileAsync(Guid fileId, bool canViewAdminOnlyRecords, CancellationToken cancellationToken = default)
    {
        var file = await GetFileWithProjectAsync(fileId, cancellationToken);
        EnsureProjectVisible(file.Project, canViewAdminOnlyRecords);

        _context.ProjectDriveFiles.Remove(file);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProjectDriveFile> MoveFileAsync(Guid fileId, Guid? targetFolderId, bool canViewAdminOnlyRecords, CancellationToken cancellationToken = default)
    {
        var file = await GetFileWithProjectAsync(fileId, cancellationToken);
        EnsureProjectVisible(file.Project, canViewAdminOnlyRecords);

        if (targetFolderId.HasValue)
        {
            await GetFolderForProjectAsync(file.ProjectId, targetFolderId.Value, cancellationToken);
        }

        file.FolderId = targetFolderId;
        await _context.SaveChangesAsync(cancellationToken);
        return file;
    }

    private static IReadOnlyList<ProjectDriveTreeNode> BuildTree(IEnumerable<ProjectFolder> folders, Guid? parentFolderId)
    {
        var children = folders
            .Where(x => x.ParentFolderId == parentFolderId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();

        return children
            .Select(x => new ProjectDriveTreeNode
            {
                Id = x.Id,
                ProjectId = x.ProjectId,
                ParentFolderId = x.ParentFolderId,
                Name = x.Name,
                SortOrder = x.SortOrder,
                Children = BuildTree(folders, x.Id)
            })
            .ToList();
    }

    private async Task EnsureProjectAccessibleAsync(Guid projectId, bool canViewAdminOnlyRecords, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken)
            ?? throw new KeyNotFoundException("Proje bulunamadı.");

        EnsureProjectVisible(project, canViewAdminOnlyRecords);
    }

    private static void EnsureProjectVisible(Project project, bool canViewAdminOnlyRecords)
    {
        if (!canViewAdminOnlyRecords && project.Visibility != RecordVisibility.General)
        {
            throw new UnauthorizedAccessException("Bu projeye erişim yetkiniz yok.");
        }
    }

    private async Task<ProjectFolder> GetFolderForProjectAsync(Guid projectId, Guid folderId, CancellationToken cancellationToken)
    {
        return await _context.ProjectFolders
            .FirstOrDefaultAsync(x => x.Id == folderId && x.ProjectId == projectId, cancellationToken)
            ?? throw new KeyNotFoundException("Klasör bulunamadı.");
    }

    private async Task<ProjectFolder> GetFolderWithProjectAsync(Guid folderId, CancellationToken cancellationToken)
    {
        return await _context.ProjectFolders
            .Include(x => x.Project)
            .FirstOrDefaultAsync(x => x.Id == folderId, cancellationToken)
            ?? throw new KeyNotFoundException("Klasör bulunamadı.");
    }

    private async Task<ProjectDriveFile> GetFileWithProjectAsync(Guid fileId, CancellationToken cancellationToken)
    {
        return await _context.ProjectDriveFiles
            .Include(x => x.Project)
            .FirstOrDefaultAsync(x => x.Id == fileId, cancellationToken)
            ?? throw new KeyNotFoundException("Dosya bulunamadı.");
    }

    private async Task EnsureFolderNameIsUniqueAsync(Guid projectId, Guid? parentFolderId, string name, Guid? currentFolderId, CancellationToken cancellationToken)
    {
        var query = _context.ProjectFolders.Where(x =>
            x.ProjectId == projectId &&
            x.ParentFolderId == parentFolderId &&
            x.Name == name);

        if (currentFolderId.HasValue)
        {
            query = query.Where(x => x.Id != currentFolderId.Value);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException("Aynı klasör seviyesinde bu isimde başka bir klasör zaten var.");
        }
    }

    private static string NormalizeFolderName(string name)
    {
        return NormalizeRequiredText(name, "Klasör adı zorunludur.", 180);
    }

    private static string NormalizeRequiredText(string? value, string errorMessage, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException(errorMessage);
        }

        if (normalized.Length > maxLength)
        {
            throw new InvalidOperationException($"Metin alanı en fazla {maxLength} karakter olabilir.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new InvalidOperationException($"Metin alanı en fazla {maxLength} karakter olabilir.");
        }

        return normalized;
    }
}
