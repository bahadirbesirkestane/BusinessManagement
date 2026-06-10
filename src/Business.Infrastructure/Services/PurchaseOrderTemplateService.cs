using Business.Application.Repositories;
using Business.Application.Services;
using Business.Domain.Entities;
using Business.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Services;

public class PurchaseOrderTemplateService : CrudService<PurchaseOrderTemplate>, IPurchaseOrderTemplateService
{
    private readonly ApplicationDbContext _context;

    public PurchaseOrderTemplateService(IRepository<PurchaseOrderTemplate> repository, ApplicationDbContext context) : base(repository)
    {
        _context = context;
    }

    protected override IQueryable<PurchaseOrderTemplate> ListQuery()
    {
        return Repository.Query()
            .Include(x => x.DefaultSupplier)
            .Include(x => x.Lines);
    }

    protected override IQueryable<PurchaseOrderTemplate> DetailsQuery()
    {
        return Repository.Query()
            .Include(x => x.DefaultSupplier)
            .Include(x => x.Lines)
                .ThenInclude(x => x.Material);
    }

    public Task<PurchaseOrderTemplate?> GetTemplateWithLinesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DetailsQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<PurchaseOrderTemplateLine?> GetLineByIdAsync(Guid templateId, Guid lineId, CancellationToken cancellationToken = default)
    {
        return _context.PurchaseOrderTemplateLines
            .FirstOrDefaultAsync(x => x.PurchaseOrderTemplateId == templateId && x.Id == lineId, cancellationToken);
    }

    public async Task AddLineAsync(PurchaseOrderTemplateLine line, CancellationToken cancellationToken = default)
    {
        line.SortOrder = await GetNextSortOrderAsync(line.PurchaseOrderTemplateId, cancellationToken);
        _context.PurchaseOrderTemplateLines.Add(line);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateLineAsync(PurchaseOrderTemplateLine line, CancellationToken cancellationToken = default)
    {
        _context.PurchaseOrderTemplateLines.Update(line);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteLineAsync(Guid templateId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var line = await GetLineByIdAsync(templateId, lineId, cancellationToken);
        if (line is null)
        {
            return;
        }

        _context.PurchaseOrderTemplateLines.Remove(line);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> GetNextSortOrderAsync(Guid templateId, CancellationToken cancellationToken)
    {
        var query = _context.PurchaseOrderTemplateLines
            .AsNoTracking()
            .Where(x => x.PurchaseOrderTemplateId == templateId);

        return await query.AnyAsync(cancellationToken)
            ? await query.MaxAsync(x => x.SortOrder, cancellationToken) + 1
            : 1;
    }
}
