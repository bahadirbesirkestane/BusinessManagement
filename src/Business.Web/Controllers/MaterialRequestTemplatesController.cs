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

[Authorize(Policy = AppPolicies.CanViewMaterialRequests)]
public class MaterialRequestTemplatesController : Controller
{
    private readonly IMaterialRequestTemplateService _templateService;
    private readonly ILookupService _lookupService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IProjectTimelineService _projectTimelineService;

    public MaterialRequestTemplatesController(
        IMaterialRequestTemplateService templateService,
        ILookupService lookupService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IProjectTimelineService projectTimelineService)
    {
        _templateService = templateService;
        _lookupService = lookupService;
        _context = context;
        _userManager = userManager;
        _projectTimelineService = projectTimelineService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var templates = await _templateService.GetAllAsync(cancellationToken);
        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Malzeme İhtiyaçları"] = Url.Action("Index", "MaterialRequests"),
            ["İhtiyaç Şablonları"] = null
        };

        var model = templates
            .OrderBy(x => x.Name)
            .Select(x => new MaterialRequestTemplateListItemViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                Description = x.Description,
                IsActive = x.IsActive,
                StatusText = x.DefaultStatus.ToDisplayName(),
                LineCount = x.Lines.Count
            })
            .ToList();

        return View(model);
    }

    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public IActionResult Create()
    {
        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["İhtiyaç Şablonları"] = Url.Action(nameof(Index)),
            ["Yeni Şablon"] = null
        };

        return View(new MaterialRequestTemplateFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> Create(MaterialRequestTemplateFormViewModel model, CancellationToken cancellationToken)
    {
        if (await _context.MaterialRequestTemplates.AnyAsync(x => x.Name == model.Name.Trim(), cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Name), "Aynı isimde bir şablon zaten var.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var template = new MaterialRequestTemplate
        {
            Name = model.Name.Trim(),
            Code = Normalize(model.Code),
            Description = Normalize(model.Description),
            DefaultStatus = model.DefaultStatus,
            IsActive = model.IsActive
        };

        await _templateService.CreateAsync(template, cancellationToken);
        TempData["Success"] = "İhtiyaç şablonu oluşturuldu.";
        return RedirectToAction(nameof(Details), new { id = template.Id });
    }

    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var template = await _templateService.GetByIdAsync(id, cancellationToken);
        if (template is null)
        {
            return NotFound();
        }

        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["İhtiyaç Şablonları"] = Url.Action(nameof(Index)),
            [template.Name] = Url.Action(nameof(Details), new { id }),
            ["Düzenle"] = null
        };

        return View(new MaterialRequestTemplateFormViewModel
        {
            Id = template.Id,
            Name = template.Name,
            Code = template.Code,
            Description = template.Description,
            DefaultStatus = template.DefaultStatus,
            IsActive = template.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> Edit(Guid id, MaterialRequestTemplateFormViewModel model, CancellationToken cancellationToken)
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

        if (await _context.MaterialRequestTemplates.AnyAsync(x => x.Id != id && x.Name == model.Name.Trim(), cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Name), "Aynı isimde bir şablon zaten var.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        template.Name = model.Name.Trim();
        template.Code = Normalize(model.Code);
        template.Description = Normalize(model.Description);
        template.DefaultStatus = model.DefaultStatus;
        template.IsActive = model.IsActive;

        await _templateService.UpdateAsync(template, cancellationToken);
        TempData["Success"] = "İhtiyaç şablonu güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var model = await BuildDetailsViewModelAsync(id, new MaterialRequestTemplateLineFormViewModel(), false, "create", cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _templateService.DeleteAsync(id, cancellationToken);
        TempData["Success"] = "İhtiyaç şablonu silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> AddLine([Bind(Prefix = "LineForm")] MaterialRequestTemplateLineFormViewModel model, CancellationToken cancellationToken)
    {
        var template = await _templateService.GetByIdAsync(model.MaterialRequestTemplateId, cancellationToken);
        if (template is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildDetailsViewModelAsync(model.MaterialRequestTemplateId, model, true, "create", cancellationToken);
            return View(nameof(Details), invalidModel);
        }

        var line = new MaterialRequestTemplateLine
        {
            MaterialRequestTemplateId = model.MaterialRequestTemplateId,
            MaterialId = model.MaterialId,
            RequestedItem = model.RequestedItem.Trim(),
            Quantity = model.Quantity,
            QuantityText = Normalize(model.QuantityText),
            Unit = Normalize(model.Unit),
            Quality = Normalize(model.Quality),
            NeededByOffsetDays = model.NeededByOffsetDays,
            Notes = Normalize(model.Notes)
        };

        await _templateService.AddLineAsync(line, cancellationToken);
        TempData["Success"] = "Şablon satırı eklendi.";
        return RedirectToAction(nameof(Details), new { id = model.MaterialRequestTemplateId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> UpdateLine([Bind(Prefix = "LineForm")] MaterialRequestTemplateLineFormViewModel model, CancellationToken cancellationToken)
    {
        if (!model.Id.HasValue)
        {
            return BadRequest();
        }

        var line = await _templateService.GetLineByIdAsync(model.MaterialRequestTemplateId, model.Id.Value, cancellationToken);
        if (line is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildDetailsViewModelAsync(model.MaterialRequestTemplateId, model, true, "edit", cancellationToken);
            return View(nameof(Details), invalidModel);
        }

        line.MaterialId = model.MaterialId;
        line.RequestedItem = model.RequestedItem.Trim();
        line.Quantity = model.Quantity;
        line.QuantityText = Normalize(model.QuantityText);
        line.Unit = Normalize(model.Unit);
        line.Quality = Normalize(model.Quality);
        line.NeededByOffsetDays = model.NeededByOffsetDays;
        line.Notes = Normalize(model.Notes);

        await _templateService.UpdateLineAsync(line, cancellationToken);
        TempData["Success"] = "Şablon satırı güncellendi.";
        return RedirectToAction(nameof(Details), new { id = model.MaterialRequestTemplateId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> DeleteLine(Guid templateId, Guid lineId, CancellationToken cancellationToken)
    {
        await _templateService.DeleteLineAsync(templateId, lineId, cancellationToken);
        TempData["Success"] = "Şablon satırı silindi.";
        return RedirectToAction(nameof(Details), new { id = templateId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanRequestMaterials)]
    public async Task<IActionResult> ApplyTemplate(MaterialRequestTemplateApplyViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.TemplateId == Guid.Empty)
        {
            TempData["Error"] = "Şablon ve gerekli tarih bilgisi zorunludur.";
            return RedirectToAction(nameof(Details), new { id = model.TemplateId });
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var createdCount = await _templateService.ApplyTemplateAsync(
            model.TemplateId,
            model.ProjectId,
            model.NeededByDate,
            currentUser?.Id,
            cancellationToken);

        if (createdCount > 0 && model.ProjectId.HasValue)
        {
            await _projectTimelineService.AddAsync(
                model.ProjectId.Value,
                "Şablondan ihtiyaç kayıtları oluşturuldu",
                $"{createdCount} kayıt eklendi",
                cancellationToken);
        }

        TempData["Success"] = createdCount > 0
            ? $"{createdCount} ihtiyaç kaydı şablondan oluşturuldu."
            : "Şablondan ihtiyaç kaydı oluşturulamadı.";

        return model.ProjectId.HasValue
            ? RedirectToAction("Index", "MaterialRequests", new { projectId = model.ProjectId })
            : RedirectToAction("Index", "MaterialRequests");
    }

    private async Task<MaterialRequestTemplateDetailsViewModel?> BuildDetailsViewModelAsync(
        Guid templateId,
        MaterialRequestTemplateLineFormViewModel lineForm,
        bool openLineForm,
        string lineFormMode,
        CancellationToken cancellationToken)
    {
        var template = await _templateService.GetTemplateWithLinesAsync(templateId, cancellationToken);
        if (template is null)
        {
            return null;
        }

        if (lineForm.MaterialRequestTemplateId == Guid.Empty)
        {
            lineForm.MaterialRequestTemplateId = template.Id;
        }

        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["İhtiyaç Şablonları"] = Url.Action(nameof(Index)),
            [template.Name] = null
        };

        var materials = await _lookupService.GetMaterialsAsync(cancellationToken);
        var projects = await _lookupService.GetProjectsAsync(cancellationToken);
        return new MaterialRequestTemplateDetailsViewModel
        {
            Id = template.Id,
            Name = template.Name,
            Code = template.Code,
            Description = template.Description,
            IsActive = template.IsActive,
            StatusText = template.DefaultStatus.ToDisplayName(),
            Lines = template.Lines
                .OrderBy(x => x.SortOrder)
                .Select(x => new MaterialRequestTemplateLineItemViewModel
                {
                    Id = x.Id,
                    SortOrder = x.SortOrder,
                    MaterialId = x.MaterialId,
                    MaterialName = x.Material?.Name,
                    RequestedItem = x.RequestedItem,
                    Quantity = x.Quantity,
                    QuantityText = x.QuantityText,
                    Unit = x.Unit,
                    Quality = x.Quality,
                    NeededByOffsetDays = x.NeededByOffsetDays,
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
            Projects = projects
                .Where(x => x.Status != Domain.Enums.ProjectStatus.Cancelled)
                .OrderBy(x => x.Code)
                .Select(x => new ProjectPlanningProjectOptionViewModel
                {
                    Id = x.Id,
                    Text = $"{x.Code} - {x.Name}"
                })
                .ToList(),
            ApplyForm = new MaterialRequestTemplateApplyViewModel
            {
                TemplateId = template.Id,
                NeededByDate = DateTime.Today
            },
            OpenLineForm = openLineForm,
            LineFormMode = lineFormMode
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
