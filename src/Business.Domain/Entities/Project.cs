using Business.Domain.Common;
using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class Project : BaseEntity, IValidatableObject
{
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Required(ErrorMessage = "Proje kodu zorunludur.")]
    [StringLength(32, ErrorMessage = "Proje kodu en fazla 32 karakter olabilir.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Proje adı zorunludur.")]
    [StringLength(220, ErrorMessage = "Proje adı en fazla 220 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(180, ErrorMessage = "Müşteri adı en fazla 180 karakter olabilir.")]
    public string? CustomerName { get; set; }

    [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
    public string? Description { get; set; }
    public RecordVisibility Visibility { get; set; } = RecordVisibility.General;
    public ProjectStatus Status { get; set; } = ProjectStatus.Planned;
    public ProjectPriority Priority { get; set; } = ProjectPriority.Normal;
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? TargetEndDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    [Range(0, 999999999, ErrorMessage = "Bütçe 0'dan küçük olamaz.")]
    public decimal? Budget { get; set; }

    [StringLength(3, MinimumLength = 3, ErrorMessage = "Para birimi 3 karakter olmalıdır.")]
    public string Currency { get; set; } = "TRY";

    [Range(typeof(decimal), "0.0001", "999999999", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true, ErrorMessage = "Euro kuru 0'dan bÃ¼yÃ¼k olmalÄ±dÄ±r.")]
    public decimal? EurToTryRate { get; set; }

    [Range(typeof(decimal), "0.0001", "999999999", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true, ErrorMessage = "Dolar kuru 0'dan bÃ¼yÃ¼k olmalÄ±dÄ±r.")]
    public decimal? UsdToTryRate { get; set; }

    [StringLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir.")]
    public string? Notes { get; set; }

    public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public ICollection<MaterialRequest> MaterialRequests { get; set; } = new List<MaterialRequest>();
    public ICollection<ProjectUpdate> Updates { get; set; } = new List<ProjectUpdate>();
    public ICollection<ProjectCostItem> CostItems { get; set; } = new List<ProjectCostItem>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<ProjectFolder> DriveFolders { get; set; } = new List<ProjectFolder>();
    public ICollection<ProjectDriveFile> DriveFiles { get; set; } = new List<ProjectDriveFile>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.HasValue && TargetEndDate.HasValue && TargetEndDate.Value.Date < StartDate.Value.Date)
        {
            yield return new ValidationResult("Hedef bitiş tarihi başlangıç tarihinden önce olamaz.", [nameof(TargetEndDate)]);
        }

        if (CompletedAt.HasValue && StartDate.HasValue && CompletedAt.Value.Date < StartDate.Value.Date)
        {
            yield return new ValidationResult("Tamamlanma tarihi başlangıç tarihinden önce olamaz.", [nameof(CompletedAt)]);
        }
    }
}
