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

public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [Authorize(Policy = AppPolicies.CanViewProjects)]
    public async Task<IActionResult> Gantt(Guid? projectId, CancellationToken cancellationToken)
    {
        var projectOptions = await _context.Projects
            .AsNoTracking()
            .ApplyRecordVisibility(User)
            .Where(x => x.Status != ProjectStatus.Cancelled)
            .OrderBy(x => x.Code)
            .Select(x => new ProjectFilterOptionViewModel { Id = x.Id, Text = x.Code + " - " + x.Name })
            .ToListAsync(cancellationToken);

        var projectQuery = _context.Projects
            .Include(x => x.Customer)
            .Include(x => x.Tasks)
            .AsNoTracking()
            .ApplyRecordVisibility(User)
            .Where(x => x.Status != ProjectStatus.Cancelled);

        if (projectId.HasValue)
        {
            projectQuery = projectQuery.Where(x => x.Id == projectId.Value);
        }

        var projects = await projectQuery
            .OrderBy(x => x.TargetEndDate ?? DateTime.MaxValue)
            .ThenBy(x => x.Code)
            .Take(projectId.HasValue ? 1 : 40)
            .ToListAsync(cancellationToken);

        if (!User.CanViewAdminOnlyRecords())
        {
            foreach (var project in projects)
            {
                project.Tasks = project.Tasks
                    .Where(x => x.IsVisibleTo(User))
                    .ToList();
            }
        }

        var datedProjects = projects
            .Where(x => x.StartDate.HasValue || x.TargetEndDate.HasValue || x.Tasks.Any(task => task.StartDate.HasValue || task.DueDate.HasValue))
            .ToList();

        var allDates = datedProjects
            .SelectMany(project => GetProjectDates(project)
                .Concat(project.Tasks.SelectMany(GetTaskDates)))
            .ToList();

        var today = DateTime.Today;
        var timelineStart = allDates.Count > 0 ? allDates.Min().Date : today.AddDays(-7);
        var timelineEnd = allDates.Count > 0 ? allDates.Max().Date : today.AddDays(30);
        timelineStart = timelineStart.AddDays(-2);
        timelineEnd = timelineEnd.AddDays(2);
        if (timelineStart == timelineEnd)
        {
            timelineEnd = timelineEnd.AddDays(1);
        }

        var rows = new List<GanttRowViewModel>();
        foreach (var project in datedProjects)
        {
            var projectStart = project.StartDate?.Date
                ?? project.Tasks.SelectMany(GetTaskDates).DefaultIfEmpty(project.CreatedAt.Date).Min().Date;
            var projectEnd = project.TargetEndDate?.Date
                ?? project.CompletedAt?.Date
                ?? project.Tasks.SelectMany(GetTaskDates).DefaultIfEmpty(projectStart).Max().Date;

            rows.Add(CreateProjectRow(project, projectStart, projectEnd, timelineStart, timelineEnd));

            foreach (var task in project.Tasks
                .Where(x => x.StartDate.HasValue || x.DueDate.HasValue)
                .OrderBy(x => x.DueDate ?? DateTime.MaxValue)
                .ThenBy(x => x.Title)
                .Take(projectId.HasValue ? 500 : 8))
            {
                var taskStart = (task.StartDate ?? task.DueDate ?? task.CreatedAt).Date;
                var taskEnd = (task.DueDate ?? task.StartDate ?? task.CompletedAt ?? task.CreatedAt).Date;
                rows.Add(CreateTaskRow(task, project, taskStart, taskEnd, timelineStart, timelineEnd));
            }
        }

        var model = new GanttReportViewModel
        {
            TimelineStart = timelineStart,
            TimelineEnd = timelineEnd,
            Segments = CreateTimelineSegments(timelineStart, timelineEnd),
            DaySegments = CreateDaySegments(timelineStart, timelineEnd),
            Projects = projectOptions,
            ProjectId = projectId,
            TimelineScaleText = CreateTimelineScaleText(timelineStart, timelineEnd),
            Rows = rows,
            ActiveProjectCount = projects.Count(x => x.Status is ProjectStatus.Planned or ProjectStatus.InProgress or ProjectStatus.Waiting),
            TaskCount = projects.Sum(x => x.Tasks.Count),
            LateTaskCount = projects.SelectMany(x => x.Tasks).Count(IsLateTask)
        };

        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Raporlar ve Diyagramlar"] = null,
            ["Gantt"] = null
        };

        return View(model);
    }

    [Authorize(Policy = AppPolicies.CanViewTasks)]
    public async Task<IActionResult> Kanban(Guid? projectId, CancellationToken cancellationToken)
    {
        var query = _context.ProjectTasks
            .Include(x => x.Project)
            .Include(x => x.Customer)
            .Include(x => x.TaskCategory)
            .Include(x => x.Assignments)
            .AsNoTracking()
            .ApplyRecordVisibility(User);

        if (projectId.HasValue)
        {
            query = query.Where(x => x.ProjectId == projectId.Value);
        }

        query = ApplyTaskVisibility(query);

        var tasks = await query
            .OrderBy(x => x.Status)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate ?? DateTime.MaxValue)
            .Take(240)
            .ToListAsync(cancellationToken);

        var userNames = await GetUserNamesAsync(tasks, cancellationToken);
        var columns = Enum.GetValues<WorkTaskStatus>()
            .Select(status => new KanbanColumnViewModel
            {
                Status = status,
                Title = status.ToDisplayName(),
                StatusCss = status.ToString().ToLowerInvariant(),
                Tasks = tasks
                    .Where(x => x.Status == status)
                    .Select(x => CreateKanbanCard(x, userNames))
                    .ToList()
            })
            .ToList();

        var model = new KanbanReportViewModel
        {
            Columns = columns,
            Projects = await _context.Projects
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .Where(x => x.Status != ProjectStatus.Cancelled)
                .OrderBy(x => x.Code)
                .Select(x => new ProjectFilterOptionViewModel { Id = x.Id, Text = x.Code + " - " + x.Name })
                .ToListAsync(cancellationToken),
            ProjectId = projectId,
            TotalTaskCount = tasks.Count,
            LateTaskCount = tasks.Count(IsLateTask),
            ReviewTaskCount = tasks.Count(x => x.Status == WorkTaskStatus.InReview)
        };

        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Raporlar ve Diyagramlar"] = null,
            ["Kanban"] = null
        };

        return View(model);
    }

    private IQueryable<ProjectTask> ApplyTaskVisibility(IQueryable<ProjectTask> query)
    {
        if (User.IsInRole(AppRoles.Admin) ||
            User.HasClaim(AppClaimTypes.Permission, AppPermissions.ProjectsManage))
        {
            return query;
        }

        var userId = _userManager.GetUserId(User);
        return string.IsNullOrWhiteSpace(userId)
            ? query.Where(x => false)
            : query.Where(x =>
                x.Assignments.Any(assignment => assignment.UserId == userId) ||
                x.AssignedToUserId == userId ||
                x.ResponsibleUserId == userId);
    }

    private static GanttRowViewModel CreateProjectRow(Project project, DateTime start, DateTime end, DateTime timelineStart, DateTime timelineEnd)
    {
        var taskProgress = project.Tasks.Count == 0 ? 0 : (int)Math.Round(project.Tasks.Average(x => x.ProgressPercent));
        var progress = project.Status == ProjectStatus.Completed ? 100 : taskProgress;
        return new GanttRowViewModel
        {
            Title = project.Code,
            Subtitle = $"{project.Name} - {project.Customer?.Name ?? project.CustomerName ?? "Müşteri yok"}",
            StatusText = project.Status.ToDisplayName(),
            StatusCss = project.Status.ToString().ToLowerInvariant(),
            PriorityText = project.Priority.ToDisplayName(),
            PriorityCss = project.Priority.ToString().ToLowerInvariant(),
            StartDate = start,
            EndDate = end,
            ProgressPercent = progress,
            OffsetPercent = GetOffsetPercent(start, timelineStart, timelineEnd),
            WidthPercent = GetWidthPercent(start, end, timelineStart, timelineEnd),
            IsProject = true,
            IsLate = project.TargetEndDate.HasValue && project.TargetEndDate.Value.Date < DateTime.Today && project.Status != ProjectStatus.Completed,
            Url = $"/Projects/Details/{project.Id}"
        };
    }

    private static GanttRowViewModel CreateTaskRow(ProjectTask task, Project project, DateTime start, DateTime end, DateTime timelineStart, DateTime timelineEnd)
    {
        return new GanttRowViewModel
        {
            Title = task.Title,
            Subtitle = project.Code,
            StatusText = task.Status.ToDisplayName(),
            StatusCss = task.Status.ToString().ToLowerInvariant(),
            PriorityText = task.Priority.ToDisplayName(),
            PriorityCss = task.Priority.ToString().ToLowerInvariant(),
            StartDate = start,
            EndDate = end,
            ProgressPercent = task.Status == WorkTaskStatus.Done ? 100 : task.ProgressPercent,
            OffsetPercent = GetOffsetPercent(start, timelineStart, timelineEnd),
            WidthPercent = GetWidthPercent(start, end, timelineStart, timelineEnd),
            IsProject = false,
            IsLate = IsLateTask(task),
            Url = $"/ProjectTasks/Details/{task.Id}"
        };
    }

    private static KanbanTaskCardViewModel CreateKanbanCard(ProjectTask task, IReadOnlyDictionary<string, string> userNames)
    {
        var assignedNames = task.Assignments
            .Select(x => userNames.TryGetValue(x.UserId, out var assignedName) ? assignedName : null)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        return new KanbanTaskCardViewModel
        {
            Id = task.Id,
            Title = task.Title,
            ProjectText = task.Project?.Code
                ?? task.Customer?.Name
                ?? task.ManualProjectName
                ?? task.ManualCustomerName
                ?? "Genel",
            CategoryText = task.TaskCategory?.Name ?? "Kategori yok",
            ResponsibleText = !string.IsNullOrWhiteSpace(task.ResponsibleUserId) && userNames.TryGetValue(task.ResponsibleUserId, out var responsibleName)
                ? responsibleName
                : "Sorumlu yok",
            AssignedText = assignedNames.Count > 0 ? string.Join(", ", assignedNames) : "Atanan yok",
            PriorityText = task.Priority.ToDisplayName(),
            PriorityCss = task.Priority.ToString().ToLowerInvariant(),
            DueDate = task.DueDate,
            ProgressPercent = task.Status == WorkTaskStatus.Done ? 100 : task.ProgressPercent,
            IsLate = IsLateTask(task)
        };
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

        return await _userManager.Users
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.FullName ?? x.Email ?? x.UserName ?? x.Id, cancellationToken);
    }

    private static IReadOnlyList<DateTime> GetProjectDates(Project project)
    {
        return new[] { project.StartDate, project.TargetEndDate, project.CompletedAt }
            .Where(x => x.HasValue)
            .Select(x => x!.Value.Date)
            .ToList();
    }

    private static IReadOnlyList<DateTime> GetTaskDates(ProjectTask task)
    {
        return new[] { task.StartDate, task.DueDate, task.CompletedAt }
            .Where(x => x.HasValue)
            .Select(x => x!.Value.Date)
            .ToList();
    }

    private static bool IsLateTask(ProjectTask task)
    {
        return task.DueDate.HasValue &&
               task.DueDate.Value.Date < DateTime.Today &&
               task.Status != WorkTaskStatus.Done &&
               task.Status != WorkTaskStatus.Cancelled;
    }

    private static IReadOnlyList<GanttTimelineSegmentViewModel> CreateTimelineSegments(DateTime start, DateTime end)
    {
        var segments = new List<GanttTimelineSegmentViewModel>();
        var cursor = new DateTime(start.Year, start.Month, 1);

        while (cursor <= end)
        {
            var segmentStart = cursor < start ? start : cursor;
            var segmentEnd = cursor.AddMonths(1).AddDays(-1);
            if (segmentEnd > end)
            {
                segmentEnd = end;
            }

            segments.Add(new GanttTimelineSegmentViewModel
            {
                Label = cursor.ToString("MMM yyyy"),
                OffsetPercent = GetOffsetPercent(segmentStart, start, end),
                WidthPercent = GetWidthPercent(segmentStart, segmentEnd, start, end)
            });
            cursor = cursor.AddMonths(1);
        }

        return segments;
    }

    private static IReadOnlyList<GanttTimelineSegmentViewModel> CreateDaySegments(DateTime start, DateTime end)
    {
        var totalDays = (end.Date - start.Date).TotalDays + 1;
        var step = totalDays <= 45 ? 1 : totalDays <= 120 ? 7 : 14;
        var segments = new List<GanttTimelineSegmentViewModel>();

        for (var cursor = start.Date; cursor <= end.Date; cursor = cursor.AddDays(step))
        {
            segments.Add(new GanttTimelineSegmentViewModel
            {
                Label = step == 1 ? cursor.ToString("dd") : cursor.ToString("dd.MM"),
                OffsetPercent = GetOffsetPercent(cursor, start, end),
                WidthPercent = 0
            });
        }

        if (segments.Count == 0 || segments[^1].OffsetPercent < 98)
        {
            segments.Add(new GanttTimelineSegmentViewModel
            {
                Label = end.ToString(step == 1 ? "dd" : "dd.MM"),
                OffsetPercent = GetOffsetPercent(end, start, end),
                WidthPercent = 0
            });
        }

        return segments;
    }

    private static string CreateTimelineScaleText(DateTime start, DateTime end)
    {
        var totalDays = (end.Date - start.Date).TotalDays + 1;
        return totalDays <= 45
            ? "Günlük görünüm"
            : totalDays <= 120
                ? "Haftalık görünüm"
                : "İki haftalık görünüm";
    }

    private static double GetOffsetPercent(DateTime date, DateTime start, DateTime end)
    {
        var totalDays = Math.Max(1, (end.Date - start.Date).TotalDays + 1);
        var days = Math.Max(0, (date.Date - start.Date).TotalDays);
        return Math.Min(100, Math.Round(days / totalDays * 100, 2));
    }

    private static double GetWidthPercent(DateTime startDate, DateTime endDate, DateTime timelineStart, DateTime timelineEnd)
    {
        var totalDays = Math.Max(1, (timelineEnd.Date - timelineStart.Date).TotalDays + 1);
        var start = startDate.Date < timelineStart.Date ? timelineStart.Date : startDate.Date;
        var end = endDate.Date > timelineEnd.Date ? timelineEnd.Date : endDate.Date;
        var days = Math.Max(1, (end - start).TotalDays + 1);
        return Math.Max(2, Math.Round(days / totalDays * 100, 2));
    }
}
