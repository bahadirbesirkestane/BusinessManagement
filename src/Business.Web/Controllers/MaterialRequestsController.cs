using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.Extensions;
using Business.Web.Services;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanViewMaterialRequests)]
public class MaterialRequestsController : Controller
{
    private readonly IMaterialRequestService _materialRequestService;
    private readonly ILookupService _lookupService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IProjectTimelineService _projectTimelineService;
    private readonly IRecordActivityService _recordActivityService;
    private readonly ApplicationDbContext _context;
    private readonly ITelegramNotificationService _telegramNotificationService;

    public MaterialRequestsController(
        IMaterialRequestService materialRequestService,
        ILookupService lookupService,
        UserManager<ApplicationUser> userManager,
        IProjectTimelineService projectTimelineService,
        IRecordActivityService recordActivityService,
        ApplicationDbContext context,
        ITelegramNotificationService telegramNotificationService)
    {
        _materialRequestService = materialRequestService;
        _lookupService = lookupService;
        _userManager = userManager;
        _projectTimelineService = projectTimelineService;
        _recordActivityService = recordActivityService;
        _context = context;
        _telegramNotificationService = telegramNotificationService;
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
        var query = await BuildListQueryAsync(projectId, q, status, materialId, requestedByUserId, neededFrom, neededTo, includeFulfilled, cancellationToken);
        query = ApplyListSorting(query, sort);

        ViewBag.FilterQ = q;
        ViewBag.FilterStatus = status;
        ViewBag.FilterMaterialId = materialId;
        ViewBag.FilterRequestedByUserId = requestedByUserId;
        ViewBag.FilterNeededFrom = neededFrom?.ToString("yyyy-MM-dd");
        ViewBag.FilterNeededTo = neededTo?.ToString("yyyy-MM-dd");
        ViewBag.Sort = sort;
        ViewBag.IncludeFulfilled = includeFulfilled;
        ViewBag.ListAction = includeFulfilled ? nameof(All) : nameof(Index);
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

    [Authorize(Policy = AppPolicies.CanViewMaterialRequests)]
    public async Task<IActionResult> ExportList(
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
        var query = await BuildListQueryAsync(projectId, q, status, materialId, requestedByUserId, neededFrom, neededTo, includeFulfilled, cancellationToken);
        query = ApplyListSorting(query, sort);

        var rows = await query
            .Select(x => (IReadOnlyList<object?>)new object?[]
            {
                x.RequestedItem,
                x.Project != null ? $"{x.Project.Code} - {x.Project.Name}" : "Genel",
                x.Material != null ? x.Material.Name : null,
                x.QuantityText ?? (x.Quantity.HasValue ? x.Quantity.Value.ToString("0.###") : null),
                x.Unit,
                x.Quality,
                x.Status.ToDisplayName(),
                x.NeededBy,
                x.RequestedByUserId,
                x.Notes
            })
            .ToListAsync(cancellationToken);

        if (rows.Count > 0)
        {
            var requesterIds = rows
                .Select(x => x[8] as string)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .Distinct()
                .ToList();

            var requesterMap = await GetRequestedByNamesAsync(requesterIds, cancellationToken);
            rows = rows
                .Select(x => (IReadOnlyList<object?>)new object?[]
                {
                    x[0],
                    x[1],
                    x[2],
                    x[3],
                    x[4],
                    x[5],
                    x[6],
                    x[7],
                    x[8] is string requesterId && requesterMap.TryGetValue(requesterId, out var requesterName) ? requesterName : "-",
                    x[9]
                })
                .ToList();
        }

        var sheetName = status == MaterialRequestStatus.Fulfilled
            ? "Karşılanan İhtiyaçlar"
            : includeFulfilled
                ? "Tüm İhtiyaçlar"
                : "Malzeme İhtiyaçları";
        var filePrefix = status == MaterialRequestStatus.Fulfilled
            ? "karsilanan-ihtiyaclar"
            : includeFulfilled
                ? "tum-malzeme-ihtiyaclari"
                : "malzeme-ihtiyaclari";

        return ExcelFile(
            [new ExcelSheet(sheetName, ["Malzeme", "Proje", "Tanımlı Malzeme", "Miktar", "Birim", "Kalite", "Durum", "Gerekli Tarih", "Veren Kişi", "Not"], rows)],
            $"{filePrefix}-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    public async Task<IActionResult> All(
        Guid? projectId,
        string? q,
        MaterialRequestStatus? status,
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
            status,
            materialId,
            requestedByUserId,
            neededFrom,
            neededTo,
            sort,
            includeFulfilled: true,
            cancellationToken);

        return result is ViewResult viewResult
            ? View(nameof(Index), viewResult.Model)
            : result;
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
        ViewBag.Breadcrumbs = request.Project is not null
            ? new Dictionary<string, string?>
            {
                ["Projeler"] = Url.Action("Index", "Projects"),
                [request.Project.Code] = Url.Action("Details", "Projects", new { id = request.Project.Id }),
                ["İhtiyaçlar"] = Url.Action(nameof(Index), new { projectId = request.Project.Id }),
                [request.RequestedItem] = null
            }
            : new Dictionary<string, string?>
            {
                ["İhtiyaçlar"] = Url.Action(nameof(Index)),
                [request.RequestedItem] = null
            };
        ViewBag.Activity = new RecordActivityViewModel
        {
            OwnerType = RecordOwnerType.MaterialRequest,
            OwnerId = request.Id,
            Comments = await _recordActivityService.GetCommentsAsync(RecordOwnerType.MaterialRequest, request.Id, cancellationToken),
            Files = await _recordActivityService.GetFilesAsync(RecordOwnerType.MaterialRequest, request.Id, cancellationToken),
            UserNames = await GetActivityUserNamesAsync(request.Id, cancellationToken),
            CanDeleteComments = CanDeleteMaterialRequestActivity(),
            CanDeleteFiles = CanDeleteMaterialRequestActivity()
        };
        return View(request);
    }

    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> Create(Guid? projectId, Guid? sourceRequestId, string? returnUrl, CancellationToken cancellationToken)
    {
        MaterialRequest model;

        if (sourceRequestId.HasValue)
        {
            var sourceRequest = await _context.MaterialRequests
                .Include(x => x.Material)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == sourceRequestId.Value, cancellationToken);
            if (sourceRequest is null)
            {
                return NotFound();
            }

            projectId ??= sourceRequest.ProjectId;
            model = new MaterialRequest
            {
                ProjectId = sourceRequest.ProjectId,
                MaterialId = sourceRequest.MaterialId,
                MaterialNameInput = sourceRequest.Material?.Name,
                RequestedItem = sourceRequest.RequestedItem,
                Quantity = sourceRequest.Quantity,
                QuantityText = sourceRequest.QuantityText,
                Unit = sourceRequest.Unit,
                Quality = sourceRequest.Quality,
                NeededBy = sourceRequest.NeededBy,
                Notes = sourceRequest.Notes,
                Status = MaterialRequestStatus.Requested
            };
        }
        else
        {
            model = new MaterialRequest
            {
                NeededBy = DateTime.Today.AddDays(3),
                Status = MaterialRequestStatus.Requested,
                ProjectId = projectId
            };
        }

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
        ViewBag.SendTelegramNotification = true;
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
        ViewBag.FormAction = nameof(QuickCreate);
        ViewBag.PageTitle = "Hızlı malzeme ihtiyaç ekleme";
        ViewBag.SubmitText = "İhtiyaçları Kaydet";
        ViewBag.SendTelegramNotification = true;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> BulkQuickEdit(Guid[] ids, string? returnUrl, CancellationToken cancellationToken)
    {
        var selectedIds = ids.Distinct().ToList();
        if (selectedIds.Count == 0)
        {
            TempData["Error"] = "Hızlı düzenleme için en az bir ihtiyaç seçin.";
            return RedirectToLocal(returnUrl);
        }

        var requests = await _context.MaterialRequests
            .Include(x => x.Material)
            .AsNoTracking()
            .Where(x => selectedIds.Contains(x.Id))
            .OrderBy(x => x.NeededBy)
            .ThenBy(x => x.CreatedAt)
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
            TempData["Error"] = "Hızlı düzenleme için seçilen ihtiyaçların aynı projeye bağlı olması gerekir.";
            return RedirectToLocal(returnUrl);
        }

        var model = new QuickMaterialRequestViewModel
        {
            ProjectId = projectIds[0],
            Status = requests[0].Status,
            NeededBy = requests[0].NeededBy,
            Lines = requests.Select(x => new QuickMaterialRequestLineViewModel
            {
                Id = x.Id,
                MaterialId = x.MaterialId,
                MaterialName = x.Material?.Name,
                RequestedItem = x.RequestedItem,
                Quantity = x.Quantity,
                QuantityText = x.QuantityText,
                Unit = x.Unit,
                Quality = x.Quality,
                Notes = x.Notes
            }).ToList()
        };

        EnsureQuickRows(model);
        await FillLookupsAsync(cancellationToken, NormalizeReturnUrl(returnUrl));
        ViewBag.FormAction = nameof(BulkQuickEditSave);
        ViewBag.PageTitle = "Toplu hızlı malzeme ihtiyaç düzenleme";
        ViewBag.SubmitText = "İhtiyaçları Güncelle";
        return View(nameof(QuickCreate), model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> Create(MaterialRequest request, bool sendTelegramNotification, string? returnUrl, CancellationToken cancellationToken)
    {
        returnUrl = NormalizeReturnUrl(returnUrl);
        NormalizeMaterialRequestInput(request);
        request.MaterialId = await ResolveMaterialIdAsync(request.MaterialId, request.MaterialNameInput, request.Unit, cancellationToken);

        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(cancellationToken, returnUrl);
            ViewBag.SendTelegramNotification = sendTelegramNotification;
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

        if (sendTelegramNotification)
        {
            var warningMessage = await SendMaterialRequestTelegramNotificationAsync([request.Id], isBulk: false, cancellationToken);
            if (!string.IsNullOrWhiteSpace(warningMessage))
            {
                TempData["Error"] = warningMessage;
            }
        }

        TempData["Success"] = "İhtiyaç kaydı oluşturuldu.";
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
            ViewBag.SendTelegramNotification = model.SendTelegramNotification;
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
                ViewBag.SendTelegramNotification = model.SendTelegramNotification;
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

        if (model.SendTelegramNotification)
        {
            var warningMessage = await SendMaterialRequestTelegramNotificationAsync(
                createdRequests.Select(x => x.Id).ToList(),
                isBulk: true,
                cancellationToken);
            if (!string.IsNullOrWhiteSpace(warningMessage))
            {
                TempData["Error"] = warningMessage;
            }
        }

        TempData["Success"] = $"{createdRequests.Count} ihtiyaç kaydı oluşturuldu.";
        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> BulkQuickEditSave(QuickMaterialRequestViewModel model, string? returnUrl, CancellationToken cancellationToken)
    {
        returnUrl = NormalizeReturnUrl(returnUrl);
        NormalizeQuickCreateInput(model);

        var selectedIds = model.Lines
            .Where(x => x.Id.HasValue)
            .Select(x => x.Id!.Value)
            .Distinct()
            .ToList();

        if (selectedIds.Count == 0)
        {
            TempData["Error"] = "Güncellenecek ihtiyaç bulunamadı.";
            return RedirectToLocal(returnUrl);
        }

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

        var existingRequests = await _context.MaterialRequests
            .Include(x => x.Material)
            .Where(x => selectedIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (existingRequests.Count != selectedIds.Count)
        {
            ModelState.AddModelError(string.Empty, "Seçilen ihtiyaçlardan bazıları bulunamadı.");
        }

        var projectIds = existingRequests
            .Select(x => x.ProjectId)
            .Distinct()
            .ToList();

        if (projectIds.Count > 1)
        {
            ModelState.AddModelError(nameof(model.ProjectId), "Hızlı düzenleme için seçilen ihtiyaçların aynı projeye bağlı olması gerekir.");
        }

        if (!ModelState.IsValid)
        {
            EnsureQuickRows(model);
            await FillLookupsAsync(cancellationToken, returnUrl);
            ViewBag.FormAction = nameof(BulkQuickEditSave);
            ViewBag.PageTitle = "Toplu hızlı malzeme ihtiyaç düzenleme";
            ViewBag.SubmitText = "İhtiyaçları Güncelle";
            return View(nameof(QuickCreate), model);
        }

        foreach (var line in model.Lines.Where(x => x.Id.HasValue))
        {
            var request = existingRequests.First(x => x.Id == line.Id!.Value);
            request.ProjectId = model.ProjectId;
            request.MaterialId = await ResolveMaterialIdAsync(line.MaterialId, line.MaterialName, line.Unit, cancellationToken);
            request.RequestedItem = line.RequestedItem?.Trim() ?? request.RequestedItem;
            request.Quantity = line.Quantity;
            request.QuantityText = line.QuantityText;
            request.Unit = line.Unit;
            request.Quality = line.Quality;
            request.Status = model.Status;
            request.NeededBy = model.NeededBy;
            request.Notes = line.Notes;
            NormalizeMaterialRequestInput(request);
        }

        if (!ModelState.IsValid)
        {
            EnsureQuickRows(model);
            await FillLookupsAsync(cancellationToken, returnUrl);
            ViewBag.FormAction = nameof(BulkQuickEditSave);
            ViewBag.PageTitle = "Toplu hızlı malzeme ihtiyaç düzenleme";
            ViewBag.SubmitText = "İhtiyaçları Güncelle";
            return View(nameof(QuickCreate), model);
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (model.ProjectId.HasValue)
        {
            foreach (var request in existingRequests)
            {
                await _projectTimelineService.AddAsync(
                    model.ProjectId.Value,
                    "İhtiyaç toplu hızlı düzenlendi",
                    request.RequestedItem,
                    cancellationToken);
            }
        }

        TempData["Success"] = $"{existingRequests.Count} ihtiyaç kaydı güncellendi.";
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

        return RedirectToAction(nameof(Create), new
        {
            projectId = sourceRequest.ProjectId,
            sourceRequestId = sourceRequest.Id,
            returnUrl = NormalizeReturnUrl(returnUrl)
        });
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

    private async Task<IQueryable<MaterialRequest>> BuildListQueryAsync(
        Guid? projectId,
        string? q,
        MaterialRequestStatus? status,
        Guid? materialId,
        string? requestedByUserId,
        DateTime? neededFrom,
        DateTime? neededTo,
        bool includeFulfilled,
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

        return query;
    }

    private static IOrderedQueryable<MaterialRequest> ApplyListSorting(IQueryable<MaterialRequest> query, string? sort)
    {
        return sort switch
        {
            "item" => query.OrderBy(x => x.RequestedItem),
            "project" => query.OrderBy(x => x.Project == null ? string.Empty : x.Project.Code),
            "material" => query.OrderBy(x => x.Material == null ? string.Empty : x.Material.Name),
            "status" => query.OrderBy(x => x.Status),
            "needed" => query.OrderBy(x => x.NeededBy),
            "oldest" => query.OrderBy(x => x.CreatedAt),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };
    }

    private FileContentResult ExcelFile(IReadOnlyList<ExcelSheet> sheets, string fileName)
    {
        var bytes = ExcelWorkbookBuilder.Build(sheets);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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

    private async Task<IReadOnlyDictionary<string, string>> GetActivityUserNamesAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var userIds = await _context.RecordComments
            .Where(x => x.OwnerType == RecordOwnerType.MaterialRequest && x.OwnerId == ownerId && x.CreatedByUserId != null)
            .Select(x => x.CreatedByUserId!)
            .Concat(_context.RecordFiles
                .Where(x => x.OwnerType == RecordOwnerType.MaterialRequest && x.OwnerId == ownerId && x.CreatedByUserId != null)
                .Select(x => x.CreatedByUserId!))
            .Distinct()
            .ToListAsync(cancellationToken);

        return await _userManager.Users
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.FullName ?? x.Email ?? x.UserName ?? x.Id, cancellationToken);
    }

    private bool CanDeleteMaterialRequestActivity()
    {
        return User.IsInRole(AppRoles.Admin) ||
               User.HasClaim(AppClaimTypes.Permission, AppPermissions.MaterialRequestsDeleteActivity);
    }

    private async Task<string?> SendMaterialRequestTelegramNotificationAsync(
        IReadOnlyCollection<Guid> requestIds,
        bool isBulk,
        CancellationToken cancellationToken)
    {
        var settings = await _context.TelegramNotificationSettings
            .AsNoTracking()
            .Include(x => x.Recipients)
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var recipientUserIds = settings?.Recipients
            .Where(x => x.Module == TelegramNotificationModule.MaterialRequest)
            .Select(x => x.UserId)
            .Distinct()
            .ToList() ?? [];

        if (recipientUserIds.Count == 0)
        {
            return "Telegram bildirimi seçildi ancak malzeme ihtiyacı için ayarlarda alıcı tanımlanmadı.";
        }

        var message = isBulk
            ? await BuildBulkMaterialRequestTelegramMessageAsync(requestIds, cancellationToken)
            : await BuildMaterialRequestTelegramMessageAsync(requestIds.First(), cancellationToken);

        var result = await _telegramNotificationService.SendMessageToUsersAsync(
            recipientUserIds,
            message,
            cancellationToken: cancellationToken);

        return BuildTelegramWarningMessage("malzeme ihtiyacı", result);
    }

    private async Task<string> BuildMaterialRequestTelegramMessageAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var request = await _context.MaterialRequests
            .Include(x => x.Project)
            .Include(x => x.Material)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken);

        if (request is null)
        {
            throw new InvalidOperationException("Malzeme ihtiyacı kaydı bulunamadığı için Telegram bildirimi hazırlanamadı.");
        }

        var requesterName = await GetRequestedByNameAsync(request.RequestedByUserId, cancellationToken) ?? "Belirtilmedi";
        var builder = new StringBuilder();
        builder.AppendLine("Yeni malzeme ihtiyacı oluşturuldu.");
        builder.AppendLine();
        AddTelegramLine(builder, "Proje", request.Project is not null ? $"{request.Project.Code} - {request.Project.Name}" : "Genel");
        AddTelegramLine(builder, "İstenen Malzeme", request.RequestedItem);
        AddTelegramLine(builder, "Tanımlı Malzeme", request.Material?.Name);
        AddTelegramLine(builder, "Miktar", request.QuantityText ?? (request.Quantity.HasValue ? request.Quantity.Value.ToString("0.###") : null));
        AddTelegramLine(builder, "Birim", request.Unit);
        AddTelegramLine(builder, "Kalite", request.Quality);
        AddTelegramLine(builder, "Gerekli Tarih", request.NeededBy.ToString("dd.MM.yyyy"));
        AddTelegramLine(builder, "Talebi Açan", requesterName);
        AddTelegramLine(builder, "Not", request.Notes);
        return builder.ToString().Trim();
    }

    private async Task<string> BuildBulkMaterialRequestTelegramMessageAsync(
        IReadOnlyCollection<Guid> requestIds,
        CancellationToken cancellationToken)
    {
        var requests = await _context.MaterialRequests
            .Include(x => x.Project)
            .Include(x => x.Material)
            .AsNoTracking()
            .Where(x => requestIds.Contains(x.Id))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        if (requests.Count == 0)
        {
            throw new InvalidOperationException("Malzeme ihtiyaçları bulunamadığı için Telegram bildirimi hazırlanamadı.");
        }

        var firstRequest = requests[0];
        var requesterName = await GetRequestedByNameAsync(firstRequest.RequestedByUserId, cancellationToken) ?? "Belirtilmedi";
        var projectName = firstRequest.Project is not null ? $"{firstRequest.Project.Code} - {firstRequest.Project.Name}" : "Genel";
        var builder = new StringBuilder();
        builder.AppendLine($"{requests.Count} adet malzeme ihtiyacı oluşturuldu.");
        builder.AppendLine();
        AddTelegramLine(builder, "Proje", projectName);
        AddTelegramLine(builder, "Talebi Açan", requesterName);
        builder.AppendLine("Özet:");

        foreach (var request in requests.Take(5))
        {
            var summary = request.QuantityText;
            if (string.IsNullOrWhiteSpace(summary) && request.Quantity.HasValue)
            {
                summary = string.IsNullOrWhiteSpace(request.Unit)
                    ? request.Quantity.Value.ToString("0.###")
                    : $"{request.Quantity.Value:0.###} {request.Unit}";
            }

            builder.Append("- ");
            builder.Append(request.RequestedItem);
            if (!string.IsNullOrWhiteSpace(summary))
            {
                builder.Append(" / ");
                builder.Append(summary);
            }

            builder.Append(" / ");
            builder.Append(request.NeededBy.ToString("dd.MM.yyyy"));
            builder.AppendLine();
        }

        if (requests.Count > 5)
        {
            builder.AppendLine($"... ve {requests.Count - 5} kayıt daha");
        }

        return builder.ToString().Trim();
    }

    private static string? BuildTelegramWarningMessage(string subject, TelegramDispatchResult result)
    {
        if (!result.IsEnabled)
        {
            return $"Telegram bildirim sistemi kapalı olduğu için {subject} bildirimi gönderilemedi.";
        }

        if (!result.IsConfigured)
        {
            return $"Telegram bot ayarı eksik olduğu için {subject} bildirimi gönderilemedi.";
        }

        var parts = new List<string>();
        if (result.MissingChatRecipients.Count > 0)
        {
            parts.Add($"Telegram hesabı bağlı olmayan kullanıcılar: {string.Join(", ", result.MissingChatRecipients)}");
        }

        if (result.FailedRecipients.Count > 0)
        {
            parts.Add($"Mesaj gönderilemeyen kullanıcılar: {string.Join(", ", result.FailedRecipients)}");
        }

        return parts.Count == 0 ? null : string.Join(" ", parts);
    }

    private static void AddTelegramLine(StringBuilder builder, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.AppendLine($"{label}: {value.Trim()}");
        }
    }
}
