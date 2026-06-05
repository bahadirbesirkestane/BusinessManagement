using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Web.ViewModels;

public class QuickPurchaseOrderViewModel
{
    public Guid? ProjectId { get; set; }
    public Guid? SupplierId { get; set; }
    public PurchaseOrderScope Scope { get; set; } = PurchaseOrderScope.General;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Requested;
    public DateTime OrderDate { get; set; } = DateTime.Today;
    public DateTime? ExpectedArrivalDate { get; set; }

    [StringLength(3, MinimumLength = 3, ErrorMessage = "Para birimi 3 karakter olmalıdır.")]
    public string Currency { get; set; } = "TRY";

    [StringLength(80, ErrorMessage = "Vade en fazla 80 karakter olabilir.")]
    public string? PaymentTerm { get; set; }
    public List<QuickPurchaseOrderLineViewModel> Lines { get; set; } =
    [
        new QuickPurchaseOrderLineViewModel(),
        new QuickPurchaseOrderLineViewModel()
    ];
}

public class QuickPurchaseOrderLineViewModel
{
    public Guid? MaterialId { get; set; }

    [StringLength(600, ErrorMessage = "Sipariş içeriği en fazla 600 karakter olabilir.")]
    public string? Content { get; set; }

    [Range(0, 999999999, ErrorMessage = "Miktar 0'dan küçük olamaz.")]
    public decimal? Quantity { get; set; }

    [StringLength(80, ErrorMessage = "Miktar metni en fazla 80 karakter olabilir.")]
    public string? QuantityText { get; set; }

    [StringLength(40, ErrorMessage = "Birim en fazla 40 karakter olabilir.")]
    public string? Unit { get; set; }

    [StringLength(120, ErrorMessage = "Kalite en fazla 120 karakter olabilir.")]
    public string? Quality { get; set; }

    [Range(0, 999999999, ErrorMessage = "Birim fiyat 0'dan küçük olamaz.")]
    public decimal? UnitPrice { get; set; }

    [Range(0, 999999999, ErrorMessage = "Sipariş tutarı 0'dan küçük olamaz.")]
    public decimal? OrderTotal { get; set; }

    [StringLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir.")]
    public string? Notes { get; set; }
}
