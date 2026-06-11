using Business.Application.Services;
using Business.Domain.Entities;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.Extensions;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanViewPurchasing)]
public class PurchaseOrderTemplatesController : Controller
{
    private readonly IPurchaseOrderTemplateService _templateService;
    private readonly ILookupService _lookupService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PurchaseOrderTemplatesController(
        IPurchaseOrderTemplateService templateService,
        ILookupService lookupService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _templateService = templateService;
        _lookupService = lookupService;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var templates = await _templateService.GetAllAsync(cancellationToken);
        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Siparişler"] = Url.Action("Index", "PurchaseOrders"),
            ["Sipariş Şablonları"] = null
        };

        var model = templates
            .OrderBy(x => x.Name)
            .Select(x => new PurchaseOrderTemplateListItemViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                Description = x.Description,
                IsActive = x.IsActive,
                ScopeText = x.DefaultScope.ToDisplayName(),
                SupplierName = x.DefaultSupplier?.Name,
                LineCount = x.Lines.Count
            })
            .ToList();

        return View(model);
    }

    [Authorize(Policy = AppPolicies.CanManagePurchasing)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await FillSuppliersAsync(cancellationToken);
        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Sipariş Şablonları"] = Url.Action(nameof(Index)),
            ["Yeni Şablon"] = null
        };

        return View(new PurchaseOrderTemplateFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManagePurchasing)]
    public async Task<IActionResult> Create(PurchaseOrderTemplateFormViewModel model, CancellationToken cancellationToken)
    {
        if (await _context.PurchaseOrderTemplates.AnyAsync(x => x.Name == model.Name.Trim(), cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Name), "Aynı isimde bir şablon zaten var.");
        }

        if (!ModelState.IsValid)
        {
            await FillSuppliersAsync(cancellationToken);
            return View(model);
        }

        var template = new PurchaseOrderTemplate
        {
            Name = model.Name.Trim(),
            Code = Normalize(model.Code),
            Description = Normalize(model.Description),
            DefaultScope = model.DefaultScope,
            DefaultStatus = model.DefaultStatus,
            DefaultSupplierId = model.DefaultSupplierId,
            DefaultPaymentTerm = Normalize(model.DefaultPaymentTerm),
            DefaultCurrency = model.DefaultCurrency.Trim().ToUpperInvariant(),
            DefaultVatRate = model.DefaultVatRate,
            IsActive = model.IsActive
        };

        await _templateService.CreateAsync(template, cancellationToken);
        TempData["Success"] = "Sipariş şablonu oluşturuldu.";
        return RedirectToAction(nameof(Details), new { id = template.Id });
    }

    [Authorize(Policy = AppPolicies.CanManagePurchasing)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var template = await _templateService.GetByIdAsync(id, cancellationToken);
        if (template is null)
        {
            return NotFound();
        }

        await FillSuppliersAsync(cancellationToken);
        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Sipariş Şablonları"] = Url.Action(nameof(Index)),
            [template.Name] = Url.Action(nameof(Details), new { id }),
            ["Düzenle"] = null
        };

        return View(new PurchaseOrderTemplateFormViewModel
        {
            Id = template.Id,
            Name = template.Name,
            Code = template.Code,
            Description = template.Description,
            DefaultScope = template.DefaultScope,
            DefaultStatus = template.DefaultStatus,
            DefaultSupplierId = template.DefaultSupplierId,
            DefaultPaymentTerm = template.DefaultPaymentTerm,
            DefaultCurrency = template.DefaultCurrency,
            DefaultVatRate = template.DefaultVatRate,
            IsActive = template.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManagePurchasing)]
    public async Task<IActionResult> Edit(Guid id, PurchaseOrderTemplateFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.Id != id)
        {
            return BadRequest();
        }

        var template = await _templateService.GetByIdAsync(id, cancellationToken);
        if (template is null)
        {
            return NotFound();
        }

        if (await _context.PurchaseOrderTemplates.AnyAsync(x => x.Id != id && x.Name == model.Name.Trim(), cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Name), "Aynı isimde bir şablon zaten var.");
        }

        if (!ModelState.IsValid)
        {
            await FillSuppliersAsync(cancellationToken);
            return View(model);
        }

        template.Name = model.Name.Trim();
        template.Code = Normalize(model.Code);
        template.Description = Normalize(model.Description);
        template.DefaultScope = model.DefaultScope;
        template.DefaultStatus = model.DefaultStatus;
        template.DefaultSupplierId = model.DefaultSupplierId;
        template.DefaultPaymentTerm = Normalize(model.DefaultPaymentTerm);
        template.DefaultCurrency = model.DefaultCurrency.Trim().ToUpperInvariant();
        template.DefaultVatRate = model.DefaultVatRate;
        template.IsActive = model.IsActive;

        await _templateService.UpdateAsync(template, cancellationToken);
        TempData["Success"] = "Sipariş şablonu güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var model = await BuildDetailsViewModelAsync(id, new PurchaseOrderTemplateLineFormViewModel(), false, "create", cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManagePurchasing)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _templateService.DeleteAsync(id, cancellationToken);
        TempData["Success"] = "Sipariş şablonu silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManagePurchasing)]
    public async Task<IActionResult> AddLine([Bind(Prefix = "LineForm")] PurchaseOrderTemplateLineFormViewModel model, CancellationToken cancellationToken)
    {
        var template = await _templateService.GetByIdAsync(model.PurchaseOrderTemplateId, cancellationToken);
        if (template is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildDetailsViewModelAsync(model.PurchaseOrderTemplateId, model, true, "create", cancellationToken);
            return View(nameof(Details), invalidModel);
        }

        var line = new PurchaseOrderTemplateLine
        {
            PurchaseOrderTemplateId = model.PurchaseOrderTemplateId,
            SupplierId = model.SupplierId,
            MaterialId = model.MaterialId,
            Content = model.Content.Trim(),
            Quantity = model.Quantity,
            QuantityText = Normalize(model.QuantityText),
            Unit = Normalize(model.Unit),
            Quality = Normalize(model.Quality),
            ExpectedArrivalOffsetDays = model.ExpectedArrivalOffsetDays,
            UnitPrice = model.UnitPrice,
            OrderTotal = model.OrderTotal,
            Notes = Normalize(model.Notes)
        };

        await _templateService.AddLineAsync(line, cancellationToken);
        TempData["Success"] = "Şablon satırı eklendi.";
        return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderTemplateId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManagePurchasing)]
    public async Task<IActionResult> UpdateLine([Bind(Prefix = "LineForm")] PurchaseOrderTemplateLineFormViewModel model, CancellationToken cancellationToken)
    {
        if (!model.Id.HasValue)
        {
            return BadRequest();
        }

        var line = await _templateService.GetLineByIdAsync(model.PurchaseOrderTemplateId, model.Id.Value, cancellationToken);
        if (line is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildDetailsViewModelAsync(model.PurchaseOrderTemplateId, model, true, "edit", cancellationToken);
            return View(nameof(Details), invalidModel);
        }

        line.SupplierId = model.SupplierId;
        line.MaterialId = model.MaterialId;
        line.Content = model.Content.Trim();
        line.Quantity = model.Quantity;
        line.QuantityText = Normalize(model.QuantityText);
        line.Unit = Normalize(model.Unit);
        line.Quality = Normalize(model.Quality);
        line.ExpectedArrivalOffsetDays = model.ExpectedArrivalOffsetDays;
        line.UnitPrice = model.UnitPrice;
        line.OrderTotal = model.OrderTotal;
        line.Notes = Normalize(model.Notes);

        await _templateService.UpdateLineAsync(line, cancellationToken);
        TempData["Success"] = "Şablon satırı güncellendi.";
        return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderTemplateId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManagePurchasing)]
    public async Task<IActionResult> DeleteLine(Guid templateId, Guid lineId, CancellationToken cancellationToken)
    {
        await _templateService.DeleteLineAsync(templateId, lineId, cancellationToken);
        TempData["Success"] = "Şablon satırı silindi.";
        return RedirectToAction(nameof(Details), new { id = templateId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanCreatePurchasing)]
    public async Task<IActionResult> ApplyTemplate(PurchaseOrderTemplateApplyViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.TemplateId == Guid.Empty)
        {
            TempData["Error"] = "Sablon ve siparis tarihi bilgisi zorunludur.";
            return RedirectToAction(nameof(Details), new { id = model.TemplateId });
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var requestedBy = currentUser?.FullName ?? User.Identity?.Name;
        var createdCount = await _templateService.ApplyTemplateAsync(
            model.TemplateId,
            model.ProjectId,
            model.OrderDate,
            currentUser?.Id,
            requestedBy,
            cancellationToken);

        TempData["Success"] = createdCount > 0
            ? $"{createdCount} siparis sablondan olusturuldu."
            : "Sablondan siparis olusturulamadi.";

        return model.ProjectId.HasValue
            ? RedirectToAction("Index", "PurchaseOrders", new { projectId = model.ProjectId })
            : RedirectToAction("Index", "PurchaseOrders");
    }

    private async Task<PurchaseOrderTemplateDetailsViewModel?> BuildDetailsViewModelAsync(
        Guid templateId,
        PurchaseOrderTemplateLineFormViewModel lineForm,
        bool openLineForm,
        string lineFormMode,
        CancellationToken cancellationToken)
    {
        var template = await _templateService.GetTemplateWithLinesAsync(templateId, cancellationToken);
        if (template is null)
        {
            return null;
        }

        if (lineForm.PurchaseOrderTemplateId == Guid.Empty)
        {
            lineForm.PurchaseOrderTemplateId = template.Id;
        }

        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Sipariş Şablonları"] = Url.Action(nameof(Index)),
            [template.Name] = null
        };

        var materials = await _lookupService.GetMaterialsAsync(cancellationToken);
        var suppliers = await _lookupService.GetSuppliersAsync(cancellationToken);
        var projects = await _lookupService.GetProjectsAsync(cancellationToken);
        return new PurchaseOrderTemplateDetailsViewModel
        {
            Id = template.Id,
            Name = template.Name,
            Code = template.Code,
            Description = template.Description,
            IsActive = template.IsActive,
            ScopeText = template.DefaultScope.ToDisplayName(),
            StatusText = template.DefaultStatus.ToDisplayName(),
            SupplierName = template.DefaultSupplier?.Name,
            DefaultPaymentTerm = template.DefaultPaymentTerm,
            DefaultCurrency = template.DefaultCurrency,
            DefaultVatRate = template.DefaultVatRate,
            Lines = template.Lines
                .OrderBy(x => x.SortOrder)
                .Select(x => new PurchaseOrderTemplateLineItemViewModel
                {
                    Id = x.Id,
                    SortOrder = x.SortOrder,
                    Content = x.Content,
                    SupplierId = x.SupplierId,
                    SupplierName = x.Supplier?.Name,
                    MaterialId = x.MaterialId,
                    MaterialName = x.Material?.Name,
                    Quantity = x.Quantity,
                    QuantityText = x.QuantityText,
                    Unit = x.Unit,
                    Quality = x.Quality,
                    ExpectedArrivalOffsetDays = x.ExpectedArrivalOffsetDays,
                    UnitPrice = x.UnitPrice,
                    OrderTotal = x.OrderTotal,
                    Notes = x.Notes
                })
                .ToList(),
            LineForm = lineForm,
            Materials = materials
                .OrderBy(x => x.Name)
                .Select(x => new ProjectTemplateLookupItemViewModel
                {
                    Value = x.Id.ToString(),
                    Text = x.Name
                })
                .ToList(),
            Suppliers = suppliers
                .OrderBy(x => x.Name)
                .Select(x => new ProjectTemplateLookupItemViewModel
                {
                    Value = x.Id.ToString(),
                    Text = x.Name
                })
                .ToList(),
            Projects = projects
                .Where(x => x.Status != Domain.Enums.ProjectStatus.Cancelled)
                .OrderBy(x => x.Code)
                .Select(x => new ProjectPlanningProjectOptionViewModel
                {
                    Id = x.Id,
                    Text = $"{x.Code} - {x.Name}"
                })
                .ToList(),
            ApplyForm = new PurchaseOrderTemplateApplyViewModel
            {
                TemplateId = template.Id,
                OrderDate = DateTime.Today
            },
            OpenLineForm = openLineForm,
            LineFormMode = lineFormMode
        };
    }

    private async Task FillSuppliersAsync(CancellationToken cancellationToken)
    {
        ViewBag.Suppliers = await _lookupService.GetSuppliersAsync(cancellationToken);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
