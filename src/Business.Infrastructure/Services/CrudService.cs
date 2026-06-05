using Business.Application.Repositories;
using Business.Application.Services;
using Business.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Services;

public class CrudService<TEntity> : ICrudService<TEntity> where TEntity : BaseEntity
{
    protected readonly IRepository<TEntity> Repository;

    public CrudService(IRepository<TEntity> repository)
    {
        Repository = repository;
    }

    protected virtual IQueryable<TEntity> ListQuery()
    {
        return Repository.Query();
    }

    protected virtual IQueryable<TEntity> DetailsQuery()
    {
        return Repository.Query();
    }

    public virtual Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return ListQuery().OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public virtual Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Repository.GetByIdAsync(id, cancellationToken);
    }

    public virtual Task<TEntity?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DetailsQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public virtual async Task CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await Repository.AddAsync(entity, cancellationToken);
        await Repository.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Repository.Update(entity);
        await Repository.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        Repository.Delete(entity);
        await Repository.SaveChangesAsync(cancellationToken);
    }
}
