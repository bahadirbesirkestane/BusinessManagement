using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Enums;

public enum InvoiceStatus
{
    [Display(Name = "Taslak")]
    Draft = 0,
    [Display(Name = "Kesildi")]
    Issued = 1,
    [Display(Name = "Kısmi Ödendi")]
    PartiallyPaid = 2,
    [Display(Name = "Ödendi")]
    Paid = 3,
    [Display(Name = "Gecikti")]
    Overdue = 4,
    [Display(Name = "İptal Edildi")]
    Cancelled = 5
}
