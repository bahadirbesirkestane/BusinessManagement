using Business.Domain.Common;
using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class MaterialRequest : BaseEntity
{
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid? MaterialId { get; set; }
    public Material? Material { get; set; }

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
    public MaterialRequestStatus Status { get; set; } = MaterialRequestStatus.Requested;
    public DateTime NeededBy { get; set; } = DateTime.Today;
    public string? RequestedByUserId { get; set; }
    [StringLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir.")]
    public string? Notes { get; set; }
}
