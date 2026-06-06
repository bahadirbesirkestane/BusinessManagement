using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.Extensions;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanViewTasks)]
public class ProjectTasksController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRecordActivityService _recordActivityService;
    private readonly IProjectTimelineService _projectTimelineService;

    public ProjectTasksController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IRecordActivityService recordActivityService,
        IProjectTimelineService projectTimelineService)
    {
        _context = context;
        _userManager = userManager;
        _recordActivityService = recordActivityService;
        _projectTimelineService = projectTimelineService;
    }

    public async Task<IActionResult> Index(Guid? projectId, string? q, WorkTaskStatus? status, ProjectPriority? priority, Guid? categoryId, Guid? customerId, string? responsibleUserId, string? assignedUserId, string? sort, CancellationToken cancellationToken)
    {
        var query = _context.ProjectTasks
            .Include(x => x.Project)
            .Include(x => x.Customer)
            .Include(x => x.TaskCategory)
            .Include(x => x.Assignments)
            .AsNoTracking();

        if (projectId.HasValue)
        {
            query = query.Where(x => x.ProjectId == projectId);
            ViewBag.ProjectId = projectId;
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.Title.Contains(term) ||
                (x.Description != null && x.Description.Contains(term)) ||
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

        ViewBag.FilterQ = q;
        ViewBag.FilterStatus = status;
        ViewBag.FilterPriority = priority;
        ViewBag.FilterCategoryId = categoryId;
        ViewBag.FilterCustomerId = customerId;
        ViewBag.FilterResponsibleUserId = responsibleUserId;
        ViewBag.FilterAssignedUserId = assignedUserId;
        ViewBag.Sort = sort;
        await FillFilterLookupsAsync(cancellationToken);

        var tasks = await query.ToListAsync(cancellationToken);
        ViewBag.UserNames = await GetTaskListUserNamesAsync(tasks, cancellationToken);

        return View(tasks);
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
            .AsNoTracking()
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

        ViewBag.TaskListTitle = "Bana Atanan Görevler";
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

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var task = await _context.ProjectTasks
            .Include(x => x.Project)
            .ThenInclude(x => x!.Customer)
            .Include(x => x.Customer)
            .Include(x => x.TaskCategory)
            .Include(x => x.Assignments)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (task is null)
        {
            return NotFound();
        }

        if (!CanAccessTask(task))
        {
            return NotFound();
        }

        ViewBag.ResponsibleName = await GetUserDisplayNameAsync(task.ResponsibleUserId);
        ViewBag.AssignedUsers = await GetAssignedUserNamesAsync(task.Assignments.Select(x => x.UserId), cancellationToken);
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

    [Authorize(Policy = AppPolicies.CanCreateTasks)]
    public async Task<IActionResult> Create(Guid? projectId, CancellationToken cancellationToken)
    {
        await FillLookupsAsync(cancellationToken);
        return View(new ProjectTask { ProjectId = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanCreateTasks)]
    public async Task<IActionResult> Create(ProjectTask task, CancellationToken cancellationToken)
    {
        NormalizeTaskRelation(task);
        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(cancellationToken);
            return View(task);
        }

        _context.ProjectTasks.Add(task);
        await _context.SaveChangesAsync(cancellationToken);
        await SyncAssignmentsAsync(task.Id, Request.Form["AssignedUserIds"].Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!), cancellationToken);
        await _projectTimelineService.AddForTaskAsync(task.Id, "Görev eklendi", task.Title, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AppPolicies.CanUpdateTasks)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var task = await _context.ProjectTasks
            .Include(x => x.Assignments)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        await FillLookupsAsync(cancellationToken);
        return View(task);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanUpdateTasks)]
    public async Task<IActionResult> Edit(Guid id, ProjectTask task, CancellationToken cancellationToken)
    {
        if (id != task.Id)
        {
            return BadRequest();
        }

        NormalizeTaskRelation(task);
        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(cancellationToken);
            return View(task);
        }

        _context.Update(task);
        await _context.SaveChangesAsync(cancellationToken);
        await SyncAssignmentsAsync(task.Id, Request.Form["AssignedUserIds"].Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!), cancellationToken);
        await _projectTimelineService.AddForTaskAsync(task.Id, "Görev güncellendi", task.Title, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanChangeTaskStatus)]
    public async Task<IActionResult> SubmitForReview(Guid id, CancellationToken cancellationToken)
    {
        var task = await _context.ProjectTasks.FindAsync([id], cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        task.Status = WorkTaskStatus.InReview;
        task.SubmittedForReviewAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await _projectTimelineService.AddForTaskAsync(id, "Görev kontrole gönderildi", task.Title, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanCompleteTasks)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var task = await _context.ProjectTasks.FindAsync([id], cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        task.Status = WorkTaskStatus.Done;
        task.ProgressPercent = 100;
        task.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await _projectTimelineService.AddForTaskAsync(id, "Görev tamamlandı", task.Title, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanChangeTaskStatus)]
    public async Task<IActionResult> UpdateStatus(Guid id, WorkTaskStatus status, string? returnUrl, CancellationToken cancellationToken)
    {
        if (status == WorkTaskStatus.Done && !(User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Manager)))
        {
            return Forbid();
        }

        var task = await _context.ProjectTasks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        if (task.Status != status)
        {
            task.Status = status;
            task.SubmittedForReviewAt = status == WorkTaskStatus.InReview ? DateTime.UtcNow : task.SubmittedForReviewAt;
            task.CompletedAt = status == WorkTaskStatus.Done ? DateTime.UtcNow : task.CompletedAt;
            task.ProgressPercent = status == WorkTaskStatus.Done ? 100 : task.ProgressPercent;
            await _context.SaveChangesAsync(cancellationToken);
            await _projectTimelineService.AddForTaskAsync(id, "Görev durumu değişti", $"{task.Title} - {status.ToDisplayName()}", cancellationToken);
        }

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
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
    [Authorize(Roles = AppRoles.Admin)]
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

    private async Task FillLookupsAsync(CancellationToken cancellationToken)
    {
        ViewBag.Projects = await _context.Projects.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken);
        ViewBag.Customers = await _context.Customers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        ViewBag.Users = await _userManager.Users.Where(x => x.IsActive).OrderBy(x => x.FullName).ToListAsync(cancellationToken);
        ViewBag.TaskCategories = await _context.TaskCategories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    private async Task FillFilterLookupsAsync(CancellationToken cancellationToken)
    {
        ViewBag.FilterProjects = await _context.Projects.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken);
        ViewBag.FilterCustomers = await _context.Customers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        ViewBag.FilterTaskCategories = await _context.TaskCategories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        ViewBag.FilterUsers = await _userManager.Users.Where(x => x.IsActive).OrderBy(x => x.FullName).ToListAsync(cancellationToken);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
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
               User.HasClaim(AppClaimTypes.Permission, AppPermissions.ProjectsManage);
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
        if (task.ProjectId.HasValue)
        {
            task.CustomerId = null;
            task.ManualProjectName = null;
            task.ManualCustomerName = null;
            return;
        }

        if (task.CustomerId.HasValue)
        {
            task.ManualCustomerName = null;
        }
    }

    private async Task<string> GetUserDisplayNameAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return "Atanmamış";
        }

        var user = await _userManager.FindByIdAsync(userId);
        return user?.FullName ?? user?.Email ?? "Atanmamış";
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
}
