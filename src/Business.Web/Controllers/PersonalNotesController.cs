using Business.Application.Services;
using Business.Domain.Entities;
using Business.Web.Extensions;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Business.Infrastructure.Data;

namespace Business.Web.Controllers;

[Authorize]
public class PersonalNotesController : Controller
{
    private readonly IPersonalNoteService _personalNoteService;
    private readonly ILookupService _lookupService;
    private readonly ApplicationDbContext _context;

    public PersonalNotesController(
        IPersonalNoteService personalNoteService,
        ILookupService lookupService,
        ApplicationDbContext context)
    {
        _personalNoteService = personalNoteService;
        _lookupService = lookupService;
        _context = context;
    }

    public async Task<IActionResult> Index(string? q, Guid? customerId, Guid? projectId, Guid? projectTaskId, CancellationToken cancellationToken)
    {
        var notes = await _personalNoteService.GetAllAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            notes = notes
                .Where(x =>
                    x.Title.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                    x.Content.Contains(term, StringComparison.CurrentCultureIgnoreCase))
                .ToList();
        }

        if (customerId.HasValue)
        {
            notes = notes.Where(x => x.CustomerId == customerId.Value).ToList();
        }

        if (projectId.HasValue)
        {
            notes = notes.Where(x => x.ProjectId == projectId.Value).ToList();
        }

        if (projectTaskId.HasValue)
        {
            notes = notes.Where(x => x.ProjectTaskId == projectTaskId.Value).ToList();
        }

        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Kişisel Alan"] = null,
            ["Kişisel Notlar"] = null
        };

        return View(new PersonalNoteIndexViewModel
        {
            TitleFilter = q,
            CustomerId = customerId,
            ProjectId = projectId,
            ProjectTaskId = projectTaskId,
            Notes = notes
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new PersonalNoteListItemViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    Content = x.Content,
                    Category = x.Category,
                    CategoryText = x.Category.ToDisplayName(),
                    CustomerName = x.Customer?.Name,
                    ProjectName = x.Project is not null ? $"{x.Project.Code} - {x.Project.Name}" : null,
                    ProjectTaskTitle = x.ProjectTask?.Title,
                    CreatedAt = x.CreatedAt,
                    ReminderAt = x.ReminderAt
                })
                .ToList(),
            Customers = await GetCustomerOptionsAsync(cancellationToken),
            Projects = await GetProjectOptionsAsync(cancellationToken),
            ProjectTasks = await GetProjectTaskOptionsAsync(cancellationToken)
        });
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var note = await _personalNoteService.GetDetailsAsync(id, cancellationToken);
        if (note is null)
        {
            return NotFound();
        }

        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Kişisel Notlar"] = Url.Action(nameof(Index)),
            [note.Title] = null
        };

        return View(new PersonalNoteDetailsViewModel
        {
            Id = note.Id,
            Title = note.Title,
            Content = note.Content,
            Category = note.Category,
            CategoryText = note.Category.ToDisplayName(),
            CustomerName = note.Customer?.Name,
            ProjectName = note.Project is not null ? $"{note.Project.Code} - {note.Project.Name}" : null,
            ProjectTaskTitle = note.ProjectTask?.Title,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt,
            ReminderAt = note.ReminderAt
        });
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Kişisel Notlar"] = Url.Action(nameof(Index)),
            ["Yeni Not"] = null
        };

        return View(await BuildFormModelAsync(new PersonalNoteFormViewModel(), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PersonalNoteFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildFormModelAsync(model, cancellationToken));
        }

        await _personalNoteService.CreateAsync(new PersonalNote
        {
            CustomerId = model.CustomerId,
            ProjectId = model.ProjectId,
            ProjectTaskId = model.ProjectTaskId,
            Category = model.Category,
            Title = model.Title.Trim(),
            Content = model.Content.Trim(),
            ReminderAt = model.ReminderAt
        }, cancellationToken);

        TempData["Success"] = "Kişisel not kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var note = await _personalNoteService.GetByIdAsync(id, cancellationToken);
        if (note is null)
        {
            return NotFound();
        }

        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Kişisel Notlar"] = Url.Action(nameof(Index)),
            [note.Title] = Url.Action(nameof(Details), new { id }),
            ["Düzenle"] = null
        };

        return View(await BuildFormModelAsync(new PersonalNoteFormViewModel
        {
            Id = note.Id,
            CustomerId = note.CustomerId,
            ProjectId = note.ProjectId,
            ProjectTaskId = note.ProjectTaskId,
            Category = note.Category,
            Title = note.Title,
            Content = note.Content,
            ReminderAt = note.ReminderAt
        }, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PersonalNoteFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var existing = await _personalNoteService.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildFormModelAsync(model, cancellationToken));
        }

        await _personalNoteService.UpdateAsync(new PersonalNote
        {
            Id = id,
            CustomerId = model.CustomerId,
            ProjectId = model.ProjectId,
            ProjectTaskId = model.ProjectTaskId,
            Category = model.Category,
            Title = model.Title.Trim(),
            Content = model.Content.Trim(),
            ReminderAt = model.ReminderAt
        }, cancellationToken);

        TempData["Success"] = "Kişisel not güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var note = await _personalNoteService.GetDetailsAsync(id, cancellationToken);
        if (note is null)
        {
            return NotFound();
        }

        return View(new PersonalNoteDetailsViewModel
        {
            Id = note.Id,
            Title = note.Title,
            Content = note.Content,
            Category = note.Category,
            CategoryText = note.Category.ToDisplayName(),
            CustomerName = note.Customer?.Name,
            ProjectName = note.Project is not null ? $"{note.Project.Code} - {note.Project.Name}" : null,
            ProjectTaskTitle = note.ProjectTask?.Title,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt,
            ReminderAt = note.ReminderAt
        });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        await _personalNoteService.DeleteAsync(id, cancellationToken);
        TempData["Success"] = "Kişisel not silindi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<PersonalNoteFormViewModel> BuildFormModelAsync(PersonalNoteFormViewModel model, CancellationToken cancellationToken)
    {
        model.Customers = await GetCustomerOptionsAsync(cancellationToken);
        model.Projects = await GetProjectOptionsAsync(cancellationToken);
        model.ProjectTasks = await GetProjectTaskOptionsAsync(cancellationToken);
        return model;
    }

    private async Task<IReadOnlyList<PersonalNoteLookupItemViewModel>> GetCustomerOptionsAsync(CancellationToken cancellationToken)
    {
        return (await _lookupService.GetCustomersAsync(cancellationToken))
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new PersonalNoteLookupItemViewModel
            {
                Value = x.Id.ToString(),
                Text = x.Name
            })
            .ToList();
    }

    private async Task<IReadOnlyList<PersonalNoteLookupItemViewModel>> GetProjectOptionsAsync(CancellationToken cancellationToken)
    {
        return (await _lookupService.GetProjectsAsync(cancellationToken))
            .OrderBy(x => x.Code)
            .Select(x => new PersonalNoteLookupItemViewModel
            {
                Value = x.Id.ToString(),
                Text = $"{x.Code} - {x.Name}"
            })
            .ToList();
    }

    private async Task<IReadOnlyList<PersonalNoteLookupItemViewModel>> GetProjectTaskOptionsAsync(CancellationToken cancellationToken)
    {
        return await _context.ProjectTasks
            .AsNoTracking()
            .Include(x => x.Project)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PersonalNoteLookupItemViewModel
            {
                Value = x.Id.ToString(),
                Text = x.Project != null
                    ? $"{x.Project.Code} - {x.Title}"
                    : x.Title
            })
            .ToListAsync(cancellationToken);
    }
}
