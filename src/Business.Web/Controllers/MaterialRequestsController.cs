using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.Extensions;
using Business.Web.ViewModels;
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
        string? requestedByUserId,
        DateTime? neededFrom,
        DateTime? neededTo,
        string? sort,
        bool includeFulfilled = false,
        CancellationToken cancellationToken = default)
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
            var matchingRequesterIds = await _userManager.Users
                .AsNoTracking()
                .Where(x =>
                    (x.FullName != null && x.FullName.Contains(term)) ||
                    (x.Email != null && x.Email.Contains(term)))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(x =>
                x.RequestedItem.Contains(term) ||
                (x.QuantityText != null && x.QuantityText.Contains(term)) ||
                (x.Quality != null && x.Quality.Contains(term)) ||
                (x.Notes != null && x.Notes.Contains(term)) ||
                (x.Project != null && (x.Project.Code.Contains(term) || x.Project.Name.Contains(term))) ||
                (x.Material != null && x.Material.Name.Contains(term)) ||
                matchingRequesterIds.Contains(x.RequestedByUserId!));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }
        else if (!includeFulfilled)
        {
            query = query.Where(x => x.Status != MaterialRequestStatus.Fulfilled);
        }

        if (materialId.HasValue)
        {
            query = query.Where(x => x.MaterialId == materialId.Value);
        }

        if (!string.IsNullOrWhiteSpace(requestedByUserId))
        {
            query = query.Where(x => x.RequestedByUserId == requestedByUserId);
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
        ViewBag.FilterRequestedByUserId = requestedByUserId;
        ViewBag.FilterNeededFrom = neededFrom?.ToString("yyyy-MM-dd");
        ViewBag.FilterNeededTo = neededTo?.ToString("yyyy-MM-dd");
        ViewBag.Sort = sort;
        ViewBag.IncludeFulfilled = includeFulfilled;
        ViewBag.ListAction = nameof(Index);
        ViewBag.RequestListTitle = includeFulfilled ? "Tüm ihtiyaçlar" : "Açık ihtiyaçlar";
        ViewBag.Projects = await _lookupService.GetProjectsAsync(cancellationToken);
        ViewBag.Materials = await _lookupService.GetMaterialsAsync(cancellationToken);
        ViewBag.Users = await GetActiveUsersAsync(cancellationToken);

        var requests = await query.ToListAsync(cancellationToken);
        ViewBag.RequestedByNames = await GetRequestedByNamesAsync(
            requests.Select(x => x.RequestedByUserId).Where(x => !string.IsNullOrWhiteSpace(x))!,
            cancellationToken);

        return View(requests);
    }

    public async Task<IActionResult> Fulfilled(
        Guid? projectId,
        string? q,
        Guid? materialId,
        string? requestedByUserId,
        DateTime? neededFrom,
        DateTime? neededTo,
        string? sort,
        CancellationToken cancellationToken)
    {
        var result = await Index(
            projectId,
            q,
            MaterialRequestStatus.Fulfilled,
            materialId,
            requestedByUserId,
            neededFrom,
            neededTo,
            sort,
            includeFulfilled: true,
            cancellationToken);

        ViewBag.ListAction = nameof(Fulfilled);
        ViewBag.RequestListTitle = "Karşılanan ihtiyaçlar";
        return result is ViewResult viewResult
            ? View(nameof(Index), viewResult.Model)
            : result;
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var request = await _materialRequestService.GetDetailsAsync(id, cancellationToken);
        if (request is null)
        {
            return NotFound();
        }

        ViewBag.RequestedByName = await GetRequestedByNameAsync(request.RequestedByUserId, cancellationToken);
        return View(request);
    }

    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> Create(Guid? projectId, string? returnUrl, CancellationToken cancellationToken)
    {
        var model = new MaterialRequest
        {
            NeededBy = DateTime.Today.AddDays(3),
            Status = MaterialRequestStatus.Requested,
            ProjectId = projectId
        };

        if (projectId.HasValue)
        {
            var projectExists = await _context.Projects
                .AsNoTracking()
                .AnyAsync(x => x.Id == projectId.Value, cancellationToken);
            if (!projectExists)
            {
                return NotFound();
            }
        }

        await FillLookupsAsync(cancellationToken, NormalizeReturnUrl(returnUrl));
        return View(model);
    }

    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> QuickCreate(Guid? projectId, string? returnUrl, CancellationToken cancellationToken)
    {
        if (projectId.HasValue)
        {
            var projectExists = await _context.Projects
                .AsNoTracking()
                .AnyAsync(x => x.Id == projectId.Value, cancellationToken);
            if (!projectExists)
            {
                return NotFound();
            }
        }

        var model = new QuickMaterialRequestViewModel
        {
            ProjectId = projectId,
            NeededBy = DateTime.Today.AddDays(3),
            Status = MaterialRequestStatus.Requested
        };

        EnsureQuickRows(model);
        await FillLookupsAsync(cancellationToken, NormalizeReturnUrl(returnUrl));
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> Create(MaterialRequest request, string? returnUrl, CancellationToken cancellationToken)
    {
        returnUrl = NormalizeReturnUrl(returnUrl);
        NormalizeMaterialRequestInput(request);
        request.MaterialId = await ResolveMaterialIdAsync(request.MaterialId, request.MaterialNameInput, request.Unit, cancellationToken);

        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(cancellationToken, returnUrl);
            return View(request);
        }

        request.RequestedByUserId = _userManager.GetUserId(User);
        await _materialRequestService.CreateAsync(request, cancellationToken);

        if (request.ProjectId.HasValue)
        {
            await _projectTimelineService.AddAsync(
                request.ProjectId.Value,
                "İhtiyaç listesine eklendi",
                request.RequestedItem,
                cancellationToken);
        }

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> QuickCreate(QuickMaterialRequestViewModel model, string? returnUrl, CancellationToken cancellationToken)
    {
        returnUrl = NormalizeReturnUrl(returnUrl);
        NormalizeQuickCreateInput(model);

        if (model.ProjectId.HasValue)
        {
            var projectExists = await _context.Projects
                .AsNoTracking()
                .AnyAsync(x => x.Id == model.ProjectId.Value, cancellationToken);
            if (!projectExists)
            {
                ModelState.AddModelError(nameof(model.ProjectId), "Seçilen proje bulunamadı.");
            }
        }

        var validLines = model.Lines
            .Where(x => !string.IsNullOrWhiteSpace(x.RequestedItem))
            .ToList();

        if (validLines.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Kaydedilecek en az bir ihtiyaç satırı girin.");
        }

        if (!ModelState.IsValid)
        {
            EnsureQuickRows(model);
            await FillLookupsAsync(cancellationToken, returnUrl);
            return View(model);
        }

        var requestedByUserId = _userManager.GetUserId(User);
        var createdRequests = new List<MaterialRequest>();

        foreach (var line in validLines)
        {
            var materialId = await ResolveMaterialIdAsync(line.MaterialId, line.MaterialName, line.Unit, cancellationToken);
            if (!ModelState.IsValid)
            {
                EnsureQuickRows(model);
                await FillLookupsAsync(cancellationToken, returnUrl);
                return View(model);
            }

            createdRequests.Add(new MaterialRequest
            {
                ProjectId = model.ProjectId,
                MaterialId = materialId,
                MaterialNameInput = line.MaterialName,
                RequestedItem = line.RequestedItem!.Trim(),
                Quantity = line.Quantity,
                QuantityText = string.IsNullOrWhiteSpace(line.QuantityText) && line.Quantity.HasValue
                    ? string.IsNullOrWhiteSpace(line.Unit)
                        ? line.Quantity.Value.ToString("0.####")
                        : $"{line.Quantity.Value:0.####} {line.Unit!.Trim()}"
                    : line.QuantityText?.Trim(),
                Unit = line.Unit?.Trim(),
                Quality = line.Quality?.Trim(),
                Status = model.Status,
                NeededBy = model.NeededBy,
                RequestedByUserId = requestedByUserId,
                Notes = line.Notes?.Trim()
            });
        }

        _context.MaterialRequests.AddRange(createdRequests);
        await _context.SaveChangesAsync(cancellationToken);

        if (model.ProjectId.HasValue)
        {
            foreach (var request in createdRequests)
            {
                await _projectTimelineService.AddAsync(
                    model.ProjectId.Value,
                    "Hızlı ihtiyaç kaydı oluşturuldu",
                    request.RequestedItem,
                    cancellationToken);
            }
        }

        TempData["Success"] = $"{createdRequests.Count} ihtiyaç kaydı oluşturuldu.";
        return RedirectToLocal(returnUrl);
    }

    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> Edit(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var request = await _materialRequestService.GetByIdAsync(id, cancellationToken);
        if (request is null)
        {
            return NotFound();
        }

        await PopulateMaterialInputNameAsync(request, cancellationToken);
        await FillLookupsAsync(cancellationToken, NormalizeReturnUrl(returnUrl));
        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> Edit(Guid id, MaterialRequest request, string? returnUrl, CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            return BadRequest();
        }

        returnUrl = NormalizeReturnUrl(returnUrl);
        NormalizeMaterialRequestInput(request);
        request.MaterialId = await ResolveMaterialIdAsync(request.MaterialId, request.MaterialNameInput, request.Unit, cancellationToken);

        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(cancellationToken, returnUrl);
            return View(request);
        }

        await _materialRequestService.UpdateAsync(request, cancellationToken);

        if (request.ProjectId.HasValue)
        {
            await _projectTimelineService.AddAsync(
                request.ProjectId.Value,
                "İhtiyaç kaydı güncellendi",
                request.RequestedItem,
                cancellationToken);
        }

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> UpdateStatus(Guid id, MaterialRequestStatus status, string? returnUrl, CancellationToken cancellationToken)
    {
        var request = await _context.MaterialRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != status)
        {
            request.Status = status;
            NormalizeMaterialRequestInput(request);
            await _context.SaveChangesAsync(cancellationToken);

            if (request.ProjectId.HasValue)
            {
                await _projectTimelineService.AddAsync(
                    request.ProjectId.Value,
                    "İhtiyaç durumu güncellendi",
                    $"{request.RequestedItem} - {status.ToDisplayName()}",
                    cancellationToken);
            }
        }

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> Repeat(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var sourceRequest = await _context.MaterialRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (sourceRequest is null)
        {
            return NotFound();
        }

        var repeatedRequest = new MaterialRequest
        {
            ProjectId = sourceRequest.ProjectId,
            MaterialId = sourceRequest.MaterialId,
            RequestedItem = sourceRequest.RequestedItem,
            Quantity = sourceRequest.Quantity,
            QuantityText = sourceRequest.QuantityText,
            Unit = sourceRequest.Unit,
            Quality = sourceRequest.Quality,
            NeededBy = sourceRequest.NeededBy,
            Notes = sourceRequest.Notes,
            Status = MaterialRequestStatus.Requested,
            RequestedByUserId = _userManager.GetUserId(User)
        };

        NormalizeMaterialRequestInput(repeatedRequest);
        await _materialRequestService.CreateAsync(repeatedRequest, cancellationToken);

        if (repeatedRequest.ProjectId.HasValue)
        {
            await _projectTimelineService.AddAsync(
                repeatedRequest.ProjectId.Value,
                "İhtiyaç kaydı tekrarlandı",
                repeatedRequest.RequestedItem,
                cancellationToken);
        }

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> BulkUpdateStatus(Guid[] ids, MaterialRequestStatus status, string? returnUrl, CancellationToken cancellationToken)
    {
        var selectedIds = ids.Distinct().ToList();
        if (selectedIds.Count == 0)
        {
            return RedirectToLocal(returnUrl);
        }

        var requests = await _context.MaterialRequests
            .Where(x => selectedIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            request.Status = status;
            NormalizeMaterialRequestInput(request);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanCreatePurchasing)]
    public async Task<IActionResult> TransferToQuickOrder(Guid[] ids, string? returnUrl, CancellationToken cancellationToken)
    {
        var selectedIds = ids.Distinct().ToList();
        if (selectedIds.Count == 0)
        {
            TempData["Error"] = "Siparişe aktarmak için en az bir ihtiyaç seçin.";
            return RedirectToLocal(returnUrl);
        }

        var requests = await _context.MaterialRequests
            .AsNoTracking()
            .Where(x => selectedIds.Contains(x.Id))
            .Select(x => new { x.Id, x.ProjectId })
            .ToListAsync(cancellationToken);

        if (requests.Count == 0)
        {
            TempData["Error"] = "Seçilen ihtiyaç kayıtları bulunamadı.";
            return RedirectToLocal(returnUrl);
        }

        var projectIds = requests
            .Select(x => x.ProjectId)
            .Distinct()
            .ToList();

        if (projectIds.Count > 1)
        {
            TempData["Error"] = "Hızlı siparişe aktarım için seçilen ihtiyaçların aynı projeye bağlı olması gerekir.";
            return RedirectToLocal(returnUrl);
        }

        return RedirectToAction(
            "QuickCreate",
            "PurchaseOrders",
            new
            {
                projectId = projectIds[0],
                materialRequestIds = requests.Select(x => x.Id).ToArray(),
                returnUrl
            });
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

    private static void NormalizeMaterialRequestInput(MaterialRequest request)
    {
        request.RequestedItem = request.RequestedItem?.Trim() ?? string.Empty;
        request.QuantityText = string.IsNullOrWhiteSpace(request.QuantityText) ? null : request.QuantityText.Trim();
        request.Unit = string.IsNullOrWhiteSpace(request.Unit) ? null : request.Unit.Trim();
        request.Quality = string.IsNullOrWhiteSpace(request.Quality) ? null : request.Quality.Trim();
        request.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        if (request.Quantity.HasValue && request.Quantity.Value <= 0)
        {
            request.Quantity = null;
        }

        if (string.IsNullOrWhiteSpace(request.QuantityText) && request.Quantity.HasValue)
        {
            var quantityText = request.Quantity.Value.ToString("0.###");
            request.QuantityText = string.IsNullOrWhiteSpace(request.Unit)
                ? quantityText
                : $"{quantityText} {request.Unit}";
        }
    }

    private async Task PopulateMaterialInputNameAsync(MaterialRequest request, CancellationToken cancellationToken)
    {
        if (request.MaterialId.HasValue && string.IsNullOrWhiteSpace(request.MaterialNameInput))
        {
            request.MaterialNameInput = await _context.Materials
                .AsNoTracking()
                .Where(x => x.Id == request.MaterialId.Value)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }

    private static void EnsureQuickRows(QuickMaterialRequestViewModel model)
    {
        while (model.Lines.Count < 4)
        {
            model.Lines.Add(new QuickMaterialRequestLineViewModel());
        }
    }

    private static void NormalizeQuickCreateInput(QuickMaterialRequestViewModel model)
    {
        foreach (var line in model.Lines)
        {
            line.MaterialName = string.IsNullOrWhiteSpace(line.MaterialName) ? null : line.MaterialName.Trim();
            line.RequestedItem = string.IsNullOrWhiteSpace(line.RequestedItem) ? null : line.RequestedItem.Trim();
            line.QuantityText = string.IsNullOrWhiteSpace(line.QuantityText) ? null : line.QuantityText.Trim();
            line.Unit = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit.Trim();
            line.Quality = string.IsNullOrWhiteSpace(line.Quality) ? null : line.Quality.Trim();
            line.Notes = string.IsNullOrWhiteSpace(line.Notes) ? null : line.Notes.Trim();
        }
    }

    private async Task<Guid?> ResolveMaterialIdAsync(Guid? materialId, string? materialName, string? unit, CancellationToken cancellationToken)
    {
        if (materialId.HasValue)
        {
            return materialId;
        }

        var normalizedName = string.IsNullOrWhiteSpace(materialName) ? null : materialName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return null;
        }

        var existingId = await _context.Materials
            .AsNoTracking()
            .Where(x => x.Name == normalizedName)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingId.HasValue)
        {
            return existingId.Value;
        }

        var material = new Material
        {
            Name = normalizedName,
            Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim()
        };

        _context.Materials.Add(material);
        return material.Id;
    }

    private static string? NormalizeReturnUrl(string? returnUrl)
    {
        return string.IsNullOrWhiteSpace(returnUrl) ? null : returnUrl;
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    private async Task FillLookupsAsync(CancellationToken cancellationToken, string? returnUrl = null)
    {
        ViewBag.Projects = await _lookupService.GetProjectsAsync(cancellationToken);
        ViewBag.Materials = await _lookupService.GetMaterialsAsync(cancellationToken);
        ViewBag.Users = await GetActiveUsersAsync(cancellationToken);
        ViewBag.ReturnUrl = returnUrl;
    }

    private async Task<Dictionary<string, string>> GetRequestedByNamesAsync(IEnumerable<string> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return [];
        }

        return await _userManager.Users
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(
                x => x.Id,
                x => x.FullName ?? x.Email ?? x.UserName ?? x.Id,
                cancellationToken);
    }

    private async Task<string?> GetRequestedByNameAsync(string? userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var user = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        return user is null ? null : user.FullName ?? user.Email ?? user.UserName ?? user.Id;
    }

    private async Task<List<ApplicationUser>> GetActiveUsersAsync(CancellationToken cancellationToken)
    {
        return await _userManager.Users
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.FullName)
            .ToListAsync(cancellationToken);
    }
}
