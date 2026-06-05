using Business.Domain.Entities;
using Business.Domain.Enums;

namespace Business.Application.Services;

public interface IRecordActivityService
{
    Task<IReadOnlyList<RecordComment>> GetCommentsAsync(RecordOwnerType ownerType, Guid ownerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecordFile>> GetFilesAsync(RecordOwnerType ownerType, Guid ownerId, CancellationToken cancellationToken = default);
    Task<RecordComment> AddCommentAsync(RecordComment comment, CancellationToken cancellationToken = default);
    Task<RecordFile> AddFileAsync(RecordFile file, CancellationToken cancellationToken = default);
    Task<RecordComment?> GetCommentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RecordFile?> GetFileAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteCommentAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(Guid id, CancellationToken cancellationToken = default);
}
