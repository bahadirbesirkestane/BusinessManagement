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

[Authorize(Policy = AppPolicies.CanViewProjects)]
public class ProjectsController : Controller
{
    private const int DefaultListTake = 50;
    private readonly IProjectService _projectService;
    private readonly ILookupService _lookupService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IRecordActivityService _recordActivityService;
    private readonly IProjectTimelineService _projectTimelineService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public ProjectsController(
        IProjectService projectService,
        ILookupService lookupService,
        IAuthorizationService authorizationService,
        IRecordActivityService recordActivityService,
        IProjectTimelineService projectTimelineService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _projectService = projectService;
        _lookupService = lookupService;
        _authorizationService = authorizationService;
        _recordActivityService = recordActivityService;
        _projectTimelineService = projectTimelineService;
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index(string? q, ProjectStatus? status, ProjectPriority? priority, Guid? customerId, string? sort, bool load = true, int take = DefaultListTake, bool showAll = false, bool archivedOnly = false, CancellationToken cancellationToken = default)
    {
        take = Math.Max(DefaultListTake, take);

        ViewBag.FilterQ = q;
        ViewBag.FilterStatus = status;
        ViewBag.FilterPriority = priority;
        ViewBag.FilterCustomerId = customerId;
        ViewBag.Sort = sort;
        ViewBag.Load = load;
        ViewBag.CurrentTake = take;
        ViewBag.ShowAll = showAll;
        ViewBag.ListAction = archivedOnly ? nameof(Archived) : nameof(Index);
        ViewBag.ProjectListTitle = archivedOnly ? "Arşiv projeler" : "Tüm projeler";
        ViewBag.IsArchiveList = archivedOnly;
        ViewBag.Customers = await _context.Customers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        ViewBag.StatusOptions = Enum.GetValues<ProjectStatus>()
            .Where(x => archivedOnly || x != ProjectStatus.Completed)
            .ToList();

        if (!load)
        {
            ViewBag.IsDeferredLoad = true;
            ViewBag.HasMore = false;
            ViewBag.ResultCount = 0;
            return View(Array.Empty<Project>());
        }

        ViewBag.IsDeferredLoad = false;

        var query = _context.Projects
            .Include(x => x.Customer)
            .AsNoTracking()
            .ApplyRecordVisibility(User, includeArchived: archivedOnly, onlyArchived: archivedOnly);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(term) ||
                x.Name.Contains(term) ||
                (x.Customer != null && x.Customer.Name.Contains(term)) ||
                (x.CustomerName != null && x.CustomerName.Contains(term)) ||
                (x.Description != null && x.Description.Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }
        else if (!archivedOnly)
        {
            query = query.Where(x => x.Status != ProjectStatus.Completed);
        }

        if (priority.HasValue)
        {
            query = query.Where(x => x.Priority == priority.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == customerId.Value);
        }

        query = sort switch
        {
            "code" => query.OrderBy(x => x.Code),
            "name" => query.OrderBy(x => x.Name),
            "target" => query.OrderBy(x => x.TargetEndDate ?? DateTime.MaxValue),
            "status" => query.OrderBy(x => x.Status),
            "created" => query.OrderBy(x => x.CreatedAt),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };

        List<Project> projects;
        var hasMore = false;

        if (showAll)
        {
            projects = await query.ToListAsync(cancellationToken);
        }
        else
        {
            projects = await query.Take(take + 1).ToListAsync(cancellationToken);
            hasMore = projects.Count > take;
            if (hasMore)
            {
                projects.RemoveAt(projects.Count - 1);
            }
        }

        ViewBag.HasMore = hasMore;
        ViewBag.NextTake = take + DefaultListTake;
        ViewBag.ResultCount = projects.Count;

        return View(projects);
    }

    public async Task<IActionResult> Completed(string? q, ProjectPriority? priority, Guid? customerId, string? sort, bool load = true, int take = DefaultListTake, bool showAll = false, CancellationToken cancellationToken = default)
    {
        var result = await Index(q, ProjectStatus.Completed, priority, customerId, sort, load, take, showAll, archivedOnly: false, cancellationToken);
        ViewBag.ProjectListTitle = "Tamamlanan projeler";
        ViewBag.ListAction = nameof(Completed);
        ViewBag.StatusOptions = new List<ProjectStatus> { ProjectStatus.Completed };
        return result is ViewResult viewResult
            ? View("Index", viewResult.Model)
            : result;
    }

    public async Task<IActionResult> Archived(string? q, ProjectStatus? status, ProjectPriority? priority, Guid? customerId, string? sort, bool load = true, int take = DefaultListTake, bool showAll = false, CancellationToken cancellationToken = default)
    {
        var result = await Index(q, status, priority, customerId, sort, load, take, showAll, archivedOnly: true, cancellationToken);
        return result is ViewResult viewResult
            ? View(nameof(Index), viewResult.Model)
            : result;
    }

    public async Task<IActionResult> Calendar(int? year, int? month, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Costs()
    {
        return RedirectToAction("Projects", "Costs");
    }

    public async Task<IActionResult> Details(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var project = await _projectService.GetDetailsAsync(id, cancellationToken);
        if (project is null || !project.IsVisibleTo(User))
        {
            return NotFound();
        }

        project.PurchaseOrders = project.PurchaseOrders
            .Where(x => x.IsVisibleTo(User))
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        ViewBag.ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Projeler"] = Url.Action(nameof(Index)),
            [project.Code] = null
        };
        ViewBag.VisibleProjectTasks = GetVisibleProjectTasks(project);
        ViewBag.TaskHierarchyLabels = CreateTaskHierarchyLabels(project.Tasks);
        ViewBag.Materials = await _lookupService.GetMaterialsAsync(cancellationToken);
        return View(new ProjectDetailsViewModel
        {
            Project = project,
            CanViewProductionUpdates = true,
            CanViewPurchasing = (await _authorizationService.AuthorizeAsync(User, AppPolicies.CanViewPurchasing)).Succeeded,
            Activity = new RecordActivityViewModel
            {
                OwnerType = RecordOwnerType.Project,
                OwnerId = project.Id,
                Comments = await _recordActivityService.GetCommentsAsync(RecordOwnerType.Project, project.Id, cancellationToken),
                Files = await _recordActivityService.GetFilesAsync(RecordOwnerType.Project, project.Id, cancellationToken)
            }
        });
    }

    [Authorize(Policy = AppPolicies.CanCreateProjects)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await FillLookupsAsync(cancellationToken);
        return View(new Project { Code = await GenerateProjectCodeAsync(cancellationToken), StartDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanCreateProjects)]
    public async Task<IActionResult> Create(Project project, CancellationToken cancellationToken)
    {
        project.Visibility = User.NormalizeRecordVisibility(project.Visibility);

        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(cancellationToken);
            return View(project);
        }

        if (string.IsNullOrWhiteSpace(project.Code))
        {
            project.Code = await GenerateProjectCodeAsync(cancellationToken);
        }
        else if (await _context.Projects.AnyAsync(x => x.Code == project.Code, cancellationToken))
        {
            ModelState.AddModelError(nameof(project.Code), "Bu proje kodu zaten kullanılıyor.");
            await FillLookupsAsync(cancellationToken);
            return View(project);
        }

        await _projectService.CreateAsync(project, cancellationToken);
        await _projectTimelineService.AddAsync(project.Id, "Proje oluşturuldu", $"{project.Code} - {project.Name}", cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AppPolicies.CanUpdateProjects)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var project = await _projectService.GetByIdAsync(id, cancellationToken);
        if (project is null || !project.IsVisibleTo(User))
        {
            return NotFound();
        }

        await FillLookupsAsync(cancellationToken);
        return View(project);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanUpdateProjects)]
    public async Task<IActionResult> Edit(Guid id, Project project, CancellationToken cancellationToken)
    {
        if (id != project.Id)
        {
            return BadRequest();
        }

        project.Visibility = User.NormalizeRecordVisibility(project.Visibility);

        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(cancellationToken);
            return View(project);
        }

        if (await _context.Projects.AnyAsync(x => x.Id != id && x.Code == project.Code, cancellationToken))
        {
            ModelState.AddModelError(nameof(project.Code), "Bu proje kodu zaten kullanılıyor.");
            await FillLookupsAsync(cancellationToken);
            return View(project);
        }

        await _projectService.UpdateAsync(project, cancellationToken);
        await _projectTimelineService.AddAsync(project.Id, "Proje düzenlendi", $"{project.Code} - {project.Name}", cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanUpdateProjects)]
    public async Task<IActionResult> AddUpdate(Guid id, string title, string? description, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            await _projectTimelineService.AddAsync(id, title.Trim(), description?.Trim(), cancellationToken);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Updates(Guid id, CancellationToken cancellationToken)
    {
        var project = await _projectService.GetDetailsAsync(id, cancellationToken);
        if (project is null || !project.IsVisibleTo(User))
        {
            return NotFound();
        }

        return View(project);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteUpdate(Guid id, Guid projectId, CancellationToken cancellationToken)
    {
        var update = await _context.ProjectUpdates.FirstOrDefaultAsync(x => x.Id == id && x.ProjectId == projectId, cancellationToken);
        if (update is null)
        {
            return NotFound();
        }

        _context.ProjectUpdates.Remove(update);
        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Updates), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanChangeProjectStatus)]
    public async Task<IActionResult> UpdateStatus(Guid id, ProjectStatus status, string? returnUrl, CancellationToken cancellationToken)
    {
        var project = await _projectService.GetByIdAsync(id, cancellationToken);
        if (project is null || !project.IsVisibleTo(User))
        {
            return NotFound();
        }

        if (project.Status != status)
        {
            project.Status = status;
            project.CompletedAt = status == ProjectStatus.Completed ? DateTime.UtcNow : project.CompletedAt;
            await _projectService.UpdateAsync(project, cancellationToken);
            await _projectTimelineService.AddAsync(project.Id, "Proje durumu değişti", $"{project.Code} - {status.ToDisplayName()}", cancellationToken);
        }

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanUpdateProjects)]
    public async Task<IActionResult> Archive(Guid id, bool archived = true, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .Include(x => x.Tasks)
            .Include(x => x.PurchaseOrders)
            .ApplyRecordVisibility(User, includeArchived: true, onlyArchived: false)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null)
        {
            return NotFound();
        }

        SetProjectArchiveState(project, archived);
        await _context.SaveChangesAsync(cancellationToken);
        await _projectTimelineService.AddAsync(project.Id, archived ? "Proje arşivlendi" : "Proje arşivden çıkarıldı", $"{project.Code} - {project.Name}", cancellationToken);
        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanUpdateProjects)]
    public async Task<IActionResult> BulkArchive(Guid[] ids, bool archived = true, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var selectedIds = ids.Distinct().ToList();
        if (selectedIds.Count == 0)
        {
            return RedirectToLocal(returnUrl);
        }

        var projects = await _context.Projects
            .Include(x => x.Tasks)
            .Include(x => x.PurchaseOrders)
            .ApplyRecordVisibility(User, includeArchived: true, onlyArchived: false)
            .Where(x => selectedIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        foreach (var project in projects)
        {
            SetProjectArchiveState(project, archived);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanChangeProjectStatus)]
    public async Task<IActionResult> BulkUpdateStatus(Guid[] ids, ProjectStatus status, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var selectedIds = ids.Distinct().ToList();
        if (selectedIds.Count == 0)
        {
            return RedirectToLocal(returnUrl);
        }

        var projects = await _context.Projects
            .ApplyRecordVisibility(User, includeArchived: true, onlyArchived: false)
            .Where(x => selectedIds.Contains(x.Id) && !x.IsArchived)
            .ToListAsync(cancellationToken);

        foreach (var project in projects)
        {
            project.Status = status;
            project.CompletedAt = status == ProjectStatus.Completed ? DateTime.UtcNow : null;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToLocal(returnUrl);
    }

    [Authorize(Policy = AppPolicies.CanViewProjects)]
    public async Task<IActionResult> DownloadArchive(CancellationToken cancellationToken)
    {
        var rows = await _context.Projects
            .Include(x => x.Customer)
            .AsNoTracking()
            .ApplyRecordVisibility(User, includeArchived: true, onlyArchived: true)
            .OrderByDescending(x => x.ArchivedAt ?? x.UpdatedAt ?? x.CreatedAt)
            .Select(x => (IReadOnlyList<object?>)new object?[]
            {
                x.Code,
                x.Name,
                x.Customer != null ? x.Customer.Name : x.CustomerName,
                x.Status.ToDisplayName(),
                x.Priority.ToDisplayName(),
                x.StartDate,
                x.TargetEndDate,
                x.CompletedAt,
                x.ArchivedAt,
                x.Description,
                x.Notes
            })
            .ToListAsync(cancellationToken);

        return ExcelFile(
            [new ExcelSheet("Arşiv Projeler", ["Kod", "Proje", "Müşteri", "Durum", "Öncelik", "Başlangıç", "Hedef", "Tamamlanma", "Arşiv Tarihi", "Açıklama", "Not"], rows)],
            $"arsiv-projeler-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    [Authorize(Policy = AppPolicies.CanDeleteProjects)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var project = await _projectService.GetDetailsAsync(id, cancellationToken);
        return project is null || !project.IsVisibleTo(User) ? NotFound() : View(project);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanDeleteProjects)]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        RemoveActivityRecords(RecordOwnerType.Project, id);
        await _projectService.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanDeleteProjects)]
    public async Task<IActionResult> BulkDelete(Guid[] ids, string? returnUrl, CancellationToken cancellationToken)
    {
        foreach (var id in ids.Distinct())
        {
            RemoveActivityRecords(RecordOwnerType.Project, id);
            await _projectService.DeleteAsync(id, cancellationToken);
        }

        return RedirectToLocal(returnUrl);
    }

    private async Task FillLookupsAsync(CancellationToken cancellationToken)
    {
        ViewBag.Customers = await _lookupService.GetCustomersAsync(cancellationToken);
        ViewBag.Users = await _userManager.Users.Where(x => x.IsActive).OrderBy(x => x.FullName).ToListAsync(cancellationToken);
    }

    private async Task<string> GenerateProjectCodeAsync(CancellationToken cancellationToken)
    {
        var baseCode = $"PRJ-{DateTime.Now:yyyyMMddHHmmss}";
        var code = baseCode;
        var sequence = 1;

        while (await _context.Projects.AnyAsync(x => x.Code == code, cancellationToken))
        {
            code = $"{baseCode}-{sequence:00}";
            sequence++;
        }

        return code;
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    private IReadOnlyList<ProjectTask> GetVisibleProjectTasks(Project project)
    {
        if (User.IsInRole(AppRoles.Admin) ||
            User.HasClaim(AppClaimTypes.Permission, AppPermissions.TasksViewAll) ||
            User.HasClaim(AppClaimTypes.Permission, AppPermissions.TasksManage) ||
            User.HasClaim(AppClaimTypes.Permission, AppPermissions.ProjectsManage))
        {
            return project.Tasks
                .Where(x => x.IsVisibleTo(User))
                .OrderByDescending(x => x.CreatedAt)
                .ToList();
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return [];
        }

        return project.Tasks
            .Where(x =>
                x.IsVisibleTo(User) &&
                (x.Assignments.Any(assignment => assignment.UserId == userId) ||
                 x.AssignedToUserId == userId ||
                 x.ResponsibleUserId == userId))
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
    }

    private static IReadOnlyDictionary<Guid, string> CreateTaskHierarchyLabels(IEnumerable<ProjectTask> tasks)
    {
        var taskList = tasks.ToList();
        var taskMap = taskList.ToDictionary(x => x.Id);
        var orderedSiblings = taskList
            .GroupBy(x => x.ParentTaskId ?? Guid.Empty)
            .ToDictionary(
                x => x.Key,
                x => x.OrderBy(task => task.SortOrder).ThenBy(task => task.Title).Select(task => task.Id).ToList());
        var labels = new Dictionary<Guid, string>();

        foreach (var task in taskList)
        {
            labels[task.Id] = CreateTaskHierarchyLabel(task);
        }

        return labels;

        string CreateTaskHierarchyLabel(ProjectTask task)
        {
            if (!string.IsNullOrWhiteSpace(task.WbsCode))
            {
                return $"Hiyerarşi: {task.WbsCode}";
            }

            var path = new List<int>();
            var current = task;
            var visitedIds = new HashSet<Guid>();

            while (visitedIds.Add(current.Id))
            {
                var siblingKey = current.ParentTaskId ?? Guid.Empty;
                var siblings = orderedSiblings.TryGetValue(siblingKey, out var siblingIds)
                    ? siblingIds
                    : [];
                var index = siblings.IndexOf(current.Id) + 1;
                path.Insert(0, index > 0 ? index : 1);

                if (!current.ParentTaskId.HasValue || !taskMap.TryGetValue(current.ParentTaskId.Value, out var parent))
                {
                    break;
                }

                current = parent;
            }

            return path.Count > 1
                ? $"Hiyerarşi: {string.Join(".", path)}"
                : "Hiyerarşi: Ana görev";
        }
    }

    private void RemoveActivityRecords(RecordOwnerType ownerType, Guid ownerId)
    {
        _context.RecordComments.RemoveRange(_context.RecordComments.Where(x => x.OwnerType == ownerType && x.OwnerId == ownerId));
        _context.RecordFiles.RemoveRange(_context.RecordFiles.Where(x => x.OwnerType == ownerType && x.OwnerId == ownerId));
    }

    private static void SetProjectArchiveState(Project project, bool archived)
    {
        var archivedAt = archived ? DateTime.UtcNow : (DateTime?)null;
        project.IsArchived = archived;
        project.ArchivedAt = archivedAt;

        foreach (var task in project.Tasks)
        {
            task.IsArchived = archived;
            task.ArchivedAt = archivedAt;
        }

        foreach (var order in project.PurchaseOrders)
        {
            order.IsArchived = archived;
            order.ArchivedAt = archivedAt;
        }
    }

    private FileContentResult ExcelFile(IReadOnlyList<ExcelSheet> sheets, string fileName)
    {
        var bytes = ExcelWorkbookBuilder.Build(sheets);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
