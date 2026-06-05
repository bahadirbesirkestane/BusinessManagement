using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Enums;

public enum ProjectStatus
{
    [Display(Name = "Planlandı")]
    Planned = 0,
    [Display(Name = "Devam Ediyor")]
    InProgress = 1,
    [Display(Name = "Beklemede")]
    Waiting = 2,
    [Display(Name = "Tamamlandı")]
    Completed = 3,
    [Display(Name = "İptal Edildi")]
    Cancelled = 4
}
