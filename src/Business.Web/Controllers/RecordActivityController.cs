using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.Extensions;
using Business.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace Business.Web.Controllers;

[Authorize]
public class RecordActivityController : Controller
{
    private const long MaxMailAttachmentTotalBytes = 25L * 1024 * 1024;
    private const string MaxMailAttachmentTotalSizeLabel = "25 MB";
    private readonly IRecordActivityService _recordActivityService;
    private readonly IRecordFileUploadService _recordFileUploadService;
    private readonly IWebHostEnvironment _environment;
    private readonly IProjectTimelineService _projectTimelineService;
    private readonly ApplicationDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly PublicAppUrlOptions _publicAppUrlOptions;

    public RecordActivityController(
        IRecordActivityService recordActivityService,
        IRecordFileUploadService recordFileUploadService,
        IWebHostEnvironment environment,
        IProjectTimelineService projectTimelineService,
        ApplicationDbContext context,
        IEmailSender emailSender,
        IOptions<PublicAppUrlOptions> publicAppUrlOptions)
    {
        _recordActivityService = recordActivityService;
        _recordFileUploadService = recordFileUploadService;
        _environment = environment;
        _projectTimelineService = projectTimelineService;
        _context = context;
        _emailSender = emailSender;
        _publicAppUrlOptions = publicAppUrlOptions.Value;
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
    public async Task<IActionResult> AddFile(
        RecordOwnerType ownerType,
        Guid ownerId,
        List<IFormFile>? files,
        string? description,
        string? returnUrl,
        bool sendTaskEmail,
        bool emailIncludeContent,
        bool emailIncludeAttachments,
        CancellationToken cancellationToken)
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

        if (sendTaskEmail && ownerType == RecordOwnerType.ProjectTask && !emailIncludeContent && !emailIncludeAttachments)
        {
            TempData["Error"] = "Mail gönderimi seçildiğinde içerik veya dosya eki seçeneklerinden en az biri işaretlenmelidir.";
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

        var warningMessages = new List<string>();

        if (savedFiles.Count > 1)
        {
            TempData["Success"] = $"{savedFiles.Count} dosya yüklendi.";
        }

        if (sendTaskEmail && ownerType == RecordOwnerType.ProjectTask)
        {
            var taskMailSettings = await GetTaskEmailSettingAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(taskMailSettings?.RecipientEmails))
            {
                warningMessages.Add("Dosyalar kaydedildi fakat görev mail alıcıları ayarlanmadığı için e-posta gönderilemedi.");
            }
            else
            {
                var includeAttachmentsInMail = emailIncludeAttachments && savedFiles.Count > 0;
                if (includeAttachmentsInMail && !AreAttachmentsWithinMailLimit(savedFiles))
                {
                    if (emailIncludeContent)
                    {
                        warningMessages.Add($"Mail eklerinin toplam boyutu {MaxMailAttachmentTotalSizeLabel} sınırını aştığı için yalnızca görev içeriği gönderildi.");
                    }
                    else
                    {
                        warningMessages.Add($"Dosyalar kaydedildi fakat mail eklerinin toplam boyutu {MaxMailAttachmentTotalSizeLabel} sınırını aştığı için e-posta gönderilemedi.");
                    }

                    includeAttachmentsInMail = false;
                }

                if (emailIncludeContent || includeAttachmentsInMail)
                {
                    try
                    {
                        await SendTaskFilesAddedEmailAsync(
                            ownerId,
                            taskMailSettings.RecipientEmails!,
                            emailIncludeContent,
                            includeAttachmentsInMail,
                            savedFiles,
                            description,
                            cancellationToken);
                    }
                    catch (Exception)
                    {
                        warningMessages.Add("Dosyalar kaydedildi fakat e-posta gönderilemedi.");
                    }
                }
            }
        }

        if (warningMessages.Count > 0)
        {
            TempData["Error"] = string.Join(" ", warningMessages.Distinct());
        }

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var comment = await _recordActivityService.GetCommentAsync(id, cancellationToken);
        if (comment is null || !await CanDeleteActivityAsync(comment.OwnerType, comment.OwnerId, cancellationToken))
        {
            return NotFound();
        }

        await _recordActivityService.DeleteCommentAsync(id, cancellationToken);
        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFile(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var file = await _recordActivityService.GetFileAsync(id, cancellationToken);
        if (file is null || !await CanDeleteActivityAsync(file.OwnerType, file.OwnerId, cancellationToken))
        {
            return NotFound();
        }

        var physicalPath = Path.Combine(_environment.WebRootPath, file.RelativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }

        await _recordActivityService.DeleteFileAsync(id, cancellationToken);
        return RedirectToLocal(returnUrl);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadFile(Guid id, CancellationToken cancellationToken)
    {
        var file = await _recordActivityService.GetFileAsync(id, cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        if (!await CanAccessOwnerAsync(file.OwnerType, file.OwnerId, cancellationToken))
        {
            return NotFound();
        }

        var relativePath = file.RelativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var physicalPath = Path.Combine(_environment.WebRootPath, relativePath);
        if (!System.IO.File.Exists(physicalPath))
        {
            return NotFound();
        }

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(file.OriginalFileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return PhysicalFile(physicalPath, contentType, file.OriginalFileName);
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

    private async Task<bool> CanAccessOwnerAsync(RecordOwnerType ownerType, Guid ownerId, CancellationToken cancellationToken)
    {
        return ownerType switch
        {
            RecordOwnerType.Project => await _context.Projects
                .AsNoTracking()
                .ApplyRecordVisibility(User, includeArchived: true, onlyArchived: false)
                .AnyAsync(x => x.Id == ownerId, cancellationToken),

            RecordOwnerType.ProjectTask => await CanAccessTaskAsync(ownerId, cancellationToken),

            RecordOwnerType.PurchaseOrder => await _context.PurchaseOrders
                .Include(x => x.Project)
                .AsNoTracking()
                .ApplyRecordVisibility(User, includeArchived: true, onlyArchived: false)
                .AnyAsync(x => x.Id == ownerId, cancellationToken),

            RecordOwnerType.MaterialRequest => await _context.MaterialRequests
                .Include(x => x.Project)
                .AsNoTracking()
                .ApplyProjectRecordVisibility(User)
                .AnyAsync(x => x.Id == ownerId, cancellationToken),

            _ => false
        };
    }

    private async Task<bool> CanAccessTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await _context.ProjectTasks
            .Include(x => x.Project)
            .Include(x => x.Assignments)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);

        if (task is null || !task.IsVisibleTo(User))
        {
            return false;
        }

        if (User.IsInRole(AppRoles.Admin) ||
            User.HasClaim(AppClaimTypes.Permission, AppPermissions.TasksViewAll))
        {
            return true;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        return task.Assignments.Any(x => x.UserId == userId) ||
               task.AssignedToUserId == userId ||
               task.ResponsibleUserId == userId;
    }

    private async Task<bool> CanDeleteActivityAsync(RecordOwnerType ownerType, Guid ownerId, CancellationToken cancellationToken)
    {
        if (!await CanAccessOwnerAsync(ownerType, ownerId, cancellationToken))
        {
            return false;
        }

        if (User.IsInRole(AppRoles.Admin))
        {
            return true;
        }

        return ownerType switch
        {
            RecordOwnerType.Project => User.HasClaim(AppClaimTypes.Permission, AppPermissions.ProjectsDeleteActivity),
            RecordOwnerType.ProjectTask => User.HasClaim(AppClaimTypes.Permission, AppPermissions.TasksDeleteActivity),
            RecordOwnerType.PurchaseOrder => User.HasClaim(AppClaimTypes.Permission, AppPermissions.PurchasingDeleteActivity),
            RecordOwnerType.MaterialRequest => User.HasClaim(AppClaimTypes.Permission, AppPermissions.MaterialRequestsDeleteActivity),
            _ => false
        };
    }

    private Task<TaskEmailNotificationSetting?> GetTaskEmailSettingAsync(CancellationToken cancellationToken)
    {
        return _context.TaskEmailNotificationSettings
            .AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool AreAttachmentsWithinMailLimit(IEnumerable<RecordFile> files)
    {
        return files.Sum(file => Math.Max(0, file.Size)) <= MaxMailAttachmentTotalBytes;
    }

    private async Task SendTaskFilesAddedEmailAsync(
        Guid taskId,
        string recipientEmails,
        bool includeContent,
        bool includeAttachments,
        IReadOnlyList<RecordFile> files,
        string? description,
        CancellationToken cancellationToken)
    {
        var task = await _context.ProjectTasks
            .Include(x => x.Project)
            .ThenInclude(x => x!.Customer)
            .Include(x => x.Customer)
            .Include(x => x.TaskCategory)
            .Include(x => x.Assignments)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);

        if (task is null)
        {
            throw new InvalidOperationException("Görev bilgisi bulunamadığı için e-posta hazırlanamadı.");
        }

        var responsibleName = await GetUserDisplayNameAsync(task.ResponsibleUserId, cancellationToken);
        var assignedNames = await GetAssignedUserNamesAsync(task.Assignments.Select(x => x.UserId), cancellationToken);
        var recipients = ParseRecipientEmails(recipientEmails);
        var taskDetailUrl = BuildTaskDetailUrl(task.Id);

        var htmlBody = includeContent
            ? BuildTaskFilesAddedHtmlBody(task, responsibleName, assignedNames, files, description, taskDetailUrl)
            : "<p>Göreve yeni dosyalar eklendi. Dosyalar ektedir.</p>";

        var textBody = includeContent
            ? BuildTaskFilesAddedTextBody(task, responsibleName, assignedNames, files, description, taskDetailUrl)
            : "Göreve yeni dosyalar eklendi. Dosyalar ektedir.";

        if (!includeContent)
        {
            htmlBody = BuildTaskLinkOnlyHtmlBody("Göreve yeni dosyalar eklendi. Dosyalar ektedir.", taskDetailUrl);
            textBody = BuildTaskLinkOnlyTextBody("Göreve yeni dosyalar eklendi. Dosyalar ektedir.", taskDetailUrl);
        }

        var attachments = includeAttachments
            ? await CreateEmailAttachmentsAsync(files, cancellationToken)
            : [];

        await _emailSender.SendAsync(new EmailMessage
        {
            ToList = recipients,
            Subject = $"Göreve yeni dosya eklendi: {task.Title}",
            HtmlBody = htmlBody,
            TextBody = textBody,
            Attachments = attachments,
            RequireConfiguredDelivery = true
        }, cancellationToken);
    }

    private static IReadOnlyList<string> ParseRecipientEmails(string recipientEmails)
    {
        return recipientEmails
            .Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<IReadOnlyList<EmailAttachment>> CreateEmailAttachmentsAsync(IReadOnlyList<RecordFile> files, CancellationToken cancellationToken)
    {
        var attachments = new List<EmailAttachment>();
        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file.RelativePath))
            {
                continue;
            }

            var normalizedPath = file.RelativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var physicalPath = Path.Combine(_environment.WebRootPath, normalizedPath);
            if (!System.IO.File.Exists(physicalPath))
            {
                continue;
            }

            attachments.Add(new EmailAttachment
            {
                FileName = file.OriginalFileName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                Content = await System.IO.File.ReadAllBytesAsync(physicalPath, cancellationToken)
            });
        }

        return attachments;
    }

    private string BuildTaskFilesAddedHtmlBody(
        ProjectTask task,
        string responsibleName,
        IReadOnlyList<string> assignedNames,
        IReadOnlyList<RecordFile> files,
        string? description,
        string? taskDetailUrl)
    {
        var lines = CreateTaskDetailLines(task, responsibleName, assignedNames, files, description);
        var builder = new StringBuilder();
        builder.Append("<p>Bir göreve yeni dosyalar eklendi.</p>");
        builder.Append("<table style=\"border-collapse:collapse;width:100%;\">");

        foreach (var line in lines)
        {
            builder.Append("<tr>");
            builder.Append("<td style=\"padding:6px 10px;border:1px solid #d7dbe0;font-weight:600;width:220px;\">");
            builder.Append(HtmlEncoder.Default.Encode(line.Key));
            builder.Append("</td>");
            builder.Append("<td style=\"padding:6px 10px;border:1px solid #d7dbe0;\">");
            builder.Append(HtmlEncoder.Default.Encode(line.Value));
            builder.Append("</td>");
            builder.Append("</tr>");
        }

        builder.Append("</table>");
        if (!string.IsNullOrWhiteSpace(taskDetailUrl))
        {
            builder.Append("<p style=\"margin-top:16px;\">");
            builder.Append("<a href=\"");
            builder.Append(HtmlEncoder.Default.Encode(taskDetailUrl));
            builder.Append("\">Görev detayını aç</a>");
            builder.Append("</p>");
        }

        return builder.ToString();
    }

    private string BuildTaskFilesAddedTextBody(
        ProjectTask task,
        string responsibleName,
        IReadOnlyList<string> assignedNames,
        IReadOnlyList<RecordFile> files,
        string? description,
        string? taskDetailUrl)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Bir göreve yeni dosyalar eklendi.");
        builder.AppendLine();

        foreach (var line in CreateTaskDetailLines(task, responsibleName, assignedNames, files, description))
        {
            builder.AppendLine($"{line.Key}: {line.Value}");
        }

        if (!string.IsNullOrWhiteSpace(taskDetailUrl))
        {
            builder.AppendLine();
            builder.AppendLine($"Görev detayı: {taskDetailUrl}");
        }

        return builder.ToString();
    }

    private static string BuildTaskLinkOnlyHtmlBody(string message, string? taskDetailUrl)
    {
        var builder = new StringBuilder();
        builder.Append("<p>");
        builder.Append(HtmlEncoder.Default.Encode(message));
        builder.Append("</p>");

        if (!string.IsNullOrWhiteSpace(taskDetailUrl))
        {
            builder.Append("<p style=\"margin-top:16px;\">");
            builder.Append("<a href=\"");
            builder.Append(HtmlEncoder.Default.Encode(taskDetailUrl));
            builder.Append("\">Görev detayını aç</a>");
            builder.Append("</p>");
        }

        return builder.ToString();
    }

    private static string BuildTaskLinkOnlyTextBody(string message, string? taskDetailUrl)
    {
        var builder = new StringBuilder();
        builder.AppendLine(message);

        if (!string.IsNullOrWhiteSpace(taskDetailUrl))
        {
            builder.AppendLine();
            builder.AppendLine($"Görev detayı: {taskDetailUrl}");
        }

        return builder.ToString();
    }

    private string? BuildTaskDetailUrl(Guid taskId)
    {
        var baseUrl = _publicAppUrlOptions.PublicBaseUrl?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/", UriKind.Absolute, out var baseUri))
        {
            return null;
        }

        return new Uri(baseUri, $"ProjectTasks/Details/{taskId}").ToString();
    }

    private IReadOnlyList<KeyValuePair<string, string>> CreateTaskDetailLines(
        ProjectTask task,
        string responsibleName,
        IReadOnlyList<string> assignedNames,
        IReadOnlyList<RecordFile> files,
        string? description)
    {
        var projectName = task.Project is not null
            ? $"{task.Project.Code} - {task.Project.Name}"
            : task.ManualProjectName;
        var customerName = task.Customer?.Name
            ?? task.Project?.Customer?.Name
            ?? task.ManualCustomerName;
        var fileNames = files.Select(x => x.OriginalFileName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var lines = new List<KeyValuePair<string, string>>
        {
            new("Görev Başlığı", task.Title),
            new("Durum", task.Status.ToDisplayName()),
            new("Öncelik", task.Priority.ToDisplayName()),
            new("İlerleme", $"%{task.ProgressPercent}"),
            new("Eklenen Dosya Sayısı", files.Count.ToString()),
            new("Eklenen Dosyalar", string.Join(", ", fileNames)),
            new("Toplam Ek Boyutu", FormatFileSize(files.Sum(x => Math.Max(0, x.Size))))
        };

        AddLineIfHasValue(lines, "Proje", projectName);
        AddLineIfHasValue(lines, "Müşteri", customerName);
        AddLineIfHasValue(lines, "Kategori", task.TaskCategory?.Name);
        AddLineIfHasValue(lines, "Görev Sahibi", responsibleName == "Atanmamış" ? null : responsibleName);
        AddLineIfHasValue(lines, "Atanan Kişiler", assignedNames.Count > 0 ? string.Join(", ", assignedNames) : null);
        AddLineIfHasValue(lines, "Başlangıç Tarihi", task.StartDate?.ToString("dd.MM.yyyy"));
        AddLineIfHasValue(lines, "Termin Tarihi", task.DueDate?.ToString("dd.MM.yyyy"));
        AddLineIfHasValue(lines, "Açıklama", task.Description);
        AddLineIfHasValue(lines, "Not", task.Notes);
        AddLineIfHasValue(lines, "Dosya Açıklaması", description);

        return lines;
    }

    private static void AddLineIfHasValue(ICollection<KeyValuePair<string, string>> lines, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            lines.Add(new KeyValuePair<string, string>(label, value.Trim()));
        }
    }

    private async Task<string> GetUserDisplayNameAsync(string? userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return "Atanmamış";
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        return user?.FullName ?? user?.Email ?? "Atanmamış";
    }

    private async Task<IReadOnlyList<string>> GetAssignedUserNamesAsync(IEnumerable<string> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        return await _context.Users
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .OrderBy(x => x.FullName)
            .Select(x => x.FullName ?? x.Email ?? x.UserName ?? x.Id)
            .ToListAsync(cancellationToken);
    }

    private static string FormatFileSize(long size)
    {
        if (size <= 0)
        {
            return "0 KB";
        }

        if (size >= 1024 * 1024)
        {
            return $"{size / 1024d / 1024d:0.0} MB";
        }

        return $"{Math.Max(1, Math.Round(size / 1024d))} KB";
    }
}
