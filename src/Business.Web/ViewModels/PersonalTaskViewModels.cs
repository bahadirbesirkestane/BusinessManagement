using Business.Domain.Entities;
using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Web.ViewModels;

public class PersonalTaskIndexViewModel
{
    public string? Filter { get; set; }
    public IReadOnlyList<PersonalTaskListItemViewModel> Tasks { get; set; } = [];
    public IReadOnlyList<PersonalNoteLookupItemViewModel> Customers { get; set; } = [];
    public IReadOnlyList<PersonalNoteLookupItemViewModel> Projects { get; set; } = [];
    public IReadOnlyList<PersonalNoteLookupItemViewModel> ProjectTasks { get; set; } = [];
    public Guid? CustomerId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? ProjectTaskId { get; set; }
}

public class PersonalTaskListItemViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PersonalTaskStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public ProjectPriority Priority { get; set; }
    public string PriorityText { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? ProjectName { get; set; }
    public string? ProjectTaskTitle { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsOverdue { get; set; }
}

public class PersonalTaskFormViewModel : IValidatableObject
{
    public Guid Id { get; set; }

    public Guid? CustomerId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? ProjectTaskId { get; set; }

    [Required(ErrorMessage = "Görev başlığı zorunludur.")]
    [StringLength(220, ErrorMessage = "Görev başlığı en fazla 220 karakter olabilir.")]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Durum")]
    public PersonalTaskStatus Status { get; set; } = PersonalTaskStatus.Todo;

    [Display(Name = "Öncelik")]
    public ProjectPriority Priority { get; set; } = ProjectPriority.Normal;

    [Display(Name = "Başlangıç tarihi")]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [Display(Name = "Termin tarihi")]
    [DataType(DataType.Date)]
    public DateTime? DueDate { get; set; }

    [Display(Name = "Tamamlanma tarihi")]
    [DataType(DataType.Date)]
    public DateTime? CompletedAt { get; set; }

    [StringLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir.")]
    [Display(Name = "Not")]
    public string? Notes { get; set; }

    public IReadOnlyList<PersonalNoteLookupItemViewModel> Customers { get; set; } = [];
    public IReadOnlyList<PersonalNoteLookupItemViewModel> Projects { get; set; } = [];
    public IReadOnlyList<PersonalNoteLookupItemViewModel> ProjectTasks { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.HasValue && DueDate.HasValue && DueDate.Value.Date < StartDate.Value.Date)
        {
            yield return new ValidationResult("Termin tarihi başlangıç tarihinden önce olamaz.", [nameof(DueDate)]);
        }

        if (CompletedAt.HasValue && StartDate.HasValue && CompletedAt.Value.Date < StartDate.Value.Date)
        {
            yield return new ValidationResult("Tamamlanma tarihi başlangıç tarihinden önce olamaz.", [nameof(CompletedAt)]);
        }
    }
}

public class PersonalTaskDetailsViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PersonalTaskStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public ProjectPriority Priority { get; set; }
    public string PriorityText { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? ProjectName { get; set; }
    public string? ProjectTaskTitle { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Notes { get; set; }
    public bool IsOverdue { get; set; }
}
