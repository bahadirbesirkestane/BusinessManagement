using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Enums;

public enum WorkTaskStatus
{
    [Display(Name = "Yapılacak")]
    Todo = 0,

    [Display(Name = "Devam Ediyor")]
    InProgress = 1,

    [Display(Name = "Beklemede")]
    Waiting = 2,

    [Display(Name = "Kontrolde")]
    InReview = 3,

    [Display(Name = "Bitti")]
    Done = 4,

    [Display(Name = "İptal Edildi")]
    Cancelled = 5
}
