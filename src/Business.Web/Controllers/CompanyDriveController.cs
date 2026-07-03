using Business.Application.Services;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.Services;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanViewCompanyFiles)]
[Route("CompanyDrive")]
public class CompanyDriveController : Controller
{
    private readonly ICompanyDriveService _companyDriveService;
    private readonly ICompanyDriveUploadService _companyDriveUploadService;
    private readonly ApplicationDbContext _context;

    public CompanyDriveController(
        ICompanyDriveService companyDriveService,
        ICompanyDriveUploadService companyDriveUploadService,
        ApplicationDbContext context)
    {
        _companyDriveService = companyDriveService;
        _companyDriveUploadService = companyDriveUploadService;
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid? folderId, CancellationToken cancellationToken)
    {
        try
        {
            var tree = await _companyDriveService.GetFolderTreeAsync(cancellationToken);
            var content = await _companyDriveService.GetFolderContentAsync(folderId, cancellationToken);
            var model = BuildIndexViewModel(tree, content);

            ViewData["Title"] = "Firma Dosyalari";
            ViewBag.Breadcrumbs = new Dictionary<string, string?>
            {
                ["Firma Dosyalari"] = null
            };

            return View(model);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("CreateFolder")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageCompanyFiles)]
    public async Task<IActionResult> CreateFolder(Guid? parentFolderId, string name, CancellationToken cancellationToken)
    {
        try
        {
            await _companyDriveService.CreateFolderAsync(parentFolderId, name, cancellationToken);
            TempData["Success"] = "Klasor olusturuldu.";
            return RedirectToAction(nameof(Index), new { folderId = parentFolderId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index), new { folderId = parentFolderId });
        }
    }

    [HttpPost("RenameFolder")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageCompanyFiles)]
    public async Task<IActionResult> RenameFolder(Guid folderId, string name, Guid? returnFolderId, CancellationToken cancellationToken)
    {
        try
        {
            await _companyDriveService.RenameFolderAsync(folderId, name, cancellationToken);
            TempData["Success"] = "Klasor adi guncellendi.";
            return RedirectToAction(nameof(Index), new { folderId = returnFolderId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index), new { folderId = returnFolderId });
        }
    }

    [HttpPost("DeleteFolder")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageCompanyFiles)]
    public async Task<IActionResult> DeleteFolder(Guid folderId, Guid? returnFolderId, CancellationToken cancellationToken)
    {
        try
        {
            await _companyDriveService.DeleteFolderAsync(folderId, cancellationToken);
            TempData["Success"] = "Klasor silindi.";
            return RedirectToAction(nameof(Index), new { folderId = returnFolderId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index), new { folderId = returnFolderId });
        }
    }

    [HttpPost("UploadFiles")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageCompanyFiles)]
    public async Task<IActionResult> UploadFiles(Guid? folderId, List<IFormFile>? files, string? description, CancellationToken cancellationToken)
    {
        try
        {
            var savedFiles = await _companyDriveUploadService.SaveFilesAsync(
                folderId,
                files,
                description,
                cancellationToken);

            TempData["Success"] = savedFiles.Count switch
            {
                0 => "Yuklenecek dosya bulunamadi.",
                1 => "Dosya yuklendi.",
                _ => $"{savedFiles.Count} dosya yuklendi."
            };

            return RedirectToAction(nameof(Index), new { folderId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index), new { folderId });
        }
    }

    [HttpPost("DeleteFile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFile(Guid fileId, Guid? folderId, CancellationToken cancellationToken)
    {
        if (!CanDeleteDriveFiles())
        {
            return NotFound();
        }

        try
        {
            await _companyDriveUploadService.DeleteFileAsync(fileId, cancellationToken);
            TempData["Success"] = "Dosya silindi.";
            return RedirectToAction(nameof(Index), new { folderId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index), new { folderId });
        }
    }

    [HttpGet("DownloadFile/{fileId:guid}")]
    public async Task<IActionResult> DownloadFile(Guid fileId, CancellationToken cancellationToken)
    {
        var fileResult = await TryGetCompanyFileResultAsync(fileId, cancellationToken);
        if (fileResult is null)
        {
            return NotFound();
        }

        return PhysicalFile(fileResult.PhysicalPath, fileResult.ContentType, fileResult.OriginalFileName);
    }

    [HttpGet("OpenFile/{fileId:guid}")]
    public async Task<IActionResult> OpenFile(Guid fileId, CancellationToken cancellationToken)
    {
        var fileResult = await TryGetCompanyFileResultAsync(fileId, cancellationToken);
        if (fileResult is null)
        {
            return NotFound();
        }

        return PhysicalFile(fileResult.PhysicalPath, fileResult.ContentType);
    }

    private bool CanManageDrive()
    {
        return User.IsInRole(AppRoles.Admin) ||
               User.HasClaim(AppClaimTypes.Permission, AppPermissions.CompanyFilesManage);
    }

    private bool CanDeleteDriveFiles()
    {
        return CanManageDrive() ||
               User.HasClaim(AppClaimTypes.Permission, AppPermissions.CompanyFilesDeleteFiles);
    }

    private CompanyDriveIndexViewModel BuildIndexViewModel(IReadOnlyList<CompanyDriveTreeNode> tree, CompanyDriveFolderContent content)
    {
        return new CompanyDriveIndexViewModel
        {
            CurrentFolderId = content.FolderId,
            CurrentFolderName = content.FolderName ?? "Kok klasor",
            CanManage = CanManageDrive(),
            CanDeleteFiles = CanDeleteDriveFiles(),
            MaxUploadSizeBytes = _companyDriveUploadService.MaxUploadSizeBytes,
            AllowedExtensionsText = _companyDriveUploadService.GetAllowedExtensionsText(),
            SubFolderCount = content.Folders.Count,
            Breadcrumbs = BuildFolderBreadcrumbs(tree, content.FolderId),
            FolderTree = tree.Select(node => MapTreeNode(node, content.FolderId)).ToList(),
            Files = content.Files
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new CompanyDriveFileListItemViewModel
                {
                    Id = x.Id,
                    OriginalFileName = x.OriginalFileName,
                    Description = x.Description,
                    ContentType = x.ContentType,
                    Size = x.Size,
                    CreatedAtText = x.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
                })
                .ToList()
        };
    }

    private static CompanyDriveFolderTreeItemViewModel MapTreeNode(CompanyDriveTreeNode node, Guid? currentFolderId)
    {
        return new CompanyDriveFolderTreeItemViewModel
        {
            Id = node.Id,
            ParentFolderId = node.ParentFolderId,
            Name = node.Name,
            IsSelected = node.Id == currentFolderId,
            Children = node.Children.Select(child => MapTreeNode(child, currentFolderId)).ToList()
        };
    }

    private static IReadOnlyList<CompanyDriveBreadcrumbItemViewModel> BuildFolderBreadcrumbs(IReadOnlyList<CompanyDriveTreeNode> tree, Guid? currentFolderId)
    {
        var breadcrumbs = new List<CompanyDriveBreadcrumbItemViewModel>
        {
            new()
            {
                Label = "Kok klasor",
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

        breadcrumbs.AddRange(path.Select(x => new CompanyDriveBreadcrumbItemViewModel
        {
            Label = x.Name,
            FolderId = x.Id,
            IsActive = x.Id == currentFolderId.Value
        }));

        return breadcrumbs;
    }

    private static List<CompanyDriveTreeNode> FindPath(IEnumerable<CompanyDriveTreeNode> nodes, Guid folderId)
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

    private async Task<CompanyFileResult?> TryGetCompanyFileResultAsync(Guid fileId, CancellationToken cancellationToken)
    {
        var file = await _context.CompanyFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == fileId && x.DepartmentId == null, cancellationToken);

        if (file is null)
        {
            return null;
        }

        string physicalPath;
        try
        {
            physicalPath = _companyDriveUploadService.GetSafePhysicalPath(file.RelativePath);
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        if (!System.IO.File.Exists(physicalPath))
        {
            return null;
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;

        return new CompanyFileResult(physicalPath, contentType, file.OriginalFileName);
    }

    private sealed record CompanyFileResult(string PhysicalPath, string ContentType, string OriginalFileName);
}
