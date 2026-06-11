using Business.Domain.Common;
using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class PersonalTask : BaseEntity, IValidatableObject
{
    [Required(ErrorMessage = "Kayıt sahibi zorunludur.")]
    [StringLength(450)]
    public string OwnerUserId { get; set; } = string.Empty;

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid? ProjectTaskId { get; set; }
    public ProjectTask? ProjectTask { get; set; }

    [Required(ErrorMessage = "Görev başlığı zorunludur.")]
    [StringLength(220, ErrorMessage = "Görev başlığı en fazla 220 karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
    public string? Description { get; set; }

    public PersonalTaskStatus Status { get; set; } = PersonalTaskStatus.Todo;
    public ProjectPriority Priority { get; set; } = ProjectPriority.Normal;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }

    [StringLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir.")]
    public string? Notes { get; set; }

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
