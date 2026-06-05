using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Services;

public class RecordActivityService : IRecordActivityService
{
    private readonly ApplicationDbContext _context;

    public RecordActivityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RecordComment>> GetCommentsAsync(RecordOwnerType ownerType, Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.RecordComments
            .AsNoTracking()
            .Where(x => x.OwnerType == ownerType && x.OwnerId == ownerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecordFile>> GetFilesAsync(RecordOwnerType ownerType, Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.RecordFiles
            .AsNoTracking()
            .Where(x => x.OwnerType == ownerType && x.OwnerId == ownerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<RecordComment> AddCommentAsync(RecordComment comment, CancellationToken cancellationToken = default)
    {
        _context.RecordComments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);
        return comment;
    }

    public async Task<RecordFile> AddFileAsync(RecordFile file, CancellationToken cancellationToken = default)
    {
        _context.RecordFiles.Add(file);
        await _context.SaveChangesAsync(cancellationToken);
        return file;
    }

    public Task<RecordComment?> GetCommentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.RecordComments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<RecordFile?> GetFileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.RecordFiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task DeleteCommentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var comment = await GetCommentAsync(id, cancellationToken);
        if (comment is null)
        {
            return;
        }

        _context.RecordComments.Remove(comment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteFileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var file = await GetFileAsync(id, cancellationToken);
        if (file is null)
        {
            return;
        }

        _context.RecordFiles.Remove(file);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
