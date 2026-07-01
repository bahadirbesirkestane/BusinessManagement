namespace Business.Web.ViewModels;

public class CompanyDriveIndexViewModel
{
    public Guid? CurrentFolderId { get; set; }
    public string CurrentFolderName { get; set; } = "Kök klasör";
    public bool CanManage { get; set; }
    public long MaxUploadSizeBytes { get; set; }
    public string AllowedExtensionsText { get; set; } = string.Empty;
    public int SubFolderCount { get; set; }
    public IReadOnlyList<CompanyDriveBreadcrumbItemViewModel> Breadcrumbs { get; set; } = [];
    public IReadOnlyList<CompanyDriveFolderTreeItemViewModel> FolderTree { get; set; } = [];
    public IReadOnlyList<CompanyDriveFileListItemViewModel> Files { get; set; } = [];
}

public class CompanyDriveBreadcrumbItemViewModel
{
    public string Label { get; set; } = string.Empty;
    public Guid? FolderId { get; set; }
    public bool IsActive { get; set; }
}

public class CompanyDriveFolderTreeItemViewModel
{
    public Guid Id { get; set; }
    public Guid? ParentFolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public IReadOnlyList<CompanyDriveFolderTreeItemViewModel> Children { get; set; } = [];
}

public class CompanyDriveFileListItemViewModel
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContentType { get; set; }
    public long Size { get; set; }
    public string CreatedAtText { get; set; } = string.Empty;
}
