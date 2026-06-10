using Business.Application.Repositories;
using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Services;

public class ProjectTemplateService : CrudService<ProjectTemplate>, IProjectTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly IProjectTimelineService _projectTimelineService;

    public ProjectTemplateService(
        IRepository<ProjectTemplate> repository,
        ApplicationDbContext context,
        IProjectTimelineService projectTimelineService) : base(repository)
    {
        _context = context;
        _projectTimelineService = projectTimelineService;
    }

    protected override IQueryable<ProjectTemplate> ListQuery()
    {
        return Repository.Query()
            .Include(x => x.Tasks);
    }

    protected override IQueryable<ProjectTemplate> DetailsQuery()
    {
        return Repository.Query()
            .Include(x => x.Tasks)
                .ThenInclude(x => x.TaskCategory);
    }

    public Task<ProjectTemplate?> GetTemplateWithTasksAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DetailsQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<ProjectTemplateTask?> GetTemplateTaskByIdAsync(Guid templateId, Guid taskId, CancellationToken cancellationToken = default)
    {
        return _context.ProjectTemplateTasks
            .FirstOrDefaultAsync(x => x.ProjectTemplateId == templateId && x.Id == taskId, cancellationToken);
    }

    public async Task AddTaskAsync(ProjectTemplateTask task, CancellationToken cancellationToken = default)
    {
        ProjectTemplateTask? parentTask = null;
        if (task.ParentTemplateTaskId.HasValue)
        {
            parentTask = await _context.ProjectTemplateTasks
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProjectTemplateId == task.ProjectTemplateId && x.Id == task.ParentTemplateTaskId.Value, cancellationToken);
        }

        task.SortOrder = await GetNextTemplateSortOrderAsync(task.ProjectTemplateId, task.ParentTemplateTaskId, cancellationToken);
        task.OutlineLevel = parentTask is null ? 0 : parentTask.OutlineLevel + 1;
        task.WbsCode = CreateTemplateWbsCode(parentTask?.WbsCode, task.SortOrder);

        _context.ProjectTemplateTasks.Add(task);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateTaskAsync(ProjectTemplateTask task, CancellationToken cancellationToken = default)
    {
        _context.ProjectTemplateTasks.Update(task);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTaskAsync(Guid templateId, Guid taskId, CancellationToken cancellationToken = default)
    {
        var tasks = await _context.ProjectTemplateTasks
            .Where(x => x.ProjectTemplateId == templateId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var task = tasks.FirstOrDefault(x => x.Id == taskId);
        if (task is null)
        {
            return;
        }

        var idsToDelete = new HashSet<Guid>();
        CollectDescendants(task.Id, tasks, idsToDelete);
        idsToDelete.Add(task.Id);

        var deleteList = tasks.Where(x => idsToDelete.Contains(x.Id)).ToList();
        _context.ProjectTemplateTasks.RemoveRange(deleteList);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> ApplyTemplateToProjectAsync(Guid templateId, Guid projectId, DateTime? baseStartDate, CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == projectId && x.Status != ProjectStatus.Cancelled, cancellationToken);

        if (project is null)
        {
            return 0;
        }

        var template = await GetTemplateWithTasksAsync(templateId, cancellationToken);
        if (template is null || !template.IsActive || template.Tasks.Count == 0)
        {
            return 0;
        }

        var orderedTasks = OrderTemplateTasks(template.Tasks);
        var effectiveBaseDate = baseStartDate ?? project.StartDate?.Date;
        var nextSortOrders = new Dictionary<Guid, int>();
        var taskMap = new Dictionary<Guid, ProjectTask>();
        var createdTasks = new List<ProjectTask>();

        nextSortOrders[Guid.Empty] = await GetExistingProjectMaxSortOrderAsync(projectId, null, cancellationToken);

        foreach (var templateTask in orderedTasks)
        {
            ProjectTask? parentProjectTask = null;
            if (templateTask.ParentTemplateTaskId.HasValue)
            {
                taskMap.TryGetValue(templateTask.ParentTemplateTaskId.Value, out parentProjectTask);
            }

            var parentProjectTaskId = parentProjectTask?.Id;
            var parentSortKey = parentProjectTaskId ?? Guid.Empty;
            if (!nextSortOrders.ContainsKey(parentSortKey))
            {
                nextSortOrders[parentSortKey] = parentProjectTask is null
                    ? await GetExistingProjectMaxSortOrderAsync(projectId, null, cancellationToken)
                    : 0;
            }

            var sortOrder = nextSortOrders[parentSortKey] + 1;
            nextSortOrders[parentSortKey] = sortOrder;
            nextSortOrders[templateTask.Id] = 0;

            var startDate = effectiveBaseDate.HasValue && templateTask.DefaultStartOffsetDays.HasValue
                ? effectiveBaseDate.Value.AddDays(templateTask.DefaultStartOffsetDays.Value)
                : effectiveBaseDate;

            DateTime? dueDate = null;
            if (startDate.HasValue && templateTask.DefaultDurationDays.HasValue)
            {
                var durationDays = Math.Max(templateTask.DefaultDurationDays.Value, 1);
                dueDate = startDate.Value.AddDays(durationDays - 1);
            }

            var projectTask = new ProjectTask
            {
                ProjectId = projectId,
                ParentTaskId = parentProjectTaskId,
                TaskCategoryId = templateTask.TaskCategoryId,
                Title = templateTask.Title,
                Description = templateTask.Description,
                Status = WorkTaskStatus.Todo,
                Priority = templateTask.DefaultPriority,
                StartDate = startDate,
                DueDate = dueDate,
                ProgressPercent = 0,
                ResponsibleUserId = templateTask.DefaultResponsibleUserId,
                AssignedToUserId = templateTask.DefaultAssignedUserId,
                SortOrder = sortOrder,
                OutlineLevel = parentProjectTask is null ? 0 : parentProjectTask.OutlineLevel + 1,
                WbsCode = CreateTemplateWbsCode(parentProjectTask?.WbsCode, sortOrder),
                IsMilestone = templateTask.IsMilestone
            };

            _context.ProjectTasks.Add(projectTask);
            createdTasks.Add(projectTask);
            taskMap[templateTask.Id] = projectTask;

            if (!string.IsNullOrWhiteSpace(templateTask.DefaultAssignedUserId))
            {
                _context.ProjectTaskAssignments.Add(new ProjectTaskAssignment
                {
                    ProjectTaskId = projectTask.Id,
                    UserId = templateTask.DefaultAssignedUserId
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _projectTimelineService.AddAsync(projectId, "Görev şablonu uygulandı", $"{template.Name} şablonundan {createdTasks.Count} görev oluşturuldu.", cancellationToken);

        return createdTasks.Count;
    }

    private async Task<int> GetNextTemplateSortOrderAsync(Guid templateId, Guid? parentTemplateTaskId, CancellationToken cancellationToken)
    {
        var siblingQuery = _context.ProjectTemplateTasks
            .AsNoTracking()
            .Where(x => x.ProjectTemplateId == templateId && x.ParentTemplateTaskId == parentTemplateTaskId);

        return await siblingQuery.AnyAsync(cancellationToken)
            ? await siblingQuery.MaxAsync(x => x.SortOrder, cancellationToken) + 1
            : 1;
    }

    private async Task<int> GetExistingProjectMaxSortOrderAsync(Guid projectId, Guid? parentTaskId, CancellationToken cancellationToken)
    {
        var siblingQuery = _context.ProjectTasks
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId && x.ParentTaskId == parentTaskId);

        return await siblingQuery.AnyAsync(cancellationToken)
            ? await siblingQuery.MaxAsync(x => x.SortOrder, cancellationToken)
            : 0;
    }

    private static string CreateTemplateWbsCode(string? parentWbsCode, int sortOrder)
    {
        return string.IsNullOrWhiteSpace(parentWbsCode)
            ? sortOrder.ToString()
            : $"{parentWbsCode}.{sortOrder}";
    }

    private static List<ProjectTemplateTask> OrderTemplateTasks(IEnumerable<ProjectTemplateTask> tasks)
    {
        var taskList = tasks.ToList();
        var taskIds = taskList.Select(x => x.Id).ToHashSet();
        var childrenByParent = taskList
            .Where(x => x.ParentTemplateTaskId.HasValue)
            .GroupBy(x => x.ParentTemplateTaskId!.Value)
            .ToDictionary(x => x.Key, x => SortTemplateTasks(x).ToList());

        var ordered = new List<ProjectTemplateTask>();
        foreach (var root in SortTemplateTasks(taskList.Where(x => !x.ParentTemplateTaskId.HasValue || !taskIds.Contains(x.ParentTemplateTaskId.Value))))
        {
            AddTask(root);
        }

        return ordered;

        void AddTask(ProjectTemplateTask task)
        {
            ordered.Add(task);
            if (!childrenByParent.TryGetValue(task.Id, out var children))
            {
                return;
            }

            foreach (var child in children)
            {
                AddTask(child);
            }
        }
    }

    private static IOrderedEnumerable<ProjectTemplateTask> SortTemplateTasks(IEnumerable<ProjectTemplateTask> tasks)
    {
        return tasks
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.WbsCode)
            .ThenBy(x => x.OutlineLevel)
            .ThenBy(x => x.Title);
    }

    private static void CollectDescendants(Guid parentId, IReadOnlyCollection<ProjectTemplateTask> tasks, ISet<Guid> idsToDelete)
    {
        var children = tasks.Where(x => x.ParentTemplateTaskId == parentId).ToList();
        foreach (var child in children)
        {
            if (idsToDelete.Add(child.Id))
            {
                CollectDescendants(child.Id, tasks, idsToDelete);
            }
        }
    }
}
