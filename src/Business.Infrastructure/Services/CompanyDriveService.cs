using Business.Application.Services;
using Business.Domain.Entities;
using Business.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Services;

public class CompanyDriveService : ICompanyDriveService
{
    private readonly ApplicationDbContext _context;

    public CompanyDriveService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CompanyDriveTreeNode>> GetFolderTreeAsync(CancellationToken cancellationToken = default)
    {
        var folders = await _context.CompanyFolders
            .AsNoTracking()
            .Where(x => x.DepartmentId == null)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return BuildTree(folders, null);
    }

    public async Task<CompanyDriveFolderContent> GetFolderContentAsync(Guid? folderId, CancellationToken cancellationToken = default)
    {
        CompanyFolder? folder = null;
        if (folderId.HasValue)
        {
            folder = await GetFolderInGeneralArchiveAsync(folderId.Value, cancellationToken);
        }

        var folders = await _context.CompanyFolders
            .AsNoTracking()
            .Where(x => x.DepartmentId == null && x.ParentFolderId == folderId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var files = await _context.CompanyFiles
            .AsNoTracking()
            .Where(x => x.DepartmentId == null && x.FolderId == folderId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return new CompanyDriveFolderContent
        {
            DepartmentId = null,
            FolderId = folderId,
            FolderName = folder?.Name,
            Folders = folders,
            Files = files
        };
    }

    public async Task<CompanyFolder> CreateFolderAsync(Guid? parentFolderId, string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeFolderName(name);

        if (parentFolderId.HasValue)
        {
            await GetFolderInGeneralArchiveAsync(parentFolderId.Value, cancellationToken);
        }

        await EnsureFolderNameIsUniqueAsync(parentFolderId, normalizedName, null, cancellationToken);

        var sortOrder = await _context.CompanyFolders
            .Where(x => x.DepartmentId == null && x.ParentFolderId == parentFolderId)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var folder = new CompanyFolder
        {
            DepartmentId = null,
            ParentFolderId = parentFolderId,
            Name = normalizedName,
            SortOrder = sortOrder + 1
        };

        _context.CompanyFolders.Add(folder);
        await _context.SaveChangesAsync(cancellationToken);
        return folder;
    }

    public async Task<CompanyFolder> RenameFolderAsync(Guid folderId, string name, CancellationToken cancellationToken = default)
    {
        var folder = await GetFolderInGeneralArchiveAsync(folderId, cancellationToken);
        var normalizedName = NormalizeFolderName(name);
        await EnsureFolderNameIsUniqueAsync(folder.ParentFolderId, normalizedName, folder.Id, cancellationToken);

        folder.Name = normalizedName;
        await _context.SaveChangesAsync(cancellationToken);
        return folder;
    }

    public async Task DeleteFolderAsync(Guid folderId, CancellationToken cancellationToken = default)
    {
        var folder = await GetFolderInGeneralArchiveAsync(folderId, cancellationToken);

        var hasChildren = await _context.CompanyFolders
            .AnyAsync(x => x.ParentFolderId == folderId, cancellationToken);
        if (hasChildren)
        {
            throw new InvalidOperationException("Alt klasör içeren klasör silinemez.");
        }

        var hasFiles = await _context.CompanyFiles
            .AnyAsync(x => x.FolderId == folderId, cancellationToken);
        if (hasFiles)
        {
            throw new InvalidOperationException("İçinde dosya bulunan klasör silinemez.");
        }

        _context.CompanyFolders.Remove(folder);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<CompanyFile> CreateFileRecordAsync(CompanyFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        file.DepartmentId = null;

        if (file.FolderId.HasValue)
        {
            await GetFolderInGeneralArchiveAsync(file.FolderId.Value, cancellationToken);
        }

        file.OriginalFileName = NormalizeRequiredText(file.OriginalFileName, "Dosya adı zorunludur.", 260);
        file.StoredFileName = NormalizeRequiredText(file.StoredFileName, "Saklanan dosya adı zorunludur.", 260);
        file.RelativePath = NormalizeRequiredText(file.RelativePath, "Dosya yolu zorunludur.", 500);
        file.ContentType = NormalizeOptionalText(file.ContentType, 120);
        file.Description = NormalizeOptionalText(file.Description, 500);

        _context.CompanyFiles.Add(file);
        await _context.SaveChangesAsync(cancellationToken);
        return file;
    }

    public async Task DeleteFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        var file = await _context.CompanyFiles
            .FirstOrDefaultAsync(x => x.Id == fileId && x.DepartmentId == null, cancellationToken)
            ?? throw new KeyNotFoundException("Dosya bulunamadı.");

        _context.CompanyFiles.Remove(file);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<CompanyDriveTreeNode> BuildTree(IEnumerable<CompanyFolder> folders, Guid? parentFolderId)
    {
        var children = folders
            .Where(x => x.ParentFolderId == parentFolderId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();

        return children
            .Select(x => new CompanyDriveTreeNode
            {
                Id = x.Id,
                DepartmentId = x.DepartmentId,
                ParentFolderId = x.ParentFolderId,
                Name = x.Name,
                SortOrder = x.SortOrder,
                Children = BuildTree(folders, x.Id)
            })
            .ToList();
    }

    private async Task<CompanyFolder> GetFolderInGeneralArchiveAsync(Guid folderId, CancellationToken cancellationToken)
    {
        return await _context.CompanyFolders
            .FirstOrDefaultAsync(x => x.Id == folderId && x.DepartmentId == null, cancellationToken)
            ?? throw new KeyNotFoundException("Klasör bulunamadı.");
    }

    private async Task EnsureFolderNameIsUniqueAsync(Guid? parentFolderId, string name, Guid? currentFolderId, CancellationToken cancellationToken)
    {
        var query = _context.CompanyFolders.Where(x =>
            x.DepartmentId == null &&
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
