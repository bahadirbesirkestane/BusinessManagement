using Business.Domain.Common;
using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class PurchaseOrder : BaseEntity, IValidatableObject
{
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public Guid? MaterialId { get; set; }
    public Material? Material { get; set; }

    [Required(ErrorMessage = "Sipariş numarası zorunludur.")]
    [StringLength(40, ErrorMessage = "Sipariş numarası en fazla 40 karakter olabilir.")]
    public string OrderNumber { get; set; } = string.Empty;
    public RecordVisibility Visibility { get; set; } = RecordVisibility.General;
    public PurchaseOrderScope Scope { get; set; } = PurchaseOrderScope.General;
    public int TrackingState { get; set; }
    [Required(ErrorMessage = "Sipariş içeriği zorunludur.")]
    [StringLength(600, ErrorMessage = "Sipariş içeriği en fazla 600 karakter olabilir.")]
    public string Content { get; set; } = string.Empty;

    [Range(0, 999999999, ErrorMessage = "Miktar 0'dan küçük olamaz.")]
    public decimal? Quantity { get; set; }

    [StringLength(80, ErrorMessage = "Miktar metni en fazla 80 karakter olabilir.")]
    public string? QuantityText { get; set; }

    [StringLength(40, ErrorMessage = "Birim en fazla 40 karakter olabilir.")]
    public string? Unit { get; set; }

    [StringLength(120, ErrorMessage = "Kalite en fazla 120 karakter olabilir.")]
    public string? Quality { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Requested;
    public DateTime? OrderDate { get; set; }
    public DateTime? ExpectedArrivalDate { get; set; }
    public DateTime? ArrivalDate { get; set; }
    [StringLength(120, ErrorMessage = "Sipariş veren en fazla 120 karakter olabilir.")]
    public string? RequestedBy { get; set; }
    public string? RequestedByUserId { get; set; }

    [StringLength(80, ErrorMessage = "Vade en fazla 80 karakter olabilir.")]
    public string? PaymentTerm { get; set; }

    [Range(0, 999999999, ErrorMessage = "Birim fiyat 0'dan küçük olamaz.")]
    public decimal? UnitPrice { get; set; }

    [StringLength(120, ErrorMessage = "Birim fiyat metni en fazla 120 karakter olabilir.")]
    public string? UnitPriceText { get; set; }

    [Range(0, 999999999, ErrorMessage = "Sipariş tutarı 0'dan küçük olamaz.")]
    public decimal? OrderTotal { get; set; }

    [StringLength(3, MinimumLength = 3, ErrorMessage = "Para birimi 3 karakter olmalıdır.")]
    public string Currency { get; set; } = "TRY";

    [Range(0, 100, ErrorMessage = "KDV oranı 0 ile 100 arasında olmalıdır.")]
    public decimal? VatRate { get; set; }

    [StringLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir.")]
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (OrderDate.HasValue && ExpectedArrivalDate.HasValue && ExpectedArrivalDate.Value.Date < OrderDate.Value.Date)
        {
            yield return new ValidationResult("Beklenen varış tarihi sipariş tarihinden önce olamaz.", [nameof(ExpectedArrivalDate)]);
        }

        if (OrderDate.HasValue && ArrivalDate.HasValue && ArrivalDate.Value.Date < OrderDate.Value.Date)
        {
            yield return new ValidationResult("Varış tarihi sipariş tarihinden önce olamaz.", [nameof(ArrivalDate)]);
        }
    }
}
