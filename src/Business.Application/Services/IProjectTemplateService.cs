using Business.Domain.Entities;

namespace Business.Application.Services;

public interface IProjectTemplateService : ICrudService<ProjectTemplate>
{
    Task<ProjectTemplate?> GetTemplateWithTasksAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProjectTemplateTask?> GetTemplateTaskByIdAsync(Guid templateId, Guid taskId, CancellationToken cancellationToken = default);
    Task AddTaskAsync(ProjectTemplateTask task, CancellationToken cancellationToken = default);
    Task UpdateTaskAsync(ProjectTemplateTask task, CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(Guid templateId, Guid taskId, CancellationToken cancellationToken = default);
    Task<int> ApplyTemplateToProjectAsync(Guid templateId, Guid projectId, DateTime? baseStartDate, CancellationToken cancellationToken = default);
}
