using Business.Application.Repositories;
using Business.Domain.Common;
using Business.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Business.Infrastructure.Repositories;

public class EfRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    private readonly ApplicationDbContext _context;

    public EfRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public IQueryable<TEntity> Query()
    {
        return _context.Set<TEntity>().AsQueryable();
    }

    public Task<List<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Query().ToListAsync(cancellationToken);
    }

    public Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return Query().Where(predicate).ToListAsync(cancellationToken);
    }

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<TEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<TEntity>().AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _context.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        _context.Set<TEntity>().Update(entity);
    }

    public void Delete(TEntity entity)
    {
        _context.Set<TEntity>().Remove(entity);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
