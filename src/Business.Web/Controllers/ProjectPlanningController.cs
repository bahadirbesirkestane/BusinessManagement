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

    public ProjectPlanningController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(Guid? projectId, CancellationToken cancellationToken)
    {
        var projects = await _context.Projects
            .AsNoTracking()
            .Where(x => x.Status != ProjectStatus.Cancelled)
            .OrderBy(x => x.Code)
            .Select(x => new ProjectPlanningProjectOptionViewModel
            {
                Id = x.Id,
                Text = x.Code + " - " + x.Name
            })
            .ToListAsync(cancellationToken);

        var selectedProjectText = projects.FirstOrDefault(x => x.Id == projectId)?.Text;
        var tasks = new List<ProjectPlanningTaskRowViewModel>();

        if (projectId.HasValue && selectedProjectText is not null)
        {
            var projectTasks = await _context.ProjectTasks
                .AsNoTracking()
                .Where(x => x.ProjectId == projectId.Value)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.WbsCode)
                .ThenBy(x => x.OutlineLevel)
                .ThenBy(x => x.Title)
                .ToListAsync(cancellationToken);

            var userNames = await GetUserNamesAsync(projectTasks, cancellationToken);
            tasks = CreateHierarchicalRows(projectTasks, userNames);
        }

        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Raporlar ve Diyagramlar"] = null,
            ["Proje Planlama Gantt"] = null
        };

        return View(new ProjectPlanningIndexViewModel
        {
            ProjectId = projectId,
            SelectedProjectText = selectedProjectText,
            Projects = projects,
            Tasks = tasks
        });
    }

    private async Task<IReadOnlyDictionary<string, string>> GetUserNamesAsync(IEnumerable<ProjectTask> tasks, CancellationToken cancellationToken)
    {
        var userIds = tasks
            .SelectMany(task => new[] { task.ResponsibleUserId, task.AssignedToUserId })
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

            rows.Add(new ProjectPlanningTaskRowViewModel
            {
                Id = task.Id,
                ParentTaskId = task.ParentTaskId,
                Title = task.Title,
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

    private static IOrderedEnumerable<ProjectTask> SortTasks(IEnumerable<ProjectTask> tasks)
    {
        return tasks
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.WbsCode)
            .ThenBy(x => x.OutlineLevel)
            .ThenBy(x => x.Title);
    }
}
