using Business.Domain.Entities;

namespace Business.Application.Services;

public class CompanyDriveTreeNode
{
    public Guid Id { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ParentFolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public IReadOnlyList<CompanyDriveTreeNode> Children { get; set; } = [];
}

public class CompanyDriveFolderContent
{
    public Guid? DepartmentId { get; set; }
    public Guid? FolderId { get; set; }
    public string? FolderName { get; set; }
    public IReadOnlyList<CompanyFolder> Folders { get; set; } = [];
    public IReadOnlyList<CompanyFile> Files { get; set; } = [];
}
