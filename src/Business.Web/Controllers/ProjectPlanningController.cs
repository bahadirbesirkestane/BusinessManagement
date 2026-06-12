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

[Authorize(Policy = AppPolicies.CanViewProjects)]
public class ProjectPlanningController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IProjectTimelineService _projectTimelineService;
    private readonly IProjectTemplateService _projectTemplateService;

    public ProjectPlanningController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IProjectTimelineService projectTimelineService,
        IProjectTemplateService projectTemplateService)
    {
        _context = context;
        _userManager = userManager;
        _projectTimelineService = projectTimelineService;
        _projectTemplateService = projectTemplateService;
    }

    public async Task<IActionResult> Index(Guid? projectId, CancellationToken cancellationToken)
    {
        var model = await BuildIndexViewModelAsync(projectId, new ProjectPlanningTaskFormViewModel(), false, "create", cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> PlanList(Guid? projectId, CancellationToken cancellationToken)
    {
        var model = await BuildIndexViewModelAsync(projectId, new ProjectPlanningTaskFormViewModel(), false, "create", cancellationToken);
        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Raporlar ve Diyagramlar"] = null,
            ["Proje Planlama Liste"] = null
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTask([Bind(Prefix = "TaskForm")] ProjectPlanningTaskFormViewModel taskForm, string? returnAction, CancellationToken cancellationToken)
    {
        if (!CanCreatePlanningTask())
        {
            return Forbid();
        }

        var project = await _context.Projects
            .AsNoTracking()
            .ApplyRecordVisibility(User)
            .FirstOrDefaultAsync(x => x.Id == taskForm.ProjectId && x.Status != ProjectStatus.Cancelled, cancellationToken);

        if (project is null)
        {
            ModelState.AddModelError(string.Empty, "Seçilen proje bulunamadı veya iptal edilmiş.");
        }

        ProjectTask? parentTask = null;
        if (taskForm.ParentTaskId.HasValue)
        {
            parentTask = await _context.ProjectTasks
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .FirstOrDefaultAsync(x => x.Id == taskForm.ParentTaskId.Value && x.ProjectId == taskForm.ProjectId, cancellationToken);

            if (parentTask is null)
            {
                ModelState.AddModelError(nameof(taskForm.ParentTaskId), "Üst görev bulunamadı.");
            }
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildIndexViewModelAsync(taskForm.ProjectId, taskForm, true, "create", cancellationToken);
            return View(GetPlanningViewName(returnAction), invalidModel);
        }

        var sortOrder = await GetNextSortOrderAsync(taskForm.ProjectId, taskForm.ParentTaskId, cancellationToken);
        var outlineLevel = parentTask is null ? 0 : parentTask.OutlineLevel + 1;
        var wbsCode = await CreateWbsCodeAsync(taskForm.ProjectId, parentTask, sortOrder, cancellationToken);

        var task = new ProjectTask
        {
            ProjectId = taskForm.ProjectId,
            ParentTaskId = taskForm.ParentTaskId,
            Title = taskForm.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(taskForm.Description) ? null : taskForm.Description.Trim(),
            Visibility = User.NormalizeRecordVisibility(taskForm.Visibility),
            StartDate = taskForm.StartDate,
            DueDate = taskForm.DueDate,
            Status = taskForm.Status,
            Priority = taskForm.Priority,
            ProgressPercent = taskForm.Status == WorkTaskStatus.Done ? 100 : taskForm.ProgressPercent,
            SortOrder = sortOrder,
            OutlineLevel = outlineLevel,
            WbsCode = wbsCode,
            IsMilestone = taskForm.IsMilestone
        };

        _context.ProjectTasks.Add(task);
        await _context.SaveChangesAsync(cancellationToken);
        await AddAssignmentsAsync(task.Id, taskForm.AssignedUserIds, cancellationToken);
        await _projectTimelineService.AddForTaskAsync(task.Id, "Görev eklendi", task.Title, cancellationToken);

        return RedirectToAction(GetPlanningActionName(returnAction), new { projectId = taskForm.ProjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTask([Bind(Prefix = "TaskForm")] ProjectPlanningTaskFormViewModel taskForm, string? returnAction, CancellationToken cancellationToken)
    {
        if (!CanUpdatePlanningTask())
        {
            return Forbid();
        }

        if (!taskForm.TaskId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Düzenlenecek görev bulunamadı.");
        }

        var task = taskForm.TaskId.HasValue
            ? await _context.ProjectTasks
                .ApplyRecordVisibility(User)
                .FirstOrDefaultAsync(x => x.Id == taskForm.TaskId.Value && x.ProjectId == taskForm.ProjectId, cancellationToken)
            : null;

        if (task is null)
        {
            ModelState.AddModelError(string.Empty, "Düzenlenecek görev bulunamadı.");
        }

        var projectExists = await _context.Projects
            .AsNoTracking()
            .ApplyRecordVisibility(User)
            .AnyAsync(x => x.Id == taskForm.ProjectId && x.Status != ProjectStatus.Cancelled, cancellationToken);

        if (!projectExists)
        {
            ModelState.AddModelError(string.Empty, "Seçilen proje bulunamadı veya iptal edilmiş.");
        }

        if (!ModelState.IsValid || task is null)
        {
            var invalidModel = await BuildIndexViewModelAsync(taskForm.ProjectId, taskForm, true, "edit", cancellationToken);
            return View(GetPlanningViewName(returnAction), invalidModel);
        }

        task.Title = taskForm.Title.Trim();
        task.Description = string.IsNullOrWhiteSpace(taskForm.Description) ? null : taskForm.Description.Trim();
        task.Visibility = User.NormalizeRecordVisibility(taskForm.Visibility);
        task.StartDate = taskForm.StartDate;
        task.DueDate = taskForm.DueDate;
        task.Status = taskForm.Status;
        task.Priority = taskForm.Priority;
        task.ProgressPercent = taskForm.Status == WorkTaskStatus.Done ? 100 : taskForm.ProgressPercent;
        task.IsMilestone = taskForm.IsMilestone;

        await _context.SaveChangesAsync(cancellationToken);
        await UpdateAssignmentsAsync(task.Id, taskForm.AssignedUserIds, cancellationToken);
        await _projectTimelineService.AddForTaskAsync(task.Id, "Görev güncellendi", task.Title, cancellationToken);

        return RedirectToAction(GetPlanningActionName(returnAction), new { projectId = taskForm.ProjectId });
    }

    private async Task<ProjectPlanningIndexViewModel> BuildIndexViewModelAsync(
        Guid? projectId,
        ProjectPlanningTaskFormViewModel taskForm,
        bool openTaskForm,
        string taskFormMode,
        CancellationToken cancellationToken)
    {
        var projects = await _context.Projects
            .AsNoTracking()
            .ApplyRecordVisibility(User)
            .Where(x => x.Status != ProjectStatus.Cancelled)
            .OrderBy(x => x.Code)
            .Select(x => new ProjectPlanningProjectOptionViewModel
            {
                Id = x.Id,
                Text = x.Code + " - " + x.Name
            })
            .ToListAsync(cancellationToken);

        var selectedProjectText = projects.FirstOrDefault(x => x.Id == projectId)?.Text;
        ProjectStatus? selectedProjectStatus = null;
        var tasks = new List<ProjectPlanningTaskRowViewModel>();
        var ganttTasks = new List<ProjectPlanningGanttTaskViewModel>();

        if (projectId.HasValue && selectedProjectText is not null)
        {
            selectedProjectStatus = await _context.Projects
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .Where(x => x.Id == projectId.Value)
                .Select(x => (ProjectStatus?)x.Status)
                .FirstOrDefaultAsync(cancellationToken);

            var projectTasks = await _context.ProjectTasks
                .Include(x => x.Assignments)
                .Include(x => x.Updates)
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .Where(x => x.ProjectId == projectId.Value)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.WbsCode)
                .ThenBy(x => x.OutlineLevel)
                .ThenBy(x => x.Title)
                .ToListAsync(cancellationToken);

            var userNames = await GetUserNamesAsync(projectTasks, cancellationToken);
            tasks = CreateHierarchicalRows(projectTasks, userNames);
            ganttTasks = CreateGanttTasks(tasks);
        }

        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Raporlar ve Diyagramlar"] = null,
            ["Proje Planlama Gantt"] = null
        };

        if (taskForm.ProjectId == Guid.Empty && projectId.HasValue)
        {
            taskForm.ProjectId = projectId.Value;
        }

        return new ProjectPlanningIndexViewModel
        {
            ProjectId = projectId,
            SelectedProjectText = selectedProjectText,
            SelectedProjectStatus = selectedProjectStatus,
            SelectedProjectStatusText = selectedProjectStatus?.ToDisplayName() ?? string.Empty,
            SelectedProjectStatusCss = selectedProjectStatus?.ToString().ToLowerInvariant() ?? string.Empty,
            Projects = projects,
            Tasks = tasks,
            GanttTasks = ganttTasks,
            Users = await GetUserOptionsAsync(cancellationToken),
            Templates = await GetTemplateOptionsAsync(cancellationToken),
            TaskForm = taskForm,
            TemplateApplyForm = new ProjectTemplateApplyViewModel
            {
                ProjectId = projectId ?? Guid.Empty
            },
            OpenTaskForm = openTaskForm,
            TaskFormMode = taskFormMode
        };
    }

    private static string GetPlanningActionName(string? returnAction)
    {
        return string.Equals(returnAction, nameof(PlanList), StringComparison.OrdinalIgnoreCase)
            ? nameof(PlanList)
            : nameof(Index);
    }

    private static string GetPlanningViewName(string? returnAction)
    {
        return string.Equals(returnAction, nameof(PlanList), StringComparison.OrdinalIgnoreCase)
            ? nameof(PlanList)
            : nameof(Index);
    }

    private async Task<int> GetNextSortOrderAsync(Guid projectId, Guid? parentTaskId, CancellationToken cancellationToken)
    {
        var siblingQuery = _context.ProjectTasks
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId && x.ParentTaskId == parentTaskId);

        return await siblingQuery.AnyAsync(cancellationToken)
            ? await siblingQuery.MaxAsync(x => x.SortOrder, cancellationToken) + 1
            : 1;
    }

    private async Task<string> CreateWbsCodeAsync(Guid projectId, ProjectTask? parentTask, int sortOrder, CancellationToken cancellationToken)
    {
        if (parentTask is null)
        {
            return sortOrder.ToString();
        }

        var parentWbsCode = !string.IsNullOrWhiteSpace(parentTask.WbsCode)
            ? parentTask.WbsCode
            : await CreateExistingTaskWbsCodeAsync(projectId, parentTask.Id, cancellationToken);

        return string.IsNullOrWhiteSpace(parentWbsCode)
            ? sortOrder.ToString()
            : $"{parentWbsCode}.{sortOrder}";
    }

    private async Task<string> CreateExistingTaskWbsCodeAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken)
    {
        var tasks = await _context.ProjectTasks
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);

        var path = new List<int>();
        var current = tasks.FirstOrDefault(x => x.Id == taskId);
        while (current is not null)
        {
            var siblings = tasks
                .Where(x => x.ParentTaskId == current.ParentTaskId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Title)
                .ToList();

            path.Insert(0, siblings.FindIndex(x => x.Id == current.Id) + 1);
            current = current.ParentTaskId.HasValue
                ? tasks.FirstOrDefault(x => x.Id == current.ParentTaskId.Value)
                : null;
        }

        return string.Join(".", path);
    }

    private async Task<IReadOnlyList<ProjectPlanningUserOptionViewModel>> GetUserOptionsAsync(CancellationToken cancellationToken)
    {
        return await _userManager.Users
            .Where(x => x.IsActive)
            .OrderBy(x => x.FullName)
            .Select(x => new ProjectPlanningUserOptionViewModel
            {
                Id = x.Id,
                Text = x.FullName ?? x.Email ?? x.UserName ?? x.Id
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ProjectPlanningTemplateOptionViewModel>> GetTemplateOptionsAsync(CancellationToken cancellationToken)
    {
        var templates = await _projectTemplateService.GetAllAsync(cancellationToken);

        return templates
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new ProjectPlanningTemplateOptionViewModel
            {
                Id = x.Id,
                Text = string.IsNullOrWhiteSpace(x.Code)
                    ? x.Name
                    : $"{x.Code} - {x.Name}"
            })
            .ToList();
    }

    private bool CanCreatePlanningTask()
    {
        return User.IsInRole(AppRoles.Admin) ||
               User.HasClaim(AppClaimTypes.Permission, AppPermissions.TasksCreate) ||
               User.HasClaim(AppClaimTypes.Permission, AppPermissions.ProjectsManage);
    }

    private bool CanUpdatePlanningTask()
    {
        return User.IsInRole(AppRoles.Admin) ||
               User.HasClaim(AppClaimTypes.Permission, AppPermissions.TasksUpdate) ||
               User.HasClaim(AppClaimTypes.Permission, AppPermissions.ProjectsManage);
    }

    private async Task AddAssignmentsAsync(Guid taskId, IEnumerable<string> selectedUserIds, CancellationToken cancellationToken)
    {
        var userIds = selectedUserIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        foreach (var userId in userIds)
        {
            _context.ProjectTaskAssignments.Add(new ProjectTaskAssignment
            {
                ProjectTaskId = taskId,
                UserId = userId
            });
        }

        if (userIds.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task UpdateAssignmentsAsync(Guid taskId, IEnumerable<string> selectedUserIds, CancellationToken cancellationToken)
    {
        var existingAssignments = await _context.ProjectTaskAssignments
            .Where(x => x.ProjectTaskId == taskId)
            .ToListAsync(cancellationToken);

        _context.ProjectTaskAssignments.RemoveRange(existingAssignments);
        await AddAssignmentsAsync(taskId, selectedUserIds, cancellationToken);

        if (existingAssignments.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<IReadOnlyDictionary<string, string>> GetUserNamesAsync(IEnumerable<ProjectTask> tasks, CancellationToken cancellationToken)
    {
        var userIds = tasks
            .SelectMany(task => task.Assignments.Select(x => x.UserId)
                .Append(task.ResponsibleUserId)
                .Append(task.AssignedToUserId))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        return await _userManager.Users
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.FullName ?? x.Email ?? x.UserName ?? x.Id, cancellationToken);
    }

    private static List<ProjectPlanningTaskRowViewModel> CreateHierarchicalRows(
        IReadOnlyCollection<ProjectTask> tasks,
        IReadOnlyDictionary<string, string> userNames)
    {
        var rows = new List<ProjectPlanningTaskRowViewModel>();
        var taskIds = tasks.Select(x => x.Id).ToHashSet();
        var visitedTaskIds = new HashSet<Guid>();
        var childrenByParent = tasks
            .Where(x => x.ParentTaskId.HasValue)
            .GroupBy(x => x.ParentTaskId!.Value)
            .ToDictionary(x => x.Key, x => SortTasks(x).ToList());

        var roots = SortTasks(tasks.Where(x => !x.ParentTaskId.HasValue || !taskIds.Contains(x.ParentTaskId.Value)));
        var rootIndex = 1;
        foreach (var root in roots)
        {
            AddTaskRow(root, 0, rootIndex.ToString());
            rootIndex++;
        }

        foreach (var remainingTask in SortTasks(tasks.Where(x => !visitedTaskIds.Contains(x.Id))))
        {
            AddTaskRow(remainingTask, remainingTask.OutlineLevel, rootIndex.ToString());
            rootIndex++;
        }

        return rows;

        void AddTaskRow(ProjectTask task, int fallbackLevel, string generatedWbsCode)
        {
            if (!visitedTaskIds.Add(task.Id))
            {
                return;
            }

            var level = task.OutlineLevel > 0 ? task.OutlineLevel : fallbackLevel;
            var hasChildren = childrenByParent.ContainsKey(task.Id);
            var responsibleText = !string.IsNullOrWhiteSpace(task.ResponsibleUserId) && userNames.TryGetValue(task.ResponsibleUserId, out var responsibleName)
                ? responsibleName
                : "Sorumlu yok";
            var assignedNames = task.Assignments
                .Select(x => userNames.TryGetValue(x.UserId, out var assignedName) ? assignedName : null)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
            var latestUpdate = task.Updates
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            rows.Add(new ProjectPlanningTaskRowViewModel
            {
                Id = task.Id,
                ParentTaskId = task.ParentTaskId,
                Title = task.Title,
                Description = task.Description,
                WbsCode = task.WbsCode,
                DisplayWbsCode = string.IsNullOrWhiteSpace(task.WbsCode) ? generatedWbsCode : task.WbsCode,
                OutlineLevel = level,
                SortOrder = task.SortOrder,
                Status = task.Status,
                StatusText = task.Status.ToDisplayName(),
                StatusCss = task.Status.ToString().ToLowerInvariant(),
                Priority = task.Priority,
                PriorityText = task.Priority.ToDisplayName(),
                PriorityCss = task.Priority.ToString().ToLowerInvariant(),
                ResponsibleText = responsibleText,
                AssignedText = assignedNames.Count > 0 ? string.Join(", ", assignedNames) : "Atanan yok",
                AssignedUserIds = task.Assignments.Select(x => x.UserId).ToList(),
                LatestUpdateText = latestUpdate is null
                    ? "Güncelleme yok"
                    : latestUpdate.Title,
                LatestUpdateDescription = latestUpdate?.Description,
                LatestUpdateAt = latestUpdate?.CreatedAt,
                StartDate = task.StartDate,
                DueDate = task.DueDate,
                ProgressPercent = task.Status == WorkTaskStatus.Done ? 100 : task.ProgressPercent,
                IsMilestone = task.IsMilestone,
                HasChildren = hasChildren
            });

            if (!hasChildren)
            {
                return;
            }

            var childIndex = 1;
            foreach (var child in childrenByParent[task.Id])
            {
                AddTaskRow(child, level + 1, $"{generatedWbsCode}.{childIndex}");
                childIndex++;
            }
        }
    }

    private static List<ProjectPlanningGanttTaskViewModel> CreateGanttTasks(IReadOnlyCollection<ProjectPlanningTaskRowViewModel> tasks)
    {
        var datedTasks = tasks
            .Where(x => x.StartDate.HasValue && x.DueDate.HasValue)
            .ToList();
        var datedTaskIds = datedTasks.Select(x => x.Id).ToHashSet();

        return datedTasks
            .Select(task =>
            {
                var start = task.StartDate!.Value.Date;
                var end = task.DueDate!.Value.Date;
                if (end < start)
                {
                    end = start;
                }

                return new ProjectPlanningGanttTaskViewModel
                {
                    Id = CreateGanttTaskId(task.Id),
                    Name = string.IsNullOrWhiteSpace(task.DisplayWbsCode)
                        ? task.Title
                        : $"{task.DisplayWbsCode} {task.Title}",
                    Start = start.ToString("yyyy-MM-dd"),
                    End = end.ToString("yyyy-MM-dd"),
                    Progress = Math.Clamp(task.ProgressPercent, 0, 100),
                    Dependencies = task.ParentTaskId.HasValue && datedTaskIds.Contains(task.ParentTaskId.Value)
                        ? CreateGanttTaskId(task.ParentTaskId.Value)
                        : string.Empty,
                    CustomClass = task.Status.ToString().ToLowerInvariant(),
                    Url = $"/ProjectTasks/Details/{task.Id}",
                    StatusText = task.StatusText,
                    PriorityText = task.PriorityText,
                    AssignedText = task.AssignedText,
                    DateRangeText = $"{start:dd.MM.yyyy} - {end:dd.MM.yyyy}",
                    LatestUpdateText = task.LatestUpdateText,
                    LatestUpdateDescription = task.LatestUpdateDescription,
                    LatestUpdateAtText = task.LatestUpdateAt.HasValue
                        ? task.LatestUpdateAt.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
                        : string.Empty
                };
            })
            .ToList();
    }

    private static string CreateGanttTaskId(Guid id)
    {
        return $"task-{id:N}";
    }

    private static IOrderedEnumerable<ProjectTask> SortTasks(IEnumerable<ProjectTask> tasks)
    {
        return tasks
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.WbsCode)
            .ThenBy(x => x.OutlineLevel)
            .ThenBy(x => x.Title);
    }
}
