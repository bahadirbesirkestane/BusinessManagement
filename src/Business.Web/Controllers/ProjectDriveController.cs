using Business.Application.Services;
using Business.Domain.Entities;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.Extensions;
using Business.Web.Services;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanViewProjects)]
[Route("Projects/{projectId:guid}/Drive")]
public class ProjectDriveController : Controller
{
    private readonly IProjectDriveService _projectDriveService;
    private readonly IProjectDriveUploadService _projectDriveUploadService;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ProjectDriveController(
        IProjectDriveService projectDriveService,
        IProjectDriveUploadService projectDriveUploadService,
        ApplicationDbContext context,
        IWebHostEnvironment environment)
    {
        _projectDriveService = projectDriveService;
        _projectDriveUploadService = projectDriveUploadService;
        _context = context;
        _environment = environment;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid projectId, Guid? folderId, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .ApplyRecordVisibility(User, includeArchived: true, onlyArchived: false)
            .FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

        if (project is null)
        {
            return NotFound();
        }

        try
        {
            var canViewAdminOnlyRecords = User.CanViewAdminOnlyRecords();
            var tree = await _projectDriveService.GetFolderTreeAsync(projectId, canViewAdminOnlyRecords, cancellationToken);
            var content = await _projectDriveService.GetFolderContentAsync(projectId, folderId, canViewAdminOnlyRecords, cancellationToken);
            var model = BuildIndexViewModel(project, tree, content);

            ViewData["Title"] = $"{project.Code} Dosyalar";
            ViewBag.Breadcrumbs = new Dictionary<string, string?>
            {
                ["Projeler"] = Url.Action("Index", "Projects"),
                [project.Code] = Url.Action("Details", "Projects", new { id = project.Id }),
                ["Dosyalar"] = null
            };

            return View(model);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
    }

    [HttpPost("CreateFolder")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanUpdateProjects)]
    public async Task<IActionResult> CreateFolder(Guid projectId, Guid? parentFolderId, string name, CancellationToken cancellationToken)
    {
        try
        {
            await _projectDriveService.CreateFolderAsync(projectId, parentFolderId, name, User.CanViewAdminOnlyRecords(), cancellationToken);
            TempData["Success"] = "Klasör oluşturuldu.";
            return RedirectToAction(nameof(Index), new { projectId, folderId = parentFolderId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index), new { projectId, folderId = parentFolderId });
        }
    }

    [HttpPost("RenameFolder")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanUpdateProjects)]
    public async Task<IActionResult> RenameFolder(Guid projectId, Guid folderId, string name, Guid? returnFolderId, CancellationToken cancellationToken)
    {
        try
        {
            await _projectDriveService.RenameFolderAsync(folderId, name, User.CanViewAdminOnlyRecords(), cancellationToken);
            TempData["Success"] = "Klasör adı güncellendi.";
            return RedirectToAction(nameof(Index), new { projectId, folderId = returnFolderId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index), new { projectId, folderId = returnFolderId });
        }
    }

    [HttpPost("DeleteFolder")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanUpdateProjects)]
    public async Task<IActionResult> DeleteFolder(Guid projectId, Guid folderId, Guid? returnFolderId, CancellationToken cancellationToken)
    {
        try
        {
            await _projectDriveService.DeleteFolderAsync(folderId, User.CanViewAdminOnlyRecords(), cancellationToken);
            TempData["Success"] = "Klasör silindi.";
            return RedirectToAction(nameof(Index), new { projectId, folderId = returnFolderId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index), new { projectId, folderId = returnFolderId });
        }
    }

    [HttpPost("UploadFiles")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanUpdateProjects)]
    public async Task<IActionResult> UploadFiles(Guid projectId, Guid? folderId, List<IFormFile>? files, string? description, CancellationToken cancellationToken)
    {
        try
        {
            var savedFiles = await _projectDriveUploadService.SaveFilesAsync(
                projectId,
                folderId,
                files,
                description,
                User.CanViewAdminOnlyRecords(),
                cancellationToken);

            TempData["Success"] = savedFiles.Count switch
            {
                0 => "Yüklenecek dosya bulunamadı.",
                1 => "Dosya yüklendi.",
                _ => $"{savedFiles.Count} dosya yüklendi."
            };

            return RedirectToAction(nameof(Index), new { projectId, folderId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index), new { projectId, folderId });
        }
    }

    [HttpPost("DeleteFile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFile(Guid projectId, Guid fileId, Guid? folderId, CancellationToken cancellationToken)
    {
        if (!CanDeleteDriveFiles())
        {
            return NotFound();
        }

        try
        {
            await _projectDriveUploadService.DeleteFileAsync(fileId, User.CanViewAdminOnlyRecords(), cancellationToken);
            TempData["Success"] = "Dosya silindi.";
            return RedirectToAction(nameof(Index), new { projectId, folderId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index), new { projectId, folderId });
        }
    }

    [HttpGet("DownloadFile/{fileId:guid}")]
    public async Task<IActionResult> DownloadFile(Guid projectId, Guid fileId, CancellationToken cancellationToken)
    {
        var file = await _context.ProjectDriveFiles
            .Include(x => x.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == fileId && x.ProjectId == projectId, cancellationToken);

        if (file is null || !file.Project.IsVisibleTo(User))
        {
            return NotFound();
        }

        string physicalPath;
        try
        {
            physicalPath = GetSafePhysicalPath(file.RelativePath);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        if (!System.IO.File.Exists(physicalPath))
        {
            return NotFound();
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;

        return PhysicalFile(physicalPath, contentType, file.OriginalFileName);
    }

    private bool CanManageDrive()
    {
        return User.HasClaim(AppClaimTypes.Permission, AppPermissions.ProjectsUpdate) ||
               User.HasClaim(AppClaimTypes.Permission, AppPermissions.ProjectsManage);
    }

    private bool CanDeleteDriveFiles()
    {
        return CanManageDrive() ||
               User.IsInRole(AppRoles.Admin) ||
               User.HasClaim(AppClaimTypes.Permission, AppPermissions.ProjectDriveDeleteFiles);
    }

    private ProjectDriveIndexViewModel BuildIndexViewModel(Project project, IReadOnlyList<ProjectDriveTreeNode> tree, ProjectDriveFolderContent content)
    {
        return new ProjectDriveIndexViewModel
        {
            ProjectId = project.Id,
            ProjectCode = project.Code,
            ProjectName = project.Name,
            CurrentFolderId = content.FolderId,
            CurrentFolderName = content.FolderName ?? "Kök klasör",
            CanManage = CanManageDrive(),
            CanDeleteFiles = CanDeleteDriveFiles(),
            MaxUploadSizeBytes = _projectDriveUploadService.MaxUploadSizeBytes,
            AllowedExtensionsText = _projectDriveUploadService.GetAllowedExtensionsText(),
            SubFolderCount = content.Folders.Count,
            Breadcrumbs = BuildFolderBreadcrumbs(tree, content.FolderId),
            FolderTree = tree.Select(node => MapTreeNode(node, content.FolderId)).ToList(),
            Files = content.Files
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ProjectDriveFileListItemViewModel
                {
                    Id = x.Id,
                    OriginalFileName = x.OriginalFileName,
                    RelativePath = x.RelativePath,
                    Description = x.Description,
                    ContentType = x.ContentType,
                    Size = x.Size,
                    CreatedAtText = x.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
                })
                .ToList()
        };
    }

    private static ProjectDriveFolderTreeItemViewModel MapTreeNode(ProjectDriveTreeNode node, Guid? currentFolderId)
    {
        return new ProjectDriveFolderTreeItemViewModel
        {
            Id = node.Id,
            ParentFolderId = node.ParentFolderId,
            Name = node.Name,
            IsSelected = node.Id == currentFolderId,
            Children = node.Children.Select(child => MapTreeNode(child, currentFolderId)).ToList()
        };
    }

    private static IReadOnlyList<ProjectDriveBreadcrumbItemViewModel> BuildFolderBreadcrumbs(IReadOnlyList<ProjectDriveTreeNode> tree, Guid? currentFolderId)
    {
        var breadcrumbs = new List<ProjectDriveBreadcrumbItemViewModel>
        {
            new()
            {
                Label = "Kök klasör",
                FolderId = null,
                IsActive = currentFolderId is null
            }
        };

        if (!currentFolderId.HasValue)
        {
            return breadcrumbs;
        }

        var path = FindPath(tree, currentFolderId.Value);
        if (path.Count == 0)
        {
            return breadcrumbs;
        }

        breadcrumbs.AddRange(path.Select(x => new ProjectDriveBreadcrumbItemViewModel
        {
            Label = x.Name,
            FolderId = x.Id,
            IsActive = x.Id == currentFolderId.Value
        }));

        return breadcrumbs;
    }

    private static List<ProjectDriveTreeNode> FindPath(IEnumerable<ProjectDriveTreeNode> nodes, Guid folderId)
    {
        foreach (var node in nodes)
        {
            if (node.Id == folderId)
            {
                return [node];
            }

            var childPath = FindPath(node.Children, folderId);
            if (childPath.Count > 0)
            {
                childPath.Insert(0, node);
                return childPath;
            }
        }

        return [];
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
}
