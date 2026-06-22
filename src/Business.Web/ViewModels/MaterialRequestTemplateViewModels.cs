using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Web.ViewModels;

public class MaterialRequestTemplateListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public int LineCount { get; set; }
}

public class MaterialRequestTemplateFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Şablon adı zorunludur.")]
    [StringLength(180, ErrorMessage = "Şablon adı en fazla 180 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(40, ErrorMessage = "Şablon kodu en fazla 40 karakter olabilir.")]
    public string? Code { get; set; }

    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
    public string? Description { get; set; }

    public MaterialRequestStatus DefaultStatus { get; set; } = MaterialRequestStatus.Requested;

    public bool IsActive { get; set; } = true;
}

public class MaterialRequestTemplateLineFormViewModel
{
    public Guid? Id { get; set; }
    public Guid MaterialRequestTemplateId { get; set; }
    public Guid? MaterialId { get; set; }
    public string? MaterialName { get; set; }

    [Required(ErrorMessage = "İhtiyaç kalemi zorunludur.")]
    [StringLength(420, ErrorMessage = "İhtiyaç kalemi en fazla 420 karakter olabilir.")]
    public string RequestedItem { get; set; } = string.Empty;

    [Range(0, 999999999, ErrorMessage = "Miktar 0'dan küçük olamaz.")]
    public decimal? Quantity { get; set; }

    [StringLength(80, ErrorMessage = "Miktar metni en fazla 80 karakter olabilir.")]
    public string? QuantityText { get; set; }

    [StringLength(40, ErrorMessage = "Birim en fazla 40 karakter olabilir.")]
    public string? Unit { get; set; }

    [StringLength(120, ErrorMessage = "Kalite en fazla 120 karakter olabilir.")]
    public string? Quality { get; set; }

    public int? NeededByOffsetDays { get; set; }

    [StringLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir.")]
    public string? Notes { get; set; }
}

public class MaterialRequestTemplateLineItemViewModel
{
    public Guid Id { get; set; }
    public int SortOrder { get; set; }
    public Guid? MaterialId { get; set; }
    public string? MaterialName { get; set; }
    public string RequestedItem { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string? QuantityText { get; set; }
    public string? Unit { get; set; }
    public string? Quality { get; set; }
    public int? NeededByOffsetDays { get; set; }
    public string? Notes { get; set; }
}

public class MaterialRequestTemplateApplyViewModel
{
    [Required]
    public Guid TemplateId { get; set; }

    public Guid? ProjectId { get; set; }

    [Required(ErrorMessage = "Gerekli tarih zorunludur.")]
    public DateTime NeededByDate { get; set; } = DateTime.Today;
}

public class MaterialRequestTemplateDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public IReadOnlyList<MaterialRequestTemplateLineItemViewModel> Lines { get; set; } = [];
    public MaterialRequestTemplateLineFormViewModel LineForm { get; set; } = new();
    public IReadOnlyList<ProjectTemplateLookupItemViewModel> Materials { get; set; } = [];
    public IReadOnlyList<ProjectPlanningProjectOptionViewModel> Projects { get; set; } = [];
    public MaterialRequestTemplateApplyViewModel ApplyForm { get; set; } = new();
    public bool OpenLineForm { get; set; }
    public string LineFormMode { get; set; } = "create";
}
