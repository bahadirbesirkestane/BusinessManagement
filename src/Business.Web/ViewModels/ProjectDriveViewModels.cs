namespace Business.Web.ViewModels;

public class ProjectDriveIndexViewModel
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public Guid? CurrentFolderId { get; set; }
    public string CurrentFolderName { get; set; } = "Kök klasör";
    public bool CanManage { get; set; }
    public long MaxUploadSizeBytes { get; set; }
    public string AllowedExtensionsText { get; set; } = string.Empty;
    public IReadOnlyList<ProjectDriveBreadcrumbItemViewModel> Breadcrumbs { get; set; } = [];
    public IReadOnlyList<ProjectDriveFolderTreeItemViewModel> FolderTree { get; set; } = [];
    public IReadOnlyList<ProjectDriveFolderListItemViewModel> Folders { get; set; } = [];
    public IReadOnlyList<ProjectDriveFileListItemViewModel> Files { get; set; } = [];
}

public class ProjectDriveBreadcrumbItemViewModel
{
    public string Label { get; set; } = string.Empty;
    public Guid? FolderId { get; set; }
    public bool IsActive { get; set; }
}

public class ProjectDriveFolderTreeItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public IReadOnlyList<ProjectDriveFolderTreeItemViewModel> Children { get; set; } = [];
}

public class ProjectDriveFolderListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ProjectDriveFileListItemViewModel
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContentType { get; set; }
    public long Size { get; set; }
    public string CreatedAtText { get; set; } = string.Empty;
}
