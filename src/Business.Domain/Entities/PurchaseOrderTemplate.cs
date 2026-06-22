using Business.Domain.Common;
using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class PurchaseOrderTemplate : BaseEntity
{
    [Required(ErrorMessage = "Şablon adı zorunludur.")]
    [StringLength(180, ErrorMessage = "Şablon adı en fazla 180 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(40, ErrorMessage = "Şablon kodu en fazla 40 karakter olabilir.")]
    public string? Code { get; set; }

    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
    public string? Description { get; set; }

    public PurchaseOrderScope DefaultScope { get; set; } = PurchaseOrderScope.General;
    public PurchaseOrderStatus DefaultStatus { get; set; } = PurchaseOrderStatus.Requested;
    public Guid? DefaultSupplierId { get; set; }
    public Supplier? DefaultSupplier { get; set; }

    [StringLength(80, ErrorMessage = "Vade en fazla 80 karakter olabilir.")]
    public string? DefaultPaymentTerm { get; set; }

    public string DefaultCurrency { get; set; } = "TRY";

    [Range(0, 100, ErrorMessage = "KDV oranı 0 ile 100 arasında olmalıdır.")]
    public decimal? DefaultVatRate { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<PurchaseOrderTemplateLine> Lines { get; set; } = new List<PurchaseOrderTemplateLine>();
}
