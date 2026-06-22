using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Identity;
using Business.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Business.Web.Controllers;

[Authorize]
public class RecordActivityController : Controller
{
    private readonly IRecordActivityService _recordActivityService;
    private readonly IRecordFileUploadService _recordFileUploadService;
    private readonly IWebHostEnvironment _environment;
    private readonly IProjectTimelineService _projectTimelineService;

    public RecordActivityController(
        IRecordActivityService recordActivityService,
        IRecordFileUploadService recordFileUploadService,
        IWebHostEnvironment environment,
        IProjectTimelineService projectTimelineService)
    {
        _recordActivityService = recordActivityService;
        _recordFileUploadService = recordFileUploadService;
        _environment = environment;
        _projectTimelineService = projectTimelineService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(RecordOwnerType ownerType, Guid ownerId, string commentText, string? returnUrl, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(commentText) && commentText.Length > 2000)
        {
            TempData["Error"] = "Yorum en fazla 2000 karakter olabilir.";
            return RedirectToLocal(returnUrl);
        }

        if (!string.IsNullOrWhiteSpace(commentText))
        {
            await _recordActivityService.AddCommentAsync(new RecordComment
            {
                OwnerType = ownerType,
                OwnerId = ownerId,
                CommentText = commentText.Trim()
            }, cancellationToken);

            await AddTimelineForActivityAsync(ownerType, ownerId, "Yorum eklendi", commentText.Trim(), cancellationToken);
        }

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFile(RecordOwnerType ownerType, Guid ownerId, List<IFormFile>? files, string? description, string? returnUrl, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(description) && description.Length > 500)
        {
            TempData["Error"] = "Dosya açıklaması en fazla 500 karakter olabilir.";
            return RedirectToLocal(returnUrl);
        }

        var validFiles = files?
            .Where(file => file is not null && file.Length > 0)
            .ToList() ?? [];

        if (validFiles.Count == 0)
        {
            TempData["Error"] = "Lütfen en az bir dosya seçin.";
            return RedirectToLocal(returnUrl);
        }

        if (!_recordFileUploadService.TryValidateFiles(validFiles, out var errorMessage))
        {
            TempData["Error"] = errorMessage;
            return RedirectToLocal(returnUrl);
        }

        var savedFiles = await _recordFileUploadService.SaveFilesAsync(ownerType, ownerId, validFiles, description, cancellationToken);
        foreach (var savedFile in savedFiles)
        {
            await _recordActivityService.AddFileAsync(savedFile, cancellationToken);
            await AddTimelineForActivityAsync(ownerType, ownerId, "Dosya eklendi", savedFile.OriginalFileName, cancellationToken);
        }

        if (savedFiles.Count > 1)
        {
            TempData["Success"] = $"{savedFiles.Count} dosya yüklendi.";
        }

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteComment(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        await _recordActivityService.DeleteCommentAsync(id, cancellationToken);
        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteFile(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var file = await _recordActivityService.GetFileAsync(id, cancellationToken);
        if (file is not null)
        {
            var physicalPath = Path.Combine(_environment.WebRootPath, file.RelativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }
        }

        await _recordActivityService.DeleteFileAsync(id, cancellationToken);
        return RedirectToLocal(returnUrl);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Dashboard");
    }

    private async Task AddTimelineForActivityAsync(RecordOwnerType ownerType, Guid ownerId, string title, string description, CancellationToken cancellationToken)
    {
        if (ownerType == RecordOwnerType.Project)
        {
            await _projectTimelineService.AddAsync(ownerId, title, description, cancellationToken);
        }
        else if (ownerType == RecordOwnerType.ProjectTask)
        {
            await _projectTimelineService.AddForTaskAsync(ownerId, title, description, cancellationToken);
        }
    }
}
