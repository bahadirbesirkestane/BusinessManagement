using Business.Domain.Entities;

namespace Business.Application.Services;

public interface ICompanyDriveService
{
    Task<IReadOnlyList<CompanyDriveTreeNode>> GetFolderTreeAsync(CancellationToken cancellationToken = default);
    Task<CompanyDriveFolderContent> GetFolderContentAsync(Guid? folderId, CancellationToken cancellationToken = default);
    Task<CompanyFolder> CreateFolderAsync(Guid? parentFolderId, string name, CancellationToken cancellationToken = default);
    Task<CompanyFolder> RenameFolderAsync(Guid folderId, string name, CancellationToken cancellationToken = default);
    Task DeleteFolderAsync(Guid folderId, CancellationToken cancellationToken = default);
    Task<CompanyFile> CreateFileRecordAsync(CompanyFile file, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(Guid fileId, CancellationToken cancellationToken = default);
}
