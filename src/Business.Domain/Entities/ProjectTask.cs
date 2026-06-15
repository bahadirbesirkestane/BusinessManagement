using Business.Domain.Common;
using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class ProjectTask : BaseEntity, IValidatableObject
{
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid? ParentTaskId { get; set; }
    public ProjectTask? ParentTask { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? TaskCategoryId { get; set; }
    public TaskCategory? TaskCategory { get; set; }

    [Required(ErrorMessage = "Görev başlığı zorunludur.")]
    [StringLength(220, ErrorMessage = "Görev başlığı en fazla 220 karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
    public string? Description { get; set; }

    [StringLength(220, ErrorMessage = "Manuel proje adı en fazla 220 karakter olabilir.")]
    public string? ManualProjectName { get; set; }

    [StringLength(220, ErrorMessage = "Manuel müşteri adı en fazla 220 karakter olabilir.")]
    public string? ManualCustomerName { get; set; }
    public RecordVisibility Visibility { get; set; } = RecordVisibility.General;
    public WorkTaskStatus Status { get; set; } = WorkTaskStatus.Todo;
    public ProjectPriority Priority { get; set; } = ProjectPriority.Normal;
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? SubmittedForReviewAt { get; set; }
    [Range(0, 100, ErrorMessage = "İlerleme 0 ile 100 arasında olmalıdır.")]
    public int ProgressPercent { get; set; }
    public string? ResponsibleUserId { get; set; }
    public string? AssignedToUserId { get; set; }
    public int SortOrder { get; set; }
    [StringLength(40, ErrorMessage = "WBS kodu en fazla 40 karakter olabilir.")]
    public string? WbsCode { get; set; }
    public int OutlineLevel { get; set; }
    public bool IsMilestone { get; set; }
    [StringLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir.")]
    public string? Notes { get; set; }

    public ICollection<ProjectTask> SubTasks { get; set; } = new List<ProjectTask>();
    public ICollection<ProjectTaskAssignment> Assignments { get; set; } = new List<ProjectTaskAssignment>();
    public ICollection<ProjectTaskUpdate> Updates { get; set; } = new List<ProjectTaskUpdate>();

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
