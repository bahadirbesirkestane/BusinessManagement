using Business.Domain.Entities;

namespace Business.Application.Services;

public interface IProjectDriveService
{
    Task<IReadOnlyList<ProjectDriveTreeNode>> GetFolderTreeAsync(Guid projectId, bool canViewAdminOnlyRecords, CancellationToken cancellationToken = default);
    Task<ProjectDriveFolderContent> GetFolderContentAsync(Guid projectId, Guid? folderId, bool canViewAdminOnlyRecords, CancellationToken cancellationToken = default);
    Task<ProjectFolder> CreateFolderAsync(Guid projectId, Guid? parentFolderId, string name, bool canViewAdminOnlyRecords, CancellationToken cancellationToken = default);
    Task<ProjectFolder> RenameFolderAsync(Guid folderId, string name, bool canViewAdminOnlyRecords, CancellationToken cancellationToken = default);
    Task DeleteFolderAsync(Guid folderId, bool canViewAdminOnlyRecords, CancellationToken cancellationToken = default);
    Task<ProjectDriveFile> CreateFileRecordAsync(ProjectDriveFile file, bool canViewAdminOnlyRecords, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(Guid fileId, bool canViewAdminOnlyRecords, CancellationToken cancellationToken = default);
    Task<ProjectDriveFile> MoveFileAsync(Guid fileId, Guid? targetFolderId, bool canViewAdminOnlyRecords, CancellationToken cancellationToken = default);
}
