using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Web.Extensions;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Business.Infrastructure.Data;

namespace Business.Web.Controllers;

[Authorize]
public class PersonalTasksController : Controller
{
    private readonly IPersonalTaskService _personalTaskService;
    private readonly ILookupService _lookupService;
    private readonly ApplicationDbContext _context;

    public PersonalTasksController(
        IPersonalTaskService personalTaskService,
        ILookupService lookupService,
        ApplicationDbContext context)
    {
        _personalTaskService = personalTaskService;
        _lookupService = lookupService;
        _context = context;
    }

    public async Task<IActionResult> Index(string? filter, Guid? customerId, Guid? projectId, Guid? projectTaskId, CancellationToken cancellationToken)
    {
        var tasks = await _personalTaskService.GetAllAsync(cancellationToken);
        var today = DateTime.Today;

        tasks = filter switch
        {
            "completed" => tasks.Where(x => x.Status == PersonalTaskStatus.Done).ToList(),
            "overdue" => tasks.Where(x => x.Status != PersonalTaskStatus.Done && x.DueDate.HasValue && x.DueDate.Value.Date < today).ToList(),
            _ => tasks.Where(x => x.Status != PersonalTaskStatus.Done).ToList()
        };

        if (customerId.HasValue)
        {
            tasks = tasks.Where(x => x.CustomerId == customerId.Value).ToList();
        }

        if (projectId.HasValue)
        {
            tasks = tasks.Where(x => x.ProjectId == projectId.Value).ToList();
        }

        if (projectTaskId.HasValue)
        {
            tasks = tasks.Where(x => x.ProjectTaskId == projectTaskId.Value).ToList();
        }

        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Kişisel Alan"] = null,
            ["Kişisel Görevler"] = null
        };

        var model = new PersonalTaskIndexViewModel
        {
            Filter = string.IsNullOrWhiteSpace(filter) ? "open" : filter,
            CustomerId = customerId,
            ProjectId = projectId,
            ProjectTaskId = projectTaskId,
            Tasks = tasks
                .OrderBy(x => x.DueDate ?? DateTime.MaxValue)
                .ThenByDescending(x => x.CreatedAt)
                .Select(x => new PersonalTaskListItemViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    Status = x.Status,
                    StatusText = x.Status.ToDisplayName(),
                    Priority = x.Priority,
                    PriorityText = x.Priority.ToDisplayName(),
                    CustomerName = x.Customer?.Name,
                    ProjectName = x.Project is not null ? $"{x.Project.Code} - {x.Project.Name}" : null,
                    ProjectTaskTitle = x.ProjectTask?.Title,
                    DueDate = x.DueDate,
                    CreatedAt = x.CreatedAt,
                    IsOverdue = x.Status != PersonalTaskStatus.Done && x.DueDate.HasValue && x.DueDate.Value.Date < today
                })
                .ToList(),
            Customers = await GetCustomerOptionsAsync(cancellationToken),
            Projects = await GetProjectOptionsAsync(cancellationToken),
            ProjectTasks = await GetProjectTaskOptionsAsync(cancellationToken)
        };

        return View(model);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var task = await _personalTaskService.GetDetailsAsync(id, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Kişisel Görevler"] = Url.Action(nameof(Index)),
            [task.Title] = null
        };

        var today = DateTime.Today;
        return View(new PersonalTaskDetailsViewModel
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            StatusText = task.Status.ToDisplayName(),
            Priority = task.Priority,
            PriorityText = task.Priority.ToDisplayName(),
            CustomerName = task.Customer?.Name,
            ProjectName = task.Project is not null ? $"{task.Project.Code} - {task.Project.Name}" : null,
            ProjectTaskTitle = task.ProjectTask?.Title,
            StartDate = task.StartDate,
            DueDate = task.DueDate,
            CompletedAt = task.CompletedAt,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            Notes = task.Notes,
            IsOverdue = task.Status != PersonalTaskStatus.Done && task.DueDate.HasValue && task.DueDate.Value.Date < today
        });
    }

    public async Task<IActionResult> Create(Guid? customerId, Guid? projectId, Guid? projectTaskId, CancellationToken cancellationToken)
    {
        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Kişisel Görevler"] = Url.Action(nameof(Index)),
            ["Yeni Görev"] = null
        };

        return View(await BuildFormModelAsync(new PersonalTaskFormViewModel
        {
            CustomerId = customerId,
            ProjectId = projectId,
            ProjectTaskId = projectTaskId
        }, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PersonalTaskFormViewModel model, CancellationToken cancellationToken)
    {
        NormalizeCompletionFields(model);

        if (!ModelState.IsValid)
        {
            return View(await BuildFormModelAsync(model, cancellationToken));
        }

        await _personalTaskService.CreateAsync(MapToEntity(model), cancellationToken);
        TempData["Success"] = "Kişisel görev kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var task = await _personalTaskService.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Kişisel Görevler"] = Url.Action(nameof(Index)),
            [task.Title] = Url.Action(nameof(Details), new { id }),
            ["Düzenle"] = null
        };

        return View(await BuildFormModelAsync(new PersonalTaskFormViewModel
        {
            Id = task.Id,
            CustomerId = task.CustomerId,
            ProjectId = task.ProjectId,
            ProjectTaskId = task.ProjectTaskId,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            StartDate = task.StartDate,
            DueDate = task.DueDate,
            CompletedAt = task.CompletedAt,
            Notes = task.Notes
        }, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PersonalTaskFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var existing = await _personalTaskService.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        NormalizeCompletionFields(model);

        if (!ModelState.IsValid)
        {
            return View(await BuildFormModelAsync(model, cancellationToken));
        }

        await _personalTaskService.UpdateAsync(MapToEntity(model), cancellationToken);
        TempData["Success"] = "Kişisel görev güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, PersonalTaskStatus status, string? returnUrl, CancellationToken cancellationToken)
    {
        var task = await _personalTaskService.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        task.Status = status;
        task.CompletedAt = status == PersonalTaskStatus.Done ? DateTime.Today : null;

        await _personalTaskService.UpdateAsync(task, cancellationToken);
        TempData["Success"] = "Kişisel görev durumu güncellendi.";
        return RedirectToLocal(returnUrl, id);
    }

    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var task = await _personalTaskService.GetDetailsAsync(id, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        var today = DateTime.Today;
        return View(new PersonalTaskDetailsViewModel
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            StatusText = task.Status.ToDisplayName(),
            Priority = task.Priority,
            PriorityText = task.Priority.ToDisplayName(),
            CustomerName = task.Customer?.Name,
            ProjectName = task.Project is not null ? $"{task.Project.Code} - {task.Project.Name}" : null,
            ProjectTaskTitle = task.ProjectTask?.Title,
            StartDate = task.StartDate,
            DueDate = task.DueDate,
            CompletedAt = task.CompletedAt,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            Notes = task.Notes,
            IsOverdue = task.Status != PersonalTaskStatus.Done && task.DueDate.HasValue && task.DueDate.Value.Date < today
        });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        await _personalTaskService.DeleteAsync(id, cancellationToken);
        TempData["Success"] = "Kişisel görev silindi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<PersonalTaskFormViewModel> BuildFormModelAsync(PersonalTaskFormViewModel model, CancellationToken cancellationToken)
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
                Text = x.Project != null ? $"{x.Project.Code} - {x.Title}" : x.Title
            })
            .ToListAsync(cancellationToken);
    }

    private static PersonalTask MapToEntity(PersonalTaskFormViewModel model)
    {
        return new PersonalTask
        {
            Id = model.Id,
            CustomerId = model.CustomerId,
            ProjectId = model.ProjectId,
            ProjectTaskId = model.ProjectTaskId,
            Title = model.Title.Trim(),
            Description = NormalizeText(model.Description),
            Status = model.Status,
            Priority = model.Priority,
            StartDate = model.StartDate,
            DueDate = model.DueDate,
            CompletedAt = model.CompletedAt,
            Notes = NormalizeText(model.Notes)
        };
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void NormalizeCompletionFields(PersonalTaskFormViewModel model)
    {
        if (model.Status != PersonalTaskStatus.Done)
        {
            model.CompletedAt = null;
        }
        else if (!model.CompletedAt.HasValue)
        {
            model.CompletedAt = DateTime.Today;
        }
    }

    private IActionResult RedirectToLocal(string? returnUrl, Guid id)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Details), new { id })!;
    }
}
