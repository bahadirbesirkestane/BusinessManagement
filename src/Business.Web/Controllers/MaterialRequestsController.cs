using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanViewMaterialRequests)]
public class MaterialRequestsController : Controller
{
    private readonly IMaterialRequestService _materialRequestService;
    private readonly ILookupService _lookupService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IProjectTimelineService _projectTimelineService;
    private readonly ApplicationDbContext _context;

    public MaterialRequestsController(
        IMaterialRequestService materialRequestService,
        ILookupService lookupService,
        UserManager<ApplicationUser> userManager,
        IProjectTimelineService projectTimelineService,
        ApplicationDbContext context)
    {
        _materialRequestService = materialRequestService;
        _lookupService = lookupService;
        _userManager = userManager;
        _projectTimelineService = projectTimelineService;
        _context = context;
    }

    public async Task<IActionResult> Index(
        Guid? projectId,
        string? q,
        MaterialRequestStatus? status,
        Guid? materialId,
        DateTime? neededFrom,
        DateTime? neededTo,
        string? sort,
        CancellationToken cancellationToken)
    {
        var query = _context.MaterialRequests
            .Include(x => x.Project)
            .Include(x => x.Material)
            .AsNoTracking();

        if (projectId.HasValue)
        {
            query = query.Where(x => x.ProjectId == projectId.Value);
            ViewBag.ProjectId = projectId.Value;
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.RequestedItem.Contains(term) ||
                (x.QuantityText != null && x.QuantityText.Contains(term)) ||
                (x.Quality != null && x.Quality.Contains(term)) ||
                (x.Notes != null && x.Notes.Contains(term)) ||
                (x.Project != null && (x.Project.Code.Contains(term) || x.Project.Name.Contains(term))) ||
                (x.Material != null && x.Material.Name.Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (materialId.HasValue)
        {
            query = query.Where(x => x.MaterialId == materialId.Value);
        }

        if (neededFrom.HasValue)
        {
            query = query.Where(x => x.NeededBy >= neededFrom.Value);
        }

        if (neededTo.HasValue)
        {
            query = query.Where(x => x.NeededBy <= neededTo.Value);
        }

        query = sort switch
        {
            "item" => query.OrderBy(x => x.RequestedItem),
            "project" => query.OrderBy(x => x.Project == null ? string.Empty : x.Project.Code),
            "material" => query.OrderBy(x => x.Material == null ? string.Empty : x.Material.Name),
            "status" => query.OrderBy(x => x.Status),
            "needed" => query.OrderBy(x => x.NeededBy),
            "oldest" => query.OrderBy(x => x.CreatedAt),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };

        ViewBag.FilterQ = q;
        ViewBag.FilterStatus = status;
        ViewBag.FilterMaterialId = materialId;
        ViewBag.FilterNeededFrom = neededFrom?.ToString("yyyy-MM-dd");
        ViewBag.FilterNeededTo = neededTo?.ToString("yyyy-MM-dd");
        ViewBag.Sort = sort;
        ViewBag.Projects = await _lookupService.GetProjectsAsync(cancellationToken);
        ViewBag.Materials = await _lookupService.GetMaterialsAsync(cancellationToken);

        return View(await query.ToListAsync(cancellationToken));
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var request = await _materialRequestService.GetDetailsAsync(id, cancellationToken);
        return request is null ? NotFound() : View(request);
    }

    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await FillLookupsAsync(cancellationToken);
        return View(new MaterialRequest { NeededBy = DateTime.Today.AddDays(3) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> Create(MaterialRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(cancellationToken);
            return View(request);
        }

        request.RequestedByUserId = _userManager.GetUserId(User);
        await _materialRequestService.CreateAsync(request, cancellationToken);
        if (request.ProjectId.HasValue)
        {
            await _projectTimelineService.AddAsync(request.ProjectId.Value, "İhtiyaç listesine eklendi", request.RequestedItem, cancellationToken);
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var request = await _materialRequestService.GetByIdAsync(id, cancellationToken);
        if (request is null)
        {
            return NotFound();
        }

        await FillLookupsAsync(cancellationToken);
        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> Edit(Guid id, MaterialRequest request, CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(cancellationToken);
            return View(request);
        }

        await _materialRequestService.UpdateAsync(request, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var request = await _materialRequestService.GetDetailsAsync(id, cancellationToken);
        return request is null ? NotFound() : View(request);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        _context.RecordComments.RemoveRange(_context.RecordComments.Where(x => x.OwnerType == RecordOwnerType.MaterialRequest && x.OwnerId == id));
        _context.RecordFiles.RemoveRange(_context.RecordFiles.Where(x => x.OwnerType == RecordOwnerType.MaterialRequest && x.OwnerId == id));
        await _context.SaveChangesAsync(cancellationToken);
        await _materialRequestService.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> BulkDelete(Guid[] ids, string? returnUrl, CancellationToken cancellationToken)
    {
        var selectedIds = ids.Distinct().ToList();
        if (selectedIds.Count == 0)
        {
            return RedirectToLocal(returnUrl);
        }

        _context.RecordComments.RemoveRange(_context.RecordComments.Where(x => x.OwnerType == RecordOwnerType.MaterialRequest && selectedIds.Contains(x.OwnerId)));
        _context.RecordFiles.RemoveRange(_context.RecordFiles.Where(x => x.OwnerType == RecordOwnerType.MaterialRequest && selectedIds.Contains(x.OwnerId)));
        _context.MaterialRequests.RemoveRange(_context.MaterialRequests.Where(x => selectedIds.Contains(x.Id)));
        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToLocal(returnUrl);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    private async Task FillLookupsAsync(CancellationToken cancellationToken)
    {
        ViewBag.Projects = await _lookupService.GetProjectsAsync(cancellationToken);
        ViewBag.Materials = await _lookupService.GetMaterialsAsync(cancellationToken);
    }
}
