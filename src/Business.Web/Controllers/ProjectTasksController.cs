using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.Extensions;
using Business.Web.Services;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanViewTasks)]
public class ProjectTasksController : Controller
{
    private const int DefaultListTake = 50;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRecordActivityService _recordActivityService;
    private readonly IRecordFileUploadService _recordFileUploadService;
    private readonly IProjectTimelineService _projectTimelineService;

    public ProjectTasksController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IRecordActivityService recordActivityService,
        IRecordFileUploadService recordFileUploadService,
        IProjectTimelineService projectTimelineService)
    {
        _context = context;
        _userManager = userManager;
        _recordActivityService = recordActivityService;
        _recordFileUploadService = recordFileUploadService;
        _projectTimelineService = projectTimelineService;
    }

    public async Task<IActionResult> Index(Guid? projectId, string? q, WorkTaskStatus? status, ProjectPriority? priority, Guid? categoryId, Guid? customerId, string? responsibleUserId, string? assignedUserId, string? sort, bool load = true, int take = DefaultListTake, bool showAll = false, bool includeCompleted = false, bool archivedOnly = false, CancellationToken cancellationToken = default)
    {
        take = Math.Max(DefaultListTake, take);

        ViewBag.FilterQ = q;
        ViewBag.FilterStatus = status;
        ViewBag.FilterPriority = priority;
        ViewBag.FilterCategoryId = categoryId;
        ViewBag.FilterCustomerId = customerId;
        ViewBag.FilterResponsibleUserId = responsibleUserId;
        ViewBag.FilterAssignedUserId = assignedUserId;
        ViewBag.Sort = sort;
        ViewBag.ProjectId = projectId;
        ViewBag.Load = load;
        ViewBag.CurrentTake = take;
        ViewBag.ShowAll = showAll;
        ViewBag.IncludeCompleted = includeCompleted;
        ViewBag.ListAction = archivedOnly ? nameof(Archived) : (includeCompleted ? nameof(All) : nameof(Index));
        ViewBag.TaskListTitle = archivedOnly ? "Arşiv görevler" : (includeCompleted ? "Tüm görevler" : "Aktif görevler");
        ViewBag.IsArchiveList = archivedOnly;
        await FillFilterLookupsAsync(cancellationToken);
        ViewBag.StatusOptions = Enum.GetValues<WorkTaskStatus>()
            .Where(x => archivedOnly || includeCompleted || x != WorkTaskStatus.Done)
            .ToList();

        if (projectId.HasValue)
        {
            var project = await _context.Projects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == projectId.Value, cancellationToken);
            if (project is not null)
            {
                ViewBag.Breadcrumbs = new Dictionary<string, string?>
                {
                    ["Projeler"] = Url.Action("Index", "Projects"),
                    [project.Code] = Url.Action("Details", "Projects", new { id = project.Id }),
                    ["GÃ¶revler"] = null
                };
            }
        }

        if (!load)
        {
            ViewBag.IsDeferredLoad = true;
            ViewBag.HasMore = false;
            ViewBag.ResultCount = 0;
            ViewBag.UserNames = new Dictionary<string, string>();
            return View(Array.Empty<ProjectTask>());
        }

        ViewBag.IsDeferredLoad = false;

        var query = _context.ProjectTasks
            .Include(x => x.Project)
            .Include(x => x.Customer)
            .Include(x => x.TaskCategory)
            .Include(x => x.Assignments)
            .AsNoTracking()
            .ApplyRecordVisibility(User, includeArchived: archivedOnly, onlyArchived: archivedOnly);

        if (projectId.HasValue)
        {
            query = query.Where(x => x.ProjectId == projectId);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.Title.Contains(term) ||
                (x.Description != null && x.Description.Contains(term)) ||
                (x.Project != null && (x.Project.Code.Contains(term) || x.Project.Name.Contains(term))) ||
                (x.Customer != null && x.Customer.Name.Contains(term)) ||
                (x.Project != null && x.Project.Customer != null && x.Project.Customer.Name.Contains(term)) ||
                (x.ManualProjectName != null && x.ManualProjectName.Contains(term)) ||
                (x.ManualCustomerName != null && x.ManualCustomerName.Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }
        else if (!archivedOnly && !includeCompleted)
        {
            query = query.Where(x => x.Status != WorkTaskStatus.Done);
        }

        if (priority.HasValue)
        {
            query = query.Where(x => x.Priority == priority.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.TaskCategoryId == categoryId.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == customerId.Value || x.Project!.CustomerId == customerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(responsibleUserId))
        {
            query = query.Where(x => x.ResponsibleUserId == responsibleUserId);
        }

        if (!string.IsNullOrWhiteSpace(assignedUserId))
        {
            query = query.Where(x =>
                x.AssignedToUserId == assignedUserId ||
                x.Assignments.Any(assignment => assignment.UserId == assignedUserId));
        }

        query = ApplyTaskVisibility(query);
        query = sort switch
        {
            "title" => query.OrderBy(x => x.Title),
            "due" => query.OrderBy(x => x.DueDate ?? DateTime.MaxValue),
            "priority" => query.OrderByDescending(x => x.Priority),
            "status" => query.OrderBy(x => x.Status),
            "progress" => query.OrderByDescending(x => x.ProgressPercent),
            "created" => query.OrderBy(x => x.CreatedAt),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };

        List<ProjectTask> tasks;
        var hasMore = false;

        if (showAll)
        {
            tasks = await query.ToListAsync(cancellationToken);
        }
        else
        {
            tasks = await query.Take(take + 1).ToListAsync(cancellationToken);
            hasMore = tasks.Count > take;
            if (hasMore)
            {
                tasks.RemoveAt(tasks.Count - 1);
            }
        }

        ViewBag.HasMore = hasMore;
        ViewBag.NextTake = take + DefaultListTake;
        ViewBag.ResultCount = tasks.Count;
        ViewBag.UserNames = await GetTaskListUserNamesAsync(tasks, cancellationToken);

        return View(tasks);
    }

    public async Task<IActionResult> All(Guid? projectId, string? q, WorkTaskStatus? status, ProjectPriority? priority, Guid? categoryId, Guid? customerId, string? responsibleUserId, string? assignedUserId, string? sort, bool load = true, int take = DefaultListTake, bool showAll = false, CancellationToken cancellationToken = default)
    {
        var result = await Index(projectId, q, status, priority, categoryId, customerId, responsibleUserId, assignedUserId, sort, load, take, showAll, includeCompleted: true, archivedOnly: false, cancellationToken: cancellationToken);
        return result is ViewResult viewResult
            ? View(nameof(Index), viewResult.Model)
            : result;
    }

    public async Task<IActionResult> Completed(Guid? projectId, string? q, ProjectPriority? priority, Guid? categoryId, Guid? customerId, string? responsibleUserId, string? assignedUserId, string? sort, bool load = true, int take = DefaultListTake, bool showAll = false, CancellationToken cancellationToken = default)
    {
        var result = await Index(projectId, q, WorkTaskStatus.Done, priority, categoryId, customerId, responsibleUserId, assignedUserId, sort, load, take, showAll, includeCompleted: false, archivedOnly: false, cancellationToken: cancellationToken);
        ViewBag.TaskListTitle = "Tamamlanan görevler";
        ViewBag.ListAction = nameof(Completed);
        ViewBag.StatusOptions = new List<WorkTaskStatus> { WorkTaskStatus.Done };
        return result is ViewResult viewResult
            ? View(nameof(Index), viewResult.Model)
            : result;
    }

    public async Task<IActionResult> Archived(Guid? projectId, string? q, WorkTaskStatus? status, ProjectPriority? priority, Guid? categoryId, Guid? customerId, string? responsibleUserId, string? assignedUserId, string? sort, bool load = true, int take = DefaultListTake, bool showAll = false, CancellationToken cancellationToken = default)
    {
        var result = await Index(projectId, q, status, priority, categoryId, customerId, responsibleUserId, assignedUserId, sort, load, take, showAll, includeCompleted: false, archivedOnly: true, cancellationToken: cancellationToken);
        return result is ViewResult viewResult
            ? View(nameof(Index), viewResult.Model)
            : result;
    }

    public async Task<IActionResult> AssignedToMe(string? q, WorkTaskStatus? status, ProjectPriority? priority, Guid? categoryId, Guid? customerId, string? sort, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var query = _context.ProjectTasks
            .Include(x => x.Project)
            .Include(x => x.Customer)
            .Include(x => x.TaskCategory)
            .Include(x => x.Assignments)
            .Include(x => x.Updates)
            .AsNoTracking()
            .ApplyRecordVisibility(User)
            .Where(x =>
                x.AssignedToUserId == userId ||
                x.ResponsibleUserId == userId ||
                x.Assignments.Any(assignment => assignment.UserId == userId));

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.Title.Contains(term) ||
                (x.Description != null && x.Description.Contains(term)) ||
                (x.Project != null && (x.Project.Code.Contains(term) || x.Project.Name.Contains(term))) ||
                (x.Customer != null && x.Customer.Name.Contains(term)) ||
                (x.Project != null && x.Project.Customer != null && x.Project.Customer.Name.Contains(term)) ||
                (x.ManualProjectName != null && x.ManualProjectName.Contains(term)) ||
                (x.ManualCustomerName != null && x.ManualCustomerName.Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(x => x.Priority == priority.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.TaskCategoryId == categoryId.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == customerId.Value || x.Project!.CustomerId == customerId.Value);
        }

        query = sort switch
        {
            "title" => query.OrderBy(x => x.Title),
            "due" => query.OrderBy(x => x.DueDate ?? DateTime.MaxValue),
            "priority" => query.OrderByDescending(x => x.Priority),
            "status" => query.OrderBy(x => x.Status),
            "progress" => query.OrderByDescending(x => x.ProgressPercent),
            "created" => query.OrderBy(x => x.CreatedAt),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };

        ViewBag.TaskListTitle = "Bana Atanan GÃ¶revler";
        ViewBag.AssignedToMe = true;
        ViewBag.FilterQ = q;
        ViewBag.FilterStatus = status;
        ViewBag.FilterPriority = priority;
        ViewBag.FilterCategoryId = categoryId;
        ViewBag.FilterCustomerId = customerId;
        ViewBag.Sort = sort;
        await FillFilterLookupsAsync(cancellationToken);

        var tasks = await query.ToListAsync(cancellationToken);
        ViewBag.UserNames = await GetTaskListUserNamesAsync(tasks, cancellationToken);

        return View(nameof(Index), tasks);
    }

    public async Task<IActionResult> Details(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var task = await _context.ProjectTasks
            .Include(x => x.Project)
            .ThenInclude(x => x!.Customer)
            .Include(x => x.Customer)
            .Include(x => x.TaskCategory)
            .Include(x => x.Assignments)
            .Include(x => x.Updates)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (task is null)
        {
            return NotFound();
        }

        if (!task.IsVisibleTo(User) || !CanAccessTask(task))
        {
            return NotFound();
        }

        ViewBag.ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        ViewBag.Breadcrumbs = task.Project is not null
            ? new Dictionary<string, string?>
            {
                ["Projeler"] = Url.Action("Index", "Projects"),
                [task.Project.Code] = Url.Action("Details", "Projects", new { id = task.Project.Id }),
                ["GÃ¶revler"] = Url.Action(nameof(Index), new { projectId = task.Project.Id }),
                [task.Title] = null
            }
            : new Dictionary<string, string?>
            {
                ["GÃ¶revler"] = Url.Action(nameof(Index)),
                [task.Title] = null
            };
        ViewBag.Breadcrumbs = await CreateTaskBreadcrumbsAsync(task, cancellationToken);
        ViewBag.ResponsibleName = await GetUserDisplayNameAsync(task.ResponsibleUserId);
        ViewBag.AssignedUsers = await GetAssignedUserNamesAsync(task.Assignments.Select(x => x.UserId), cancellationToken);
        ViewBag.TaskUpdates = task.Updates
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
        ViewBag.Activity = new RecordActivityViewModel
        {
            OwnerType = RecordOwnerType.ProjectTask,
            OwnerId = task.Id,
            Comments = await _recordActivityService.GetCommentsAsync(RecordOwnerType.ProjectTask, task.Id, cancellationToken),
            Files = await _recordActivityService.GetFilesAsync(RecordOwnerType.ProjectTask, task.Id, cancellationToken),
            UserNames = await GetActivityUserNamesAsync(RecordOwnerType.ProjectTask, task.Id, cancellationToken)
        };

        return View(task);
    }

    public async Task<IActionResult> Updates(Guid id, CancellationToken cancellationToken)
    {
        var task = await _context.ProjectTasks
            .Include(x => x.Project)
            .ThenInclude(x => x!.Customer)
            .Include(x => x.Customer)
            .Include(x => x.Updates)
            .Include(x => x.Assignments)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (task is null)
        {
            return NotFound();
        }

        if (!task.IsVisibleTo(User) || !CanAccessTask(task))
        {
            return NotFound();
        }

        ViewBag.Breadcrumbs = await CreateTaskUpdateBreadcrumbsAsync(task, cancellationToken);
        ViewBag.CanDeleteTaskUpdates = CanDeleteTaskUpdates();
        return View(task);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUpdate(Guid id, Guid taskId, CancellationToken cancellationToken)
    {
        if (!CanDeleteTaskUpdates())
        {
            return Forbid();
        }

        var task = await _context.ProjectTasks
            .Include(x => x.Project)
            .Include(x => x.Assignments)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);

        if (task is null || !task.IsVisibleTo(User) || !CanAccessTask(task))
        {
            return NotFound();
        }

        var update = await _context.ProjectTaskUpdates
            .FirstOrDefaultAsync(x => x.Id == id && x.ProjectTaskId == taskId, cancellationToken);

        if (update is null)
        {
            return NotFound();
        }

        _context.ProjectTaskUpdates.Remove(update);
        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Updates), new { id = taskId });
    }

    [Authorize(Policy = AppPolicies.CanCreateTasks)]
    public async Task<IActionResult> Create(Guid? projectId, string? returnUrl, CancellationToken cancellationToken)
    {
        var task = new ProjectTask { ProjectId = projectId };
        await FillLookupsAsync(task.Status, cancellationToken);
        if (projectId.HasValue)
        {
            var canUseProject = await _context.Projects
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .AnyAsync(x => x.Id == projectId.Value, cancellationToken);
            if (!canUseProject)
            {
                return NotFound();
            }

            task.CustomerId = await _context.Projects
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .Where(x => x.Id == projectId.Value)
                .Select(x => x.CustomerId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        ViewBag.ReturnUrl = NormalizeReturnUrl(returnUrl);
        return View(task);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanCreateTasks)]
    public async Task<IActionResult> Create(ProjectTask task, string? returnUrl, List<IFormFile>? files, CancellationToken cancellationToken)
    {
        task.Visibility = User.NormalizeRecordVisibility(task.Visibility);
        NormalizeTaskRelation(task);
        ApplyTaskProgressByStatus(task);
        var validFiles = files?
            .Where(file => file is not null && file.Length > 0)
            .ToList() ?? [];
        if (task.ProjectId.HasValue)
        {
            var canUseProject = await _context.Projects
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .AnyAsync(x => x.Id == task.ProjectId.Value, cancellationToken);
            if (!canUseProject)
            {
                ModelState.AddModelError(nameof(task.ProjectId), "SeÃƒÂ§ilen proje iÃƒÂ§in yetkiniz bulunmuyor.");
            }
        }

        if (task.Status == WorkTaskStatus.Done && !CanCompleteTasks())
        {
            ModelState.AddModelError(nameof(task.Status), "GÃ¶revi tamamlandÄ± olarak kaydetme yetkiniz yok.");
        }

        if (!_recordFileUploadService.TryValidateFiles(validFiles, out var fileErrorMessage))
        {
            ModelState.AddModelError(string.Empty, fileErrorMessage);
        }

        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(task.Status, cancellationToken);
            ViewBag.ReturnUrl = NormalizeReturnUrl(returnUrl);
            return View(task);
        }

        _context.ProjectTasks.Add(task);
        await _context.SaveChangesAsync(cancellationToken);
        await SyncAssignmentsAsync(task.Id, Request.Form["AssignedUserIds"].Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!), cancellationToken);
        await _projectTimelineService.AddForTaskAsync(task.Id, "GÃ¶rev eklendi", task.Title, cancellationToken);
        if (validFiles.Count > 0)
        {
            try
            {
                var savedFiles = await _recordFileUploadService.SaveFilesAsync(RecordOwnerType.ProjectTask, task.Id, validFiles, null, cancellationToken);
                foreach (var savedFile in savedFiles)
                {
                    await _recordActivityService.AddFileAsync(savedFile, cancellationToken);
                    await _projectTimelineService.AddForTaskAsync(task.Id, "Dosya eklendi", savedFile.OriginalFileName, cancellationToken);
                }

                if (savedFiles.Count > 1)
                {
                    TempData["Success"] = $"{savedFiles.Count} dosya yÃ¼klendi.";
                }
            }
            catch (IOException)
            {
                TempData["Error"] = "GÃ¶rev kaydedildi fakat dosyalar yÃ¼klenirken bir hata oluÅŸtu.";
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "GÃ¶rev kaydedildi fakat dosyalar yÃ¼klenemedi.";
            }
        }

        return RedirectToLocal(returnUrl, fallbackRouteValues: task.ProjectId.HasValue ? new { projectId = task.ProjectId } : null);
    }

    [Authorize(Policy = AppPolicies.CanUpdateTasks)]
    public async Task<IActionResult> Edit(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var task = await _context.ProjectTasks
            .Include(x => x.Project)
            .Include(x => x.Assignments)
            .ApplyRecordVisibility(User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        await FillLookupsAsync(task.Status, cancellationToken);
        ViewBag.ReturnUrl = NormalizeReturnUrl(returnUrl);
        return View(task);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanUpdateTasks)]
    public async Task<IActionResult> Edit(Guid id, ProjectTask task, string? returnUrl, CancellationToken cancellationToken)
    {
        if (id != task.Id)
        {
            return BadRequest();
        }

        task.Visibility = User.NormalizeRecordVisibility(task.Visibility);
        NormalizeTaskRelation(task);
        ApplyTaskProgressByStatus(task);
        if (task.ProjectId.HasValue)
        {
            var canUseProject = await _context.Projects
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .AnyAsync(x => x.Id == task.ProjectId.Value, cancellationToken);
            if (!canUseProject)
            {
                ModelState.AddModelError(nameof(task.ProjectId), "SeÃƒÂ§ilen proje iÃƒÂ§in yetkiniz bulunmuyor.");
            }
        }

        if (task.Status == WorkTaskStatus.Done && !CanCompleteTasks())
        {
            ModelState.AddModelError(nameof(task.Status), "GÃ¶revi tamamlandÄ± olarak kaydetme yetkiniz yok.");
        }

        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(task.Status, cancellationToken);
            ViewBag.ReturnUrl = NormalizeReturnUrl(returnUrl);
            return View(task);
        }

        _context.Update(task);
        await _context.SaveChangesAsync(cancellationToken);
        await SyncAssignmentsAsync(task.Id, Request.Form["AssignedUserIds"].Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!), cancellationToken);
        await _projectTimelineService.AddForTaskAsync(task.Id, "GÃ¶rev gÃ¼ncellendi", task.Title, cancellationToken);
        return RedirectToLocal(returnUrl, fallbackRouteValues: task.ProjectId.HasValue ? new { projectId = task.ProjectId } : null);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanChangeTaskStatus)]
    public async Task<IActionResult> SubmitForReview(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var task = await _context.ProjectTasks
            .Include(x => x.Project)
            .ApplyRecordVisibility(User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (task is null || !task.IsVisibleTo(User))
        {
            return NotFound();
        }

        task.Status = WorkTaskStatus.InReview;
        ApplyTaskProgressByStatus(task);
        task.SubmittedForReviewAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await _projectTimelineService.AddForTaskAsync(id, "GÃ¶rev kontrole gÃ¶nderildi", task.Title, cancellationToken);
        return RedirectToLocal(returnUrl, nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanCompleteTasks)]
    public async Task<IActionResult> Complete(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var task = await _context.ProjectTasks
            .Include(x => x.Project)
            .ApplyRecordVisibility(User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (task is null || !task.IsVisibleTo(User))
        {
            return NotFound();
        }

        task.Status = WorkTaskStatus.Done;
        ApplyTaskProgressByStatus(task);
        task.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await _projectTimelineService.AddForTaskAsync(id, "GÃ¶rev tamamlandÄ±", task.Title, cancellationToken);
        return RedirectToLocal(returnUrl, nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanChangeTaskStatus)]
    public async Task<IActionResult> UpdateStatus(Guid id, WorkTaskStatus status, string? returnUrl, CancellationToken cancellationToken)
    {
        if (status == WorkTaskStatus.Done &&
            !CanCompleteTasks())
        {
            return Forbid();
        }

        var task = await _context.ProjectTasks
            .Include(x => x.Project)
            .ApplyRecordVisibility(User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (task is null || !task.IsVisibleTo(User))
        {
            return NotFound();
        }

        if (task.Status != status)
        {
            task.Status = status;
            ApplyTaskProgressByStatus(task);
            task.SubmittedForReviewAt = status == WorkTaskStatus.InReview ? DateTime.UtcNow : task.SubmittedForReviewAt;
            task.CompletedAt = status == WorkTaskStatus.Done ? DateTime.UtcNow : task.CompletedAt;
            await _context.SaveChangesAsync(cancellationToken);
            await _projectTimelineService.AddForTaskAsync(id, "GÃ¶rev durumu deÄŸiÅŸti", $"{task.Title} - {status.ToDisplayName()}", cancellationToken);
        }

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanUpdateTasks)]
    public async Task<IActionResult> Archive(Guid id, bool archived = true, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var task = await _context.ProjectTasks
            .Include(x => x.Project)
            .ApplyRecordVisibility(User, includeArchived: true, onlyArchived: false)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        SetTaskArchiveState(task, archived);
        await _context.SaveChangesAsync(cancellationToken);
        await _projectTimelineService.AddForTaskAsync(task.Id, archived ? "GÃ¶rev arÅŸivlendi" : "GÃ¶rev arÅŸivden Ã§Ä±karÄ±ldÄ±", task.Title, cancellationToken);
        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanUpdateTasks)]
    public async Task<IActionResult> BulkArchive(Guid[] ids, bool archived = true, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var selectedIds = ids.Distinct().ToList();
        if (selectedIds.Count == 0)
        {
            return RedirectToLocal(returnUrl);
        }

        var tasks = await _context.ProjectTasks
            .Include(x => x.Project)
            .ApplyRecordVisibility(User, includeArchived: true, onlyArchived: false)
            .Where(x => selectedIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        foreach (var task in tasks)
        {
            SetTaskArchiveState(task, archived);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanChangeTaskStatus)]
    public async Task<IActionResult> BulkUpdateStatus(Guid[] ids, WorkTaskStatus status, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (status == WorkTaskStatus.Done &&
            !CanCompleteTasks())
        {
            return Forbid();
        }

        var selectedIds = ids.Distinct().ToList();
        if (selectedIds.Count == 0)
        {
            return RedirectToLocal(returnUrl);
        }

        var tasks = await _context.ProjectTasks
            .ApplyRecordVisibility(User, includeArchived: true, onlyArchived: false)
            .Where(x => selectedIds.Contains(x.Id) && !x.IsArchived)
            .ToListAsync(cancellationToken);

        foreach (var task in tasks)
        {
            task.Status = status;
            task.SubmittedForReviewAt = status == WorkTaskStatus.InReview ? DateTime.UtcNow : null;
            task.CompletedAt = status == WorkTaskStatus.Done ? DateTime.UtcNow : null;
            ApplyTaskProgressByStatus(task);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToLocal(returnUrl);
    }

    [Authorize(Policy = AppPolicies.CanViewTasks)]
    public async Task<IActionResult> DownloadArchive(CancellationToken cancellationToken)
    {
        var userNames = await _userManager.Users
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.FullName ?? x.Email ?? x.UserName ?? x.Id, cancellationToken);
        var tasks = await _context.ProjectTasks
            .Include(x => x.Project)
            .Include(x => x.Customer)
            .Include(x => x.TaskCategory)
            .AsNoTracking()
            .ApplyRecordVisibility(User, includeArchived: true, onlyArchived: true)
            .OrderByDescending(x => x.ArchivedAt ?? x.UpdatedAt ?? x.CreatedAt)
            .ToListAsync(cancellationToken);

        var rows = tasks.Select(x => (IReadOnlyList<object?>)
        [
            x.Title,
            x.Project != null ? $"{x.Project.Code} - {x.Project.Name}" : x.ManualProjectName,
            x.Customer != null ? x.Customer.Name : x.ManualCustomerName,
            x.TaskCategory?.Name,
            x.Status.ToDisplayName(),
            x.Priority.ToDisplayName(),
            userNames.GetValueOrDefault(x.ResponsibleUserId ?? string.Empty, string.Empty),
            x.ProgressPercent,
            x.DueDate,
            x.CompletedAt,
            x.ArchivedAt,
            x.Description
        ]).ToList();

        return ExcelFile(
            [new ExcelSheet("ArÅŸiv GÃ¶revler", ["GÃ¶rev", "Proje", "MÃ¼ÅŸteri", "Kategori", "Durum", "Ã–ncelik", "GÃ¶rev Sahibi", "Ä°lerleme", "Termin", "Tamamlanma", "ArÅŸiv Tarihi", "AÃ§Ä±klama"], rows)],
            $"arsiv-gorevler-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanDeleteTasks)]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var task = await _context.ProjectTasks.FindAsync([id], cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        _context.RecordComments.RemoveRange(_context.RecordComments.Where(x => x.OwnerType == RecordOwnerType.ProjectTask && x.OwnerId == id));
        _context.RecordFiles.RemoveRange(_context.RecordFiles.Where(x => x.OwnerType == RecordOwnerType.ProjectTask && x.OwnerId == id));
        _context.ProjectTasks.Remove(task);
        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanDeleteTasks)]
    public async Task<IActionResult> BulkDelete(Guid[] ids, string? returnUrl, CancellationToken cancellationToken)
    {
        if (ids.Length == 0)
        {
            return RedirectToLocal(returnUrl);
        }

        var selectedIds = ids.Distinct().ToList();
        _context.RecordComments.RemoveRange(_context.RecordComments.Where(x => x.OwnerType == RecordOwnerType.ProjectTask && selectedIds.Contains(x.OwnerId)));
        _context.RecordFiles.RemoveRange(_context.RecordFiles.Where(x => x.OwnerType == RecordOwnerType.ProjectTask && selectedIds.Contains(x.OwnerId)));
        _context.ProjectTasks.RemoveRange(_context.ProjectTasks.Where(x => selectedIds.Contains(x.Id)));
        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToLocal(returnUrl);
    }

    private async Task FillLookupsAsync(WorkTaskStatus? selectedStatus, CancellationToken cancellationToken)
    {
        ViewBag.Projects = await _context.Projects.AsNoTracking().ApplyRecordVisibility(User).OrderBy(x => x.Code).ToListAsync(cancellationToken);
        ViewBag.Customers = await _context.Customers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        ViewBag.Users = await _userManager.Users.Where(x => x.IsActive).OrderBy(x => x.FullName).ToListAsync(cancellationToken);
        ViewBag.TaskCategories = await _context.TaskCategories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        ViewBag.FormStatusOptions = GetFormStatusOptions(selectedStatus);
    }

    private async Task FillFilterLookupsAsync(CancellationToken cancellationToken)
    {
        ViewBag.FilterProjects = await _context.Projects.AsNoTracking().ApplyRecordVisibility(User).OrderBy(x => x.Code).ToListAsync(cancellationToken);
        ViewBag.FilterCustomers = await _context.Customers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        ViewBag.FilterTaskCategories = await _context.TaskCategories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        ViewBag.FilterUsers = await _userManager.Users.Where(x => x.IsActive).OrderBy(x => x.FullName).ToListAsync(cancellationToken);
    }

    private IActionResult RedirectToLocal(string? returnUrl, string fallbackAction = nameof(Index), object? fallbackRouteValues = null)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(fallbackAction, fallbackRouteValues);
    }

    private string? NormalizeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : null;
    }

    private IQueryable<ProjectTask> ApplyTaskVisibility(IQueryable<ProjectTask> query)
    {
        if (CanSeeAllTasks())
        {
            return query;
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return query.Where(x => false);
        }

        return query.Where(x =>
            x.Assignments.Any(assignment => assignment.UserId == userId) ||
            x.AssignedToUserId == userId ||
            x.ResponsibleUserId == userId);
    }

    private bool CanAccessTask(ProjectTask task)
    {
        if (CanSeeAllTasks())
        {
            return true;
        }

        var userId = _userManager.GetUserId(User);
        return !string.IsNullOrWhiteSpace(userId) &&
               (task.Assignments.Any(x => x.UserId == userId) ||
                task.AssignedToUserId == userId ||
                task.ResponsibleUserId == userId);
    }

    private bool CanSeeAllTasks()
    {
        return User.IsInRole(AppRoles.Admin) ||
               User.HasClaim(AppClaimTypes.Permission, AppPermissions.TasksViewAll);
    }

    private bool CanDeleteTaskUpdates()
    {
        return User.IsInRole(AppRoles.Admin) ||
               User.HasClaim(AppClaimTypes.Permission, AppPermissions.TasksManage);
    }

    private bool CanCompleteTasks()
    {
        return User.IsInRole(AppRoles.Admin) ||
               User.HasClaim(AppClaimTypes.Permission, AppPermissions.TasksComplete);
    }

    private IReadOnlyList<WorkTaskStatus> GetFormStatusOptions(WorkTaskStatus? selectedStatus = null)
    {
        return Enum.GetValues<WorkTaskStatus>()
            .Where(status => status != WorkTaskStatus.Done || CanCompleteTasks() || selectedStatus == WorkTaskStatus.Done)
            .ToList();
    }

    private async Task<IReadOnlyList<KeyValuePair<string, string?>>> CreateTaskBreadcrumbsAsync(ProjectTask task, CancellationToken cancellationToken)
    {
        var breadcrumbs = new List<KeyValuePair<string, string?>>();
        if (task.Project is not null)
        {
            breadcrumbs.Add(new("Projeler", Url.Action("Index", "Projects")));
            breadcrumbs.Add(new(task.Project.Code, Url.Action("Details", "Projects", new { id = task.Project.Id })));
            breadcrumbs.Add(new("GÃ¶revler", Url.Action(nameof(Index), new { projectId = task.Project.Id })));
        }
        else
        {
            breadcrumbs.Add(new("GÃ¶revler", Url.Action(nameof(Index))));
        }

        var parentChain = await GetParentTaskChainAsync(task, cancellationToken);
        foreach (var parent in parentChain)
        {
            breadcrumbs.Add(new(parent.Title, Url.Action(nameof(Details), new { id = parent.Id })));
        }

        breadcrumbs.Add(new(task.Title, null));
        return breadcrumbs;
    }

    private async Task<IReadOnlyList<KeyValuePair<string, string?>>> CreateTaskUpdateBreadcrumbsAsync(ProjectTask task, CancellationToken cancellationToken)
    {
        var breadcrumbs = (await CreateTaskBreadcrumbsAsync(task, cancellationToken)).ToList();
        if (breadcrumbs.Count > 0)
        {
            breadcrumbs[^1] = new KeyValuePair<string, string?>(task.Title, Url.Action(nameof(Details), new { id = task.Id }));
        }

        breadcrumbs.Add(new KeyValuePair<string, string?>("GÃ¼ncelleme GeÃ§miÅŸi", null));
        return breadcrumbs;
    }

    private async Task<IReadOnlyList<ProjectTask>> GetParentTaskChainAsync(ProjectTask task, CancellationToken cancellationToken)
    {
        var chain = new List<ProjectTask>();
        var visitedIds = new HashSet<Guid> { task.Id };
        var parentId = task.ParentTaskId;

        while (parentId.HasValue && visitedIds.Add(parentId.Value))
        {
            var parent = await _context.ProjectTasks
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == parentId.Value, cancellationToken);

            if (parent is null)
            {
                break;
            }

            chain.Insert(0, parent);
            parentId = parent.ParentTaskId;
        }

        return chain;
    }

    private async Task SyncAssignmentsAsync(Guid taskId, IEnumerable<string> selectedUserIds, CancellationToken cancellationToken)
    {
        var selected = selectedUserIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var existing = await _context.ProjectTaskAssignments.Where(x => x.ProjectTaskId == taskId).ToListAsync(cancellationToken);

        _context.ProjectTaskAssignments.RemoveRange(existing.Where(x => !selected.Contains(x.UserId)));

        var existingIds = existing.Select(x => x.UserId).ToHashSet();
        foreach (var userId in selected.Where(x => !existingIds.Contains(x)))
        {
            _context.ProjectTaskAssignments.Add(new ProjectTaskAssignment
            {
                ProjectTaskId = taskId,
                UserId = userId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static void NormalizeTaskRelation(ProjectTask task)
    {
        task.ManualProjectName = null;
        task.ManualCustomerName = null;
    }

    private static void ApplyTaskProgressByStatus(ProjectTask task)
    {
        task.ProgressPercent = task.Status switch
        {
            WorkTaskStatus.Todo => 0,
            WorkTaskStatus.InProgress => 25,
            WorkTaskStatus.Waiting => 50,
            WorkTaskStatus.InReview => 75,
            WorkTaskStatus.Done => 100,
            _ => task.ProgressPercent
        };
    }

    private async Task<string> GetUserDisplayNameAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return "AtanmamÄ±ÅŸ";
        }

        var user = await _userManager.FindByIdAsync(userId);
        return user?.FullName ?? user?.Email ?? "AtanmamÄ±ÅŸ";
    }

    private async Task<IReadOnlyList<string>> GetAssignedUserNamesAsync(IEnumerable<string> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        return await _userManager.Users
            .Where(x => ids.Contains(x.Id))
            .OrderBy(x => x.FullName)
            .Select(x => x.FullName ?? x.Email ?? x.UserName ?? x.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyDictionary<string, string>> GetTaskListUserNamesAsync(IEnumerable<ProjectTask> tasks, CancellationToken cancellationToken)
    {
        var ids = tasks
            .SelectMany(task => task.Assignments.Select(x => x.UserId)
                .Append(task.AssignedToUserId)
                .Append(task.ResponsibleUserId))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct()
            .ToList();

        return await _userManager.Users
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.FullName ?? x.Email ?? x.UserName ?? x.Id, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<string, string>> GetActivityUserNamesAsync(RecordOwnerType ownerType, Guid ownerId, CancellationToken cancellationToken)
    {
        var userIds = await _context.RecordComments
            .Where(x => x.OwnerType == ownerType && x.OwnerId == ownerId && x.CreatedByUserId != null)
            .Select(x => x.CreatedByUserId!)
            .Concat(_context.RecordFiles
                .Where(x => x.OwnerType == ownerType && x.OwnerId == ownerId && x.CreatedByUserId != null)
                .Select(x => x.CreatedByUserId!))
            .Distinct()
            .ToListAsync(cancellationToken);

        return await _userManager.Users
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.FullName ?? x.Email ?? x.UserName ?? x.Id, cancellationToken);
    }

    private static void SetTaskArchiveState(ProjectTask task, bool archived)
    {
        task.IsArchived = archived;
        task.ArchivedAt = archived ? DateTime.UtcNow : null;
    }

    private FileContentResult ExcelFile(IReadOnlyList<ExcelSheet> sheets, string fileName)
    {
        var bytes = ExcelWorkbookBuilder.Build(sheets);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}



