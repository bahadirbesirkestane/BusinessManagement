using Business.Application.Repositories;
using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Services;

public class MaterialRequestTemplateService : CrudService<MaterialRequestTemplate>, IMaterialRequestTemplateService
{
    private readonly ApplicationDbContext _context;

    public MaterialRequestTemplateService(IRepository<MaterialRequestTemplate> repository, ApplicationDbContext context) : base(repository)
    {
        _context = context;
    }

    protected override IQueryable<MaterialRequestTemplate> ListQuery()
    {
        return Repository.Query()
            .Include(x => x.Lines);
    }

    protected override IQueryable<MaterialRequestTemplate> DetailsQuery()
    {
        return Repository.Query()
            .Include(x => x.Lines)
                .ThenInclude(x => x.Material);
    }

    public Task<MaterialRequestTemplate?> GetTemplateWithLinesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DetailsQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<MaterialRequestTemplateLine?> GetLineByIdAsync(Guid templateId, Guid lineId, CancellationToken cancellationToken = default)
    {
        return _context.MaterialRequestTemplateLines
            .FirstOrDefaultAsync(x => x.MaterialRequestTemplateId == templateId && x.Id == lineId, cancellationToken);
    }

    public async Task AddLineAsync(MaterialRequestTemplateLine line, CancellationToken cancellationToken = default)
    {
        line.SortOrder = await GetNextSortOrderAsync(line.MaterialRequestTemplateId, cancellationToken);
        _context.MaterialRequestTemplateLines.Add(line);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateLineAsync(MaterialRequestTemplateLine line, CancellationToken cancellationToken = default)
    {
        _context.MaterialRequestTemplateLines.Update(line);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteLineAsync(Guid templateId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var line = await GetLineByIdAsync(templateId, lineId, cancellationToken);
        if (line is null)
        {
            return;
        }

        _context.MaterialRequestTemplateLines.Remove(line);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> ApplyTemplateAsync(Guid templateId, Guid? projectId, DateTime neededByDate, string? requestedByUserId, CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateWithLinesAsync(templateId, cancellationToken);
        if (template is null || !template.IsActive)
        {
            return 0;
        }

        if (projectId.HasValue)
        {
            var projectExists = await _context.Projects
                .AsNoTracking()
                .AnyAsync(x => x.Id == projectId.Value && x.Status != ProjectStatus.Cancelled, cancellationToken);

            if (!projectExists)
            {
                return 0;
            }
        }

        var validLines = template.Lines
            .OrderBy(x => x.SortOrder)
            .Where(x => !string.IsNullOrWhiteSpace(x.RequestedItem))
            .ToList();

        if (validLines.Count == 0)
        {
            return 0;
        }

        var createdRequests = new List<MaterialRequest>();

        foreach (var line in validLines)
        {
            var request = new MaterialRequest
            {
                ProjectId = projectId,
                MaterialId = line.MaterialId,
                RequestedItem = line.RequestedItem.Trim(),
                Quantity = line.Quantity,
                QuantityText = string.IsNullOrWhiteSpace(line.QuantityText) && line.Quantity.HasValue
                    ? line.Quantity.Value.ToString("0.###")
                    : line.QuantityText?.Trim(),
                Unit = line.Unit?.Trim(),
                Quality = line.Quality?.Trim(),
                Status = template.DefaultStatus,
                NeededBy = line.NeededByOffsetDays.HasValue
                    ? neededByDate.Date.AddDays(line.NeededByOffsetDays.Value)
                    : neededByDate.Date,
                RequestedByUserId = requestedByUserId,
                Notes = line.Notes?.Trim()
            };

            _context.MaterialRequests.Add(request);
            createdRequests.Add(request);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return createdRequests.Count;
    }

    private async Task<int> GetNextSortOrderAsync(Guid templateId, CancellationToken cancellationToken)
    {
        var query = _context.MaterialRequestTemplateLines
            .AsNoTracking()
            .Where(x => x.MaterialRequestTemplateId == templateId);

        return await query.AnyAsync(cancellationToken)
            ? await query.MaxAsync(x => x.SortOrder, cancellationToken) + 1
            : 1;
    }
}
