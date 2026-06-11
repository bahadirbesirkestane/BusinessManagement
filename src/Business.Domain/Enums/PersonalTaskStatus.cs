using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Enums;

public enum PersonalTaskStatus
{
    [Display(Name = "Yapılacak")]
    Todo = 1,

    [Display(Name = "Devam Ediyor")]
    InProgress = 2,

    [Display(Name = "Tamamlandı")]
    Done = 3,

    [Display(Name = "İptal Edildi")]
    Cancelled = 4
}
