using Business.Domain.Entities;

namespace Business.Application.Services;

public class ProjectDriveTreeNode
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? ParentFolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public IReadOnlyList<ProjectDriveTreeNode> Children { get; set; } = [];
}

public class ProjectDriveFolderContent
{
    public Guid ProjectId { get; set; }
    public Guid? FolderId { get; set; }
    public string? FolderName { get; set; }
    public IReadOnlyList<ProjectFolder> Folders { get; set; } = [];
    public IReadOnlyList<ProjectDriveFile> Files { get; set; } = [];
}
