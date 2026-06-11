using Business.Application.Common;
using Business.Application.Services;
using Business.Domain.Entities;
using Business.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Services;

public class PersonalNoteService : IPersonalNoteService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public PersonalNoteService(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public Task<List<PersonalNote>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return ListQuery()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<PersonalNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        return _context.PersonalNotes
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == userId, cancellationToken);
    }

    public Task<PersonalNote?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DetailsQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task CreateAsync(PersonalNote entity, CancellationToken cancellationToken = default)
    {
        entity.OwnerUserId = GetRequiredUserId();
        await _context.PersonalNotes.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PersonalNote entity, CancellationToken cancellationToken = default)
    {
        var existing = await _context.PersonalNotes
            .FirstOrDefaultAsync(x => x.Id == entity.Id && x.OwnerUserId == GetRequiredUserId(), cancellationToken);

        if (existing is null)
        {
            return;
        }

        existing.CustomerId = entity.CustomerId;
        existing.ProjectId = entity.ProjectId;
        existing.ProjectTaskId = entity.ProjectTaskId;
        existing.Category = entity.Category;
        existing.Title = entity.Title;
        existing.Content = entity.Content;
        existing.ReminderAt = entity.ReminderAt;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _context.PersonalNotes
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == GetRequiredUserId(), cancellationToken);

        if (existing is null)
        {
            return;
        }

        _context.PersonalNotes.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<PersonalNote> ListQuery()
    {
        var userId = GetRequiredUserId();
        return _context.PersonalNotes
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Project)
            .Include(x => x.ProjectTask)
            .Where(x => x.OwnerUserId == userId);
    }

    private IQueryable<PersonalNote> DetailsQuery()
    {
        return ListQuery();
    }

    private string GetRequiredUserId()
    {
        return _currentUserService.UserId
            ?? throw new InvalidOperationException("Kişisel kayıtlara erişmek için giriş yapan kullanıcı gereklidir.");
    }
}
