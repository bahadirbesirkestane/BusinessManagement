using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Enums;

public enum PurchaseOrderStatus
{
    [Display(Name = "Taslak")]
    Draft = 0,
    [Display(Name = "Talep Edildi")]
    Requested = 1,
    [Display(Name = "Sipariş Verildi")]
    Ordered = 2,
    [Display(Name = "Kısmi Teslim")]
    PartiallyDelivered = 3,
    [Display(Name = "Teslim Edildi")]
    Delivered = 4,
    [Display(Name = "İptal Edildi")]
    Cancelled = 5
}
