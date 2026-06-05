using Business.Domain.Common;
using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class ProjectCostItem : BaseEntity
{
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public CostItemType Type { get; set; } = CostItemType.Material;
    [Required(ErrorMessage = "Maliyet açıklaması zorunludur.")]
    [StringLength(320, ErrorMessage = "Maliyet açıklaması en fazla 320 karakter olabilir.")]
    public string Description { get; set; } = string.Empty;

    [Range(0, 999999999, ErrorMessage = "Tutar 0'dan küçük olamaz.")]
    public decimal Amount { get; set; }

    [StringLength(3, MinimumLength = 3, ErrorMessage = "Para birimi 3 karakter olmalıdır.")]
    public string Currency { get; set; } = "TRY";
    public DateTime? CostDate { get; set; }
    [StringLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir.")]
    public string? Notes { get; set; }
}
