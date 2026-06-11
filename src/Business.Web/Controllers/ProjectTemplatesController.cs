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

[Authorize(Policy = AppPolicies.CanViewProjects)]
public class ProjectTemplatesController : Controller
{
    private readonly IProjectTemplateService _projectTemplateService;
    private readonly ILookupService _lookupService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProjectTemplatesController(
        IProjectTemplateService projectTemplateService,
        ILookupService lookupService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _projectTemplateService = projectTemplateService;
        _lookupService = lookupService;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var templates = await _projectTemplateService.GetAllAsync(cancellationToken);
        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Gorevler"] = null,
            ["Görev Şablonları"] = null
        };

        var model = templates
            .OrderBy(x => x.Name)
            .Select(x => new ProjectTemplateListItemViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                Description = x.Description,
                IsActive = x.IsActive,
                TaskCount = x.Tasks.Count
            })
            .ToList();

        return View(model);
    }

    [Authorize(Policy = AppPolicies.CanManageProjects)]
    public IActionResult Create()
    {
        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Görev Şablonları"] = Url.Action(nameof(Index)),
            ["Yeni Şablon"] = null
        };

        return View(new ProjectTemplateFormViewModel { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageProjects)]
    public async Task<IActionResult> Create(ProjectTemplateFormViewModel model, CancellationToken cancellationToken)
    {
        if (await _context.ProjectTemplates.AnyAsync(x => x.Name == model.Name.Trim(), cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Name), "Aynı isimde bir şablon zaten var.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var template = new ProjectTemplate
        {
            Name = model.Name.Trim(),
            Code = NormalizeText(model.Code),
            Description = NormalizeText(model.Description),
            IsActive = model.IsActive
        };

        await _projectTemplateService.CreateAsync(template, cancellationToken);
        TempData["Success"] = "Görev şablonu oluşturuldu.";
        return RedirectToAction(nameof(Details), new { id = template.Id });
    }

    [Authorize(Policy = AppPolicies.CanManageProjects)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var template = await _projectTemplateService.GetByIdAsync(id, cancellationToken);
        if (template is null)
        {
            return NotFound();
        }

        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Görev Şablonları"] = Url.Action(nameof(Index)),
            [template.Name] = Url.Action(nameof(Details), new { id }),
            ["Düzenle"] = null
        };

        return View(new ProjectTemplateFormViewModel
        {
            Id = template.Id,
            Name = template.Name,
            Code = template.Code,
            Description = template.Description,
            IsActive = template.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageProjects)]
    public async Task<IActionResult> Edit(Guid id, ProjectTemplateFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.Id != id)
        {
            return BadRequest();
        }

        var template = await _projectTemplateService.GetByIdAsync(id, cancellationToken);
        if (template is null)
        {
            return NotFound();
        }

        if (await _context.ProjectTemplates.AnyAsync(x => x.Id != id && x.Name == model.Name.Trim(), cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Name), "Aynı isimde bir şablon zaten var.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        template.Name = model.Name.Trim();
        template.Code = NormalizeText(model.Code);
        template.Description = NormalizeText(model.Description);
        template.IsActive = model.IsActive;

        await _projectTemplateService.UpdateAsync(template, cancellationToken);
        TempData["Success"] = "Görev şablonu güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var model = await BuildDetailsViewModelAsync(id, new ProjectTemplateTaskFormViewModel(), false, "create", cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageProjects)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _projectTemplateService.DeleteAsync(id, cancellationToken);
        TempData["Success"] = "Görev şablonu silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageProjects)]
    public async Task<IActionResult> AddTask([Bind(Prefix = "TaskForm")] ProjectTemplateTaskFormViewModel taskForm, CancellationToken cancellationToken)
    {
        var template = await _projectTemplateService.GetByIdAsync(taskForm.ProjectTemplateId, cancellationToken);
        if (template is null)
        {
            return NotFound();
        }

        if (taskForm.ParentTemplateTaskId.HasValue)
        {
            var parentTask = await _projectTemplateService.GetTemplateTaskByIdAsync(taskForm.ProjectTemplateId, taskForm.ParentTemplateTaskId.Value, cancellationToken);
            if (parentTask is null)
            {
                ModelState.AddModelError(nameof(taskForm.ParentTemplateTaskId), "Üst şablon görevi bulunamadı.");
            }
        }

        if (!await IsValidTaskReferencesAsync(taskForm, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "Seçilen kategori veya kullanıcı bilgisi geçersiz.");
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildDetailsViewModelAsync(taskForm.ProjectTemplateId, taskForm, true, "create", cancellationToken);
            return View(nameof(Details), invalidModel);
        }

        var task = new ProjectTemplateTask
        {
            ProjectTemplateId = taskForm.ProjectTemplateId,
            ParentTemplateTaskId = taskForm.ParentTemplateTaskId,
            Title = taskForm.Title.Trim(),
            Description = NormalizeText(taskForm.Description),
            TaskCategoryId = taskForm.TaskCategoryId,
            DefaultDurationDays = taskForm.DefaultDurationDays,
            DefaultStartOffsetDays = taskForm.DefaultStartOffsetDays,
            DefaultPriority = taskForm.DefaultPriority,
            DefaultAssignedUserId = NormalizeText(taskForm.DefaultAssignedUserId),
            DefaultResponsibleUserId = NormalizeText(taskForm.DefaultResponsibleUserId),
            IsMilestone = taskForm.IsMilestone
        };

        await _projectTemplateService.AddTaskAsync(task, cancellationToken);
        TempData["Success"] = "Şablon görevi eklendi.";
        return RedirectToAction(nameof(Details), new { id = taskForm.ProjectTemplateId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageProjects)]
    public async Task<IActionResult> UpdateTask([Bind(Prefix = "TaskForm")] ProjectTemplateTaskFormViewModel taskForm, CancellationToken cancellationToken)
    {
        if (!taskForm.Id.HasValue)
        {
            return BadRequest();
        }

        var task = await _projectTemplateService.GetTemplateTaskByIdAsync(taskForm.ProjectTemplateId, taskForm.Id.Value, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        if (!await IsValidTaskReferencesAsync(taskForm, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "Seçilen kategori veya kullanıcı bilgisi geçersiz.");
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildDetailsViewModelAsync(taskForm.ProjectTemplateId, taskForm, true, "edit", cancellationToken);
            return View(nameof(Details), invalidModel);
        }

        task.Title = taskForm.Title.Trim();
        task.Description = NormalizeText(taskForm.Description);
        task.TaskCategoryId = taskForm.TaskCategoryId;
        task.DefaultDurationDays = taskForm.DefaultDurationDays;
        task.DefaultStartOffsetDays = taskForm.DefaultStartOffsetDays;
        task.DefaultPriority = taskForm.DefaultPriority;
        task.DefaultAssignedUserId = NormalizeText(taskForm.DefaultAssignedUserId);
        task.DefaultResponsibleUserId = NormalizeText(taskForm.DefaultResponsibleUserId);
        task.IsMilestone = taskForm.IsMilestone;

        await _projectTemplateService.UpdateTaskAsync(task, cancellationToken);
        TempData["Success"] = "Şablon görevi güncellendi.";
        return RedirectToAction(nameof(Details), new { id = taskForm.ProjectTemplateId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageProjects)]
    public async Task<IActionResult> DeleteTask(Guid templateId, Guid taskId, CancellationToken cancellationToken)
    {
        await _projectTemplateService.DeleteTaskAsync(templateId, taskId, cancellationToken);
        TempData["Success"] = "Şablon görevi silindi.";
        return RedirectToAction(nameof(Details), new { id = templateId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyToProject(ProjectTemplateApplyViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.TemplateId == Guid.Empty || model.ProjectId == Guid.Empty)
        {
            TempData["Error"] = "Şablonu uygulamak için şablon ve proje seçimi zorunludur.";
            return RedirectToTemplateReturn(model);
        }

        var createdCount = await _projectTemplateService.ApplyTemplateToProjectAsync(model.TemplateId, model.ProjectId, model.BaseStartDate, cancellationToken);
        TempData["Success"] = createdCount > 0
            ? $"{createdCount} görev şablondan projeye eklendi."
            : "Şablondan görev oluşturulamadı.";

        return RedirectToTemplateReturn(model);
    }

    private async Task<ProjectTemplateDetailsViewModel?> BuildDetailsViewModelAsync(
        Guid templateId,
        ProjectTemplateTaskFormViewModel taskForm,
        bool openTaskForm,
        string taskFormMode,
        CancellationToken cancellationToken)
    {
        var template = await _projectTemplateService.GetTemplateWithTasksAsync(templateId, cancellationToken);
        if (template is null)
        {
            return null;
        }

        var users = await _userManager.Users
            .Where(x => x.IsActive)
            .OrderBy(x => x.FullName)
            .Select(x => new ProjectTemplateLookupItemViewModel
            {
                Value = x.Id,
                Text = x.FullName ?? x.Email ?? x.UserName ?? x.Id
            })
            .ToListAsync(cancellationToken);

        var userMap = users.ToDictionary(x => x.Value, x => x.Text);
        var categories = await _context.TaskCategories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new ProjectTemplateLookupItemViewModel
            {
                Value = x.Id.ToString(),
                Text = x.Name
            })
            .ToListAsync(cancellationToken);

        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Görev Şablonları"] = Url.Action(nameof(Index)),
            [template.Name] = null
        };

        if (taskForm.ProjectTemplateId == Guid.Empty)
        {
            taskForm.ProjectTemplateId = template.Id;
        }

        return new ProjectTemplateDetailsViewModel
        {
            Id = template.Id,
            Name = template.Name,
            Code = template.Code,
            Description = template.Description,
            IsActive = template.IsActive,
            Tasks = BuildTaskRows(template.Tasks, userMap),
            TaskForm = taskForm,
            TaskCategories = categories,
            Users = users,
            Projects = (await _lookupService.GetProjectsAsync(cancellationToken))
                .Where(x => x.Status != Domain.Enums.ProjectStatus.Cancelled)
                .OrderBy(x => x.Code)
                .Select(x => new ProjectPlanningProjectOptionViewModel
                {
                    Id = x.Id,
                    Text = $"{x.Code} - {x.Name}"
                })
                .ToList(),
            ApplyForm = new ProjectTemplateApplyViewModel
            {
                TemplateId = template.Id
            },
            OpenTaskForm = openTaskForm,
            TaskFormMode = taskFormMode
        };
    }

    private async Task<bool> IsValidTaskReferencesAsync(ProjectTemplateTaskFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.TaskCategoryId.HasValue)
        {
            var categoryExists = await _context.TaskCategories
                .AsNoTracking()
                .AnyAsync(x => x.Id == model.TaskCategoryId.Value, cancellationToken);

            if (!categoryExists)
            {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(model.DefaultAssignedUserId))
        {
            var assignedExists = await _userManager.Users
                .AsNoTracking()
                .AnyAsync(x => x.Id == model.DefaultAssignedUserId, cancellationToken);

            if (!assignedExists)
            {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(model.DefaultResponsibleUserId))
        {
            var responsibleExists = await _userManager.Users
                .AsNoTracking()
                .AnyAsync(x => x.Id == model.DefaultResponsibleUserId, cancellationToken);

            if (!responsibleExists)
            {
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<ProjectTemplateTaskItemViewModel> BuildTaskRows(
        IEnumerable<ProjectTemplateTask> tasks,
        IReadOnlyDictionary<string, string> userMap)
    {
        var taskList = tasks.ToList();
        var taskIds = taskList.Select(x => x.Id).ToHashSet();
        var childrenByParent = taskList
            .Where(x => x.ParentTemplateTaskId.HasValue)
            .GroupBy(x => x.ParentTemplateTaskId!.Value)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.SortOrder).ThenBy(y => y.Title).ToList());

        var rows = new List<ProjectTemplateTaskItemViewModel>();
        foreach (var root in taskList.Where(x => !x.ParentTemplateTaskId.HasValue || !taskIds.Contains(x.ParentTemplateTaskId.Value))
                     .OrderBy(x => x.SortOrder)
                     .ThenBy(x => x.Title))
        {
            AddTask(root);
        }

        return rows;

        void AddTask(ProjectTemplateTask task)
        {
            rows.Add(new ProjectTemplateTaskItemViewModel
            {
                Id = task.Id,
                ParentTemplateTaskId = task.ParentTemplateTaskId,
                Title = task.Title,
                Description = task.Description,
                OutlineLevel = task.OutlineLevel,
                SortOrder = task.SortOrder,
                WbsCode = task.WbsCode ?? string.Empty,
                TaskCategoryId = task.TaskCategoryId,
                CategoryName = task.TaskCategory?.Name,
                DefaultDurationDays = task.DefaultDurationDays,
                DefaultStartOffsetDays = task.DefaultStartOffsetDays,
                DefaultPriority = task.DefaultPriority,
                PriorityText = task.DefaultPriority.ToDisplayName(),
                DefaultAssignedUserId = task.DefaultAssignedUserId,
                AssignedUserText = !string.IsNullOrWhiteSpace(task.DefaultAssignedUserId) && userMap.TryGetValue(task.DefaultAssignedUserId, out var assignedText)
                    ? assignedText
                    : null,
                DefaultResponsibleUserId = task.DefaultResponsibleUserId,
                ResponsibleUserText = !string.IsNullOrWhiteSpace(task.DefaultResponsibleUserId) && userMap.TryGetValue(task.DefaultResponsibleUserId, out var responsibleText)
                    ? responsibleText
                    : null,
                IsMilestone = task.IsMilestone,
                HasChildren = childrenByParent.ContainsKey(task.Id)
            });

            if (!childrenByParent.TryGetValue(task.Id, out var children))
            {
                return;
            }

            foreach (var child in children)
            {
                AddTask(child);
            }
        }
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private IActionResult RedirectToTemplateReturn(ProjectTemplateApplyViewModel model)
    {
        if (string.Equals(model.ReturnAction, "PlanList", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("PlanList", "ProjectPlanning", new { projectId = model.ProjectId });
        }

        if (string.Equals(model.ReturnAction, "Index", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Index", "ProjectPlanning", new { projectId = model.ProjectId });
        }

        return RedirectToAction(nameof(Details), new { id = model.TemplateId });
    }
}
