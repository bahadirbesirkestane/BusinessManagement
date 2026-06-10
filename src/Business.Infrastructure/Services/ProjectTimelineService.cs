using Business.Application.Services;
using Business.Domain.Entities;
using Business.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Services;

public class ProjectTimelineService : IProjectTimelineService
{
    private readonly ApplicationDbContext _context;

    public ProjectTimelineService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Guid projectId, string title, string? description = null, CancellationToken cancellationToken = default)
    {
        _context.ProjectUpdates.Add(new ProjectUpdate
        {
            ProjectId = projectId,
            Title = title,
            Description = description
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddForTaskAsync(Guid taskId, string title, string? description = null, CancellationToken cancellationToken = default)
    {
        var task = await _context.ProjectTasks
            .Where(x => x.Id == taskId)
            .Select(x => new { x.Id, x.ProjectId })
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
        {
            return;
        }

        _context.ProjectTaskUpdates.Add(new ProjectTaskUpdate
        {
            ProjectTaskId = task.Id,
            Title = title,
            Description = description
        });

        if (task.ProjectId.HasValue)
        {
            _context.ProjectUpdates.Add(new ProjectUpdate
            {
                ProjectId = task.ProjectId.Value,
                Title = title,
                Description = description
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddForOrderAsync(Guid orderId, string title, string? description = null, CancellationToken cancellationToken = default)
    {
        var projectId = await _context.PurchaseOrders
            .Where(x => x.Id == orderId)
            .Select(x => x.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);

        if (projectId.HasValue)
        {
            await AddAsync(projectId.Value, title, description, cancellationToken);
        }
    }
}
