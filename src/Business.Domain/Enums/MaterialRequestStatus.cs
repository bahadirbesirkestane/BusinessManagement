using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Enums;

public enum MaterialRequestStatus
{
    [Display(Name = "Taslak")]
    Draft = 0,
    [Display(Name = "Talep Edildi")]
    Requested = 1,
    [Display(Name = "Onaylandı")]
    Approved = 2,
    [Display(Name = "Siparişe Döndü")]
    Ordered = 3,
    [Display(Name = "Karşılandı")]
    Fulfilled = 4,
    [Display(Name = "İptal Edildi")]
    Cancelled = 5
}
