using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Business.Web.Controllers;

[Authorize]
public class RecordActivityController : Controller
{
    private const long MaxUploadSize = 25 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string[]> AllowedUploadTypes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ["application/pdf"],
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".png"] = ["image/png"],
        [".webp"] = ["image/webp"],
        [".gif"] = ["image/gif"],
        [".doc"] = ["application/msword"],
        [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
        [".xls"] = ["application/vnd.ms-excel"],
        [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
        [".csv"] = ["text/csv", "application/csv", "application/vnd.ms-excel", "text/plain"],
        [".txt"] = ["text/plain"],
        [".dwg"] = ["image/vnd.dwg", "application/acad", "application/x-acad", "application/autocad_dwg", "application/octet-stream"],
        [".dxf"] = ["image/vnd.dxf", "application/dxf", "application/x-dxf", "application/octet-stream"]
    };

    private readonly IRecordActivityService _recordActivityService;
    private readonly IWebHostEnvironment _environment;
    private readonly IProjectTimelineService _projectTimelineService;

    public RecordActivityController(
        IRecordActivityService recordActivityService,
        IWebHostEnvironment environment,
        IProjectTimelineService projectTimelineService)
    {
        _recordActivityService = recordActivityService;
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
    public async Task<IActionResult> AddFile(RecordOwnerType ownerType, Guid ownerId, IFormFile? file, string? description, string? returnUrl, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(description) && description.Length > 500)
        {
            TempData["Error"] = "Dosya açıklaması en fazla 500 karakter olabilir.";
            return RedirectToLocal(returnUrl);
        }

        if (file is not null && file.Length > 0)
        {
            if (file.Length > MaxUploadSize)
            {
                TempData["Error"] = "Dosya boyutu en fazla 25 MB olabilir.";
                return RedirectToLocal(returnUrl);
            }

            var originalFileName = Path.GetFileName(file.FileName);
            if (originalFileName.Length > 260)
            {
                TempData["Error"] = "Dosya adı çok uzun.";
                return RedirectToLocal(returnUrl);
            }

            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            if (!IsAllowedUploadType(extension, file.ContentType))
            {
                TempData["Error"] = $"Bu dosya turu yuklenemez. Izin verilen turler: {GetAllowedExtensionsText()}";
                return RedirectToLocal(returnUrl);
            }

            var uploadRoot = Path.Combine(_environment.WebRootPath, "uploads", ownerType.ToString(), ownerId.ToString("N"));
            Directory.CreateDirectory(uploadRoot);

            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(uploadRoot, storedFileName);

            await using (var stream = System.IO.File.Create(physicalPath))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var relativePath = $"/uploads/{ownerType}/{ownerId:N}/{storedFileName}";
            await _recordActivityService.AddFileAsync(new RecordFile
            {
                OwnerType = ownerType,
                OwnerId = ownerId,
                OriginalFileName = originalFileName,
                StoredFileName = storedFileName,
                RelativePath = relativePath,
                ContentType = file.ContentType,
                Size = file.Length,
                Description = description
            }, cancellationToken);

            await AddTimelineForActivityAsync(ownerType, ownerId, "Dosya eklendi", originalFileName, cancellationToken);
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

    private static bool IsAllowedUploadType(string extension, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(extension) || !AllowedUploadTypes.TryGetValue(extension, out var allowedContentTypes))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            return true;
        }

        return allowedContentTypes.Contains(contentType.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private static string GetAllowedExtensionsText()
    {
        return string.Join(", ", AllowedUploadTypes.Keys.Order(StringComparer.OrdinalIgnoreCase));
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
