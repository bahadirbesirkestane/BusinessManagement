using Business.Application.Common;
using Business.Application.Services;
using Business.Domain.Entities;
using Business.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Services;

public class PersonalTaskService : IPersonalTaskService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public PersonalTaskService(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public Task<List<PersonalTask>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return ListQuery()
            .OrderBy(x => x.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<PersonalTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        return _context.PersonalTasks
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == userId, cancellationToken);
    }

    public Task<PersonalTask?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DetailsQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task CreateAsync(PersonalTask entity, CancellationToken cancellationToken = default)
    {
        entity.OwnerUserId = GetRequiredUserId();
        await _context.PersonalTasks.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PersonalTask entity, CancellationToken cancellationToken = default)
    {
        var existing = await _context.PersonalTasks
            .FirstOrDefaultAsync(x => x.Id == entity.Id && x.OwnerUserId == GetRequiredUserId(), cancellationToken);

        if (existing is null)
        {
            return;
        }

        existing.CustomerId = entity.CustomerId;
        existing.ProjectId = entity.ProjectId;
        existing.ProjectTaskId = entity.ProjectTaskId;
        existing.Title = entity.Title;
        existing.Description = entity.Description;
        existing.Status = entity.Status;
        existing.Priority = entity.Priority;
        existing.StartDate = entity.StartDate;
        existing.DueDate = entity.DueDate;
        existing.CompletedAt = entity.CompletedAt;
        existing.Notes = entity.Notes;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _context.PersonalTasks
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == GetRequiredUserId(), cancellationToken);

        if (existing is null)
        {
            return;
        }

        _context.PersonalTasks.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<PersonalTask> ListQuery()
    {
        var userId = GetRequiredUserId();
        return _context.PersonalTasks
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Project)
            .Include(x => x.ProjectTask)
            .Where(x => x.OwnerUserId == userId);
    }

    private IQueryable<PersonalTask> DetailsQuery()
    {
        return ListQuery();
    }

    private string GetRequiredUserId()
    {
        return _currentUserService.UserId
            ?? throw new InvalidOperationException("Kişisel kayıtlara erişmek için giriş yapan kullanıcı gereklidir.");
    }
}
